namespace Niuro.Core.Domain.Entities;

/// <summary>
/// Dirección del solicitante (value object: se persiste como columnas propias en Customers).
/// Incluye el estado, que usa el rule engine para denegar solicitudes de NY (UC-09).
/// </summary>
public sealed record Address(string Street, string City, string State, string ZipCode);