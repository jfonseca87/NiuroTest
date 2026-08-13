using System.Text;
using System.Text.Json;

namespace Niuro.Core.Infrastructure.Messaging;

/// <summary>
/// Cliente HTTP tipado para comunicarse con el servicio externo (mock).
/// Implementa el contrato: POST /api/customers (create) y PUT /api/customers/{ssn} (update).
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
    /// Envía POST /api/customers para crear un cliente en el servicio externo.
    /// </summary>
    public async Task<HttpResponseMessage> CreateCustomerAsync(object payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.PostAsync("/api/customers", content, ct);
    }

    /// <summary>
    /// Envía PUT /api/customers/{ssn} para actualizar un cliente en el servicio externo.
    /// </summary>
    public async Task<HttpResponseMessage> UpdateCustomerAsync(string ssn, object payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.PutAsync($"/api/customers/{ssn}", content, ct);
    }
}
