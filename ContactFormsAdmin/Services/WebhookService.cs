using ContactFormsAdmin.Data;
using ContactFormsAdmin.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace ContactFormsAdmin.Services;

public class WebhookService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task TriggerWebhooksAsync(string siteId, object payload)
    {
        var webhooks = await _context.Webhooks
            .Where(w => w.SiteId == siteId && w.IsActive)
            .ToListAsync();

        var tasks = webhooks.Select(webhook => TriggerWebhookAsync(webhook, payload));
        await Task.WhenAll(tasks);
    }

    public async Task<(bool Success, string? Error)> TriggerWebhookAsync(Webhook webhook, object payload)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            // Increase timeout to allow slower endpoints and retry a few times on transient failures
            client.Timeout = TimeSpan.FromSeconds(30);

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            const int maxAttempts = 3;
            var attempt = 0;
            HttpResponseMessage? response = null;
            string? lastError = null;

            while (attempt < maxAttempts)
            {
                attempt++;
                try
                {
                    response = await client.PostAsync(webhook.Url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        lastError = null;
                        break;
                    }

                    // Read response body for more context when non-success
                    var body = await response.Content.ReadAsStringAsync();
                    lastError = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}. Body: {body}";
                    _logger.LogWarning("Webhook {WebhookId} for site {SiteId} returned {(int)StatusCode}: {Reason} (attempt {Attempt})", webhook.Id, webhook.SiteId, (int)response.StatusCode, response.ReasonPhrase, attempt);
                }
                catch (TaskCanceledException tce)
                {
                    // This usually indicates a timeout
                    lastError = "Request timed out" + (tce.Message != null ? $": {tce.Message}" : "");
                    _logger.LogWarning(tce, "Timeout triggering webhook {WebhookId} for site {SiteId} (attempt {Attempt})", webhook.Id, webhook.SiteId, attempt);
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    _logger.LogError(ex, "Error triggering webhook {WebhookId} for site {SiteId} (attempt {Attempt})", webhook.Id, webhook.SiteId, attempt);
                }

                // If not last attempt, wait with exponential backoff
                if (attempt < maxAttempts)
                {
                    var delayMs = (int)(Math.Pow(2, attempt) * 500); // 1s, 2s, ...
                    await Task.Delay(delayMs);
                }
            }

            webhook.LastTriggeredAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            webhook.LastSuccess = response != null && response.IsSuccessStatusCode;
            webhook.LastError = lastError;

            await _context.SaveChangesAsync();

            if (response != null && response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Webhook {WebhookId} for site {SiteId} triggered successfully",
                    webhook.Id, webhook.SiteId);
                return (true, null);
            }
            else
            {
                _logger.LogWarning(
                    "Webhook {WebhookId} for site {SiteId} failed after {Attempts} attempts: {Error}",
                    webhook.Id, webhook.SiteId, attempt, lastError);
                return (false, webhook.LastError);
            }
        }
        catch (Exception ex)
        {
            webhook.LastTriggeredAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            webhook.LastSuccess = false;
            webhook.LastError = ex.Message;

            await _context.SaveChangesAsync();

            _logger.LogError(ex,
                "Error triggering webhook {WebhookId} for site {SiteId}",
                webhook.Id, webhook.SiteId);

            return (false, ex.Message);
        }
    }
}
