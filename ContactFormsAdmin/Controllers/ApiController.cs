using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContactFormsAdmin.Data;
using ContactFormsAdmin.Models;
using ContactFormsAdmin.Services;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace ContactFormsAdmin.Controllers;

[Route("api")]
[ApiController]
public class ApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly WebhookService _webhookService;
    private readonly IServiceScopeFactory _scopeFactory;

    public ApiController(ApplicationDbContext context, WebhookService webhookService, IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _webhookService = webhookService;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Get submissions for a specific site
    /// </summary>
    /// <param name="siteId">The site identifier</param>
    /// <param name="limit">Maximum number of results (default: 100)</param>
    /// <param name="offset">Number of results to skip (default: 0)</param>
    [HttpGet("submissions/{siteId}")]
    public async Task<IActionResult> GetSubmissions(string siteId, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
    {
        if (limit > 1000) limit = 1000; // Max limit

        var query = _context.ContactSubmissions
            .Where(s => s.SiteId == siteId)
            .OrderByDescending(s => s.SubmittedAt);

        var totalCount = await query.CountAsync();
        var submissions = await query
            .Skip(offset)
            .Take(limit)
            .Select(s => new
            {
                s.Id,
                s.SiteId,
                s.Name,
                s.Email,
                s.Message,
                s.ClientIp,
                s.SubmittedAt,
                Metadata = s.MetadataJson != null ? s.GetMetadata() : new Dictionary<string, string>(),
                s.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            siteId,
            totalCount,
            limit,
            offset,
            count = submissions.Count,
            submissions
        });
    }

    /// <summary>
    /// Get a specific submission by ID
    /// </summary>
    [HttpGet("submissions/{siteId}/{id}")]
    public async Task<IActionResult> GetSubmission(string siteId, long id)
    {
        var submission = await _context.ContactSubmissions
            .Where(s => s.Id == id && s.SiteId == siteId)
            .FirstOrDefaultAsync();

        if (submission == null)
        {
            return NotFound(new { error = "Submission not found" });
        }

        return Ok(new
        {
            submission.Id,
            submission.SiteId,
            submission.Name,
            submission.Email,
            submission.Message,
            submission.ClientIp,
            submission.SubmittedAt,
            Metadata = submission.GetMetadata(),
            submission.CreatedAt
        });
    }

    /// <summary>
    /// Get submission statistics for a site
    /// </summary>
    [HttpGet("stats/{siteId}")]
    public async Task<IActionResult> GetStats(string siteId)
    {
        var totalSubmissions = await _context.ContactSubmissions
            .Where(s => s.SiteId == siteId)
            .CountAsync();

        var last24Hours = await _context.ContactSubmissions
            .Where(s => s.SiteId == siteId && s.SubmittedAt >= DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc).AddDays(-1))
            .CountAsync();

        var last7Days = await _context.ContactSubmissions
            .Where(s => s.SiteId == siteId && s.SubmittedAt >= DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc).AddDays(-7))
            .CountAsync();

        var last30Days = await _context.ContactSubmissions
            .Where(s => s.SiteId == siteId && s.SubmittedAt >= DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc).AddDays(-30))
            .CountAsync();

        return Ok(new
        {
            siteId,
            totalSubmissions,
            last24Hours,
            last7Days,
            last30Days
        });
    }

    /// <summary>
    /// Delete a submission by ID
    /// </summary>
    [HttpDelete("submissions/{id}")]
    public async Task<IActionResult> DeleteSubmission(long id)
    {
        try
        {
            var submission = await _context.ContactSubmissions.FindAsync(id);

            if (submission == null)
            {
                return NotFound(new { success = false, error = "Submission not found" });
            }

            _context.ContactSubmissions.Remove(submission);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Submission deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = "Failed to delete submission", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete a submission by ID with site verification
    /// </summary>
    [HttpDelete("submissions/{siteId}/{id}")]
    public async Task<IActionResult> DeleteSubmissionBySite(string siteId, long id)
    {
        try
        {
            var submission = await _context.ContactSubmissions
                .Where(s => s.Id == id && s.SiteId == siteId)
                .FirstOrDefaultAsync();

            if (submission == null)
            {
                return NotFound(new { success = false, error = "Submission not found" });
            }

            _context.ContactSubmissions.Remove(submission);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Submission deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = "Failed to delete submission", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all submissions across all sites (with pagination)
    /// </summary>
    [HttpGet("submissions")]
    public async Task<IActionResult> GetAllSubmissions(
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        [FromQuery] string? siteId = null)
    {
        try
        {
            if (limit > 1000) limit = 1000; // Max limit

            var query = _context.ContactSubmissions.AsQueryable();

            // Filter by siteId if provided
            if (!string.IsNullOrWhiteSpace(siteId))
            {
                query = query.Where(s => s.SiteId == siteId);
            }

            query = query.OrderByDescending(s => s.SubmittedAt);

            var totalCount = await query.CountAsync();
            var submissions = await query
                .Skip(offset)
                .Take(limit)
                .Select(s => new
                {
                    s.Id,
                    s.SiteId,
                    s.Name,
                    s.Email,
                    s.Message,
                    s.ClientIp,
                    s.SubmittedAt,
                    Metadata = s.MetadataJson != null ? s.GetMetadata() : new Dictionary<string, string>(),
                    s.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                totalCount,
                limit,
                offset,
                count = submissions.Count,
                siteId,
                submissions
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = "Failed to retrieve submissions", details = ex.Message });
        }
    }

    /// <summary>
    /// Get submissions by date range
    /// </summary>
    [HttpGet("submissions/{siteId}/by-date")]
    public async Task<IActionResult> GetSubmissionsByDateRange(
        string siteId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int limit = 100)
    {
        try
        {
            if (limit > 1000) limit = 1000;

            var query = _context.ContactSubmissions.Where(s => s.SiteId == siteId);

            if (startDate.HasValue)
            {
                var start = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
                query = query.Where(s => s.SubmittedAt >= start);
            }

            if (endDate.HasValue)
            {
                var end = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
                query = query.Where(s => s.SubmittedAt <= end);
            }

            var submissions = await query
                .OrderByDescending(s => s.SubmittedAt)
                .Take(limit)
                .Select(s => new
                {
                    s.Id,
                    s.SiteId,
                    s.Name,
                    s.Email,
                    s.Message,
                    s.ClientIp,
                    s.SubmittedAt,
                    Metadata = s.MetadataJson != null ? s.GetMetadata() : new Dictionary<string, string>(),
                    s.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                siteId,
                startDate,
                endDate,
                count = submissions.Count,
                submissions
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = "Failed to retrieve submissions", details = ex.Message });
        }
    }

    /// <summary>
    /// Create a new submission for a site
    /// </summary>
    [HttpPost("submissions")]
    public async Task<IActionResult> CreateSubmission([FromBody] CreateSubmissionRequest request, [FromQuery] bool triggerWebhooks = false)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.SiteId))
            {
                return BadRequest(new { success = false, error = "siteId is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { success = false, error = "name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, error = "email is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { success = false, error = "message is required" });
            }

            // Verify site exists
            var siteExists = await _context.Sites.AnyAsync(s => s.SiteId == request.SiteId);
            if (!siteExists)
            {
                return NotFound(new { success = false, error = $"Site '{request.SiteId}' not found" });
            }

            // Create submission
            var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            
            var submission = new ContactSubmission
            {
                SiteId = request.SiteId,
                Name = request.Name,
                Email = request.Email,
                Message = request.Message,
                ClientIp = request.ClientIp ?? "API",
                SubmittedAt = request.SubmittedAt.HasValue 
                    ? DateTime.SpecifyKind(request.SubmittedAt.Value, DateTimeKind.Utc) 
                    : now,
                MetadataJson = request.Metadata != null && request.Metadata.Count > 0
                    ? JsonSerializer.Serialize(request.Metadata, new JsonSerializerOptions { WriteIndented = false })
                    : null,
                CreatedAt = now
            };

            _context.ContactSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            // Optionally trigger webhooks (non-blocking)
            var shouldTrigger = triggerWebhooks || (request.TriggerWebhooks ?? false);
            if (shouldTrigger)
            {
                var payload = new
                {
                    id = submission.Id,
                    siteId = submission.SiteId,
                    name = submission.Name,
                    email = submission.Email,
                    message = submission.Message,
                    clientIp = submission.ClientIp,
                    submittedAt = submission.SubmittedAt,
                    metadata = submission.GetMetadata(),
                    createdAt = submission.CreatedAt
                };

                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Create a new DI scope so scoped services (like DbContext) are valid
                        using var scope = _scopeFactory.CreateScope();
                        var scopedWebhookService = scope.ServiceProvider.GetRequiredService<WebhookService>();
                        await scopedWebhookService.TriggerWebhooksAsync(submission.SiteId, payload);
                    }
                    catch
                    {
                        // Intentionally swallow to avoid impacting API response
                    }
                });
            }

            return CreatedAtAction(
                nameof(GetSubmission),
                new { siteId = submission.SiteId, id = submission.Id },
                new
                {
                    success = true,
                    message = "Submission created successfully",
                    webhooksRequested = shouldTrigger,
                    submission = new
                    {
                        submission.Id,
                        submission.SiteId,
                        submission.Name,
                        submission.Email,
                        submission.Message,
                        submission.ClientIp,
                        submission.SubmittedAt,
                        Metadata = submission.GetMetadata(),
                        submission.CreatedAt
                    }
                });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = "Failed to create submission", details = ex.Message });
        }
    }
}

/// <summary>
/// Request model for creating a new submission
/// </summary>
public class CreateSubmissionRequest
{
    public string SiteId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ClientIp { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public bool? TriggerWebhooks { get; set; }
}
