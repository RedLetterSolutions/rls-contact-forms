using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContactFormsAdmin.Data;

namespace ContactFormsAdmin.Controllers;

[Route("api")]
[ApiController]
public class ApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ApiController(ApplicationDbContext context)
    {
        _context = context;
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
    /// Get list of all site IDs
    /// </summary>
    [HttpGet("sites")]
    public async Task<IActionResult> GetSites()
    {
        var sites = await _context.ContactSubmissions
            .Select(s => s.SiteId)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        var siteStats = new List<object>();
        foreach (var site in sites)
        {
            var count = await _context.ContactSubmissions
                .Where(s => s.SiteId == site)
                .CountAsync();

            siteStats.Add(new { siteId = site, submissionCount = count });
        }

        return Ok(new { sites = siteStats });
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
}
