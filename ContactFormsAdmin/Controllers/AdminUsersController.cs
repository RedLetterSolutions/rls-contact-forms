using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContactFormsAdmin.Services;

namespace ContactFormsAdmin.Controllers;

[Authorize]
public class AdminUsersController : Controller
{
    private readonly AdminUserService _service;

    public AdminUsersController(AdminUserService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _service.ListAsync();
        return View(users);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Username and password are required.";
            return View();
        }

        await _service.CreateAsync(username, password);
        TempData["Success"] = "Admin user created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Reset(long id)
    {
        var user = await _service.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        ViewBag.UserId = id;
        ViewBag.Username = user.Username;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset(long id, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Password is required.";
            var user = await _service.GetByIdAsync(id);
            ViewBag.UserId = id;
            ViewBag.Username = user?.Username ?? "";
            return View();
        }
        if (password != confirmPassword)
        {
            ViewBag.Error = "Passwords do not match.";
            var user = await _service.GetByIdAsync(id);
            ViewBag.UserId = id;
            ViewBag.Username = user?.Username ?? "";
            return View();
        }

        var ok = await _service.UpdatePasswordAsync(id, password);
        if (!ok)
        {
            return NotFound();
        }
        TempData["Success"] = "Password updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        await _service.DeleteAsync(id);
        TempData["Success"] = "Admin user deleted.";
        return RedirectToAction(nameof(Index));
    }
}
