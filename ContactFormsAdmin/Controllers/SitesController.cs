using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContactFormsAdmin.Data;
using ContactFormsAdmin.Models;
using Microsoft.AspNetCore.Authorization;

namespace ContactFormsAdmin.Controllers;

[Authorize]
public class SitesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SitesController> _logger;

    public SitesController(ApplicationDbContext context, ILogger<SitesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var sites = await _context.Sites
            .OrderBy(s => s.Name)
            .ToListAsync();

        return View(sites);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Site site)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Check if site ID already exists
                var existingSite = await _context.Sites.FirstOrDefaultAsync(s => s.SiteId == site.SiteId);
                if (existingSite != null)
                {
                    ModelState.AddModelError("SiteId", "A site with this ID already exists");
                    return View(site);
                }

                site.CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                site.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

                _context.Sites.Add(site);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Site '{site.Name}' created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating site {SiteId}", site.SiteId);
                TempData["Error"] = $"Error creating site: {ex.Message}";
            }
        }

        return View(site);
    }

    public async Task<IActionResult> Edit(long id)
    {
        var site = await _context.Sites.FindAsync(id);
        if (site == null)
        {
            return NotFound();
        }

        return View(site);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, Site site)
    {
        if (id != site.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Ensure DateTime properties have UTC kind before saving
                site.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                if (site.CreatedAt.Kind == DateTimeKind.Unspecified)
                {
                    site.CreatedAt = DateTime.SpecifyKind(site.CreatedAt, DateTimeKind.Utc);
                }

                _context.Update(site);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Site '{site.Name}' updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SiteExists(site.Id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating site {SiteId}", site.SiteId);
                TempData["Error"] = $"Error updating site: {ex.Message}";
            }
        }

        return View(site);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var site = await _context.Sites.FindAsync(id);
            if (site != null)
            {
                _context.Sites.Remove(site);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Site '{site.Name}' deleted successfully";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting site {Id}", id);
            TempData["Error"] = $"Error deleting site: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(long id)
    {
        try
        {
            var site = await _context.Sites.FindAsync(id);
            if (site != null)
            {
                site.IsActive = !site.IsActive;
                site.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Site '{site.Name}' {(site.IsActive ? "activated" : "deactivated")} successfully";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling site {Id}", id);
            TempData["Error"] = $"Error updating site: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> SiteExists(long id)
    {
        return await _context.Sites.AnyAsync(e => e.Id == id);
    }
}