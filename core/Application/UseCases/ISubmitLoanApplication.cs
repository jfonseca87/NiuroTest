using Niuro.Core.Application.DTOs;
using Niuro.Core.Application.Results;
using Niuro.Core.Domain.Entities;

namespace Niuro.Core.Application.UseCases;

/// <summary>
/// Use case: process an approved loan application and persist it transactionally.
/// Abstraction to allow testing the controller without coupling to the implementation (DIP).
/// </summary>
public interface ISubmitLoanApplication
{
    /// <summary>
    /// Persists customer + application + outbox event in an atomic transaction.
    /// </summary>
    Task<Result<LoanApplication>> ExecuteAsync(LoanApplicationRequest request, CancellationToken ct = default);
}
