using Niuro.Core.Application.DTOs;
using Niuro.Core.Domain.Entities;

namespace Niuro.Tests.Domain;

public class CustomerTests
{
    [Fact]
    public void UpdateFromRequest_UpdatesFieldsAndUppercasesState()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Ssn = "123-45-6789",
            FirstName = "Old",
            LastName = "Name",
            CompanyName = "Old Co",
            Address = new Address("1 Old St", "Oldville", "CA", "90210")
        };

        var request = new LoanApplicationRequest
        {
            FirstName = "New",
            LastName = "Name",
            CompanyName = "New Co",
            RequestedAmount = 1000m,
            Ssn = "123-45-6789",
            Address = new AddressDto
            {
                Street = "2 New St",
                City = "Newville",
                State = "ny",
                ZipCode = "10001"
            }
        };

        customer.UpdateFromRequest(request);

        Assert.Equal("New", customer.FirstName);
        Assert.Equal("New Co", customer.CompanyName);
        Assert.Equal("2 New St", customer.Address.Street);
        Assert.Equal("Newville", customer.Address.City);
        Assert.Equal("NY", customer.Address.State);
        Assert.Equal("10001", customer.Address.ZipCode);
    }

    [Fact]
    public void UpdateFromRequest_DoesNotChangeSsn()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Ssn = "123-45-6789",
            FirstName = "Old",
            LastName = "Name",
            CompanyName = "Old Co",
            Address = new Address("1 Old St", "Oldville", "CA", "90210")
        };

        var request = new LoanApplicationRequest
        {
            FirstName = "New",
            LastName = "Name",
            CompanyName = "New Co",
            RequestedAmount = 1000m,
            Ssn = "999-99-9999", // a different SSN must not alter the business key
            Address = new AddressDto
            {
                Street = "2 New St",
                City = "Newville",
                State = "NY",
                ZipCode = "10001"
            }
        };

        customer.UpdateFromRequest(request);

        Assert.Equal("123-45-6789", customer.Ssn);
    }
}
