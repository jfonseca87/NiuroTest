using Microsoft.EntityFrameworkCore;
using Niuro.Core.Domain.Entities;
using Niuro.Core.Domain.Queries;
using Niuro.Core.Infrastructure;

namespace Niuro.Core.Infrastructure.Queries;

/// <summary>
/// Implementación de ICustomerQuery usando EF Core.
/// </summary>
public sealed class CustomerQuery : ICustomerQuery
{
    private readonly NiuroDbContext _dbContext;

    public CustomerQuery(NiuroDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Customer?> GetBySsnAsync(string ssn, CancellationToken ct = default)
    {
        return await _dbContext.Customers
            .Include(c => c.Application)
            .FirstOrDefaultAsync(c => c.Ssn == ssn, ct);
    }

    public async Task<bool> ExistsAsync(string ssn, CancellationToken ct = default)
    {
        return await _dbContext.Customers.AnyAsync(c => c.Ssn == ssn, ct);
    }
}
