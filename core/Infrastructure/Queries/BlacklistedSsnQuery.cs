using Microsoft.EntityFrameworkCore;
using Niuro.Core.Domain.Queries;
using Niuro.Core.Infrastructure;

namespace Niuro.Core.Infrastructure.Queries;

/// <summary>
/// Implementación de IBlacklistedSsnQuery usando EF Core.
/// </summary>
public sealed class BlacklistedSsnQuery : IBlacklistedSsnQuery
{
    private readonly NiuroDbContext _dbContext;

    public BlacklistedSsnQuery(NiuroDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsBlacklistedAsync(string ssn, CancellationToken ct = default)
    {
        return await _dbContext.BlacklistedSsns
            .AnyAsync(b => b.Ssn == ssn, ct);
    }
}
