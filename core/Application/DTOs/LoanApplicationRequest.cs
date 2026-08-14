namespace Niuro.Core.Application.DTOs;

/// <summary>
/// Input DTO for requesting a loan.
/// </summary>
public class LoanApplicationRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required AddressDto Address { get; init; }
    public required string CompanyName { get; init; }
    public required decimal RequestedAmount { get; init; }
    public required string Ssn { get; init; }
}

/// <summary>
/// Applicant's address.
/// </summary>
public class AddressDto
{
    public required string Street { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string ZipCode { get; init; }
}
