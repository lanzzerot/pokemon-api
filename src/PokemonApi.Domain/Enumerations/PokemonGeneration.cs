namespace PokemonApi.Domain.Enumerations;

/// <summary>
/// Generaciones de Pokemon, desde la primera hasta la novena.
/// </summary>
/// <remarks>
/// El valor numerico coincide con el identificador de generacion de la fuente
/// de datos (PokeAPI), lo que permite mapear directamente sin tablas de
/// conversion.
/// </remarks>
public enum PokemonGeneration
{
    /// <summary>Generacion I (Kanto, 1996).</summary>
    GenerationI = 1,

    /// <summary>Generacion II (Johto, 1999).</summary>
    GenerationII = 2,

    /// <summary>Generacion III (Hoenn, 2002).</summary>
    GenerationIII = 3,

    /// <summary>Generacion IV (Sinnoh, 2006).</summary>
    GenerationIV = 4,

    /// <summary>Generacion V (Unova, 2010).</summary>
    GenerationV = 5,

    /// <summary>Generacion VI (Kalos, 2013).</summary>
    GenerationVI = 6,

    /// <summary>Generacion VII (Alola, 2016).</summary>
    GenerationVII = 7,

    /// <summary>Generacion VIII (Galar, 2019).</summary>
    GenerationVIII = 8,

    /// <summary>Generacion IX (Paldea, 2022).</summary>
    GenerationIX = 9,
}
