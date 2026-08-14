using Niuro.Core.Domain.Queries;

namespace Niuro.Core.Domain.Rules;

/// <summary>
/// Denial rule: if the SSN is on the blacklist → Denied.
/// </summary>
public sealed class BlacklistedSsnRule : IDenialRule
{
    private readonly IBlacklistedSsnQuery _query;

    public BlacklistedSsnRule(IBlacklistedSsnQuery query)
    {
        _query = query;
    }

    public string ReasonCode => "SSN_BLACKLISTED";

    public async Task<bool> AppliesAsync(LoanCandidate candidate, CancellationToken ct = default)
    {
        return await _query.IsBlacklistedAsync(candidate.Ssn, ct);
    }
}
