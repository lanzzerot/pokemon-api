using Microsoft.Extensions.Logging;

namespace PokemonApi.Api.Infrastructure;

/// <summary>
/// Mensajes de registro de la capa de presentacion.
/// </summary>
/// <remarks>
/// Se declaran con <see cref="LoggerMessageAttribute"/> porque el generador de
/// origen los compila a delegados estaticos: la plantilla se resuelve al
/// compilar y los parametros conservan su tipo.
/// </remarks>
internal static partial class ApiLog
{
    /// <summary>Una peticion se ha rechazado por no cumplir la validacion.</summary>
    /// <param name="logger">Registrador.</param>
    /// <param name="failureCount">Numero de campos con fallos.</param>
    /// <param name="failures">Detalle de los fallos, indexado por campo.</param>
    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Information,
        Message = "Rejected a request with {FailureCount} validation failures: {Failures}")]
    public static partial void RequestValidationFailed(
        ILogger logger,
        int failureCount,
        string failures);

    /// <summary>Se ha producido una excepcion no controlada.</summary>
    /// <param name="logger">Registrador.</param>
    /// <param name="exception">Excepcion capturada.</param>
    /// <param name="method">Metodo HTTP de la peticion.</param>
    /// <param name="path">Ruta de la peticion.</param>
    /// <param name="elapsedMilliseconds">Duracion en milisegundos.</param>
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Error,
        Message = "Unhandled exception on {Method} {Path} after {ElapsedMilliseconds} ms")]
    public static partial void UnhandledException(
        ILogger logger,
        Exception exception,
        string method,
        string path,
        double elapsedMilliseconds);
}
