namespace PokemonApi.Domain.Enumerations;

/// <summary>
/// Las seis caracteristicas base de un Pokemon.
/// </summary>
/// <remarks>
/// El orden de los miembros coincide con el habitual en la interfaz de los
/// juegos y con el de los <c>stats</c> de la API, de modo que el valor
/// numerico puede usarse directamente para indexar el diccionario de
/// estadisticas.
/// </remarks>
public enum PokemonStat
{
    /// <summary>Puntos de vida.</summary>
    Hp = 1,

    /// <summary>Ataque fisico.</summary>
    Attack = 2,

    /// <summary>Defensa fisica.</summary>
    Defense = 3,

    /// <summary>Ataque especial.</summary>
    SpecialAttack = 4,

    /// <summary>Defensa especial.</summary>
    SpecialDefense = 5,

    /// <summary>Velocidad.</summary>
    Speed = 6,
}
