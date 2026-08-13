namespace Niuro.Core.Domain.Rules;

/// <summary>
/// Regla de denegación: si el estado es NY (New York) → Denied.
/// </summary>
public sealed class StateNyRule : IDenialRule
{
    public string ReasonCode => "STATE_NY";

    public Task<bool> AppliesAsync(LoanCandidate candidate, CancellationToken ct = default)
    {
        return Task.FromResult(candidate.State == "NY");
    }
}
