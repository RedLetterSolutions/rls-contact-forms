using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContactFormsAdmin.Data;
using ContactFormsAdmin.Models;

namespace ContactFormsAdmin.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? siteId, int page = 1, int pageSize = 50)
    {
        var query = _context.ContactSubmissions.AsQueryable();

        if (!string.IsNullOrEmpty(siteId))
        {
            query = query.Where(s => s.SiteId == siteId);
        }

        var totalCount = await query.CountAsync();
        var submissions = await query
            .OrderByDescending(s => s.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var sites = await _context.ContactSubmissions
            .Select(s => s.SiteId)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        ViewBag.Sites = sites;
        ViewBag.CurrentSiteId = siteId;
        ViewBag.TotalCount = totalCount;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return View(submissions);
    }

    public async Task<IActionResult> Details(long id)
    {
        var submission = await _context.ContactSubmissions.FindAsync(id);
        if (submission == null)
        {
            return NotFound();
        }

        return View(submission);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(long id)
    {
        var submission = await _context.ContactSubmissions.FindAsync(id);
        if (submission != null)
        {
            _context.ContactSubmissions.Remove(submission);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
