using System.Text.Json.Serialization;

namespace Niuro.Core.Application.Responses;

/// <summary>
/// Rule engine response with the application decision.
/// </summary>
public class LoanDecision
{
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("applicationId")]
    public string? ApplicationId { get; init; }

    public static LoanDecision Approved(string? applicationId = null) =>
        new() { Status = "approved", Reason = null, ApplicationId = applicationId };

    public static LoanDecision Denied(string reason) =>
        new() { Status = "denied", Reason = reason, ApplicationId = null };
}
