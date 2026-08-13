namespace Niuro.Core.Domain.Queries;

/// <summary>
/// Query para verificar si un SSN está en la blacklist.
/// Implementada en infraestructura para mantener el dominio sin dependencia de EF Core.
/// </summary>
public interface IBlacklistedSsnQuery
{
    Task<bool> IsBlacklistedAsync(string ssn, CancellationToken ct = default);
}
