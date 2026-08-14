namespace Niuro.Core.Domain.Rules;

/// <summary>
/// Value object with the applicant's normalized data for rule evaluation.
/// SSN and State are always normalized (SSN with dashes, State uppercase).
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
    /// Normalizes SSN to ###-##-#### format.
    /// </summary>
    public static string NormalizeSsn(string ssn)
    {
        var digits = new string(ssn.Where(char.IsDigit).ToArray());
        if (digits.Length != 9)
            return ssn; // Returns original if it does not have 9 digits

        return $"{digits[..3]}-{digits[3..5]}-{digits[5..]}";
    }
}
