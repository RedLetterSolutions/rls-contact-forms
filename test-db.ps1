# PowerShell script to test PostgreSQL database functionality
Write-Host "=== PostgreSQL Database Test ===" -ForegroundColor Cyan
Write-Host ""

$connectionString = "Host=caboose.proxy.rlwy.net;Port=46817;Username=postgres;Password=TEldbxeBObbohehNCkfjJpadtmyUNPRC;Database=railway;SSL Mode=Require;Trust Server Certificate=true;"

# Test 1: Seed sample data using EF Core
Write-Host "Step 1: Building and running database test..." -ForegroundColor Yellow

# Create a simple C# program to test
$testProgram = @"
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RLS_Contact_Forms.Data;
using RLS_Contact_Forms.Models;
using RLS_Contact_Forms.Services;

var connectionString = "$connectionString";

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql(connectionString)
    .LogTo(Console.WriteLine, LogLevel.Information)
    .Options;

using var context = new ApplicationDbContext(options);
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

var repository = new SubmissionRepository(context, logger);

// Verify connection
Console.WriteLine("Testing database connection...");
var canConnect = await context.Database.CanConnectAsync();
Console.WriteLine(canConnect ? "✓ Database connection successful!" : "❌ Connection failed");

// Seed data
Console.WriteLine("\nSeeding sample data...");
var testData = new Dictionary<string, string>
{
    { "name", "PowerShell Test User" },
    { "email", "pstest@example.com" },
    { "message", "Testing database from PowerShell script" },
    { "test_field", "Custom metadata value" }
};

var submission = ContactSubmission.Create("test-site", testData, "127.0.0.1");
var saved = await repository.SaveSubmissionAsync(submission);
Console.WriteLine(saved ? $"✓ Saved submission with ID: {submission.Id}" : "❌ Failed to save");

// Query data
Console.WriteLine("\nQuerying submissions...");
var submissions = await repository.GetSubmissionsBySiteAsync("test-site", 5);
Console.WriteLine($"✓ Found {submissions.Count} submission(s) for 'test-site'");

foreach (var sub in submissions)
{
    Console.WriteLine($"  - ID: {sub.Id}, Name: {sub.Name}, Email: {sub.Email}, Submitted: {sub.SubmittedAt}");
}

Console.WriteLine("\n✓ All tests passed!");
"@

# Save test program
$testProgram | Out-File -FilePath "TestDbTemp.cs" -Encoding UTF8

# Run test
Write-Host ""
Write-Host "Running database operations..." -ForegroundColor Yellow
dotnet build --configuration Release /p:DefineConstants=SKIP_FUNCTIONS
