using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Niuro.Core.Application.DTOs;
using Niuro.Core.Application.Responses;
using Niuro.Core.Application.Results;
using Niuro.Core.Application.UseCases;
using Niuro.Core.Domain.Rules;

namespace Niuro.Api.Controllers;

[ApiController]
[Route("api/loan-applications")]
public class LoanApplicationsController : ControllerBase
{
    private readonly IValidator<LoanApplicationRequest> _validator;
    private readonly IRuleEngine _ruleEngine;
    private readonly ISubmitLoanApplication _submitLoanApplication;
    private readonly ILogger<LoanApplicationsController> _logger;

    public LoanApplicationsController(
        IValidator<LoanApplicationRequest> validator,
        IRuleEngine ruleEngine,
        ISubmitLoanApplication submitLoanApplication,
        ILogger<LoanApplicationsController> logger)
    {
        _validator = validator;
        _ruleEngine = ruleEngine;
        _submitLoanApplication = submitLoanApplication;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(LoanDecision), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Submit([FromBody] LoanApplicationRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => ToCamelCase(e.PropertyName))
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return UnprocessableEntity(new ValidationProblemDetails(errors)
            {
                Title = "Validation Failed",
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }

        // UC-08: evaluar con el rule engine.
        var candidate = LoanCandidate.FromRequest(request);
        var ruleResult = await _ruleEngine.EvaluateAsync(candidate);

        if (ruleResult.IsFailure)
        {
            _logger.LogInformation(
                "Loan application for {FirstName} {LastName}: denied ({Reason})",
                request.FirstName, request.LastName, ruleResult.Error);

            return Ok(LoanDecision.Denied(ruleResult.Error!));
        }

        // UC-11/12: persistir customer + application + outbox (transaccional)
        var submitResult = await _submitLoanApplication.ExecuteAsync(request);

        if (submitResult.IsFailure)
        {
            _logger.LogError("Failed to submit loan application: {Error}", submitResult.Error);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = 500,
                Detail = "Failed to process the application. Please try again."
            });
        }

        _logger.LogInformation(
            "Loan application for {FirstName} {LastName}: approved (ApplicationId={ApplicationId})",
            request.FirstName, request.LastName, submitResult.Value.Id);

        return Ok(LoanDecision.Approved(submitResult.Value.Id.ToString()));
    }

    /// <summary>
    /// Convierte un PropertyName de FluentValidation (PascalCase, ej. "Address.State")
    /// a camelCase por segmento (ej. "address.state"), consistente con los campos del frontend.
    /// </summary>
    private static string ToCamelCase(string propertyName)
    {
        var segments = propertyName.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (segment.Length > 0)
            {
                segments[i] = char.ToLowerInvariant(segment[0]) + segment[1..];
            }
        }
        return string.Join('.', segments);
    }
}
