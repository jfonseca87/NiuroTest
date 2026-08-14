using Microsoft.EntityFrameworkCore;
using Serilog;
using Niuro.Core.Infrastructure;
using Niuro.Core.Infrastructure.Logging;
using Niuro.Core.Infrastructure.Messaging;
using Niuro.Worker.Infrastructure;

// Same rolling file as the API; the "Service" property differentiates the process.
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Niuro.Worker")
    .WriteTo.File(
        path: LogFilePath.Resolve("niuro-backend-.log"),
        rollingInterval: RollingInterval.Day,
        shared: true,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Niuro.Worker host");

    var builder = Host.CreateApplicationBuilder(args);
    builder.Logging.ClearProviders();
    builder.Services.AddSerilog();

    // Persistence: PostgreSQL via user secrets (ConnectionStrings:Postgres)
    builder.Services.AddDbContext<NiuroDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

    // HTTP client for the external service (mock)
    builder.Services.AddHttpClient<MockExternalClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ExternalService:BaseUrl"] ?? "https://localhost:7124");
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    // Outbox processor (testable logic). Stateless: it resolves its own scoped NiuroDbContext
    // per batch via IServiceScopeFactory, so it must be a singleton to be consumed by the
    // hosted service (OutboxWorker) without a scoped-from-singleton violation.
    builder.Services.AddSingleton<OutboxProcessor>();

    // Worker that processes the outbox in the background
    builder.Services.AddHostedService<OutboxWorker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    Log.Fatal(ex, "Niuro.Worker terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}