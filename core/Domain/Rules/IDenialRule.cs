namespace Niuro.Core.Domain.Rules;

/// <summary>
/// Interface for denial rules. Each rule is independent (Open/Closed).
/// </summary>
public interface IDenialRule
{
    /// <summary>
    /// Denial reason code (e.g. "STATE_NY", "SSN_BLACKLISTED").
    /// </summary>
    string ReasonCode { get; }

    /// <summary>
    /// Evaluates whether this rule applies to the candidate.
    /// </summary>
    Task<bool> AppliesAsync(LoanCandidate candidate, CancellationToken ct = default);
}
