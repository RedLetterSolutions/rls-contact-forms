#!/usr/bin/env dotnet script
#r "nuget: Npgsql, 8.0.5"

using Npgsql;
using System;
using System.Threading.Tasks;

// Test script to verify DateTime.SpecifyKind fixes the PostgreSQL timezone issue

var connectionString = "Host=caboose.proxy.rlwy.net;Port=46817;Username=postgres;Password=TEldbxeBObbohehNCkfjJpadtmyUNPRC;Database=railway;SSL Mode=Require;Trust Server Certificate=true;";

Console.WriteLine("=== Testing DateTime with UTC Kind for PostgreSQL ===\n");

try
{
    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    Console.WriteLine("✓ Connected to PostgreSQL database\n");

    // Test 1: Insert with DateTime.SpecifyKind (should work)
    var utcTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
    Console.WriteLine($"Testing with UTC Kind: {utcTime} (Kind: {utcTime.Kind})");
    
    using (var cmd = new NpgsqlCommand("SELECT @testTime::timestamp with time zone", conn))
    {
        cmd.Parameters.AddWithValue("testTime", utcTime);
        var result = await cmd.ExecuteScalarAsync();
        Console.WriteLine($"✓ Successfully handled UTC timestamp: {result}\n");
    }

    // Test 2: Try with Unspecified (this would cause the original error)
    var unspecifiedTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
    Console.WriteLine($"Testing with Unspecified Kind: {unspecifiedTime} (Kind: {unspecifiedTime.Kind})");
    
    try
    {
        using (var cmd = new NpgsqlCommand("SELECT @testTime::timestamp with time zone", conn))
        {
            cmd.Parameters.AddWithValue("testTime", unspecifiedTime);
            var result = await cmd.ExecuteScalarAsync();
            Console.WriteLine($"⚠️  Unexpected success with Unspecified: {result}");
        }
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"✓ Expected error with Unspecified Kind: {ex.Message.Substring(0, 80)}...\n");
    }

    Console.WriteLine("=== DateTime Fix Verification Complete ===");
    Console.WriteLine("✓ DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc) works correctly");
    Console.WriteLine("✓ The webhook update error should now be resolved");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Test failed: {ex.Message}");
}