using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Niuro.Core.Domain.Entities;
using Niuro.Core.Infrastructure;

namespace Niuro.Tests.Integration;

[Collection("Postgres")]
public class LoanApplicationsApiTests : IDisposable
{
    private readonly PostgresFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public LoanApplicationsApiTests(PostgresFixture fixture)
    {
        _fixture = fixture;

        // Program.cs applies Migrate() (including the blacklist seed) at startup against the fixture.
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.UseSetting("ConnectionStrings:Postgres", _fixture.ConnectionString));
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static object ValidPayload(string ssn = "123-45-6789", string state = "CA") => new
    {
        firstName = "John",
        lastName = "Doe",
        companyName = "Acme Corp",
        requestedAmount = 5000m,
        ssn,
        address = new
        {
            street = "1 Main St",
            city = "Springfield",
            state,
            zipCode = "90210"
        }
    };

    private async Task<int> CountAsync<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        where T : class
    {
        using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NiuroDbContext>();
        return await db.Set<T>().CountAsync(predicate);
    }

    [Fact]
    public async Task Submit_WhenValid_ReturnsApprovedAndPersists()
    {
        await _fixture.ResetDbAsync();
        var client = _client;

        var response = await client.PostAsJsonAsync("/api/loan-applications", ValidPayload());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var decision = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("approved", decision.GetProperty("status").GetString());
        Assert.False(string.IsNullOrEmpty(decision.GetProperty("applicationId").GetString()));

        Assert.Equal(1, await CountAsync<Customer>(c => c.Ssn == "123-45-6789"));
        Assert.Equal(1, await CountAsync<OutboxEvent>(e => e.Operation == OutboxOperation.Create));
    }

    [Fact]
    public async Task Submit_WhenStateNy_ReturnsDeniedWithStateNyAndPersistsNothing()
    {
        await _fixture.ResetDbAsync();
        var client = _client;

        var response = await client.PostAsJsonAsync("/api/loan-applications", ValidPayload(state: "NY"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var decision = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("denied", decision.GetProperty("status").GetString());
        Assert.Equal("STATE_NY", decision.GetProperty("reason").GetString());

        Assert.Equal(0, await CountAsync<Customer>(c => true));
        Assert.Equal(0, await CountAsync<OutboxEvent>(e => true));
    }

    [Fact]
    public async Task Submit_WhenSsnBlacklisted_ReturnsDeniedWithBlacklistedReason()
    {
        await _fixture.ResetDbAsync();
        var client = _client;

        var response = await client.PostAsJsonAsync("/api/loan-applications", ValidPayload(ssn: "111-11-1111"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var decision = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("denied", decision.GetProperty("status").GetString());
        Assert.Equal("SSN_BLACKLISTED", decision.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Submit_WhenInvalidRequest_ReturnsUnprocessableEntity()
    {
        await _fixture.ResetDbAsync();
        var client = _client;

        var invalid = new
        {
            firstName = "",
            lastName = "Doe",
            companyName = "Acme",
            requestedAmount = 5000m,
            ssn = "bad-format",
            address = new { street = "1 Main", city = "Springfield", state = "ca", zipCode = "123" }
        };
        var response = await client.PostAsJsonAsync("/api/loan-applications", invalid);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Keys are normalized to camelCase so the frontend can map them (getFieldError).
        Assert.True(problem.GetProperty("errors").TryGetProperty("ssn", out _));
        Assert.True(problem.GetProperty("errors").TryGetProperty("firstName", out _));
        Assert.True(problem.GetProperty("errors").TryGetProperty("address.state", out _));
    }

    [Fact]
    public async Task Submit_TwiceWithSameSsn_SecondCreatesUpdateOutboxEvent()
    {
        await _fixture.ResetDbAsync();
        var client = _client;

        await client.PostAsJsonAsync("/api/loan-applications", ValidPayload(ssn: "999-99-9999"));
        await client.PostAsJsonAsync("/api/loan-applications", ValidPayload(ssn: "999-99-9999"));

        Assert.Equal(1, await CountAsync<Customer>(c => c.Ssn == "999-99-9999")); // no duplicate
        Assert.Equal(1, await CountAsync<OutboxEvent>(e => e.Operation == OutboxOperation.Create));
        Assert.Equal(1, await CountAsync<OutboxEvent>(e => e.Operation == OutboxOperation.Update));
    }
}
