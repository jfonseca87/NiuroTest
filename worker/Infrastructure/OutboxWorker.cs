namespace Niuro.Worker.Infrastructure;

/// <summary>
/// BackgroundService que procesa eventos de Outbox pendientes en un bucle de polling.
/// La lógica de proceso está delegada en <see cref="OutboxProcessor"/>.
/// </summary>
public sealed class OutboxWorker(
    OutboxProcessor processor,
    ILogger<OutboxWorker> logger) : BackgroundService
{
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
                await processor.ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error processing outbox events");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        logger.LogInformation("OutboxWorker stopped");
    }
}
