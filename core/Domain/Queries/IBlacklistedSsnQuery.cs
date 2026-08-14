namespace Niuro.Core.Domain.Queries;

/// <summary>
/// Query to check whether an SSN is on the blacklist.
/// Implemented in infrastructure to keep the domain free of EF Core dependency.
/// </summary>
public interface IBlacklistedSsnQuery
{
    Task<bool> IsBlacklistedAsync(string ssn, CancellationToken ct = default);
}
