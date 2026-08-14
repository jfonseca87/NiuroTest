using Niuro.Core.Application.Results;

namespace Niuro.Core.Domain.Rules;

/// <summary>
/// Motor de reglas de denegación. Abstracción para permitir sustituir la implementación
/// y para testear consumidores sin acoplar a la implementación concreta (DIP).
/// </summary>
public interface IRuleEngine
{
    /// <summary>
    /// Evalúa el candidato contra todas las reglas de denegación.
    /// </summary>
    /// <returns>Result.Success() si aprobado, Result.Failure(reasonCode) si denegado.</returns>
    Task<Result> EvaluateAsync(LoanCandidate candidate, CancellationToken ct = default);
}
