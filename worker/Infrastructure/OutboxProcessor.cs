using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Niuro.Core.Domain.Entities;
using Niuro.Core.Infrastructure;
using Niuro.Core.Infrastructure.Messaging;

namespace Niuro.Worker.Infrastructure;

/// <summary>
/// Processes pending outbox events: reads from the database, sends them to the external service (mock)
/// via HTTP and updates their status. Logic separated from the BackgroundService so it can be tested.
/// </summary>
public sealed class OutboxProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>
    /// Limit of events processed per polling cycle.
    /// </summary>
    public const int BatchSize = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MockExternalClient _externalClient;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        MockExternalClient externalClient,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _externalClient = externalClient;
        _logger = logger;
    }

    /// <summary>
    /// Processes a batch of pending events (at most <see cref="BatchSize"/>, ordered by CreatedAt).
    /// </summary>
    public async Task<int> ProcessPendingEventsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NiuroDbContext>();

        var pendingEvents = await dbContext.OutboxEvents
            .Where(e => e.Status == OutboxStatus.Pending)
            .OrderBy(e => e.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (pendingEvents.Count == 0)
            return 0;

        _logger.LogInformation("Processing {Count} pending outbox events", pendingEvents.Count);

        foreach (var outboxEvent in pendingEvents)
        {
            try
            {
                await ProcessEventAsync(outboxEvent, dbContext, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to process outbox event {EventId}", outboxEvent.Id);
                MarkFailed(outboxEvent, ex.Message);
                await dbContext.SaveChangesAsync(ct);
            }
        }

        return pendingEvents.Count;
    }

    private async Task ProcessEventAsync(OutboxEvent outboxEvent, NiuroDbContext dbContext, CancellationToken ct)
    {
        // Extract SSN from the payload so we can PUT with the SSN as the key
        var payloadJson = JsonDocument.Parse(outboxEvent.Payload);
        var ssn = payloadJson.RootElement
            .GetProperty("customer")
            .GetProperty("ssn")
            .GetString()!;

        HttpResponseMessage response;

        if (outboxEvent.Operation == OutboxOperation.Create)
        {
            _logger.LogDebug("Sending CREATE event {EventId} for SSN {Ssn}", outboxEvent.Id, ssn[^4..]);
            response = await _externalClient.CreateCustomerAsync(outboxEvent.Payload, ct);
        }
        else
        {
            _logger.LogDebug("Sending UPDATE event {EventId} for SSN {Ssn}", outboxEvent.Id, ssn[^4..]);
            response = await _externalClient.UpdateCustomerAsync(ssn, outboxEvent.Payload, ct);
        }

        if (response.IsSuccessStatusCode)
        {
            outboxEvent.Status = OutboxStatus.Sent;
            outboxEvent.ProcessedAt = DateTime.UtcNow;
            outboxEvent.Error = null;
            _logger.LogInformation(
                "Outbox event {EventId} sent successfully (Operation={Operation})",
                outboxEvent.Id, outboxEvent.Operation);
        }
        else
        {
            MarkFailed(outboxEvent, $"HTTP {response.StatusCode}");
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static void MarkFailed(OutboxEvent outboxEvent, string error)
    {
        outboxEvent.Status = OutboxStatus.Failed;
        outboxEvent.ProcessedAt = DateTime.UtcNow;
        outboxEvent.Error = error;
    }
}
