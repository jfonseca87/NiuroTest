using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
/// 4. Si existe → actualiza Customer + Application + OutboxEvent (Update)
/// 5. Todo en una transacción explícita (BeginTransaction): si cualquier paso falla, rollback total.
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
        var isReturningCustomer = existingCustomer is not null;

        if (isReturningCustomer)
        {
            // UC-12: Customer existente → actualizar
            return await HandleReturningCustomerAsync(existingCustomer!, request, ct);
        }

        // UC-11: Customer nuevo → crear
        return await HandleNewCustomerAsync(normalizedSsn, request, ct);
    }

    private async Task<Result<LoanApplication>> HandleNewCustomerAsync(
        string normalizedSsn,
        LoanApplicationRequest request,
        CancellationToken ct)
    {
        // Iniciar transacción explícita
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            // Crear nuevo customer
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

            // Crear application
            var application = new LoanApplication
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                RequestedAmount = request.RequestedAmount
            };
            customer.Application = application;

            // Crear evento outbox (Create)
            var outboxPayload = CreateOutboxPayload("Create", customer, application);
            var outboxEvent = new OutboxEvent
            {
                Id = Guid.NewGuid(),
                Operation = OutboxOperation.Create,
                Status = OutboxStatus.Pending,
                Payload = JsonSerializer.Serialize(outboxPayload),
                CreatedAt = DateTime.UtcNow
            };

            // Persistir todo en la transacción
            _dbContext.Customers.Add(customer);
            _dbContext.Applications.Add(application);
            _dbContext.OutboxEvents.Add(outboxEvent);

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return application;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<Result<LoanApplication>> HandleReturningCustomerAsync(
        Customer customer,
        LoanApplicationRequest request,
        CancellationToken ct)
    {
        // Si no tiene Application, crear una nueva (caso edge)
        var application = customer.Application;
        if (application is null)
        {
            application = new LoanApplication
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                RequestedAmount = request.RequestedAmount
            };
            customer.Application = application;
            _dbContext.Applications.Add(application);
        }
        else
        {
            application.RequestedAmount = request.RequestedAmount;
        }

        // Iniciar transacción explícita
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            // UC-12: Actualizar customer existente con nuevos datos
            customer.UpdateFromRequest(request);

            // Crear evento outbox (Update)
            var outboxPayload = CreateOutboxPayload("Update", customer, application);
            var outboxEvent = new OutboxEvent
            {
                Id = Guid.NewGuid(),
                Operation = OutboxOperation.Update,
                Status = OutboxStatus.Pending,
                Payload = JsonSerializer.Serialize(outboxPayload),
                CreatedAt = DateTime.UtcNow
            };

            // Persistir en la transacción (no se necesita Update() explícito porque las entidades ya están trackeadas)
            _dbContext.OutboxEvents.Add(outboxEvent);

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return application;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static object CreateOutboxPayload(string operation, Customer customer, LoanApplication application)
    {
        return new
        {
            Operation = operation,
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
    }
}
