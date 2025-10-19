using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContactFormsAdmin.Data;
using ContactFormsAdmin.Models;
using ContactFormsAdmin.Services;

namespace ContactFormsAdmin.Controllers;

[Authorize]
public class WebhooksController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly WebhookService _webhookService;

    public WebhooksController(ApplicationDbContext context, WebhookService webhookService)
    {
        _context = context;
        _webhookService = webhookService;
    }

    public async Task<IActionResult> Index()
    {
        var webhooks = await _context.Webhooks
            .OrderBy(w => w.SiteId)
            .ThenByDescending(w => w.CreatedAt)
            .ToListAsync();

        return View(webhooks);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Webhook webhook)
    {
        if (ModelState.IsValid)
        {
            webhook.CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            _context.Webhooks.Add(webhook);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Webhook created successfully";
            return RedirectToAction(nameof(Index));
        }

        return View(webhook);
    }

    public async Task<IActionResult> Edit(long id)
    {
        var webhook = await _context.Webhooks.FindAsync(id);
        if (webhook == null)
        {
            return NotFound();
        }

        return View(webhook);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, Webhook webhook)
    {
        if (id != webhook.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Ensure DateTime properties have UTC kind before saving
                webhook.CreatedAt = DateTime.SpecifyKind(webhook.CreatedAt, DateTimeKind.Utc);
                if (webhook.LastTriggeredAt.HasValue)
                {
                    webhook.LastTriggeredAt = DateTime.SpecifyKind(webhook.LastTriggeredAt.Value, DateTimeKind.Utc);
                }
                
                _context.Update(webhook);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Webhook updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await WebhookExists(webhook.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        return View(webhook);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(long id)
    {
        var webhook = await _context.Webhooks.FindAsync(id);
        if (webhook != null)
        {
            _context.Webhooks.Remove(webhook);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Webhook deleted successfully";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Test(long id)
    {
        var webhook = await _context.Webhooks.FindAsync(id);
        if (webhook == null)
        {
            return NotFound();
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
                message = "This is a test webhook trigger from the admin panel",
                metadata = new { test_field = "test_value" }
            }
        };

        var (success, error) = await _webhookService.TriggerWebhookAsync(webhook, testPayload);

        if (success)
        {
            TempData["Success"] = "Webhook tested successfully!";
        }
        else
        {
            TempData["Error"] = $"Webhook test failed: {error}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var webhook = await _context.Webhooks.FindAsync(id);
        if (webhook != null)
        {
            webhook.IsActive = !webhook.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Webhook {(webhook.IsActive ? "activated" : "deactivated")} successfully";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> WebhookExists(long id)
    {
        return await _context.Webhooks.AnyAsync(e => e.Id == id);
    }
}
