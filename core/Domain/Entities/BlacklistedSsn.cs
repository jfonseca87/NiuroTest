namespace Niuro.Core.Domain.Entities;

/// <summary>
/// SSN en la lista negra (seeda por migración, UC-06). Si coincide con el del formulario,
/// la solicitud es denegada por el rule engine (UC-10).
/// </summary>
public class BlacklistedSsn
{
    public string Ssn { get; set; } = string.Empty;
}