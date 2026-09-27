using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using PokemonApi.Application.Common.Validation;

namespace PokemonApi.Api.Infrastructure;

/// <summary>
/// Ultimo recurso ante una excepcion no controlada.
/// </summary>
/// <remarks>
/// Se registra como manejador para que la respuesta sea siempre un
/// <c>ProblemDetails</c> con la misma forma que el resto de errores, en lugar
/// del volcado de pila o la pagina en blanco que produce el host por defecto.
/// El detalle se registra en el log y no se devuelve: la respuesta solo lleva el
/// <c>traceId</c> que permite encontrarlo.
/// </remarks>
/// <param name="logger">Registrador de la aplicacion.</param>
public sealed class UnexpectedExceptionHandler(ILogger<UnexpectedExceptionHandler> logger)
    : IExceptionHandler
{
    private readonly ILogger<UnexpectedExceptionHandler> _logger = logger;

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Se deja pasar todo lo que ya tiene manejador propio. El framework
        // evalua los manejadores en orden inverso al de registro, asi que esta
        // comprobacion es una red de seguridad: si alguien vuelve a registrar
        // este manejador el ultimo, no puede degradar un 400 a un 500.
        if (exception is ValidationException or BadHttpRequestException)
        {
            return false;
        }

        var timestamp = Stopwatch.GetTimestamp();

        ApiLog.UnhandledException(
            _logger,
            exception,
            httpContext.Request.Method,
            httpContext.Request.Path,
            Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);

        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Unexpected error.",
            Type = ProblemFactory.TypeFor("server.unexpected_error"),
            Detail = "An unexpected error occurred while processing the request.",
        };

        problem.Extensions["code"] = "server.unexpected_error";
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        // ExceptionHandlerMiddleware deja la respuesta en 500 antes de invocar los
        // manejadores, y WriteAsJsonAsync no toca la linea de estado: hay que
        // fijarla explicitamente o el cuerpo 500 llega con status 500.
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

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
