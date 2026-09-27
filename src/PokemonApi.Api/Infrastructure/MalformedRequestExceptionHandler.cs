using System.Collections.Frozen;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Diagnostics;

namespace PokemonApi.Api.Infrastructure;

/// <summary>
/// Convierte un fallo de enlace de parametros en un <c>ProblemDetails</c> 400.
/// </summary>
/// <remarks>
/// Los endpoints declaran sus parametros con tipos reales (<c>int</c>,
/// <c>PokemonSortField</c>...) para que OpenAPI los documente y el contrato quede
/// tipado. Cuando el cliente envia un valor que no se puede enlazar, el framework
/// lanza <see cref="BadHttpRequestException"/> antes de entrar al endpoint; sin
/// este manejador esa excepcion caeria en el manejador generico y un simple error
/// de tecleo se responderia con un 500.
/// <para>
/// El mensaje del framework tiene la forma
/// <c>Failed to bind parameter 'System.Int32 page' from "abc".</c>; de ahi se extrae el
/// nombre del parametro para devolver el detalle en el mismo formato que el resto
/// de errores de validacion. El nombre del tipo precede al del parametro y las
/// comillas con las que el framework delimita cada parte han cambiado entre
/// versiones, asi que la expresion regular los cubre sin fija; si el formato
/// cambiara, el manejador degrada a un mensaje generico en lugar de fallar.
/// </para>
/// </remarks>
public sealed partial class MalformedRequestExceptionHandler : IExceptionHandler
{
    private static readonly FrozenDictionary<string, string> ParameterMessages = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["page"] = "The page number must be a positive integer.",
        ["pageSize"] = "The page size must be a positive integer.",
        ["sortBy"] = "The sort field is not recognised.",
        ["sortDirection"] = "The sort direction must be 'asc' or 'desc'.",
        ["minTotalStats"] = "The minimum total stats must be an integer.",
        ["maxTotalStats"] = "The maximum total stats must be an integer.",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException malformed)
        {
            return false;
        }

        var match = BindingFailurePattern().Match(malformed.Message);
        var field = match.Success ? match.Groups["name"].Value : "request";
        var value = match.Success ? match.Groups["value"].Value : null;

        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = malformed.StatusCode,
            Title = "Invalid request.",
            Type = ProblemFactory.TypeFor("request.malformed_parameter"),
            Detail = ParameterMessages.TryGetValue(field, out var message)
                ? message
                : "One or more query string parameters could not be parsed.",
        };

        problem.Extensions["code"] = "request.malformed_parameter";
        problem.Extensions["errors"] = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = [value is null
                ? "The value is missing or cannot be parsed."
                : $"The value '{value}' is not valid for this parameter."],
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        // El middleware deja la respuesta en 500; hay que fijar el estado real
        // del problema o el cliente recibiria 500 con cuerpo 400.
        httpContext.Response.StatusCode = malformed.StatusCode;

        await httpContext.Response
            .WriteAsJsonAsync(
                problem,
                options: null,
                contentType: ProblemFactory.ProblemContentType,
                cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    // El framework escribe "Failed to bind parameter 'System.Int32 page' from \"abc\"."
    // o la variante con comillas dobles, y puede anteponer o no el nombre del
    // tipo. El grupo 'name' captura siempre el ultimo token sin espacios, que es
    // el nombre del parametro en cualquiera de los dos formatos.
    [GeneratedRegex("Failed to bind parameter ['\"](?:[^'\"]*?\\s)?(?<name>[^'\"\\s]+)['\"] from \"(?<value>[^\"]*)\"")]
    private static partial Regex BindingFailurePattern();
}
