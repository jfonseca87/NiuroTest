using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Niuro.Core.Application.DTOs;
using Niuro.Core.Application.Responses;
using Niuro.Core.Application.UseCases;
using Niuro.Core.Domain.Rules;

namespace Niuro.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for loan applications.
/// Registered via <see cref="MapLoanApplicationEndpoints"/>; startup only invokes the module.
/// </summary>
public static class LoanApplicationEndpoints
{
    /// <summary>
    /// Maps the application endpoints under "api/loan-applications".
    /// </summary>
    public static IEndpointRouteBuilder MapLoanApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/loan-applications");
        group.MapPost("/", Submit);
        return app;
    }

    /// <summary>
    /// Receives an application, evaluates the rule engine and, if approved, persists transactionally.
    /// </summary>
    public static async Task<IResult> Submit(
        LoanApplicationRequest request,
        IValidator<LoanApplicationRequest> validator,
        IRuleEngine ruleEngine,
        ISubmitLoanApplication submitLoanApplication,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("LoanApplicationEndpoints");
        var validationResult = await validator.ValidateAsync(request, ct);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => ToCamelCase(e.PropertyName))
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return Results.UnprocessableEntity(new ValidationProblemDetails(errors)
            {
                Title = "Validation Failed",
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }

        // Evaluate with the rule engine.
        var candidate = LoanCandidate.FromRequest(request);
        var ruleResult = await ruleEngine.EvaluateAsync(candidate, ct);

        if (ruleResult.IsFailure)
        {
            logger.LogInformation(
                "Loan application for {FirstName} {LastName}: denied ({Reason})",
                request.FirstName, request.LastName, ruleResult.Error);

            return Results.Ok(LoanDecision.Denied(ruleResult.Error!));
        }

        // Persist customer + application + outbox (transactional)
        var submitResult = await submitLoanApplication.ExecuteAsync(request, ct);

        if (submitResult.IsFailure)
        {
            logger.LogError("Failed to submit loan application: {Error}", submitResult.Error);
            return Results.Problem(new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "Failed to process the application. Please try again."
            });
        }

        logger.LogInformation(
            "Loan application for {FirstName} {LastName}: approved (ApplicationId={ApplicationId})",
            request.FirstName, request.LastName, submitResult.Value.Id);

        return Results.Ok(LoanDecision.Approved(submitResult.Value.Id.ToString()));
    }

    /// <summary>
    /// Converts a FluentValidation PropertyName (PascalCase, e.g. "Address.State")
    /// to camelCase per segment (e.g. "address.state"), consistent with the frontend fields.
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
