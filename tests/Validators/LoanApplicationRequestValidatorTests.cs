using FluentValidation.TestHelper;
using Niuro.Core.Application.DTOs;
using Niuro.Core.Application.Validators;

namespace Niuro.Tests.Validators;

public class LoanApplicationRequestValidatorTests
{
    private readonly LoanApplicationRequestValidator _validator = new();

    private static LoanApplicationRequest ValidRequest() => new()
    {
        FirstName = "John",
        LastName = "Doe",
        CompanyName = "Acme Corp",
        RequestedAmount = 5000m,
        Ssn = "123-45-6789",
        Address = new AddressDto
        {
            Street = "1 Main St",
            City = "Springfield",
            State = "CA",
            ZipCode = "90210"
        }
    };

    private static LoanApplicationRequest RequestWith(
        string? firstName = null,
        string? lastName = null,
        string? companyName = null,
        decimal? requestedAmount = null,
        string? ssn = null,
        AddressDto? address = null)
    {
        var valid = ValidRequest();
        return new LoanApplicationRequest
        {
            FirstName = firstName ?? valid.FirstName,
            LastName = lastName ?? valid.LastName,
            CompanyName = companyName ?? valid.CompanyName,
            RequestedAmount = requestedAmount ?? valid.RequestedAmount,
            Ssn = ssn ?? valid.Ssn,
            Address = address ?? valid.Address
        };
    }

    private static AddressDto AddressWith(string? state = null, string? zipCode = null)
    {
        var valid = ValidRequest().Address;
        return new AddressDto
        {
            Street = valid.Street,
            City = valid.City,
            State = state ?? valid.State,
            ZipCode = zipCode ?? valid.ZipCode
        };
    }

    [Theory]
    [InlineData(nameof(LoanApplicationRequest.FirstName))]
    [InlineData(nameof(LoanApplicationRequest.LastName))]
    [InlineData(nameof(LoanApplicationRequest.CompanyName))]
    public async Task Validate_WhenRequiredFieldMissing_HasErrorForField(string field)
    {
        var request = field switch
        {
            nameof(LoanApplicationRequest.FirstName) => RequestWith(firstName: ""),
            nameof(LoanApplicationRequest.LastName) => RequestWith(lastName: ""),
            _ => RequestWith(companyName: "")
        };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(field);
    }

    [Fact]
    public async Task Validate_WhenAddressNull_HasError()
    {
        var valid = ValidRequest();
        var request = new LoanApplicationRequest
        {
            FirstName = valid.FirstName,
            LastName = valid.LastName,
            CompanyName = valid.CompanyName,
            RequestedAmount = valid.RequestedAmount,
            Ssn = valid.Ssn,
            Address = null!
        };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Address);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validate_WhenRequestedAmountNotPositive_HasError(decimal amount)
    {
        var request = RequestWith(requestedAmount: amount);

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.RequestedAmount);
    }

    [Fact]
    public async Task Validate_WhenRequestedAmountPositive_IsValid()
    {
        var request = RequestWith(requestedAmount: 1000m);

        var result = await _validator.TestValidateAsync(request);

        result.ShouldNotHaveValidationErrorFor(x => x.RequestedAmount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123-45-678")]
    [InlineData("123456789")]
    [InlineData("123-456-789")]
    [InlineData("abc-de-fghi")]
    public async Task Validate_WhenSsnFormatInvalid_HasError(string ssn)
    {
        var request = RequestWith(ssn: ssn);

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Ssn);
    }

    [Fact]
    public async Task Validate_WhenSsnFormatValid_IsValid()
    {
        var request = RequestWith(ssn: "123-45-6789");

        var result = await _validator.TestValidateAsync(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Ssn);
    }

    [Theory]
    [InlineData("ca")]
    [InlineData("C")]
    [InlineData("CAL")]
    [InlineData("C1")]
    public async Task Validate_WhenStateNotUppercaseTwoLetters_HasError(string state)
    {
        var request = RequestWith(address: AddressWith(state: state));

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Address.State);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("123456")]
    [InlineData("abcde")]
    [InlineData("12345-678")]
    public async Task Validate_WhenZipCodeFormatInvalid_HasError(string zipCode)
    {
        var request = RequestWith(address: AddressWith(zipCode: zipCode));

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Address.ZipCode);
    }

    [Fact]
    public async Task Validate_WhenRequestFullyValid_IsValid()
    {
        var result = await _validator.TestValidateAsync(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }
}
