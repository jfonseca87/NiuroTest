using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Niuro.Core.Application.DTOs;
using Niuro.Core.Application.Responses;
using Niuro.Core.Application.UseCases;
using Niuro.Core.Domain.Rules;

namespace Niuro.Api.Endpoints;

/// <summary>
/// Endpoints de minimal API para las solicitudes de préstamo.
/// Se registran vía <see cref="MapLoanApplicationEndpoints"/>; el startup solo invoca el módulo.
/// </summary>
public static class LoanApplicationEndpoints
{
    /// <summary>
    /// Mapea los endpoints de solicitudes bajo "api/loan-applications".
    /// </summary>
    public static IEndpointRouteBuilder MapLoanApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/loan-applications");
        group.MapPost("/", Submit);
        return app;
    }

    /// <summary>
    /// Recibe una solicitud, evalúa el rule engine y, si aprueba, persiste de forma transaccional.
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

        // UC-08: evaluar con el rule engine.
        var candidate = LoanCandidate.FromRequest(request);
        var ruleResult = await ruleEngine.EvaluateAsync(candidate, ct);

        if (ruleResult.IsFailure)
        {
            logger.LogInformation(
                "Loan application for {FirstName} {LastName}: denied ({Reason})",
                request.FirstName, request.LastName, ruleResult.Error);

            return Results.Ok(LoanDecision.Denied(ruleResult.Error!));
        }

        // UC-11/12: persistir customer + application + outbox (transaccional)
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
