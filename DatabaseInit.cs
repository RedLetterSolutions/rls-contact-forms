using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RLS_Contact_Forms.Data;
using RLS_Contact_Forms.Models;
using RLS_Contact_Forms.Services;

namespace RLS_Contact_Forms;

/// <summary>
/// Azure Function for initializing and seeding the PostgreSQL contact submissions database.
/// This is a utility function for database setup and testing.
/// </summary>
public class DatabaseInit
{
    private readonly ILogger<DatabaseInit> _logger;
    private readonly SubmissionRepository _submissionRepository;

    public DatabaseInit(ILogger<DatabaseInit> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _submissionRepository = new SubmissionRepository(dbContext, _logger);
    }

    /// <summary>
    /// Initializes the contact_submissions table in PostgreSQL.
    /// GET /v1/database/init - Initialize table (run migrations) only
    /// POST /v1/database/init?seed=true - Initialize and add sample data
    /// </summary>
    [Function("DatabaseInit")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "v1/database/init")] HttpRequestData req)
    {
        _logger.LogInformation("Database initialization request received");

        try
        {
            // Initialize the database (run migrations)
            await _submissionRepository.InitializeAsync();

            var result = new
            {
                ok = true,
                message = "Database migrations applied successfully",
                tableName = "contact_submissions",
                seeded = false,
                sampleCount = 0
            };

            // Check if seeding is requested (POST with seed=true query parameter)
            var shouldSeed = req.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                           req.Query["seed"]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

            if (shouldSeed)
            {
                var sampleCount = await SeedSampleData();
                result = result with { seeded = true, sampleCount = sampleCount };
                _logger.LogInformation("Database seeded with {Count} sample submissions", sampleCount);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            response.WriteString(json);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
            var errorJson = JsonSerializer.Serialize(new
            {
                ok = false,
                error = "Failed to initialize database",
                message = ex.Message
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            errorResponse.WriteString(errorJson);

            return errorResponse;
        }
    }

    /// <summary>
    /// Seeds the database with sample contact form submissions for testing purposes.
    /// Creates sample data for multiple sites to demonstrate multi-tenant isolation.
    /// </summary>
    private async Task<int> SeedSampleData()
    {
        var sampleSubmissions = new List<Dictionary<string, string>>
        {
            // Sample for site: world1
            new Dictionary<string, string>
            {
                { "name", "John Doe" },
                { "email", "john.doe@example.com" },
                { "message", "I'm interested in learning more about your services. Could you please provide more details?" },
                { "phone_number", "+1-555-0123" },
                { "company", "Acme Corp" }
            },
            new Dictionary<string, string>
            {
                { "name", "Jane Smith" },
                { "email", "jane.smith@example.com" },
                { "message", "I need help with my current project. When can we schedule a consultation?" },
                { "budget_range", "$5000-$10000" }
            },

            // Sample for site: test-site
            new Dictionary<string, string>
            {
                { "name", "Bob Johnson" },
                { "email", "bob.johnson@example.com" },
                { "message", "Great website! I'd like to get a quote for a similar design." },
                { "phone_number", "+1-555-0456" },
                { "preferred_contact", "Email" },
                { "budget_range", "$10000+" }
            },
            new Dictionary<string, string>
            {
                { "name", "Alice Williams" },
                { "email", "alice.williams@example.com" },
                { "message", "Do you offer maintenance packages? I need ongoing support for my website." },
                { "website", "https://example-client.com" }
            }
        };

        var sites = new[] { "world1", "world1", "test-site", "test-site" };
        var count = 0;

        for (int i = 0; i < sampleSubmissions.Count; i++)
        {
            var formData = sampleSubmissions[i];
            var siteId = sites[i];
            var submission = ContactSubmission.Create(siteId, formData, "127.0.0.1");

            // Add a slight delay to ensure different timestamps
            await Task.Delay(10);

            var saved = await _submissionRepository.SaveSubmissionAsync(submission);
            if (saved)
            {
                count++;
                _logger.LogInformation("Seeded sample submission {Count} for site {SiteId}", count, siteId);
            }
        }

        return count;
    }
}
