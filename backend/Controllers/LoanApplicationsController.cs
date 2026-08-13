using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Niuro.Core.Application.DTOs;
using Niuro.Core.Application.Responses;
using Niuro.Core.Application.Results;
using Niuro.Core.Domain.Rules;

namespace Niuro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoanApplicationsController : ControllerBase
{
    private readonly IValidator<LoanApplicationRequest> _validator;
    private readonly RuleEngine _ruleEngine;
    private readonly ILogger<LoanApplicationsController> _logger;

    public LoanApplicationsController(
        IValidator<LoanApplicationRequest> validator,
        RuleEngine ruleEngine,
        ILogger<LoanApplicationsController> logger)
    {
        _validator = validator;
        _ruleEngine = ruleEngine;
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
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return ValidationProblem(new ValidationProblemDetails(errors)
            {
                Title = "Validation Failed",
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }

        // UC-08: evaluar con el rule engine.
        var candidate = LoanCandidate.FromRequest(request);
        var result = await _ruleEngine.EvaluateAsync(candidate);

        _logger.LogInformation(
            "Loan application for {FirstName} {LastName}: {Status} ({Reason})",
            request.FirstName, request.LastName,
            result.IsSuccess ? "approved" : "denied",
            result.Error ?? "none");

        if (result.IsSuccess)
        {
            return Ok(LoanDecision.Approved());
        }

        return Ok(LoanDecision.Denied(result.Error!));
    }
}
