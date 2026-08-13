namespace Niuro.Core.Domain.Rules;

/// <summary>
/// Interfaz para reglas de denegación. Cada regla es independiente (Open/Closed).
/// </summary>
public interface IDenialRule
{
    /// <summary>
    /// Código de razón de denegación (ej: "STATE_NY", "SSN_BLACKLISTED").
    /// </summary>
    string ReasonCode { get; }

    /// <summary>
    /// Evalúa si esta regla aplica al candidato.
    /// </summary>
    Task<bool> AppliesAsync(LoanCandidate candidate, CancellationToken ct = default);
}
