using ContactFormsAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace ContactFormsAdmin.Data;

public static class SiteSeeder
{
    public static async Task SeedSitesAsync(ApplicationDbContext context)
    {
        // Check if any sites already exist
        if (await context.Sites.AnyAsync())
        {
            Console.WriteLine("Sites already exist. Skipping seed.");
            return;
        }

        var sites = new List<Site>
        {
            new Site
            {
                SiteId = "guitar_repair_of_tampa_bay",
                Name = "Guitar Repair of Tampa Bay",
                Description = "Contact form for Guitar Repair of Tampa Bay website",
                ToEmail = "cody@redlettersolutions.io",
                FromEmail = "admin@redlettersolutions.io",
                RedirectUrl = "http://localhost:5173/form-sent",
                AllowedOrigins = "http://localhost:5173,http://www.guitarrepairoftampabay.com",
                Secret = null,
                IsActive = true,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
            },
            new Site
            {
                SiteId = "logos",
                Name = "Logos Helix Partners",
                Description = "Contact form for Logos Helix Partners website",
                ToEmail = "codyjg10@gmail.com",
                FromEmail = "admin@redlettersolutions.io",
                RedirectUrl = "http://localhost:5173#form-complete",
                AllowedOrigins = "https://logoshelixpartners.com,http://localhost:5173",
                Secret = null,
                IsActive = true,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
            },
            new Site
            {
                SiteId = "test",
                Name = "Test Site",
                Description = "Testing and development site configuration",
                ToEmail = "cody@redlettersolutions.io",
                FromEmail = "admin@redlettersolutions.io",
                RedirectUrl = "http://127.0.0.1:5500/test-website.html#form-complete",
                AllowedOrigins = "https://codygordon.com,https://www.codygordon.com,http://127.0.0.1:5500,http://localhost:5500,file://",
                Secret = null,
                IsActive = true,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
            }
        };

        await context.Sites.AddRangeAsync(sites);
        await context.SaveChangesAsync();

        Console.WriteLine($"Successfully seeded {sites.Count} sites.");
    }
}
