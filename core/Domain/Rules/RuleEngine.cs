using Niuro.Core.Application.Results;

namespace Niuro.Core.Domain.Rules;

/// <summary>
/// Motor de reglas de denegación. Evalúa todas las reglas registradas;
/// la primera que aplica retorna Denied. Si ninguna aplica → Approved.
/// </summary>
public sealed class RuleEngine
{
    private readonly IEnumerable<IDenialRule> _rules;

    public RuleEngine(IEnumerable<IDenialRule> rules)
    {
        _rules = rules;
    }

    /// <summary>
    /// Evalúa el candidato contra todas las reglas de denegación.
    /// </summary>
    /// <returns>Result.Success() si aprobado, Result.Failure(reasonCode) si denegado.</returns>
    public async Task<Result> EvaluateAsync(LoanCandidate candidate, CancellationToken ct = default)
    {
        foreach (var rule in _rules)
        {
            if (await rule.AppliesAsync(candidate, ct))
            {
                return Result.Failure(rule.ReasonCode);
            }
        }

        return Result.Success();
    }
}
