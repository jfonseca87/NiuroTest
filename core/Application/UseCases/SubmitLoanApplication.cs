using System.Text.Json;
using Niuro.Core.Application.DTOs;
using Niuro.Core.Application.Results;
using Niuro.Core.Domain.Entities;
using Niuro.Core.Domain.Queries;
using Niuro.Core.Domain.Rules;
using Niuro.Core.Infrastructure;

namespace Niuro.Core.Application.UseCases;

/// <summary>
/// Caso de uso: procesar una solicitud de préstamo aprobada.
/// 1. Normaliza SSN
/// 2. Busca customer por SSN
/// 3. Si no existe → crea Customer + Application + OutboxEvent (Create)
/// 4. Si existe → delega a UpdateLoanApplication (UC-12)
/// 5. Todo en una transacción (SaveChanges atómico)
/// </summary>
public sealed class SubmitLoanApplication
{
    private readonly NiuroDbContext _dbContext;
    private readonly ICustomerQuery _customerQuery;
    private readonly RuleEngine _ruleEngine;

    public SubmitLoanApplication(
        NiuroDbContext dbContext,
        ICustomerQuery customerQuery,
        RuleEngine ruleEngine)
    {
        _dbContext = dbContext;
        _customerQuery = customerQuery;
        _ruleEngine = ruleEngine;
    }

    public async Task<Result<LoanApplication>> ExecuteAsync(
        LoanApplicationRequest request,
        CancellationToken ct = default)
    {
        // 1. Normalizar SSN
        var normalizedSsn = LoanCandidate.NormalizeSsn(request.Ssn);

        // 2. Evaluar reglas (UC-08) - por si acaso llegó sin evaluar
        var candidate = LoanCandidate.FromRequest(request);
        var ruleResult = await _ruleEngine.EvaluateAsync(candidate, ct);
        if (ruleResult.IsFailure)
        {
            return Result.Failure<LoanApplication>(ruleResult.Error!);
        }

        // 3. Buscar customer existente
        var existingCustomer = await _customerQuery.GetBySsnAsync(normalizedSsn, ct);
        if (existingCustomer is not null)
        {
            // Customer existente → UC-12
            return Result.Failure<LoanApplication>("RETURNING_CUSTOMER");
        }

        // 4. Crear nuevo customer
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Ssn = normalizedSsn,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CompanyName = request.CompanyName,
            Address = new Address(
                request.Address.Street,
                request.Address.City,
                request.Address.State.ToUpperInvariant(),
                request.Address.ZipCode)
        };

        // 5. Crear application
        var application = new LoanApplication
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            RequestedAmount = request.RequestedAmount
        };
        customer.Application = application;

        // 6. Crear evento outbox (UC-13)
        var outboxPayload = new
        {
            Operation = "Create",
            Customer = new
            {
                customer.Ssn,
                customer.FirstName,
                customer.LastName,
                customer.CompanyName,
                Address = new
                {
                    customer.Address.Street,
                    customer.Address.City,
                    customer.Address.State,
                    customer.Address.ZipCode
                }
            },
            Application = new
            {
                application.Id,
                application.RequestedAmount
            }
        };

        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            Operation = OutboxOperation.Create,
            Status = OutboxStatus.Pending,
            Payload = JsonSerializer.Serialize(outboxPayload),
            CreatedAt = DateTime.UtcNow
        };

        // 7. Persistir todo en una transacción
        _dbContext.Customers.Add(customer);
        _dbContext.Applications.Add(application);
        _dbContext.OutboxEvents.Add(outboxEvent);

        await _dbContext.SaveChangesAsync(ct);

        return application;
    }
}
