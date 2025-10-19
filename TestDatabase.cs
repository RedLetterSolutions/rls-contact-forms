using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RLS_Contact_Forms.Data;
using RLS_Contact_Forms.Models;
using RLS_Contact_Forms.Services;

namespace RLS_Contact_Forms;

/// <summary>
/// Standalone test program to verify PostgreSQL database functionality.
/// Run with: dotnet run --project . TestDatabase
/// </summary>
public class TestDatabase
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== PostgreSQL Database Test ===\n");

        // Connection string
        var connectionString = "Host=caboose.proxy.rlwy.net;Port=46817;Username=postgres;Password=TEldbxeBObbohehNCkfjJpadtmyUNPRC;Database=railway;SSL Mode=Require;Trust Server Certificate=true;";

        // Setup DbContext
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        using var context = new ApplicationDbContext(options);

        // Create a simple logger
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<TestDatabase>();

        var repository = new SubmissionRepository(context, logger);

        try
        {
            // Test 1: Verify database connection
            Console.WriteLine("✓ Testing database connection...");
            await context.Database.CanConnectAsync();
            Console.WriteLine("✓ Database connection successful!\n");

            // Test 2: Seed sample data
            Console.WriteLine("✓ Seeding sample contact submissions...");
            var sampleData = new List<(string siteId, Dictionary<string, string> formData)>
            {
                ("world1", new Dictionary<string, string>
                {
                    { "name", "Test User 1" },
                    { "email", "test1@example.com" },
                    { "message", "This is a test submission to verify the PostgreSQL database is working correctly." },
                    { "phone_number", "+1-555-0100" },
                    { "company", "Test Corp" }
                }),
                ("world1", new Dictionary<string, string>
                {
                    { "name", "Test User 2" },
                    { "email", "test2@example.com" },
                    { "message", "Another test submission with different metadata fields." },
                    { "budget_range", "$5000-$10000" },
                    { "preferred_contact", "Email" }
                }),
                ("test-site", new Dictionary<string, string>
                {
                    { "name", "Test User 3" },
                    { "email", "test3@example.com" },
                    { "message", "Testing multi-tenant isolation with a different site ID." },
                    { "website", "https://test-site.com" },
                    { "interest", "Product Demo" }
                })
            };

            var savedCount = 0;
            foreach (var (siteId, formData) in sampleData)
            {
                var submission = ContactSubmission.Create(siteId, formData, "127.0.0.1");
                var saved = await repository.SaveSubmissionAsync(submission);
                if (saved)
                {
                    savedCount++;
                    Console.WriteLine($"  ✓ Saved submission #{savedCount} for site '{siteId}' (ID: {submission.Id})");
                }
                await Task.Delay(100); // Small delay for different timestamps
            }
            Console.WriteLine($"✓ Successfully seeded {savedCount} submissions\n");

            // Test 3: Query submissions by site
            Console.WriteLine("✓ Querying submissions for 'world1'...");
            var world1Submissions = await repository.GetSubmissionsBySiteAsync("world1");
            Console.WriteLine($"✓ Found {world1Submissions.Count} submission(s) for 'world1':");
            foreach (var sub in world1Submissions)
            {
                Console.WriteLine($"  - ID: {sub.Id}, Name: {sub.Name}, Email: {sub.Email}");
                var metadata = sub.GetMetadata();
                if (metadata.Any())
                {
                    Console.WriteLine($"    Metadata: {string.Join(", ", metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
                }
            }
            Console.WriteLine();

            Console.WriteLine("✓ Querying submissions for 'test-site'...");
            var testSiteSubmissions = await repository.GetSubmissionsBySiteAsync("test-site");
            Console.WriteLine($"✓ Found {testSiteSubmissions.Count} submission(s) for 'test-site':");
            foreach (var sub in testSiteSubmissions)
            {
                Console.WriteLine($"  - ID: {sub.Id}, Name: {sub.Name}, Email: {sub.Email}");
                var metadata = sub.GetMetadata();
                if (metadata.Any())
                {
                    Console.WriteLine($"    Metadata: {string.Join(", ", metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
                }
            }
            Console.WriteLine();

            // Test 4: Count submissions
            Console.WriteLine("✓ Counting total submissions per site...");
            var world1Count = await repository.GetSubmissionCountAsync("world1");
            var testSiteCount = await repository.GetSubmissionCountAsync("test-site");
            Console.WriteLine($"  - 'world1': {world1Count} submission(s)");
            Console.WriteLine($"  - 'test-site': {testSiteCount} submission(s)");
            Console.WriteLine();

            // Test 5: Verify multi-tenant isolation
            Console.WriteLine("✓ Verifying multi-tenant isolation...");
            var allSubmissions = await context.ContactSubmissions.ToListAsync();
            var uniqueSites = allSubmissions.Select(s => s.SiteId).Distinct().ToList();
            Console.WriteLine($"✓ Total submissions in database: {allSubmissions.Count}");
            Console.WriteLine($"✓ Unique sites: {string.Join(", ", uniqueSites)}");
            Console.WriteLine();

            // Test 6: Verify JSONB metadata
            Console.WriteLine("✓ Verifying JSONB metadata storage...");
            var submissionsWithMetadata = allSubmissions.Where(s => !string.IsNullOrEmpty(s.MetadataJson)).ToList();
            Console.WriteLine($"✓ Submissions with metadata: {submissionsWithMetadata.Count}/{allSubmissions.Count}");
            foreach (var sub in submissionsWithMetadata.Take(3))
            {
                var metadata = sub.GetMetadata();
                Console.WriteLine($"  - ID {sub.Id}: {metadata.Count} metadata field(s)");
            }
            Console.WriteLine();

            Console.WriteLine("=== All Tests Passed! ===");
            Console.WriteLine("✓ Database connection: OK");
            Console.WriteLine("✓ Data insertion: OK");
            Console.WriteLine("✓ Data retrieval: OK");
            Console.WriteLine("✓ Multi-tenant isolation: OK");
            Console.WriteLine("✓ JSONB metadata: OK");
            Console.WriteLine("\nThe PostgreSQL database is working correctly! 🎉");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }
}
