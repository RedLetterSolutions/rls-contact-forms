using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContactFormsAdmin.Data;
using ContactFormsAdmin.Models;
using ContactFormsAdmin.Services;

namespace ContactFormsAdmin.Controllers.Api;

/// <summary>
/// REST API for managing webhooks
/// </summary>
[Route("api/webhooks")]
[ApiController]
public class WebhooksApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly WebhookService _webhookService;
    private readonly ILogger<WebhooksApiController> _logger;

    public WebhooksApiController(
        ApplicationDbContext context,
        WebhookService webhookService,
        ILogger<WebhooksApiController> logger)
    {
        _context = context;
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Get all webhooks (optionally filtered by siteId)
    /// </summary>
    /// <param name="siteId">Filter by site ID (optional)</param>
    /// <param name="includeInactive">Include inactive webhooks (default: false)</param>
    [HttpGet]
    public async Task<IActionResult> GetWebhooks(
        [FromQuery] string? siteId = null,
        [FromQuery] bool includeInactive = false)
    {
        try
        {
            var query = _context.Webhooks.AsQueryable();

            if (!string.IsNullOrWhiteSpace(siteId))
            {
                query = query.Where(w => w.SiteId == siteId);
            }

            if (!includeInactive)
            {
                query = query.Where(w => w.IsActive);
            }

            var webhooks = await query
                .OrderBy(w => w.SiteId)
                .ThenByDescending(w => w.CreatedAt)
                .Select(w => new
                {
                    w.Id,
                    w.SiteId,
                    w.Url,
                    w.Description,
                    w.Events,
                    HasSecret = !string.IsNullOrEmpty(w.Secret),
                    w.IsActive,
                    w.CreatedAt,
                    w.LastTriggeredAt,
                    w.LastSuccess,
                    w.LastError
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                count = webhooks.Count,
                siteId,
                webhooks
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhooks");
            return StatusCode(500, new { success = false, error = "Failed to retrieve webhooks" });
        }
    }

    /// <summary>
    /// Get a specific webhook by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetWebhook(long id)
    {
        try
        {
            var webhook = await _context.Webhooks.FindAsync(id);

            if (webhook == null)
            {
                return NotFound(new { success = false, error = "Webhook not found" });
            }

            return Ok(new
            {
                success = true,
                webhook = new
                {
                    webhook.Id,
                    webhook.SiteId,
                    webhook.Url,
                    webhook.Description,
                    webhook.Events,
                    HasSecret = !string.IsNullOrEmpty(webhook.Secret),
                    webhook.IsActive,
                    webhook.CreatedAt,
                    webhook.LastTriggeredAt,
                    webhook.LastSuccess,
                    webhook.LastError
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook {Id}", id);
            return StatusCode(500, new { success = false, error = "Failed to retrieve webhook" });
        }
    }

    /// <summary>
    /// Create a new webhook
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateWebhook([FromBody] CreateWebhookRequest request)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.SiteId))
            {
                return BadRequest(new { success = false, error = "SiteId is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Url))
            {
                return BadRequest(new { success = false, error = "Url is required" });
            }

            // Validate URL format
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            {
                return BadRequest(new { success = false, error = "Invalid URL format" });
            }

            // Check if site exists
            var siteExists = await _context.Sites.AnyAsync(s => s.SiteId == request.SiteId);
            if (!siteExists)
            {
                return BadRequest(new { success = false, error = "Site does not exist" });
            }

            var webhook = new Webhook
            {
                SiteId = request.SiteId,
                Url = request.Url,
                Description = request.Description,
                Events = request.Events,
                Secret = request.Secret,
                IsActive = request.IsActive ?? true,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
            };

            _context.Webhooks.Add(webhook);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created webhook {Id} for site {SiteId}", webhook.Id, webhook.SiteId);

            return CreatedAtAction(nameof(GetWebhook), new { id = webhook.Id }, new
            {
                success = true,
                webhook = new
                {
                    webhook.Id,
                    webhook.SiteId,
                    webhook.Url,
                    webhook.Description,
                    webhook.Events,
                    HasSecret = !string.IsNullOrEmpty(webhook.Secret),
                    webhook.IsActive,
                    webhook.CreatedAt,
                    webhook.LastTriggeredAt,
                    webhook.LastSuccess,
                    webhook.LastError
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating webhook");
            return StatusCode(500, new { success = false, error = "Failed to create webhook" });
        }
    }

    /// <summary>
    /// Update an existing webhook
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWebhook(long id, [FromBody] UpdateWebhookRequest request)
    {
        try
        {
            var webhook = await _context.Webhooks.FindAsync(id);

            if (webhook == null)
            {
                return NotFound(new { success = false, error = "Webhook not found" });
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.Url))
            {
                // Validate URL format
                if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
                {
                    return BadRequest(new { success = false, error = "Invalid URL format" });
                }
                webhook.Url = request.Url;
            }

            if (request.Description != null)
            {
                webhook.Description = request.Description;
            }

            if (request.Events != null)
            {
                webhook.Events = request.Events;
            }

            if (request.Secret != null)
            {
                webhook.Secret = request.Secret;
            }

            if (request.IsActive.HasValue)
            {
                webhook.IsActive = request.IsActive.Value;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated webhook {Id} for site {SiteId}", webhook.Id, webhook.SiteId);

            return Ok(new
            {
                success = true,
                webhook = new
                {
                    webhook.Id,
                    webhook.SiteId,
                    webhook.Url,
                    webhook.Description,
                    webhook.Events,
                    HasSecret = !string.IsNullOrEmpty(webhook.Secret),
                    webhook.IsActive,
                    webhook.CreatedAt,
                    webhook.LastTriggeredAt,
                    webhook.LastSuccess,
                    webhook.LastError
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating webhook {Id}", id);
            return StatusCode(500, new { success = false, error = "Failed to update webhook" });
        }
    }

    /// <summary>
    /// Delete a webhook
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWebhook(long id)
    {
        try
        {
            var webhook = await _context.Webhooks.FindAsync(id);

            if (webhook == null)
            {
                return NotFound(new { success = false, error = "Webhook not found" });
            }

            _context.Webhooks.Remove(webhook);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted webhook {Id} for site {SiteId}", webhook.Id, webhook.SiteId);

            return Ok(new { success = true, message = "Webhook deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook {Id}", id);
            return StatusCode(500, new { success = false, error = "Failed to delete webhook" });
        }
    }

    /// <summary>
    /// Toggle webhook active status
    /// </summary>
    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(long id)
    {
        try
        {
            var webhook = await _context.Webhooks.FindAsync(id);

            if (webhook == null)
            {
                return NotFound(new { success = false, error = "Webhook not found" });
            }

            webhook.IsActive = !webhook.IsActive;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Toggled active status for webhook {Id} to {IsActive}", webhook.Id, webhook.IsActive);

            return Ok(new
            {
                success = true,
                isActive = webhook.IsActive,
                message = $"Webhook {(webhook.IsActive ? "activated" : "deactivated")} successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling active status for webhook {Id}", id);
            return StatusCode(500, new { success = false, error = "Failed to toggle active status" });
        }
    }

    /// <summary>
    /// Test a webhook by sending a test payload
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<IActionResult> TestWebhook(long id)
    {
        try
        {
            var webhook = await _context.Webhooks.FindAsync(id);

            if (webhook == null)
            {
                return NotFound(new { success = false, error = "Webhook not found" });
            }

            var testPayload = new
            {
                test = true,
                siteId = webhook.SiteId,
                timestamp = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                data = new
                {
                    name = "Test User",
                    email = "test@example.com",
                    message = "This is a test webhook trigger from the API",
                    metadata = new { test_field = "test_value" }
                }
            };

            var (success, error) = await _webhookService.TriggerWebhookAsync(webhook, testPayload);

            if (success)
            {
                _logger.LogInformation("Successfully tested webhook {Id}", webhook.Id);
                return Ok(new
                {
                    success = true,
                    message = "Webhook test successful",
                    webhook = new
                    {
                        webhook.Id,
                        webhook.LastTriggeredAt,
                        webhook.LastSuccess,
                        webhook.LastError
                    }
                });
            }
            else
            {
                _logger.LogWarning("Webhook test failed for webhook {Id}: {Error}", webhook.Id, error);
                return Ok(new
                {
                    success = false,
                    message = "Webhook test failed",
                    error,
                    webhook = new
                    {
                        webhook.Id,
                        webhook.LastTriggeredAt,
                        webhook.LastSuccess,
                        webhook.LastError
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing webhook {Id}", id);
            return StatusCode(500, new { success = false, error = "Failed to test webhook" });
        }
    }
}

// Request DTOs
public class CreateWebhookRequest
{
    public string SiteId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Events { get; set; }
    public string? Secret { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateWebhookRequest
{
    public string? Url { get; set; }
    public string? Description { get; set; }
    public string? Events { get; set; }
    public string? Secret { get; set; }
    public bool? IsActive { get; set; }
}
