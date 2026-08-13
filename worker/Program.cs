using Serilog;
using Niuro.Worker;
using Niuro.Core.Infrastructure.Logging;

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
    builder.Services.AddHostedService<Worker>();
    builder.Logging.ClearProviders();
    builder.Services.AddSerilog();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Niuro.Worker terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}