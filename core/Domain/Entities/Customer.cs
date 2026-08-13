using Niuro.Core.Application.DTOs;

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

    /// <summary>
    /// Actualiza los datos del cliente con los de una nueva solicitud (UC-12).
    /// El SSN no cambia (es la clave de negocio).
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