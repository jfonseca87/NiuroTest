using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Niuro.Core.Application.DTOs;
using Niuro.Core.Application.UseCases;
using Niuro.Core.Domain.Entities;
using Niuro.Core.Infrastructure;

namespace Niuro.Tests.Integration;

[Collection("Postgres")]
public class SubmitLoanApplicationTests
{
    private readonly PostgresFixture _fixture;

    public SubmitLoanApplicationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static LoanApplicationRequest ValidRequest(string ssn = "123-45-6789", string state = "CA") => new()
    {
        FirstName = "John",
        LastName = "Doe",
        CompanyName = "Acme Corp",
        RequestedAmount = 5000m,
        Ssn = ssn,
        Address = new AddressDto
        {
            Street = "1 Main St",
            City = "Springfield",
            State = state,
            ZipCode = "90210"
        }
    };

    private async Task<ISubmitLoanApplication> ResolveUseCaseAsync()
    {
        var scope = _fixture.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ISubmitLoanApplication>();
    }

    private async Task<T?> QueryAsync<T>(Func<NiuroDbContext, Task<T>> query)
    {
        using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NiuroDbContext>();
        return await query(db);
    }

    private async Task<int> CountAsync<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        where T : class
    {
        using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NiuroDbContext>();
        return await db.Set<T>().CountAsync(predicate);
    }

    [Fact]
    public async Task Execute_WhenNewCustomer_CreatesCustomerApplicationAndCreateOutboxEvent()
    {
        await _fixture.ResetDbAsync();
        var useCase = await ResolveUseCaseAsync();

        var result = await useCase.ExecuteAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await CountAsync<Customer>(c => c.Ssn == "123-45-6789"));
        Assert.Equal(1, await CountAsync<LoanApplication>(a => a.CustomerId == result.Value.CustomerId));
        Assert.Equal(1, await CountAsync<OutboxEvent>(e => e.Operation == OutboxOperation.Create && e.Status == OutboxStatus.Pending));

        // El payload del outbox se guarda en snake_case (contrato del worker y del mock).
        var payload = await QueryAsync(db => db.OutboxEvents
            .Where(e => e.Operation == OutboxOperation.Create)
            .Select(e => e.Payload)
            .FirstAsync());
        Assert.False(string.IsNullOrEmpty(payload));
        var doc = JsonDocument.Parse(payload!);
        Assert.Equal("123-45-6789", doc.RootElement.GetProperty("customer").GetProperty("ssn").GetString());
        Assert.Equal("CA", doc.RootElement.GetProperty("customer").GetProperty("address").GetProperty("state").GetString());
    }

    [Fact]
    public async Task Execute_WhenReturningCustomer_UpdatesCustomerAndAddsUpdateOutboxEvent()
    {
        await _fixture.ResetDbAsync();

        // Primer submit crea el customer.
        var useCase = await ResolveUseCaseAsync();
        await useCase.ExecuteAsync(ValidRequest(ssn: "123-45-6789"));

        // Segundo submit con datos nuevos → actualiza customer + OutboxEvent(Update).
        var updated = ValidRequest(ssn: "123-45-6789");
        updated = new LoanApplicationRequest
        {
            FirstName = "Jane",
            LastName = updated.LastName,
            CompanyName = "Acme 2",
            RequestedAmount = 8000m,
            Ssn = updated.Ssn,
            Address = updated.Address
        };
        var result2 = await useCase.ExecuteAsync(updated);

        Assert.True(result2.IsSuccess);
        Assert.Equal(1, await CountAsync<Customer>(c => c.Ssn == "123-45-6789")); // no se duplica
        Assert.Equal(1, await CountAsync<OutboxEvent>(e => e.Operation == OutboxOperation.Update));

        var firstName = await QueryAsync(db => db.Customers
            .Where(c => c.Ssn == "123-45-6789")
            .Select(c => c.FirstName)
            .FirstAsync());
        Assert.Equal("Jane", firstName);
    }

    [Fact]
    public async Task Execute_WhenStateNy_ReturnsFailureAndPersistsNothing()
    {
        await _fixture.ResetDbAsync();
        var useCase = await ResolveUseCaseAsync();

        var result = await useCase.ExecuteAsync(ValidRequest(state: "NY"));

        Assert.True(result.IsFailure);
        Assert.Equal("STATE_NY", result.Error);
        Assert.Equal(0, await CountAsync<Customer>(c => true));
        Assert.Equal(0, await CountAsync<OutboxEvent>(e => true));
    }

    [Fact]
    public async Task Execute_WhenSsnBlacklisted_ReturnsFailureAndPersistsNothing()
    {
        await _fixture.ResetDbAsync();
        var useCase = await ResolveUseCaseAsync();

        var result = await useCase.ExecuteAsync(ValidRequest(ssn: "111-11-1111"));

        Assert.True(result.IsFailure);
        Assert.Equal("SSN_BLACKLISTED", result.Error);
        Assert.Equal(0, await CountAsync<Customer>(c => true));
        Assert.Equal(0, await CountAsync<OutboxEvent>(e => true));
    }

    [Fact]
    public async Task Execute_WhenReturningCustomerWithoutApplication_CreatesNewApplication()
    {
        await _fixture.ResetDbAsync();

        // Customer existente SIN application (caso edge: se crea directo en BD).
        using (var seedScope = _fixture.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<NiuroDbContext>();
            db.Customers.Add(new Customer
            {
                Id = Guid.NewGuid(),
                Ssn = "555-55-5555",
                FirstName = "Old",
                LastName = "Customer",
                CompanyName = "Old Co",
                Address = new Address("1 Old St", "Oldville", "CA", "90210")
            });
            await db.SaveChangesAsync();
        }

        var useCase = await ResolveUseCaseAsync();
        var result = await useCase.ExecuteAsync(ValidRequest(ssn: "555-55-5555"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await CountAsync<Customer>(c => c.Ssn == "555-55-5555")); // no duplica
        Assert.Equal(1, await CountAsync<LoanApplication>(a => a.CustomerId == result.Value.CustomerId));
        Assert.Equal(1, await CountAsync<OutboxEvent>(e => e.Operation == OutboxOperation.Update));
    }
}
