using System.Text;
using System.Text.Json;

namespace Niuro.Core.Infrastructure.Messaging;

/// <summary>
/// Typed HTTP client for communicating with the external service (mock).
/// Implements the contract: POST /api/customers (create) and PUT /api/customers/{ssn} (update).
/// </summary>
public sealed class MockExternalClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    public MockExternalClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Sends POST /api/customers to create a customer in the external service.
    /// </summary>
    public async Task<HttpResponseMessage> CreateCustomerAsync(object payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.PostAsync("/api/customers", content, ct);
    }

    /// <summary>
    /// Sends PUT /api/customers/{ssn} to update a customer in the external service.
    /// </summary>
    public async Task<HttpResponseMessage> UpdateCustomerAsync(string ssn, object payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.PutAsync($"/api/customers/{ssn}", content, ct);
    }
}
