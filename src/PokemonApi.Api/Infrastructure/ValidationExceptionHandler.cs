using Microsoft.AspNetCore.Diagnostics;
using PokemonApi.Application.Common.Validation;

namespace PokemonApi.Api.Infrastructure;

/// <summary>
/// Convierte un <see cref="ValidationException"/> en un <c>ProblemDetails</c>
/// con codigo 400 y el detalle de cada campo.
/// </summary>
/// <remarks>
/// La validacion de la peticion se ejecuta en la cadena de comportamientos, fuera
/// del endpoint, asi que su excepcion llega aqui. Devolver 400 en lugar de 500
/// es lo que permite a un cliente de API distinguir "te he misunderstood" de
/// "algo se ha roto por mi culpa".
/// </remarks>
public sealed class ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger)
    : IExceptionHandler
{
    private readonly ILogger<ValidationExceptionHandler> _logger = logger;

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validation)
        {
            return false;
        }

        ApiLog.RequestValidationFailed(
            _logger,
            validation.Failures.Count,
            string.Join(
                "; ",
                validation.Failures.Select(entry => $"{entry.Key}: {string.Join(", ", entry.Value)}")));

        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid request.",
            Type = ProblemFactory.TypeFor("request.validation_failed"),
            Detail = "One or more validation errors occurred.",
        };

        problem.Extensions["code"] = "request.validation_failed";
        problem.Extensions["errors"] = validation.Failures;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        // El middleware deja la respuesta en 500; sin fijarla aqui, un 400
        // bien construido llegaria al cliente con status 500.
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response
            .WriteAsJsonAsync(
                problem,
                options: null,
                contentType: ProblemFactory.ProblemContentType,
                cancellationToken)
            .ConfigureAwait(false);

        return true;
    }
}
