using Niuro.Core.Application.Results;

namespace Niuro.Core.Domain.Rules;

/// <summary>
/// Denial rule engine. Evaluates all registered rules;
/// the first one that applies returns Denied. If none applies → Approved.
/// </summary>
public sealed class RuleEngine : IRuleEngine
{
    private readonly IEnumerable<IDenialRule> _rules;

    public RuleEngine(IEnumerable<IDenialRule> rules)
    {
        _rules = rules;
    }

    /// <summary>
    /// Evaluates the candidate against all denial rules.
    /// </summary>
    /// <returns>Result.Success() if approved, Result.Failure(reasonCode) if denied.</returns>
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
