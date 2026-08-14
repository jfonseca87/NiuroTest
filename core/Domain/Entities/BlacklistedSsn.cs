namespace Niuro.Core.Domain.Entities;

/// <summary>
/// SSN on the blacklist (seeded by migration). If it matches the one from the form,
/// the application is denied by the rule engine.
/// </summary>
public class BlacklistedSsn
{
    public string Ssn { get; set; } = string.Empty;
}