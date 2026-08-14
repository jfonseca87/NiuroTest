namespace Niuro.Core.Domain.Entities;

/// <summary>
/// Outbox event: record persisted in the same transaction as the Customer+Application
/// and sent to the external service in the background by the worker.
/// </summary>
public class OutboxEvent
{
    public Guid Id { get; set; }
    public OutboxOperation Operation { get; set; }
    public OutboxStatus Status { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
}

public enum OutboxOperation
{
    Create,
    Update,
}

public enum OutboxStatus
{
    Pending,
    Sent,
    Failed,
}