using ContactFormsAdmin.Data;
using ContactFormsAdmin.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ContactFormsAdmin.Services;

public class ApiKeyService
{
    private readonly ApplicationDbContext _context;

    public ApiKeyService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(string ApiKey, ApiKey Entity)> CreateApiKeyAsync(string name, DateTime? expiresAt = null)
    {
        // Generate a random API key
        var apiKey = GenerateApiKey();
        var keyHash = HashApiKey(apiKey);
        var keyPrefix = apiKey.Substring(0, Math.Min(10, apiKey.Length));

        var entity = new ApiKey
        {
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt.HasValue ? DateTime.SpecifyKind(expiresAt.Value, DateTimeKind.Utc) : null
        };

        _context.ApiKeys.Add(entity);
        await _context.SaveChangesAsync();

        return (apiKey, entity);
    }

    public async Task<ApiKey?> ValidateApiKeyAsync(string apiKey)
    {
        var keyHash = HashApiKey(apiKey);

        var key = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive);

        if (key == null)
            return null;

        if (key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTime.UtcNow)
            return null;

        // Update last used timestamp
        key.LastUsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return key;
    }

    private static string GenerateApiKey()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return "cfadmin_" + Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "");
    }

    private static string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
