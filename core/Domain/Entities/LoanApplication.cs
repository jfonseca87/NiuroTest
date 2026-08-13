namespace Niuro.Core.Domain.Entities;

/// <summary>
/// Solicitud de préstamo del cliente. En este negocio lo importante no es el monto en sí,
/// sino la petición de crédito que se envía al servicio externo con los datos del cliente.
/// </summary>
public class LoanApplication
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public decimal RequestedAmount { get; set; }
}