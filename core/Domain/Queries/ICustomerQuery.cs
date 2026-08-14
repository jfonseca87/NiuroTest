using Niuro.Core.Domain.Entities;

namespace Niuro.Core.Domain.Queries;

/// <summary>
/// Query to check whether a customer exists by SSN.
/// </summary>
public interface ICustomerQuery
{
    Task<Customer?> GetBySsnAsync(string ssn, CancellationToken ct = default);
    Task<bool> ExistsAsync(string ssn, CancellationToken ct = default);
}
