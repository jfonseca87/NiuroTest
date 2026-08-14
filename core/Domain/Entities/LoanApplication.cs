namespace Niuro.Core.Domain.Entities;

/// <summary>
/// Customer's loan application. In this business, what matters is not the amount itself,
/// but the credit request sent to the external service with the customer data.
/// </summary>
public class LoanApplication
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public decimal RequestedAmount { get; set; }
}