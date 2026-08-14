namespace Niuro.Worker.Infrastructure;

/// <summary>
/// BackgroundService that processes pending Outbox events in a polling loop.
/// The processing logic is delegated to <see cref="OutboxProcessor"/>.
/// </summary>
public sealed class OutboxWorker(
    OutboxProcessor processor,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    /// <summary>
    /// Interval between polling cycles.
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
