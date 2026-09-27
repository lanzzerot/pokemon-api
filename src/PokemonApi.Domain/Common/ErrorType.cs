namespace PokemonApi.Domain.Common;

/// <summary>
/// Clasifica un <see cref="Error"/> para que la capa HTTP pueda mapearlo a la
/// respuesta correspondiente sin conocer los casos de uso concretos.
/// </summary>
public enum ErrorType
{
    /// <summary>La peticion es sintacticamente valida pero semanticamente incorrecta.</summary>
    Validation,

    /// <summary>El recurso solicitado no existe.</summary>
    NotFound,

    /// <summary>El recurso existe pero el estado actual impide completar la operacion.</summary>
    Conflict,

    /// <summary>Fallo no previsto. Se registra y se oculta al cliente por seguridad.</summary>
    Unexpected,
}
