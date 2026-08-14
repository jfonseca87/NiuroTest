using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Niuro.Core.Domain.Entities;
using Niuro.Core.Infrastructure;
using Niuro.Core.Infrastructure.Messaging;
using Niuro.Worker.Infrastructure;
using Testcontainers.PostgreSql;

namespace Niuro.Tests.Worker;

/// <summary>
/// Integration test of the OutboxProcessor against real PostgreSQL (Testcontainers).
/// </summary>
public class OutboxProcessorTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("niuro_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private ServiceProvider _provider = null!;
    private ConfigurableHandler _externalHandler = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _externalHandler = new ConfigurableHandler();

        _provider = new ServiceCollection()
            .AddLogging(b => b.ClearProviders())
            .AddDbContext<NiuroDbContext>(o => o.UseNpgsql(_postgres.GetConnectionString()))
            .AddSingleton(_externalHandler)
            .AddSingleton<MockExternalClient>(sp => new MockExternalClient(
                new HttpClient(sp.GetRequiredService<ConfigurableHandler>())
                {
                    BaseAddress = new Uri("https://mock.test")
                }))
            .AddScoped<OutboxProcessor>()
            .BuildServiceProvider();

        // Schema sufficient for the outbox (does not require the blacklist seed migrations).
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NiuroDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private static readonly JsonSerializerOptions SnakeCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private static string ValidPayload() => JsonSerializer.Serialize(new
    {
        Operation = "Create",
        Customer = new
        {
            Ssn = "123-45-6789",
            FirstName = "John",
            LastName = "Doe"
        },
        Application = new
        {
            Id = Guid.NewGuid(),
            RequestedAmount = 5000m
        }
    }, SnakeCase);

    private async Task<OutboxEvent> SeedEventAsync(OutboxStatus status = OutboxStatus.Pending)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NiuroDbContext>();

        var evt = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            Operation = OutboxOperation.Create,
            Status = status,
            Payload = ValidPayload(),
            CreatedAt = DateTime.UtcNow
        };
        db.OutboxEvents.Add(evt);
        await db.SaveChangesAsync();
        return evt;
    }

    private async Task<OutboxEvent?> GetByIdAsync(Guid id)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NiuroDbContext>();
        return await db.OutboxEvents.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id);
    }

    private OutboxProcessor BuildProcessor()
    {
        using var scope = _provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
    }

    [Fact]
    public async Task ProcessPendingEvents_WhenExternalSucceeds_MarksEventSent()
    {
        _externalHandler.ResponseStatus = HttpStatusCode.Created;

        var evt = await SeedEventAsync();
        var processor = BuildProcessor();

        var processed = await processor.ProcessPendingEventsAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        var persisted = await GetByIdAsync(evt.Id);
        Assert.NotNull(persisted);
        Assert.Equal(OutboxStatus.Sent, persisted!.Status);
        Assert.NotNull(persisted.ProcessedAt);
        Assert.Null(persisted.Error);
    }

    [Fact]
    public async Task ProcessPendingEvents_WhenExternalFails_MarksEventFailedWithHttpError()
    {
        _externalHandler.ResponseStatus = HttpStatusCode.InternalServerError;

        var evt = await SeedEventAsync();
        var processor = BuildProcessor();

        var processed = await processor.ProcessPendingEventsAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        var persisted = await GetByIdAsync(evt.Id);
        Assert.NotNull(persisted);
        Assert.Equal(OutboxStatus.Failed, persisted!.Status);
        Assert.NotNull(persisted.ProcessedAt);
        Assert.Contains("InternalServerError", persisted.Error);
    }

    [Fact]
    public async Task ProcessPendingEvents_OnlyProcessesPendingEvents()
    {
        _externalHandler.ResponseStatus = HttpStatusCode.Created;

        await SeedEventAsync(OutboxStatus.Sent);    // Must not be reprocessed
        await SeedEventAsync(OutboxStatus.Failed);  // Must not be reprocessed
        var pending = await SeedEventAsync();       // The only candidate
        var processor = BuildProcessor();

        var processed = await processor.ProcessPendingEventsAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        var persistedPending = await GetByIdAsync(pending.Id);
        Assert.Equal(OutboxStatus.Sent, persistedPending!.Status);
    }

    /// <summary>
    /// HttpMessageHandler stub that responds with a configurable status and captures the request.
    /// </summary>
    internal sealed class ConfigurableHandler : HttpMessageHandler
    {
        public HttpStatusCode ResponseStatus { get; set; } = HttpStatusCode.Created;
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var response = new HttpResponseMessage(ResponseStatus)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
