#!/usr/bin/env dotnet script
#r "nuget: Npgsql, 8.0.5"

using Npgsql;
using System;
using System.Threading.Tasks;

var connectionString = "Host=caboose.proxy.rlwy.net;Port=46817;Username=postgres;Password=TEldbxeBObbohehNCkfjJpadtmyUNPRC;Database=railway;SSL Mode=Require;Trust Server Certificate=true;";

Console.WriteLine("=== PostgreSQL Database Test ===\n");

try
{
    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    Console.WriteLine("✓ Database connection successful!\n");

    // Test 1: Check if table exists
    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM contact_submissions", conn))
    {
        var count = (long)await cmd.ExecuteScalarAsync();
        Console.WriteLine($"✓ Table 'contact_submissions' exists with {count} row(s)\n");
    }

    // Test 2: Insert a test submission
    var now = DateTime.UtcNow;
    var metadata = "{\"test_field\": \"test_value\", \"source\": \"csx_script\"}";

    using (var cmd = new NpgsqlCommand(@"
        INSERT INTO contact_submissions
        (site_id, name, email, message, client_ip, submitted_at, metadata_json, created_at)
        VALUES (@siteId, @name, @email, @message, @clientIp, @submittedAt, @metadata::jsonb, @createdAt)
        RETURNING id", conn))
    {
        cmd.Parameters.AddWithValue("siteId", "test-site");
        cmd.Parameters.AddWithValue("name", "Script Test User");
        cmd.Parameters.AddWithValue("email", "scripttest@example.com");
        cmd.Parameters.AddWithValue("message", "This is a test submission from the C# script to verify PostgreSQL integration.");
        cmd.Parameters.AddWithValue("clientIp", "127.0.0.1");
        cmd.Parameters.AddWithValue("submittedAt", now);
        cmd.Parameters.AddWithValue("metadata", metadata);
        cmd.Parameters.AddWithValue("createdAt", now);

        var id = (long)await cmd.ExecuteScalarAsync();
        Console.WriteLine($"✓ Inserted test submission with ID: {id}\n");
    }

    // Test 3: Query submissions by site
    using (var cmd = new NpgsqlCommand(@"
        SELECT id, site_id, name, email, message, submitted_at, metadata_json
        FROM contact_submissions
        WHERE site_id = @siteId
        ORDER BY submitted_at DESC
        LIMIT 5", conn))
    {
        cmd.Parameters.AddWithValue("siteId", "test-site");

        using var reader = await cmd.ExecuteReaderAsync();
        Console.WriteLine("✓ Recent submissions for 'test-site':");

        var foundAny = false;
        while (await reader.ReadAsync())
        {
            foundAny = true;
            var id = reader.GetInt64(0);
            var siteId = reader.GetString(1);
            var name = reader.GetString(2);
            var email = reader.GetString(3);
            var message = reader.GetString(4);
            var submittedAt = reader.GetDateTime(5);
            var metadataJson = reader.IsDBNull(6) ? null : reader.GetString(6);

            Console.WriteLine($"  - ID: {id}");
            Console.WriteLine($"    Name: {name}");
            Console.WriteLine($"    Email: {email}");
            Console.WriteLine($"    Message: {message.Substring(0, Math.Min(50, message.Length))}...");
            Console.WriteLine($"    Submitted: {submittedAt:yyyy-MM-dd HH:mm:ss}");
            if (!string.IsNullOrEmpty(metadataJson))
            {
                Console.WriteLine($"    Metadata: {metadataJson}");
            }
            Console.WriteLine();
        }

        if (!foundAny)
        {
            Console.WriteLine("  (No submissions found)\n");
        }
    }

    // Test 4: Count submissions by site
    using (var cmd = new NpgsqlCommand(@"
        SELECT site_id, COUNT(*) as count
        FROM contact_submissions
        GROUP BY site_id
        ORDER BY count DESC", conn))
    {
        using var reader = await cmd.ExecuteReaderAsync();
        Console.WriteLine("✓ Submissions by site:");

        while (await reader.ReadAsync())
        {
            var siteId = reader.GetString(0);
            var count = reader.GetInt64(1);
            Console.WriteLine($"  - '{siteId}': {count} submission(s)");
        }
    }

    Console.WriteLine("\n=== All Tests Passed! ===");
    Console.WriteLine("✓ Database connection: OK");
    Console.WriteLine("✓ Table structure: OK");
    Console.WriteLine("✓ Data insertion: OK");
    Console.WriteLine("✓ Data retrieval: OK");
    Console.WriteLine("✓ JSONB metadata: OK");
    Console.WriteLine("\nThe PostgreSQL database is working correctly! 🎉");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
    Environment.Exit(1);
}
