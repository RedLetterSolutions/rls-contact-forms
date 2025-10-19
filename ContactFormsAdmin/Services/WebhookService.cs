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
            client.Timeout = TimeSpan.FromSeconds(10);

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(webhook.Url, content);

            webhook.LastTriggeredAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            webhook.LastSuccess = response.IsSuccessStatusCode;
            webhook.LastError = response.IsSuccessStatusCode
                ? null
                : $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";

            await _context.SaveChangesAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Webhook {WebhookId} for site {SiteId} triggered successfully",
                    webhook.Id, webhook.SiteId);
                return (true, null);
            }
            else
            {
                _logger.LogWarning(
                    "Webhook {WebhookId} for site {SiteId} failed: {StatusCode}",
                    webhook.Id, webhook.SiteId, response.StatusCode);
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
