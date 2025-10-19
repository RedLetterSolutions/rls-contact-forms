using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContactFormsAdmin.Data;
using ContactFormsAdmin.Services;

namespace ContactFormsAdmin.Controllers;

[Authorize]
public class ApiKeysController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ApiKeyService _apiKeyService;

    public ApiKeysController(ApplicationDbContext context, ApiKeyService apiKeyService)
    {
        _context = context;
        _apiKeyService = apiKeyService;
    }

    public async Task<IActionResult> Index()
    {
        var apiKeys = await _context.ApiKeys
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();

        return View(apiKeys);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, DateTime? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("name", "Name is required");
            return View();
        }

        var (apiKey, entity) = await _apiKeyService.CreateApiKeyAsync(name, expiresAt);

        TempData["NewApiKey"] = apiKey;
        TempData["Success"] = "API Key created successfully. SAVE IT NOW - it won't be shown again!";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(long id)
    {
        var apiKey = await _context.ApiKeys.FindAsync(id);
        if (apiKey != null)
        {
            _context.ApiKeys.Remove(apiKey);
            await _context.SaveChangesAsync();
            TempData["Success"] = "API Key deleted successfully";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var apiKey = await _context.ApiKeys.FindAsync(id);
        if (apiKey != null)
        {
            apiKey.IsActive = !apiKey.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"API Key {(apiKey.IsActive ? "activated" : "deactivated")} successfully";
        }

        return RedirectToAction(nameof(Index));
    }
}
