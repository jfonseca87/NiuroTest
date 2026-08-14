using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Niuro.Api.Endpoints;
using Niuro.Core.Application.DTOs;
using Niuro.Core.Application.Results;
using Niuro.Core.Application.Responses;
using Niuro.Core.Application.UseCases;
using Niuro.Core.Domain.Entities;
using Niuro.Core.Domain.Rules;

namespace Niuro.Tests.Endpoints;

public class LoanApplicationEndpointsTests
{
    private readonly Mock<IValidator<LoanApplicationRequest>> _validator = new();
    private readonly Mock<IRuleEngine> _ruleEngine = new();
    private readonly Mock<ISubmitLoanApplication> _submitLoanApplication = new();
    private readonly ILoggerFactory _loggerFactory;

    public LoanApplicationEndpointsTests()
    {
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());
        _loggerFactory = loggerFactory.Object;
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

    private static Task<IResult> Invoke(
        Mock<IValidator<LoanApplicationRequest>> validator,
        Mock<IRuleEngine> ruleEngine,
        Mock<ISubmitLoanApplication> submit,
        ILoggerFactory loggerFactory,
        LoanApplicationRequest? request = null)
        => LoanApplicationEndpoints.Submit(
            request ?? ValidRequest(),
            validator.Object,
            ruleEngine.Object,
            submit.Object,
            loggerFactory,
            CancellationToken.None);

    private static int StatusOf(IResult result)
        => result is IStatusCodeHttpResult status ? status.StatusCode!.Value : throw new Exception("No status");

    [Fact]
    public async Task Submit_WhenValidationFails_ReturnsUnprocessableEntityWithCamelCaseKeys()
    {
        SetupInvalidValidation(_validator);

        var result = await Invoke(_validator, _ruleEngine, _submitLoanApplication, _loggerFactory);

        var problem = Assert.IsType<UnprocessableEntity<ValidationProblemDetails>>(result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, StatusOf(result));
        Assert.True(problem.Value!.Errors.ContainsKey("firstName"));
    }

    [Fact]
    public async Task Submit_WhenRuleDenies_ReturnsOkDeniedWithReason()
    {
        SetupValidValidation(_validator);
        _ruleEngine
            .Setup(r => r.EvaluateAsync(It.IsAny<LoanCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("STATE_NY"));

        var result = await Invoke(_validator, _ruleEngine, _submitLoanApplication, _loggerFactory);

        Assert.Equal(StatusCodes.Status200OK, StatusOf(result));
        var ok = Assert.IsType<Ok<LoanDecision>>(result);
        Assert.Equal("denied", ok.Value!.Status);
        Assert.Equal("STATE_NY", ok.Value.Reason);
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

        var result = await Invoke(_validator, _ruleEngine, _submitLoanApplication, _loggerFactory);

        Assert.Equal(StatusCodes.Status200OK, StatusOf(result));
        var ok = Assert.IsType<Ok<LoanDecision>>(result);
        Assert.Equal("approved", ok.Value!.Status);
        Assert.Equal(applicationId.ToString(), ok.Value.ApplicationId);
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

        var result = await Invoke(_validator, _ruleEngine, _submitLoanApplication, _loggerFactory);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.StatusCode);
        Assert.Equal("Internal Server Error", problem.ProblemDetails.Title);
    }
}
