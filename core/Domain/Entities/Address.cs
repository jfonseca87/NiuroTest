namespace Niuro.Core.Domain.Entities;

/// <summary>
/// Applicant's address (value object: persisted as its own columns in Customers).
/// Includes the state, used by the rule engine to deny NY applications.
/// </summary>
public sealed record Address(string Street, string City, string State, string ZipCode);