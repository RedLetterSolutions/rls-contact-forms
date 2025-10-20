using System.Security.Cryptography;
using System.Text;
using ContactFormsAdmin.Data;
using ContactFormsAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace ContactFormsAdmin.Services;

public class AdminUserService
{
    private readonly ApplicationDbContext _db;

    public AdminUserService(ApplicationDbContext db)
    {
        _db = db;
    }

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public async Task<AdminUser?> GetByUsernameAsync(string username)
    {
        return await _db.AdminUsers.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        var user = await GetByUsernameAsync(username);
        if (user == null) return false;
        var hash = HashPassword(password);
        return string.Equals(user.PasswordHash, hash, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<AdminUser> CreateAsync(string username, string password)
    {
        var user = new AdminUser
        {
            Username = username.Trim(),
            PasswordHash = HashPassword(password),
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            IsActive = true
        };
        _db.AdminUsers.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<List<AdminUser>> ListAsync()
    {
        return await _db.AdminUsers.OrderBy(u => u.Username).ToListAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var user = await _db.AdminUsers.FindAsync(id);
        if (user != null)
        {
            _db.AdminUsers.Remove(user);
            await _db.SaveChangesAsync();
        }
    }
}
