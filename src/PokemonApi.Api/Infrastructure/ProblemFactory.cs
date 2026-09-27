using Microsoft.AspNetCore.Mvc;
using PokemonApi.Domain.Common;
using PokemonApi.Domain.Enumerations;

namespace PokemonApi.Api.Infrastructure;

/// <summary>
/// Traduce los errores del dominio a respuestas <c>ProblemDetails</c>.
/// </summary>
/// <remarks>
/// <para>
/// Es el unico punto donde se decide que codigo HTTP corresponde a cada fallo de
/// negocio. Concentrarlo aqui evita que cada endpoint invente su propio
/// mapeo y que dos endpoints devuelvan 500 ante el mismo <see cref="Error"/>.
/// </para>
/// <para>
/// Cada respuesta incluye un <c>code</c> estable y legible por maquina
/// (<c>pokemon.not_found</c>) y el <c>traceId</c> de la peticion, para que el
/// cliente pueda distinguir dos fallos distintos que compartan mensaje.
/// </para>
/// </remarks>
public static class ProblemFactory
{
    /// <summary>Prefijo de los tipos de problema, en forma de URN.</summary>
    public const string ProblemTypePrefix = "urn:pokemon-api:error:";

    /// <summary>
    /// Construye el valor de <c>type</c> de un <c>ProblemDetails</c>.
    /// </summary>
    /// <remarks>
    /// RFC 7807 solo pide que <c>type</c> identifique de forma estable el tipo de
    /// problema, y un URN cumple sin obligar a publicar una pagina de
    /// documentacion que este proyecto no tiene. Se deriva del mismo
    /// <c>code</c> que ya viaja en el cuerpo, de modo que <c>type</c> y
    /// <c>code</c> no puedan contradecirse.
    /// </remarks>
    /// <param name="code">Codigo estable del error.</param>
    /// <returns>El URN que identifica ese tipo de problema.</returns>
    public static string TypeFor(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return ProblemTypePrefix + code;
    }

    /// <summary>
    /// Tipo de medio que RFC 7807 reserva para <c>ProblemDetails</c>.
    /// </summary>
    /// <remarks>
    /// No es cosmetico: un cliente que decide como tratar un error mirando el
    /// <c>Content-Type</c> no puede reconocer el problema si la respuesta llega
    /// como <c>application/json</c>.
    /// </remarks>
    public const string ProblemContentType = "application/problem+json";

    /// <summary>Convierte un resultado fallido en una respuesta HTTP.</summary>
    /// <param name="error">Error de dominio.</param>
    /// <param name="httpContext">
    /// Contexto de la peticion, del que se toma el identificador de seguimiento.
    /// Si se omite, la respuesta se emite sin <c>traceId</c>.
    /// </param>
    /// <returns>Un <see cref="IResult"/> con el codigo y el cuerpo adecuados.</returns>
    public static IResult FromError(Error error, HttpContext? httpContext = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        var (status, title, detail) = Describe(error);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = TypeFor(error.Code),

            // Un error inesperado nunca expone su detalle al cliente: podria
            // contener rutas de ficheros o mensajes de infraestructura. El
            // traceId permite localizar el detalle real en el log del servidor.
            Detail = error.Type is ErrorType.Unexpected ? "An unexpected error occurred." : error.Message,
            Instance = null,
        };

        problem.Extensions["code"] = error.Code;
        AddTraceId(problem, httpContext);

        // Results.Problem escribe con application/problem+json; Results.Json
        // usaria application/json y perderia la convencion de RFC 7807. Cuando se
        // le pasa ya un ProblemDetails, Results.Problem no fija ese tipo de medio
        // y la respuesta llega como application/json, asi que el tipo se declara
        // de forma explicita.
        return Results.Json(problem, contentType: ProblemContentType, statusCode: status);
    }

    /// <summary>
    /// Convierte un resultado con valor en una respuesta HTTP, o en el
    /// <c>ProblemDetails</c> que corresponda si fallo.
    /// </summary>
    /// <typeparam name="TValue">Tipo del valor.</typeparam>
    /// <param name="result">Resultado del caso de uso.</param>
    /// <param name="httpContext">Contexto de la peticion, para el identificador de seguimiento.</param>
    /// <returns>La respuesta HTTP.</returns>
    public static IResult FromResult<TValue>(Result<TValue> result, HttpContext? httpContext = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : FromError(result.Error!, httpContext);
    }

    /// <summary>
    /// Escribe la respuesta de limite de peticiones excedido.
    /// </summary>
    /// <param name="httpContext">Contexto de la peticion rechazada.</param>
    /// <param name="retryAfter">Tiempo restante hasta el reinicio de la ventana.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Tarea que representa la escritura de la respuesta.</returns>
    public static Task WriteRateLimitedAsync(
        HttpContext httpContext,
        TimeSpan retryAfter,
        CancellationToken cancellationToken)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests.",
            Type = TypeFor("rate_limit.exceeded"),
            Detail = "The request was rejected because the client exceeded its rate limit.",
        };

        problem.Extensions["code"] = "rate_limit.exceeded";
        problem.Extensions["retryAfterSeconds"] = (int)retryAfter.TotalSeconds;
        AddTraceId(problem, httpContext);

        return httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: ProblemContentType,
            cancellationToken);
    }

    private static void AddTraceId(ProblemDetails problem, HttpContext? httpContext)
    {
        if (httpContext is not null)
        {
            problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        }
    }

    private static (int Status, string Title, string Detail) Describe(Error error) => error.Type switch
    {
        ErrorType.NotFound => (StatusCodes.Status404NotFound, "Resource not found.", error.Message),
        ErrorType.Validation => (StatusCodes.Status400BadRequest, "Invalid request.", error.Message),
        ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict.", error.Message),
        _ => (StatusCodes.Status500InternalServerError, "Unexpected error.", error.Message),
    };
}
