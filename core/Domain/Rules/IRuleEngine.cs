using Niuro.Core.Application.Results;

namespace Niuro.Core.Domain.Rules;

/// <summary>
/// Denial rule engine. Abstraction that allows replacing the implementation
/// and testing consumers without coupling to the concrete implementation (DIP).
/// </summary>
public interface IRuleEngine
{
    /// <summary>
    /// Evaluates the candidate against all denial rules.
    /// </summary>
    /// <returns>Result.Success() if approved, Result.Failure(reasonCode) if denied.</returns>
    Task<Result> EvaluateAsync(LoanCandidate candidate, CancellationToken ct = default);
}
