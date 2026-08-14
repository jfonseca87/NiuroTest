using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Niuro.Core.Domain.Queries;
using Niuro.Core.Domain.Rules;
using Niuro.Core.Infrastructure;
using Niuro.Core.Infrastructure.Queries;
using Testcontainers.PostgreSql;

namespace Niuro.Tests.Integration;

/// <summary>
/// Define la colección "Postgres": los tests de integración comparten un único contenedor.
/// </summary>
[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}

/// <summary>
/// Fixture compartido con un único PostgreSQL (Testcontainers) y el DI real de la app.
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("niuro_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private ServiceProvider _provider = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _provider = new ServiceCollection()
            .AddLogging()
            .AddDbContext<NiuroDbContext>(o => o.UseNpgsql(ConnectionString))
            .AddScoped<IBlacklistedSsnQuery, BlacklistedSsnQuery>()
            .AddScoped<ICustomerQuery, CustomerQuery>()
            .AddScoped<IDenialRule, StateNyRule>()
            .AddScoped<IDenialRule, BlacklistedSsnRule>()
            .AddScoped<IRuleEngine, Niuro.Core.Domain.Rules.RuleEngine>()
            .AddScoped<Niuro.Core.Application.UseCases.ISubmitLoanApplication, Niuro.Core.Application.UseCases.SubmitLoanApplication>()
            .BuildServiceProvider();

        // Aplica esquema + seed de SSNs en blacklist (idempotente).
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NiuroDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Crea un scope con el contenedor de DI real (para resolver use cases / queries).
    /// </summary>
    public IServiceScope CreateScope() => _provider.CreateScope();

    /// <summary>
    /// Limpia las tablas de negocio dejando el seed de blacklist intacto.
    /// </summary>
    public async Task ResetDbAsync()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NiuroDbContext>();
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"OutboxEvents\";");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Applications\";");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Customers\";");
    }
}
