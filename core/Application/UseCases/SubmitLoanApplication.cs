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
/// Use case: process an approved loan application.
/// 1. Normalize SSN
/// 2. Look up customer by SSN
/// 3. If it does not exist → create Customer + Application + OutboxEvent (Create)
/// 4. If it exists → update Customer + Application + OutboxEvent (Update)
/// 5. Everything in an explicit transaction (BeginTransaction): if any step fails, full rollback.
/// </summary>
public sealed class SubmitLoanApplication : ISubmitLoanApplication
{
    // The outbox payload is serialized in snake_case, consistent with the OutboxProcessor
    // (which extracts customer.ssn) and with the external service contract.
    private static readonly JsonSerializerOptions OutboxJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly NiuroDbContext _dbContext;
    private readonly ICustomerQuery _customerQuery;
    private readonly IRuleEngine _ruleEngine;

    public SubmitLoanApplication(
        NiuroDbContext dbContext,
        ICustomerQuery customerQuery,
        IRuleEngine ruleEngine)
    {
        _dbContext = dbContext;
        _customerQuery = customerQuery;
        _ruleEngine = ruleEngine;
    }

    public async Task<Result<LoanApplication>> ExecuteAsync(
        LoanApplicationRequest request,
        CancellationToken ct = default)
    {
        // 1. Normalize SSN
        var normalizedSsn = LoanCandidate.NormalizeSsn(request.Ssn);

        // 2. Evaluate rules, in case it arrived un-evaluated
        var candidate = LoanCandidate.FromRequest(request);
        var ruleResult = await _ruleEngine.EvaluateAsync(candidate, ct);
        if (ruleResult.IsFailure)
        {
            return Result.Failure<LoanApplication>(ruleResult.Error!);
        }

        // 3. Look up existing customer
        var existingCustomer = await _customerQuery.GetBySsnAsync(normalizedSsn, ct);
        var isReturningCustomer = existingCustomer is not null;

        if (isReturningCustomer)
        {
            // Returning customer: update existing
            return await HandleReturningCustomerAsync(existingCustomer!, request, ct);
        }

        // New customer: create
        return await HandleNewCustomerAsync(normalizedSsn, request, ct);
    }

    private async Task<Result<LoanApplication>> HandleNewCustomerAsync(
        string normalizedSsn,
        LoanApplicationRequest request,
        CancellationToken ct)
    {
        // Begin explicit transaction
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            // Create new customer
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

            // Create application
            var application = new LoanApplication
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                RequestedAmount = request.RequestedAmount
            };
            customer.Application = application;

            // Create outbox event (Create)
            var outboxPayload = CreateOutboxPayload("Create", customer, application);
            var outboxEvent = new OutboxEvent
            {
                Id = Guid.NewGuid(),
                Operation = OutboxOperation.Create,
                Status = OutboxStatus.Pending,
                Payload = JsonSerializer.Serialize(outboxPayload, OutboxJsonOptions),
                CreatedAt = DateTime.UtcNow
            };

            // Persist everything within the transaction
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
        // If it has no Application, create a new one (edge case)
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

        // Begin explicit transaction
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            // Update existing customer with new data
            customer.UpdateFromRequest(request);

            // Create outbox event (Update)
            var outboxPayload = CreateOutboxPayload("Update", customer, application);
            var outboxEvent = new OutboxEvent
            {
                Id = Guid.NewGuid(),
                Operation = OutboxOperation.Update,
                Status = OutboxStatus.Pending,
                Payload = JsonSerializer.Serialize(outboxPayload, OutboxJsonOptions),
                CreatedAt = DateTime.UtcNow
            };

            // Persist within the transaction (no explicit Update() needed because entities are already tracked)
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
