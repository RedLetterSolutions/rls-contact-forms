#!/usr/bin/env dotnet script
#r "nuget: Npgsql, 8.0.5"

using Npgsql;
using System.Threading.Tasks;

var connectionString = "Host=caboose.proxy.rlwy.net;Port=46817;Username=postgres;Password=TEldbxeBObbohehNCkfjJpadtmyUNPRC;Database=railway;SSL Mode=Require;Trust Server Certificate=true;";

Console.WriteLine("Cleaning up failed migration...");

await Task.Run(async () =>
{
    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    // Drop api_keys table if it exists (was created before the error)
    using (var cmd = new NpgsqlCommand("DROP TABLE IF EXISTS api_keys", conn))
    {
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("✓ Dropped api_keys table (if it existed)");
    }

    // Remove the failed migration record
    using (var cmd = new NpgsqlCommand("DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20251019010242_AddWebhooksAndApiKeys'", conn))
    {
        var rows = await cmd.ExecuteNonQueryAsync();
        Console.WriteLine($"✓ Removed {rows} migration record(s)");
    }

    Console.WriteLine("\n✅ Database cleaned up! You can now run the app again.");
});
