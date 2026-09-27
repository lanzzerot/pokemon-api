namespace PokemonApi.Domain.Common;

/// <summary>
/// Descripcion de un fallo de negocio, independiente del protocolo de transporte.
/// </summary>
/// <remarks>
/// Los <see cref="Error"/> son parte del dominio: un caso de uso devuelve un
/// error de dominio y es la capa de presentacion quien lo traduce a un
/// <c>ProblemDetails</c> con el codigo HTTP adecuado. Gracias a esto el dominio
/// no depende de HTTP y los casos de uso son faciles de probar.
/// </remarks>
/// <param name="Code">Codigo estable y legible por maquina (p. ej. <c>pokemon.not_found</c>).</param>
/// <param name="Message">Descripcion legible por una persona, en ingles.</param>
/// <param name="Type">Clasificacion del fallo.</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Crea un error de recurso inexistente.</summary>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>Crea un error de peticion invalida.</summary>
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>Crea un error de conflicto de estado.</summary>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>Crea un error no previsto, cuyo detalle no debe exponerse al cliente.</summary>
    public static Error Unexpected(string code, string message) => new(code, message, ErrorType.Unexpected);
}
