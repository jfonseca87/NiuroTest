using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Niuro.Api.Controllers;
using Niuro.Core.Application.DTOs;
using Niuro.Core.Application.Results;
using Niuro.Core.Application.Responses;
using Niuro.Core.Application.UseCases;
using Niuro.Core.Domain.Entities;
using Niuro.Core.Domain.Rules;

namespace Niuro.Tests.Controllers;

public class LoanApplicationsControllerTests
{
    private readonly Mock<IValidator<LoanApplicationRequest>> _validator = new();
    private readonly Mock<IRuleEngine> _ruleEngine = new();
    private readonly Mock<ISubmitLoanApplication> _submitLoanApplication = new();
    private readonly LoanApplicationsController _controller;

    public LoanApplicationsControllerTests()
    {
        _controller = new LoanApplicationsController(
            _validator.Object,
            _ruleEngine.Object,
            _submitLoanApplication.Object,
            Mock.Of<ILogger<LoanApplicationsController>>());
    }

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

    private static void SetupValidValidation(Mock<IValidator<LoanApplicationRequest>> validator)
    {
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<LoanApplicationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private static void SetupInvalidValidation(Mock<IValidator<LoanApplicationRequest>> validator)
    {
        var failures = new List<ValidationFailure>
        {
            new(nameof(LoanApplicationRequest.FirstName), "First name is required.")
        };
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<LoanApplicationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));
    }

    [Fact]
    public async Task Submit_WhenValidationFails_ReturnsUnprocessableEntity()
    {
        SetupInvalidValidation(_validator);

        var result = await _controller.Submit(ValidRequest());

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, objectResult.StatusCode);
        Assert.IsType<ValidationProblemDetails>(objectResult.Value);
    }

    [Fact]
    public async Task Submit_WhenRuleDenies_ReturnsOkDeniedWithReason()
    {
        SetupValidValidation(_validator);
        _ruleEngine
            .Setup(r => r.EvaluateAsync(It.IsAny<LoanCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("STATE_NY"));

        var result = await _controller.Submit(ValidRequest());

        var okResult = Assert.IsType<OkObjectResult>(result);
        var decision = Assert.IsType<LoanDecision>(okResult.Value);
        Assert.Equal("denied", decision.Status);
        Assert.Equal("STATE_NY", decision.Reason);
        Assert.Null(decision.ApplicationId);
    }

    [Fact]
    public async Task Submit_WhenApprovedAndSubmitSucceeds_ReturnsOkApprovedWithApplicationId()
    {
        SetupValidValidation(_validator);
        _ruleEngine
            .Setup(r => r.EvaluateAsync(It.IsAny<LoanCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var applicationId = Guid.NewGuid();
        _submitLoanApplication
            .Setup(s => s.ExecuteAsync(It.IsAny<LoanApplicationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new LoanApplication { Id = applicationId }));

        var result = await _controller.Submit(ValidRequest());

        var okResult = Assert.IsType<OkObjectResult>(result);
        var decision = Assert.IsType<LoanDecision>(okResult.Value);
        Assert.Equal("approved", decision.Status);
        Assert.Equal(applicationId.ToString(), decision.ApplicationId);
        Assert.Null(decision.Reason);
    }

    [Fact]
    public async Task Submit_WhenApprovedAndSubmitFails_ReturnsInternalServerError()
    {
        SetupValidValidation(_validator);
        _ruleEngine
            .Setup(r => r.EvaluateAsync(It.IsAny<LoanCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _submitLoanApplication
            .Setup(s => s.ExecuteAsync(It.IsAny<LoanApplicationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LoanApplication>("boom"));

        var result = await _controller.Submit(ValidRequest());

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.IsType<ProblemDetails>(objectResult.Value);
    }
}
