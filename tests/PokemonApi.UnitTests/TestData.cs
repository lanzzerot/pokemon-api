using PokemonApi.Domain.Entities;
using PokemonApi.Domain.Enumerations;
using PokemonApi.Domain.ValueObjects;

namespace PokemonApi.UnitTests;

/// <summary>
/// Datos compartidos por las pruebas. Centralizarlos evita duplicar un
/// constructor de 29 parametros en cada prueba y deja el foco en el
/// comportamiento que se quiere comprobar.
/// </summary>
internal static class TestData
{
    /// <summary>Generaciones presentes en el catalogo.</summary>
    public static IReadOnlyList<Generation> Generations { get; } =
    [
        new(1, SlugOf("generation-i"), "Generation I", SlugOf("kanto"), "Kanto", 151),
        new(2, SlugOf("generation-ii"), "Generation II", SlugOf("johto"), "Johto", 100),
        new(3, SlugOf("generation-iii"), "Generation III", SlugOf("hoenn"), "Hoenn", 135),
    ];

    /// <summary>Tipos presentes en el catalogo.</summary>
    public static IReadOnlyList<PokemonTypeInfo> Types { get; } =
    [
        new(1, SlugOf("normal"), "Normal", 400),
        new(4, SlugOf("fire"), "Fire", 81),
        new(6, SlugOf("water"), "Water", 153),
        new(12, SlugOf("ghost"), "Ghost", 106),
    ];

    /// <summary>Habilidades presentes en el catalogo.</summary>
    public static IReadOnlyList<AbilityInfo> Abilities { get; } =
    [
        new(9, SlugOf("static"), "Static", true, "Prevents paralysis.", 43),
        new(66, SlugOf("blaze"), "Blaze", true, "Powers up fire moves.", 104),
    ];

    /// <summary>Grupos de huevo presentes en el catalogo.</summary>
    public static IReadOnlyList<EggGroupInfo> EggGroups { get; } =
    [
        new(1, SlugOf("monster"), "Monster"),
        new(2, SlugOf("water-1"), "Water 1"),
        new(15, SlugOf("no-eggs"), "No Eggs"),
    ];

    /// <summary>Habitats presentes en el catalogo.</summary>
    public static IReadOnlyList<CatalogEntryInfo> Habitats { get; } =
    [
        new(1, SlugOf("cave"), "Cave", 40),
        new(2, SlugOf("forest"), "Forest", 71),
    ];

    /// <summary>Regiones presentes en el catalogo.</summary>
    public static IReadOnlyList<CatalogEntryInfo> Regions { get; } =
    [
        new(1, SlugOf("kanto"), "Kanto", 151),
        new(3, SlugOf("hoenn"), "Hoenn", 135),
    ];

    /// <summary>Crea un Pokemon valido con los valores por defecto.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="name">Slug del nombre.</param>
    /// <param name="generation">Generacion de pertenencia.</param>
    /// <param name="evolvesFrom">Slug del Pokemon del que evoluciona.</param>
    /// <param name="evolvesTo">Evoluciones posibles.</param>
    /// <param name="chainId">Identificador de la cadena evolutiva.</param>
    /// <returns>El Pokemon construido.</returns>
    public static Pokemon Pokemon(
        int id = 25,
        string name = "pikachu",
        PokemonGeneration generation = PokemonGeneration.GenerationI,
        Slug? evolvesFrom = null,
        IReadOnlyList<Evolution>? evolvesTo = null,
        int chainId = 10) => new(
        id,
        SlugOf(name),
        ToDisplayName(name),
        "Mouse Pokemon",
        "Stores electricity in its cheek sacs.",
        generation,
        generation == PokemonGeneration.GenerationI ? SlugOf("kanto") : SlugOf("hoenn"),
        [SlugOf("electric")],
        Measure.FromDecimetres(4),
        Measure.FromHectograms(60),
        112,
        PokemonStats.Create(35, 55, 40, 50, 50, 90),
        [new PokemonAbility(SlugOf("static"), "Static", false, 1)],
        false,
        false,
        false,
        190,
        50,
        4,
        false,
        10,
        SlugOf("medium"),
        [SlugOf("fairy"), SlugOf("ground")],
        SlugOf("yellow"),
        SlugOf("forest"),
        SlugOf("quadruped"),
        evolvesFrom,
        chainId,
        evolvesTo ?? [],
        new PokemonSprites(
            new Uri($"https://example.test/artwork/{id}.png"),
            new Uri($"https://example.test/artwork-shiny/{id}.png"),
            new Uri($"https://example.test/home/{id}.png"),
            new Uri($"https://example.test/front/{id}.png"),
            new Uri($"https://example.test/front-shiny/{id}.png"),
            new Uri($"https://example.test/pixel/{id}.png"),
            new Uri($"https://example.test/sheet/{id}.png")));

    /// <summary>Crea un slug, fallando la prueba si el valor no es valido.</summary>
    /// <param name="value">Valor a normalizar.</param>
    /// <returns>El slug creado.</returns>
    public static Slug SlugOf(string value) => Slug.Create(value).Value;

    private static string ToDisplayName(string slug) => slug
        .Split('-', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => char.ToUpperInvariant(part[0]) + part[1..])
        .Aggregate((left, right) => $"{left} {right}");
}
