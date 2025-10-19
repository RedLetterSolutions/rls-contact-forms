using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContactFormsAdmin.Data;
using ContactFormsAdmin.Models;

namespace ContactFormsAdmin.Controllers.Api;

/// <summary>
/// REST API for managing sites
/// </summary>
[Route("api/sites")]
[ApiController]
public class SitesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SitesApiController> _logger;

    public SitesApiController(ApplicationDbContext context, ILogger<SitesApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all sites
    /// </summary>
    /// <param name="includeInactive">Include inactive sites (default: false)</param>
    [HttpGet]
    public async Task<IActionResult> GetSites([FromQuery] bool includeInactive = false)
    {
        try
        {
            var query = _context.Sites.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            var sites = await query
                .OrderBy(s => s.Name)
                .Select(s => new
                {
                    s.Id,
                    s.SiteId,
                    s.Name,
                    s.Description,
                    s.ToEmail,
                    s.FromEmail,
                    s.RedirectUrl,
                    AllowedOrigins = s.AllowedOriginsList,
                    HasSecret = !string.IsNullOrEmpty(s.Secret),
                    s.IsActive,
                    s.CreatedAt,
                    s.UpdatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                count = sites.Count,
                sites
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sites");
            return StatusCode(500, new { success = false, error = "Failed to retrieve sites" });
        }
    }

    /// <summary>
    /// Get a specific site by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSite(long id)
    {
        try
        {
            var site = await _context.Sites.FindAsync(id);

            if (site == null)
            {
                return NotFound(new { success = false, error = "Site not found" });
            }

            return Ok(new
            {
                success = true,
                site = new
                {
                    site.Id,
                    site.SiteId,
                    site.Name,
                    site.Description,
                    site.ToEmail,
                    site.FromEmail,
                    site.RedirectUrl,
                    AllowedOrigins = site.AllowedOriginsList,
                    HasSecret = !string.IsNullOrEmpty(site.Secret),
                    site.IsActive,
                    site.CreatedAt,
                    site.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving site {Id}", id);
            return StatusCode(500, new { success = false, error = "Failed to retrieve site" });
        }
    }

    /// <summary>
    /// Get a specific site by SiteId (slug)
    /// </summary>
    [HttpGet("by-slug/{siteId}")]
    public async Task<IActionResult> GetSiteBySiteId(string siteId)
    {
        try
        {
            var site = await _context.Sites
                .Where(s => s.SiteId == siteId)
                .FirstOrDefaultAsync();

            if (site == null)
            {
                return NotFound(new { success = false, error = "Site not found" });
            }

            return Ok(new
            {
                success = true,
                site = new
                {
                    site.Id,
                    site.SiteId,
                    site.Name,
                    site.Description,
                    site.ToEmail,
                    site.FromEmail,
                    site.RedirectUrl,
                    AllowedOrigins = site.AllowedOriginsList,
                    HasSecret = !string.IsNullOrEmpty(site.Secret),
                    site.IsActive,
                    site.CreatedAt,
                    site.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving site {SiteId}", siteId);
            return StatusCode(500, new { success = false, error = "Failed to retrieve site" });
        }
    }

    /// <summary>
    /// Create a new site
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSite([FromBody] CreateSiteRequest request)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.SiteId))
            {
                return BadRequest(new { success = false, error = "SiteId is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { success = false, error = "Name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.ToEmail))
            {
                return BadRequest(new { success = false, error = "ToEmail is required" });
            }

            if (string.IsNullOrWhiteSpace(request.FromEmail))
            {
                return BadRequest(new { success = false, error = "FromEmail is required" });
            }

            // Check if site ID already exists
            var existingSite = await _context.Sites
                .Where(s => s.SiteId == request.SiteId)
                .FirstOrDefaultAsync();

            if (existingSite != null)
            {
                return Conflict(new { success = false, error = "A site with this SiteId already exists" });
            }

            var site = new Site
            {
                SiteId = request.SiteId,
                Name = request.Name,
                Description = request.Description,
                ToEmail = request.ToEmail,
                FromEmail = request.FromEmail,
                RedirectUrl = request.RedirectUrl,
                AllowedOrigins = request.AllowedOrigins != null && request.AllowedOrigins.Any()
                    ? string.Join(",", request.AllowedOrigins)
                    : null,
                Secret = request.Secret,
                IsActive = request.IsActive ?? true,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
            };

            _context.Sites.Add(site);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created site {SiteId} with ID {Id}", site.SiteId, site.Id);

            return CreatedAtAction(nameof(GetSite), new { id = site.Id }, new
            {
                success = true,
                site = new
                {
                    site.Id,
                    site.SiteId,
                    site.Name,
                    site.Description,
                    site.ToEmail,
                    site.FromEmail,
                    site.RedirectUrl,
                    AllowedOrigins = site.AllowedOriginsList,
                    HasSecret = !string.IsNullOrEmpty(site.Secret),
                    site.IsActive,
                    site.CreatedAt,
                    site.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating site");
            return StatusCode(500, new { success = false, error = "Failed to create site" });
        }
    }

    /// <summary>
    /// Update an existing site
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSite(long id, [FromBody] UpdateSiteRequest request)
    {
        try
        {
            var site = await _context.Sites.FindAsync(id);

            if (site == null)
            {
                return NotFound(new { success = false, error = "Site not found" });
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                site.Name = request.Name;
            }

            if (request.Description != null)
            {
                site.Description = request.Description;
            }

            if (!string.IsNullOrWhiteSpace(request.ToEmail))
            {
                site.ToEmail = request.ToEmail;
            }

            if (!string.IsNullOrWhiteSpace(request.FromEmail))
            {
                site.FromEmail = request.FromEmail;
            }

            if (request.RedirectUrl != null)
            {
                site.RedirectUrl = request.RedirectUrl;
            }

            if (request.AllowedOrigins != null)
            {
                site.AllowedOrigins = request.AllowedOrigins.Any()
                    ? string.Join(",", request.AllowedOrigins)
                    : null;
            }

            if (request.Secret != null)
            {
                site.Secret = request.Secret;
            }

            if (request.IsActive.HasValue)
            {
                site.IsActive = request.IsActive.Value;
            }

            site.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated site {SiteId} with ID {Id}", site.SiteId, site.Id);

            return Ok(new
            {
                success = true,
                site = new
                {
                    site.Id,
                    site.SiteId,
                    site.Name,
                    site.Description,
                    site.ToEmail,
                    site.FromEmail,
                    site.RedirectUrl,
                    AllowedOrigins = site.AllowedOriginsList,
                    HasSecret = !string.IsNullOrEmpty(site.Secret),
                    site.IsActive,
                    site.CreatedAt,
                    site.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating site {Id}", id);
            return StatusCode(500, new { success = false, error = "Failed to update site" });
        }
    }

    /// <summary>
    /// Delete a site
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSite(long id)
    {
        try
        {
            var site = await _context.Sites.FindAsync(id);

            if (site == null)
            {
                return NotFound(new { success = false, error = "Site not found" });
            }

            _context.Sites.Remove(site);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted site {SiteId} with ID {Id}", site.SiteId, site.Id);

            return Ok(new { success = true, message = "Site deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting site {Id}", id);
            return StatusCode(500, new { success = false, error = "Failed to delete site" });
        }
    }

    /// <summary>
    /// Toggle site active status
    /// </summary>
    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(long id)
    {
        try
        {
            var site = await _context.Sites.FindAsync(id);

            if (site == null)
            {
                return NotFound(new { success = false, error = "Site not found" });
            }

            site.IsActive = !site.IsActive;
            site.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Toggled active status for site {SiteId} to {IsActive}", site.SiteId, site.IsActive);

            return Ok(new
            {
                success = true,
                isActive = site.IsActive,
                message = $"Site {(site.IsActive ? "activated" : "deactivated")} successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling active status for site {Id}", id);
            return StatusCode(500, new { success = false, error = "Failed to toggle active status" });
        }
    }
}

// Request DTOs
public class CreateSiteRequest
{
    public string SiteId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public List<string>? AllowedOrigins { get; set; }
    public string? Secret { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateSiteRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ToEmail { get; set; }
    public string? FromEmail { get; set; }
    public string? RedirectUrl { get; set; }
    public List<string>? AllowedOrigins { get; set; }
    public string? Secret { get; set; }
    public bool? IsActive { get; set; }
}
