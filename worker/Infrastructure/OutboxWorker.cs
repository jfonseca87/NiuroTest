using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Niuro.Core.Domain.Entities;
using Niuro.Core.Infrastructure;
using Niuro.Core.Infrastructure.Messaging;

namespace Niuro.Worker.Infrastructure;

/// <summary>
/// Worker que procesa eventos de Outbox pendientes.
/// Lee de la BD y envía al servicio externo (mock) por HTTP.
/// </summary>
public sealed class OutboxWorker(
    IServiceScopeFactory scopeFactory,
    MockExternalClient externalClient,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>
    /// Intervalo entre ciclos de polling.
    /// </summary>
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error processing outbox events");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        logger.LogInformation("OutboxWorker stopped");
    }

    private async Task ProcessPendingEventsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NiuroDbContext>();

        // Consultar eventos pendientes (sin tracking, ordenados por CreatedAt)
        var pendingEvents = await dbContext.OutboxEvents
            .Where(e => e.Status == OutboxStatus.Pending)
            .OrderBy(e => e.CreatedAt)
            .Take(100) // Límite por ciclo
            .ToListAsync(ct);

        if (pendingEvents.Count == 0)
            return;

        logger.LogInformation("Processing {Count} pending outbox events", pendingEvents.Count);

        foreach (var outboxEvent in pendingEvents)
        {
            try
            {
                await ProcessEventAsync(outboxEvent, dbContext, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to process outbox event {EventId}", outboxEvent.Id);
                outboxEvent.Status = OutboxStatus.Failed;
                outboxEvent.Error = ex.Message;
                outboxEvent.ProcessedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(ct);
            }
        }
    }

    private async Task ProcessEventAsync(OutboxEvent outboxEvent, NiuroDbContext dbContext, CancellationToken ct)
    {
        // Extraer SSN del payload para poder hacer PUT con el SSN como key
        var payloadJson = JsonDocument.Parse(outboxEvent.Payload);
        var ssn = payloadJson.RootElement
            .GetProperty("customer")
            .GetProperty("ssn")
            .GetString()!;

        HttpResponseMessage response;

        if (outboxEvent.Operation == OutboxOperation.Create)
        {
            logger.LogDebug("Sending CREATE event {EventId} for SSN {Ssn}", outboxEvent.Id, ssn[^4..]);
            response = await externalClient.CreateCustomerAsync(outboxEvent.Payload, ct);
        }
        else
        {
            logger.LogDebug("Sending UPDATE event {EventId} for SSN {Ssn}", outboxEvent.Id, ssn[^4..]);
            response = await externalClient.UpdateCustomerAsync(ssn, outboxEvent.Payload, ct);
        }

        if (response.IsSuccessStatusCode)
        {
            outboxEvent.Status = OutboxStatus.Sent;
            outboxEvent.ProcessedAt = DateTime.UtcNow;
            outboxEvent.Error = null;
            logger.LogInformation("Outbox event {EventId} sent successfully (Operation={Operation})", outboxEvent.Id, outboxEvent.Operation);
        }
        else
        {
            outboxEvent.Status = OutboxStatus.Failed;
            outboxEvent.ProcessedAt = DateTime.UtcNow;
            outboxEvent.Error = $"HTTP {response.StatusCode}";
            logger.LogWarning("Outbox event {EventId} failed with HTTP {StatusCode}", outboxEvent.Id, response.StatusCode);
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
