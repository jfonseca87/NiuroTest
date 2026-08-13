using Microsoft.EntityFrameworkCore;
using Serilog;
using Niuro.Core.Infrastructure;
using Niuro.Core.Infrastructure.Logging;
using Niuro.Core.Infrastructure.Messaging;
using Niuro.Worker.Infrastructure;

// Mismo archivo rolling que el API (UC-02); la property "Service" diferencia el proceso.
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

    // Persistencia: PostgreSQL vía user secrets (ConnectionStrings:Postgres)
    builder.Services.AddDbContext<NiuroDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

    // HTTP Client para el servicio externo (mock) - UC-13
    builder.Services.AddHttpClient<MockExternalClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ExternalService:BaseUrl"] ?? "http://localhost:5200");
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    // Worker que procesa el outbox - UC-13
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