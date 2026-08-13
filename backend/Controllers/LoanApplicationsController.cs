using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Niuro.Core.Application.DTOs;
using Niuro.Core.Application.Responses;
using Niuro.Core.Application.Results;

namespace Niuro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoanApplicationsController : ControllerBase
{
    private readonly IValidator<LoanApplicationRequest> _validator;
    private readonly ILogger<LoanApplicationsController> _logger;

    public LoanApplicationsController(
        IValidator<LoanApplicationRequest> validator,
        ILogger<LoanApplicationsController> logger)
    {
        _validator = validator;
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

        // UC-08 (rule engine) se integra aquí.
        // Por ahora retornamos approved; el rule engine implementará las reglas de denegación.
        _logger.LogInformation("Loan application received for {FirstName} {LastName}", request.FirstName, request.LastName);

        return Ok(LoanDecision.Approved());
    }
}
