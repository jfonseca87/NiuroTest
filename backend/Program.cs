using Serilog;
using Niuro.Core.Infrastructure;
using Niuro.Core.Infrastructure.Logging;
using Niuro.Core.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Niuro.Core.Application.DTOs;
using Niuro.Core.Application.Validators;
using Niuro.Core.Domain.Rules;
using Niuro.Core.Domain.Queries;
using Niuro.Core.Application.UseCases;
using Niuro.Api.Endpoints;

// Startup logging: the API is an entry point with logging rights (the mock does not log here).
// The worker will share the same rolling file; the "Service" property differentiates which process wrote it.
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
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();
    // MVC services are registered because WebApplicationFactory needs them to configure
    // the host in integration tests; no controller is mapped (the API is minimal).
    builder.Services.AddControllers();

    builder.Services.AddCors(options => options.AddPolicy("TestPolicy", policy =>
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod()));

    // FluentValidation validation.
    builder.Services.AddScoped<IValidator<LoanApplicationRequest>, LoanApplicationRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<LoanApplicationRequestValidator>();

    // Persistence: PostgreSQL via user secrets (ConnectionStrings:Postgres), never in committed appsettings.
    builder.Services.AddDbContext<NiuroDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

    // Rule Engine: denial rules registered via DI.
    builder.Services.AddScoped<IBlacklistedSsnQuery, BlacklistedSsnQuery>();
    builder.Services.AddScoped<ICustomerQuery, CustomerQuery>();
    builder.Services.AddScoped<IDenialRule, StateNyRule>();
    builder.Services.AddScoped<IDenialRule, BlacklistedSsnRule>();
    builder.Services.AddScoped<IRuleEngine, RuleEngine>();

    // Use case: transactional persistence.
    builder.Services.AddScoped<ISubmitLoanApplication, SubmitLoanApplication>();

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

    if (app.Environment.IsDevelopment())
    {
        app.UseCors("TestPolicy");
    }

    app.UseAuthorization();

    // Minimal APIs: endpoints live in Endpoints/ and are registered here in a single line.
    app.MapLoanApplicationEndpoints();

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

/// <summary>
/// Public marker so WebApplicationFactory can boot the app in integration tests.
/// </summary>
public partial class Program { }