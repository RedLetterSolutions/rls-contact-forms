using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContactFormsAdmin.Data;
using ContactFormsAdmin.Models;

namespace ContactFormsAdmin.Controllers;

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
    [HttpGet]
    public async Task<IActionResult> GetAllSites()
    {
        try
        {
            var sites = await _context.Sites
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
                    s.AllowedOrigins,
                    s.Secret,
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
            return StatusCode(500, new { success = false, error = "Failed to retrieve sites", details = ex.Message });
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
            var site = await _context.Sites
                .Where(s => s.Id == id)
                .Select(s => new
                {
                    s.Id,
                    s.SiteId,
                    s.Name,
                    s.Description,
                    s.ToEmail,
                    s.FromEmail,
                    s.RedirectUrl,
                    s.AllowedOrigins,
                    s.Secret,
                    s.IsActive,
                    s.CreatedAt,
                    s.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (site == null)
            {
                return NotFound(new { success = false, error = "Site not found" });
            }

            return Ok(new { success = true, site });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving site {Id}", id);
            return StatusCode(500, new { success = false, error = "Failed to retrieve site", details = ex.Message });
        }
    }

    /// <summary>
    /// Get a site by site_id (string identifier)
    /// </summary>
    [HttpGet("by-site-id/{siteId}")]
    public async Task<IActionResult> GetSiteBySiteId(string siteId)
    {
        try
        {
            var site = await _context.Sites
                .Where(s => s.SiteId == siteId)
                .Select(s => new
                {
                    s.Id,
                    s.SiteId,
                    s.Name,
                    s.Description,
                    s.ToEmail,
                    s.FromEmail,
                    s.RedirectUrl,
                    s.AllowedOrigins,
                    s.Secret,
                    s.IsActive,
                    s.CreatedAt,
                    s.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (site == null)
            {
                return NotFound(new { success = false, error = "Site not found" });
            }

            return Ok(new { success = true, site });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving site {SiteId}", siteId);
            return StatusCode(500, new { success = false, error = "Failed to retrieve site", details = ex.Message });
        }
    }

    /// <summary>
    /// Create a new site
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSite([FromBody] SiteCreateRequest request)
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

            if (string.IsNullOrWhiteSpace(request.ToEmail))
            {
                return BadRequest(new { success = false, error = "toEmail is required" });
            }

            if (string.IsNullOrWhiteSpace(request.FromEmail))
            {
                return BadRequest(new { success = false, error = "fromEmail is required" });
            }

            // Check if site ID already exists
            var existingSite = await _context.Sites.FirstOrDefaultAsync(s => s.SiteId == request.SiteId);
            if (existingSite != null)
            {
                return Conflict(new { success = false, error = "A site with this siteId already exists" });
            }

            var site = new Site
            {
                SiteId = request.SiteId,
                Name = request.Name,
                Description = request.Description,
                ToEmail = request.ToEmail,
                FromEmail = request.FromEmail,
                RedirectUrl = request.RedirectUrl,
                AllowedOrigins = request.AllowedOrigins,
                Secret = request.Secret,
                IsActive = request.IsActive ?? true,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
            };

            _context.Sites.Add(site);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSite), new { id = site.Id }, new
            {
                success = true,
                message = "Site created successfully",
                site = new
                {
                    site.Id,
                    site.SiteId,
                    site.Name,
                    site.Description,
                    site.ToEmail,
                    site.FromEmail,
                    site.RedirectUrl,
                    site.AllowedOrigins,
                    site.Secret,
                    site.IsActive,
                    site.CreatedAt,
                    site.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating site");
            return StatusCode(500, new { success = false, error = "Failed to create site", details = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing site
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSite(long id, [FromBody] SiteUpdateRequest request)
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
                site.AllowedOrigins = request.AllowedOrigins;
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

            return Ok(new
            {
                success = true,
                message = "Site updated successfully",
                site = new
                {
                    site.Id,
                    site.SiteId,
                    site.Name,
                    site.Description,
                    site.ToEmail,
                    site.FromEmail,
                    site.RedirectUrl,
                    site.AllowedOrigins,
                    site.Secret,
                    site.IsActive,
                    site.CreatedAt,
                    site.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating site {Id}", id);
            return StatusCode(500, new { success = false, error = "Failed to update site", details = ex.Message });
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

            // Check if there are submissions for this site
            var hasSubmissions = await _context.ContactSubmissions.AnyAsync(s => s.SiteId == site.SiteId);
            if (hasSubmissions)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Cannot delete site with existing submissions. Consider deactivating instead."
                });
            }

            _context.Sites.Remove(site);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Site deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting site {Id}", id);
            return StatusCode(500, new { success = false, error = "Failed to delete site", details = ex.Message });
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

            return Ok(new
            {
                success = true,
                message = $"Site {(site.IsActive ? "activated" : "deactivated")} successfully",
                site = new
                {
                    site.Id,
                    site.SiteId,
                    site.Name,
                    site.IsActive,
                    site.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling site {Id}", id);
            return StatusCode(500, new { success = false, error = "Failed to toggle site status", details = ex.Message });
        }
    }
}

public class SiteCreateRequest
{
    public string SiteId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public string? AllowedOrigins { get; set; }
    public string? Secret { get; set; }
    public bool? IsActive { get; set; }
}

public class SiteUpdateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ToEmail { get; set; }
    public string? FromEmail { get; set; }
    public string? RedirectUrl { get; set; }
    public string? AllowedOrigins { get; set; }
    public string? Secret { get; set; }
    public bool? IsActive { get; set; }
}
