namespace Niuro.Core.Domain.Entities;

/// <summary>
/// Evento del outbox: registro persistido en la misma transacción que el Customer+Application
/// (UC-11/12) y enviado al servicio externo en background por el worker (UC-13).
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