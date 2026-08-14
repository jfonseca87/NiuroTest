using Niuro.Core.Application.DTOs;
using Niuro.Core.Application.Results;
using Niuro.Core.Domain.Entities;

namespace Niuro.Core.Application.UseCases;

/// <summary>
/// Caso de uso: procesar una solicitud de préstamo aprobada y persistirla de forma transaccional.
/// Abstracción para permitir testear el controller sin acoplar a la implementación (DIP).
/// </summary>
public interface ISubmitLoanApplication
{
    /// <summary>
    /// Persiste customer + application + outbox event en una transacción atómica.
    /// </summary>
    Task<Result<LoanApplication>> ExecuteAsync(LoanApplicationRequest request, CancellationToken ct = default);
}
