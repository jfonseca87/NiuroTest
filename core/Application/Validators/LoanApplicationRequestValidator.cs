using FluentValidation;
using Niuro.Core.Application.DTOs;
using System.Text.RegularExpressions;

namespace Niuro.Core.Application.Validators;

public partial class LoanApplicationRequestValidator : AbstractValidator<LoanApplicationRequest>
{
    public LoanApplicationRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.");

        RuleFor(x => x.Address)
            .NotNull().WithMessage("Address is required.")
            .SetValidator(new AddressDtoValidator());

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.");

        RuleFor(x => x.RequestedAmount)
            .GreaterThan(0).WithMessage("Requested amount must be greater than zero.");

        RuleFor(x => x.Ssn)
            .NotEmpty().WithMessage("SSN is required.")
            .Matches(SsnRegex()).WithMessage("SSN must be in format ###-##-####.");
    }

    [GeneratedRegex(@"^\d{3}-\d{2}-\d{4}$")]
    private static partial Regex SsnRegex();
}

public class AddressDtoValidator : AbstractValidator<AddressDto>
{
    public AddressDtoValidator()
    {
        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street is required.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required.")
            .Length(2).WithMessage("State must be a 2-letter code.")
            .Matches(@"^[A-Z]{2}$").WithMessage("State must be uppercase (e.g., NY, CA).");

        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("Zip code is required.")
            .Matches(@"^\d{5}(-\d{4})?$").WithMessage("Zip code must be 5 digits or 5+4 format.");
    }
}
