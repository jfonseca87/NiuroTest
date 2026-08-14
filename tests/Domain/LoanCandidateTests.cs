using Niuro.Core.Application.DTOs;
using Niuro.Core.Domain.Rules;

namespace Niuro.Tests.Domain;

public class LoanCandidateTests
{
    [Theory]
    [InlineData("123456789", "123-45-6789")]
    [InlineData("123-45-6789", "123-45-6789")]
    public void NormalizeSsn_WhenNineDigits_ReturnsFormatted(string input, string expected)
    {
        Assert.Equal(expected, LoanCandidate.NormalizeSsn(input));
    }

    [Fact]
    public void NormalizeSsn_WhenLessThanNineDigits_ReturnsOriginal()
    {
        const string input = "123-45";
        Assert.Equal(input, LoanCandidate.NormalizeSsn(input));
    }

    [Fact]
    public void FromRequest_UppercasesState()
    {
        var request = new LoanApplicationRequest
        {
            FirstName = "John",
            LastName = "Doe",
            CompanyName = "Acme",
            RequestedAmount = 5000m,
            Ssn = "123-45-6789",
            Address = new AddressDto
            {
                Street = "1 Main St",
                City = "Springfield",
                State = "ca",
                ZipCode = "90210"
            }
        };

        var candidate = LoanCandidate.FromRequest(request);

        Assert.Equal("CA", candidate.State);
    }

    [Fact]
    public void FromRequest_NormalizesSsnAndKeepsAmount()
    {
        var request = new LoanApplicationRequest
        {
            FirstName = "John",
            LastName = "Doe",
            CompanyName = "Acme",
            RequestedAmount = 12345m,
            Ssn = "123456789",
            Address = new AddressDto
            {
                Street = "1 Main St",
                City = "Springfield",
                State = "NY",
                ZipCode = "10001"
            }
        };

        var candidate = LoanCandidate.FromRequest(request);

        Assert.Equal("123-45-6789", candidate.Ssn);
        Assert.Equal(12345m, candidate.RequestedAmount);
    }
}
