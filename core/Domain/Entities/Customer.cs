namespace Niuro.Core.Domain.Entities;

/// <summary>
/// Solicitante de un préstamo. Se identifica por SSN (clave natural, normalizada con guiones).
/// Un mismo SSN = un único Customer en la BD (UC-11/12).
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
}