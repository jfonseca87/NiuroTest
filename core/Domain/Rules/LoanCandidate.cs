namespace Niuro.Core.Domain.Rules;

/// <summary>
/// Value object con los datos del solicitante normalizados para evaluación de reglas.
/// SSN y State siempre vienen normalizados (SSN con guiones, State mayúsculas).
/// </summary>
public sealed record LoanCandidate
{
    public required string Ssn { get; init; }
    public required string State { get; init; }
    public decimal? RequestedAmount { get; init; }

    public static LoanCandidate FromRequest(Application.DTOs.LoanApplicationRequest request)
    {
        return new LoanCandidate
        {
            Ssn = NormalizeSsn(request.Ssn),
            State = request.Address.State.ToUpperInvariant(),
            RequestedAmount = request.RequestedAmount
        };
    }

    /// <summary>
    /// Normaliza SSN a formato ###-##-####.
    /// </summary>
    public static string NormalizeSsn(string ssn)
    {
        var digits = new string(ssn.Where(char.IsDigit).ToArray());
        if (digits.Length != 9)
            return ssn; // Retorna original si no tiene 9 dígitos

        return $"{digits[..3]}-{digits[3..5]}-{digits[5..]}";
    }
}
