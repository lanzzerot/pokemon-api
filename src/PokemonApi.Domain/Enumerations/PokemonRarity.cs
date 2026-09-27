namespace PokemonApi.Domain.Enumerations;

/// <summary>
/// Rareza de un Pokemon, derivada de su clasificacion en la especie.
/// </summary>
public enum PokemonRarity
{
    /// <summary>Pokemon de aparicion habitual.</summary>
    Common = 1,

    /// <summary>Pokemon legendary: aparece una sola vez por partida y es muy poderoso.</summary>
    Legendary = 2,

    /// <summary>Pokemon mythical: no es jugable de forma normal.</summary>
    Mythical = 3,
}
