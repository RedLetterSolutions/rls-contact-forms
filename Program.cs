using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RLS_Contact_Forms.Data;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Register PostgreSQL DbContext
        var connectionString = context.Configuration["PostgresConnectionString"];
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));
    })
    .Build();

host.Run();
