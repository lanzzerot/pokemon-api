using Microsoft.Extensions.Logging;

namespace PokemonApi.Infrastructure.Logging;

/// <summary>
/// Mensajes de registro de la capa de infraestructura.
/// </summary>
/// <remarks>
/// Se declaran con <see cref="LoggerMessageAttribute"/> en lugar de llamar a
/// <c>LogInformation</c> directamente porque el generador de origen compila cada
/// mensaje a un delegado estatico: la plantilla y el formateo se resuelven una
/// unica vez al compilar, no en cada registro, y los parametros quedan
/// fuertemente tipados en lugar de ser <c>object?</c>.
/// </remarks>
internal static partial class InfrastructureLog
{
    /// <summary>Un caso de uso ha recibido una peticion.</summary>
    /// <param name="logger">Registrador.</param>
    /// <param name="requestName">Nombre del caso de uso.</param>
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Handling {RequestName}")]
    public static partial void RequestStarted(ILogger logger, string requestName);

    /// <summary>Un caso de uso ha terminado con exito.</summary>
    /// <param name="logger">Registrador.</param>
    /// <param name="requestName">Nombre del caso de uso.</param>
    /// <param name="elapsedMilliseconds">Duracion en milisegundos.</param>
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Debug,
        Message = "Handled {RequestName} in {ElapsedMilliseconds} ms")]
    public static partial void RequestHandled(
        ILogger logger,
        string requestName,
        double elapsedMilliseconds);

    /// <summary>
    /// Un caso de uso ha terminado devolviendo un error de dominio.
    /// </summary>
    /// <remarks>
    /// Se registra a nivel <see cref="LogLevel.Information"/> y no
    /// <see cref="LogLevel.Warning"/> porque un 404 o un filtro sin coincidencias
    /// son trafico normal de una API de consulta, no un incidente. Solo las
    /// excepciones suben a <see cref="LogLevel.Error"/>.
    /// </remarks>
    /// <param name="logger">Registrador.</param>
    /// <param name="requestName">Nombre del caso de uso.</param>
    /// <param name="elapsedMilliseconds">Duracion en milisegundos.</param>
    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Handled {RequestName} with a domain error in {ElapsedMilliseconds} ms")]
    public static partial void RequestCompletedWithDomainError(
        ILogger logger,
        string requestName,
        double elapsedMilliseconds);

    /// <summary>Un caso de uso ha fallado con una excepcion.</summary>
    /// <param name="logger">Registrador.</param>
    /// <param name="exception">Excepcion capturada.</param>
    /// <param name="requestName">Nombre del caso de uso.</param>
    /// <param name="elapsedMilliseconds">Duracion en milisegundos.</param>
    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "Request {RequestName} failed after {ElapsedMilliseconds} ms")]
    public static partial void RequestFailed(
        ILogger logger,
        Exception exception,
        string requestName,
        double elapsedMilliseconds);

    /// <summary>El dataset incrustado se ha materializado.</summary>
    /// <param name="logger">Registrador.</param>
    /// <param name="pokemonCount">Numero de Pokemon cargados.</param>
    /// <param name="generationCount">Numero de generaciones cargadas.</param>
    /// <param name="schemaVersion">Version del formato del dataset.</param>
    /// <param name="source">Fuente de los datos.</param>
    /// <param name="sourceVersion">Version de la fuente.</param>
    /// <param name="generatedAtUtc">Momento de generacion del recurso.</param>
    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Loaded {PokemonCount} Pokemon across {GenerationCount} generations from schema " +
                  "{SchemaVersion} (source {Source} {SourceVersion}, generated at {GeneratedAtUtc})")]
    public static partial void DataSetLoaded(
        ILogger logger,
        int pokemonCount,
        int generationCount,
        string schemaVersion,
        string source,
        string sourceVersion,
        DateTimeOffset generatedAtUtc);
}
