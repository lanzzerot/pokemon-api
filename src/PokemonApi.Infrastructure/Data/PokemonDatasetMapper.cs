using PokemonApi.Domain.Entities;
using PokemonApi.Domain.Enumerations;
using PokemonApi.Domain.ValueObjects;

namespace PokemonApi.Infrastructure.Data;

/// <summary>
/// Convierte el contenido de <c>Data/pokemon.json</c> en entidades de dominio.
/// </summary>
/// <remarks>
/// <para>
/// La conversion se ejecuta una unica vez, durante el arranque, y cualquier
/// dato incoherente aborta el arranque con un
/// <see cref="InvalidDataException"/> que identifica la especie y el problema.
/// Es deliberado: un dataset corrupto debe detectarse al desplegar, no servir
/// respuestas silenciosamente incompletas durante meses.
/// </para>
/// <para>
/// El coste de la validacion es irrelevante aqui (ocurre una vez sobre mil
/// entidades) y a cambio el resto de la aplicacion puede asumir que el dominio
/// es valido.
/// </para>
/// </remarks>
internal sealed class PokemonDatasetMapper
{
    /// <summary>Version del formato de dataset que este mapper entiende.</summary>
    public const string SupportedSchemaVersion = "1.0.0";

    private readonly IReadOnlyDictionary<string, string> _abilityNames;
    private readonly IReadOnlyDictionary<string, int> _generationIds;

    /// <summary>Inicializa el mapper con los catalogos que necesita resolver.</summary>
    /// <param name="file">Dataset recien deserializado.</param>
    /// <exception cref="InvalidDataException">Si falta un catalogo imprescindible.</exception>
    public PokemonDatasetMapper(PokemonDatasetFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        EnsureSchemaVersion(file);

        _abilityNames = file.Abilities.ToDictionary(
            ability => ability.Slug,
            ability => ability.Name,
            StringComparer.Ordinal);

        _generationIds = file.Generations.ToDictionary(
            generation => generation.Slug,
            generation => generation.Id,
            StringComparer.Ordinal);
    }

    /// <summary>Convierte la lista de generaciones del dataset.</summary>
    /// <param name="entries">Generaciones del dataset.</param>
    /// <returns>Entidades <see cref="Generation"/> ordenadas por identificador.</returns>
    public static IReadOnlyList<Generation> MapGenerations(IReadOnlyList<DatasetGeneration> entries) =>
        [.. entries
            .OrderBy(entry => entry.Id)
            .Select(entry => new Generation(
                entry.Id,
                RequiredSlug(entry.Slug, $"generation {entry.Id}"),
                RequiredText(entry.Name, $"generation {entry.Id}"),
                RequiredSlug(entry.Region, $"generation {entry.Id}"),
                RequiredText(entry.RegionName, $"generation {entry.Id}"),
                entry.PokemonCount))];

    /// <summary>Convierte la ficha de un Pokemon.</summary>
    /// <param name="entry">Pokemon del dataset.</param>
    /// <returns>La entidad de dominio.</returns>
    public Pokemon MapPokemon(DatasetPokemon entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var label = $"pokemon {entry.Id} ('{entry.Name}')";

        var generation = MapGeneration(entry.GenerationId, entry.Generation, label);
        var stats = MapStats(entry.Stats, label);

        if (stats.Total != entry.TotalStats)
        {
            throw new InvalidDataException(
                $"Declared total of {label} is {entry.TotalStats}, but the six base stats add up to {stats.Total}.");
        }

        return new Pokemon(
            entry.Id,
            RequiredSlug(entry.Name, label),
            RequiredText(entry.DisplayName, label),
            entry.Genus,
            entry.Description,
            generation,
            RequiredSlug(entry.Region, label),
            [.. entry.Types.Select(type => RequiredSlug(type, $"{label} type"))],
            Measure.FromDecimetres(entry.Height),
            Measure.FromHectograms(entry.Weight),
            entry.BaseExperience,
            stats,
            [.. entry.Abilities.Select(ability => MapAbility(ability, label))],
            entry.IsLegendary,
            entry.IsMythical,
            entry.IsBaby,
            entry.CaptureRate,
            entry.BaseHappiness,
            entry.GenderRate,
            entry.HasGenderDifferences,
            entry.HatchCounter,
            RequiredSlug(entry.GrowthRate, $"{label} growth rate"),
            [.. entry.EggGroups.Select(group => RequiredSlug(group, $"{label} egg group"))],
            RequiredSlug(entry.Color, $"{label} colour"),
            OptionalSlug(entry.Habitat, $"{label} habitat"),
            RequiredSlug(entry.Shape, $"{label} shape"),
            OptionalSlug(entry.EvolvesFrom, $"{label} evolvesFrom"),
            entry.EvolutionChainId,
            [.. entry.EvolvesTo.Select(evolution => MapEvolution(evolution, label))],
            MapSprites(entry.Sprites, label));
    }

    /// <summary>Convierte el catalogo de tipos, acompanado de sus recuentos.</summary>
    /// <param name="entries">Tipos del dataset.</param>
    /// <param name="counts">Numero de Pokemon por slug de tipo.</param>
    /// <returns>Los tipos con su recuento.</returns>
    public static IReadOnlyList<PokemonTypeInfo> MapTypes(
        IReadOnlyList<DatasetCatalogEntry> entries,
        IReadOnlyDictionary<string, int> counts) =>
        [.. entries
            .OrderBy(entry => entry.Id)
            .Select(entry => new PokemonTypeInfo(
                entry.Id,
                RequiredSlug(entry.Slug, $"type {entry.Id}"),
                RequiredText(entry.Name, $"type {entry.Id}"),
                counts.GetValueOrDefault(entry.Slug)))];

    /// <summary>Convierte el catalogo de habilidades, acompanado de sus recuentos.</summary>
    /// <param name="entries">Habilidades del dataset.</param>
    /// <param name="counts">Numero de Pokemon por slug de habilidad.</param>
    /// <returns>Las habilidades con su recuento.</returns>
    public static IReadOnlyList<AbilityInfo> MapAbilities(
        IReadOnlyList<DatasetAbility> entries,
        IReadOnlyDictionary<string, int> counts) =>
        [.. entries
            .OrderBy(entry => entry.Id)
            .Select(entry => new AbilityInfo(
                entry.Id,
                RequiredSlug(entry.Slug, $"ability {entry.Id}"),
                RequiredText(entry.Name, $"ability {entry.Id}"),
                entry.IsMainSeries,
                entry.ShortEffect,
                counts.GetValueOrDefault(entry.Slug)))];

    /// <summary>Convierte el catalogo de grupos de huevo.</summary>
    /// <param name="entries">Grupos de huevo del dataset.</param>
    /// <returns>Los grupos de huevo, ordenados por identificador.</returns>
    public static IReadOnlyList<EggGroupInfo> MapEggGroups(IReadOnlyList<DatasetCatalogEntry> entries) =>
        [.. entries
            .OrderBy(entry => entry.Id)
            .Select(entry => new EggGroupInfo(
                entry.Id,
                RequiredSlug(entry.Slug, $"egg group {entry.Id}"),
                RequiredText(entry.Name, $"egg group {entry.Id}")))];

    /// <summary>
    /// Convierte un catalogo simple (habitats) acompanado de sus recuentos,
    /// ordenado alfabeticamente por nombre.
    /// </summary>
    /// <param name="entries">Entradas del dataset.</param>
    /// <param name="counts">Numero de Pokemon por slug.</param>
    /// <param name="kind">Nombre del catalogo, usado en los mensajes de error.</param>
    /// <returns>Las entradas con su recuento.</returns>
    public static IReadOnlyList<CatalogEntryInfo> MapSimpleCatalog(
        IReadOnlyList<DatasetCatalogEntry> entries,
        IReadOnlyDictionary<string, int> counts,
        string kind) =>
        [.. entries
            .OrderBy(entry => entry.Name, StringComparer.Ordinal)
            .Select(entry => new CatalogEntryInfo(
                entry.Id,
                RequiredSlug(entry.Slug, $"{kind} {entry.Id}"),
                RequiredText(entry.Name, $"{kind} {entry.Id}"),
                counts.GetValueOrDefault(entry.Slug)))];

    private static void EnsureSchemaVersion(PokemonDatasetFile file)
    {
        var version = file.Metadata?.SchemaVersion;

        if (!string.Equals(version, SupportedSchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"The embedded dataset uses schema version '{version ?? "(missing)"}', but this build " +
                $"understands '{SupportedSchemaVersion}'. Regenerate the dataset with tools/build-dataset.mjs.");
        }
    }

    private PokemonGeneration MapGeneration(int generationId, string slug, string label)
    {
        if (!_generationIds.TryGetValue(slug, out var expectedId) || expectedId != generationId)
        {
            throw new InvalidDataException(
                $"{label} declares generation '{slug}' (id {generationId}), which is not a known generation.");
        }

        if (!Enum.IsDefined((PokemonGeneration)generationId))
        {
            throw new InvalidDataException(
                $"{label} declares generation id {generationId}, which is outside the domain enumeration.");
        }

        return (PokemonGeneration)generationId;
    }

    private static PokemonStats MapStats(DatasetStats stats, string label)
    {
        try
        {
            return PokemonStats.Create(
                stats.Hp,
                stats.Attack,
                stats.Defense,
                stats.SpecialAttack,
                stats.SpecialDefense,
                stats.Speed);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new InvalidDataException(
                $"A base stat of {label} is outside the allowed range: {exception.Message}",
                exception);
        }
    }

    private PokemonAbility MapAbility(DatasetAbilityReference reference, string label)
    {
        var slug = RequiredSlug(reference.Name, $"{label} ability");

        // El nombre presentable vive en el catalogo de habilidades, no en la
        // ficha del Pokemon: el dataset los normaliza para no duplicarlo 1026
        // veces.
        if (!_abilityNames.TryGetValue(slug.Value, out var displayName))
        {
            throw new InvalidDataException(
                $"{label} references ability '{slug}', which is not present in the ability catalogue.");
        }

        return new PokemonAbility(slug, displayName, reference.IsHidden, reference.Slot);
    }

    private static Evolution MapEvolution(DatasetEvolution evolution, string label) => new(
        evolution.Id,
        RequiredSlug(evolution.Name, $"{label} evolution"),
        RequiredText(evolution.DisplayName, $"{label} evolution"),
        [.. evolution.Requirements.Select(requirement => new EvolutionRequirement(
            RequiredText(requirement.Trigger, $"{label} evolution requirement"),
            requirement.MinLevel,
            OptionalSlug(requirement.Item, $"{label} evolution item"),
            OptionalSlug(requirement.HeldItem, $"{label} evolution held item"),
            OptionalSlug(requirement.Location, $"{label} evolution location"),
            requirement.Gender,
            OptionalSlug(requirement.KnownMove, $"{label} evolution known move"),
            OptionalSlug(requirement.KnownMoveType, $"{label} evolution known move type"),
            requirement.TimeOfDay,
            requirement.MinHappiness,
            requirement.MinAffection,
            requirement.MinBeauty,
            requirement.NeedsOverworldRain,
            requirement.TurnUpsideDown,
            requirement.RelativePhysicalStats,
            OptionalSlug(requirement.PartyType, $"{label} evolution party type"),
            OptionalSlug(requirement.TradeSpecies, $"{label} evolution trade species")))]);

    private static PokemonSprites MapSprites(DatasetSprites sprites, string label) => new(
        RequiredUri(sprites.OfficialArtwork, $"{label} official artwork"),
        RequiredUri(sprites.OfficialArtworkShiny, $"{label} shiny official artwork"),
        RequiredUri(sprites.HomeArtwork, $"{label} home artwork"),
        RequiredUri(sprites.FrontDefault, $"{label} front sprite"),
        RequiredUri(sprites.FrontShiny, $"{label} shiny front sprite"),
        RequiredUri(sprites.PixelArt, $"{label} pixel sprite"),
        RequiredUri(sprites.Spritesheet, $"{label} spritesheet"));

    private static Slug RequiredSlug(string? value, string label) =>
        Slug.Create(value).TryGetValue(out var slug)
            ? slug
            : throw new InvalidDataException(
                $"The dataset has a missing or invalid identifier for {label}: '{value ?? "(null)"}'.");

    private static Slug? OptionalSlug(string? value, string label) =>
        string.IsNullOrWhiteSpace(value) ? null : RequiredSlug(value, label);

    private static string RequiredText(string? value, string label) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidDataException($"The dataset has a missing name for {label}.")
            : value;

    private static Uri RequiredUri(string? value, string label) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? uri
            : throw new InvalidDataException(
                $"The dataset has an invalid absolute URL for {label}: '{value ?? "(null)"}'.");
}
