using Serilog;
using Niuro.Core.Infrastructure;
using Niuro.Core.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Niuro.Core.Application.DTOs;
using Niuro.Core.Application.Validators;

// Logging de arranque: el API es un entry point con derechos de logging (el mock no loguea aquí).
// El worker compartirá el mismo archivo rolling; la property "Service" diferencia qué proceso escribió.
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Niuro.Api")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: LogFilePath.Resolve("niuro-backend-.log"),
        rollingInterval: RollingInterval.Day,
        shared: true,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Niuro.Api host");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    // Validación con FluentValidation (UC-07).
    builder.Services.AddScoped<IValidator<LoanApplicationRequest>, LoanApplicationRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<LoanApplicationRequestValidator>();

    // Persistencia: PostgreSQL vía user secrets (ConnectionStrings:Postgres), nunca en appsettings commiteado.
    builder.Services.AddDbContext<NiuroDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider
            .GetRequiredService<NiuroDbContext>();

        dbContext.Database.Migrate();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Niuro.Api terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}