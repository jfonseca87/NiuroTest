using Niuro.Core.Application.DTOs;

namespace Niuro.Core.Domain.Entities;

/// <summary>
/// Loan applicant. Identified by SSN (natural key, normalized with dashes).
/// The same SSN = a single Customer in the database.
/// </summary>
public class Customer
{
    public Guid Id { get; set; }
    public string Ssn { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Address Address { get; set; } = null!;
    public string CompanyName { get; set; } = string.Empty;
    public LoanApplication? Application { get; set; }

    /// <summary>
    /// Updates the customer data with that of a new application.
    /// The SSN does not change (it is the business key).
    /// </summary>
    public void UpdateFromRequest(LoanApplicationRequest request)
    {
        FirstName = request.FirstName;
        LastName = request.LastName;
        CompanyName = request.CompanyName;
        Address = new Address(
            request.Address.Street,
            request.Address.City,
            request.Address.State.ToUpperInvariant(),
            request.Address.ZipCode);
    }
}