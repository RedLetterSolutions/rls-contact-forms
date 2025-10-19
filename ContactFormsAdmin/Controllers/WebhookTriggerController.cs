using Microsoft.AspNetCore.Mvc;
using ContactFormsAdmin.Services;

namespace ContactFormsAdmin.Controllers;

[Route("api")]
[ApiController]
public class WebhookTriggerController : ControllerBase
{
    private readonly WebhookService _webhookService;
    private readonly ILogger<WebhookTriggerController> _logger;

    public WebhookTriggerController(WebhookService webhookService, ILogger<WebhookTriggerController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint called by Azure Function to trigger webhooks for a site
    /// This endpoint is NOT protected by API key auth (internal use only)
    /// </summary>
    [HttpPost("trigger-webhook")]
    public async Task<IActionResult> TriggerWebhook([FromBody] WebhookTriggerRequest request)
    {
        if (string.IsNullOrEmpty(request.SiteId))
        {
            return BadRequest(new { error = "siteId is required" });
        }

        try
        {
            await _webhookService.TriggerWebhooksAsync(request.SiteId, request.Data ?? new { });
            return Ok(new { success = true, message = "Webhooks triggered" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering webhooks for site {SiteId}", request.SiteId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

public class WebhookTriggerRequest
{
    public string SiteId { get; set; } = string.Empty;
    public object? Data { get; set; }
}
