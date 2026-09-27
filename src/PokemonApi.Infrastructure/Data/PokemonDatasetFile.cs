namespace PokemonApi.Infrastructure.Data;

/// <summary>
/// Forma del recurso <c>Data/pokemon.json</c> tal y como lo produce
/// <c>tools/build-dataset.mjs</c>.
/// </summary>
/// <remarks>
/// <para>
/// Son records planos y mutables por construccion, sin logica de negocio: su
/// unico proposito es describir el JSON. La conversion a entidades de dominio la
/// realiza <see cref="PokemonDatasetMapper"/>, que es donde se aplican las
/// invariantes.
/// </para>
/// <para>
/// Se deserializan con <c>PropertyNameCaseInsensitive</c>, de modo que los
/// nombres en <c>PascalCase</c> de estos records corresponden a las claves
/// <c>camelCase</c> del JSON sin necesidad de atributos en cada propiedad.
/// </para>
/// <para>
/// Los tipos de enumeracion del dominio (generacion, rareza, estadistica) no se
/// usan aqui a proposito: el dataset solo contiene identificadores, y resolverlos
/// contra las enumeraciones del dominio es responsabilidad del mapper.
/// </para>
/// </remarks>
internal sealed record PokemonDatasetFile
{
    public DatasetMetadata? Metadata { get; init; }

    public IReadOnlyList<DatasetGeneration> Generations { get; init; } = [];

    public IReadOnlyList<DatasetCatalogEntry> Types { get; init; } = [];

    public IReadOnlyList<DatasetAbility> Abilities { get; init; } = [];

    public IReadOnlyList<DatasetCatalogEntry> EggGroups { get; init; } = [];

    public IReadOnlyList<DatasetCatalogEntry> Habitats { get; init; } = [];

    public IReadOnlyList<DatasetPokemon> Pokemon { get; init; } = [];
}

/// <summary>Procedencia y trazabilidad del dataset.</summary>
/// <param name="SchemaVersion">Version del formato de este recurso.</param>
/// <param name="Source">Nombre de la fuente de datos.</param>
/// <param name="SourceVersion">Version de la fuente de datos.</param>
/// <param name="SourceUrl">Sitio web de la fuente de datos.</param>
/// <param name="License">Licencia de los datos.</param>
/// <param name="Attribution">Atribucion que debe mostrarse a los consumidores.</param>
/// <param name="GeneratedAtUtc">Momento de generacion del recurso, en UTC.</param>
/// <param name="PokemonCount">Numero de Pokemon incluidos.</param>
/// <param name="GenerationCount">Numero de generaciones incluidas.</param>
/// <param name="EvolutionCount">Numero de relaciones evolutivas incluidas.</param>
internal sealed record DatasetMetadata(
    string SchemaVersion,
    string Source,
    string SourceVersion,
    string SourceUrl,
    string License,
    string Attribution,
    DateTimeOffset GeneratedAtUtc,
    int PokemonCount,
    int GenerationCount,
    int EvolutionCount);

/// <summary>Generacion del catalogo, con la region a la que pertenece.</summary>
internal sealed record DatasetGeneration(
    int Id,
    string Slug,
    string Name,
    string Region,
    string RegionName,
    int PokemonCount);

/// <summary>Entrada de catalogo simple (tipos, grupos de huevo, habitats).</summary>
internal sealed record DatasetCatalogEntry(int Id, string Slug, string Name);

/// <summary>Habilidad del catalogo, con su descripcion resumida.</summary>
internal sealed record DatasetAbility(
    int Id,
    string Slug,
    string Name,
    bool IsMainSeries,
    string? ShortEffect);

/// <summary>Especie del catalogo.</summary>
/// <param name="Id">Identificador de la Poke&#x27;dex nacional.</param>
/// <param name="Name">Slug canonico del nombre.</param>
/// <param name="DisplayName">Nombre presentable.</param>
/// <param name="Genus">Categoria a la que pertenece.</param>
/// <param name="Description">Descripcion de la Poke&#x27;dex.</param>
/// <param name="SpeciesId">Identificador de la especie.</param>
/// <param name="GenerationId">Identificador numerico de la generacion.</param>
/// <param name="Generation">Slug de la generacion.</param>
/// <param name="Region">Slug de la region.</param>
/// <param name="Types">Slugs de los tipos, en orden de prioridad.</param>
/// <param name="Height">Altura en decimetros.</param>
/// <param name="Weight">Peso en hectogramos.</param>
/// <param name="BaseExperience">Experiencia base, o <see langword="null"/> si no se conoce.</param>
/// <param name="Stats">Estadisticas base de combate.</param>
/// <param name="TotalStats">Suma de las seis estadisticas.</param>
/// <param name="Abilities">Habilidades que puede poseer.</param>
/// <param name="IsLegendary">Si es legendary.</param>
/// <param name="IsMythical">Si es mythical.</param>
/// <param name="IsBaby">Si es una forma bebe.</param>
/// <param name="CaptureRate">Tasa de captura, de 0 a 255.</param>
/// <param name="BaseHappiness">Felicidad base.</param>
/// <param name="GenderRate">Probabilidad de ser hembra, de 0 a 8; -1 si es indeterminado.</param>
/// <param name="HasGenderDifferences">Si su forma hembra difiere visualmente.</param>
/// <param name="HatchCounter">Pasos necesarios para eclosionar del huevo.</param>
/// <param name="GrowthRate">Slug de la velocidad de crecimiento.</param>
/// <param name="EggGroups">Slugs de los grupos de huevo.</param>
/// <param name="Color">Slug del color predominante.</param>
/// <param name="Habitat">Slug del habitat, o <see langword="null"/> si no tiene.</param>
/// <param name="Shape">Slug de la silueta.</param>
/// <param name="EvolvesFrom">Slug del Pokemon del que evoluciona, o <see langword="null"/>.</param>
/// <param name="EvolutionChainId">Identificador de su cadena evolutiva.</param>
/// <param name="EvolvesTo">Evoluciones posibles.</param>
/// <param name="Sprites">Direcciones de sus imagenes.</param>
internal sealed record DatasetPokemon(
    int Id,
    string Name,
    string DisplayName,
    string? Genus,
    string? Description,
    int SpeciesId,
    int GenerationId,
    string Generation,
    string Region,
    IReadOnlyList<string> Types,
    int Height,
    int Weight,
    int? BaseExperience,
    DatasetStats Stats,
    int TotalStats,
    IReadOnlyList<DatasetAbilityReference> Abilities,
    bool IsLegendary,
    bool IsMythical,
    bool IsBaby,
    int CaptureRate,
    int BaseHappiness,
    int GenderRate,
    bool HasGenderDifferences,
    int HatchCounter,
    string GrowthRate,
    IReadOnlyList<string> EggGroups,
    string Color,
    string? Habitat,
    string Shape,
    string? EvolvesFrom,
    int EvolutionChainId,
    IReadOnlyList<DatasetEvolution> EvolvesTo,
    DatasetSprites Sprites);

/// <summary>Las seis estadisticas base de combate.</summary>
internal sealed record DatasetStats(
    int Hp,
    int Attack,
    int Defense,
    int SpecialAttack,
    int SpecialDefense,
    int Speed);

/// <summary>Referencia a una habilidad desde la ficha de un Pokemon.</summary>
internal sealed record DatasetAbilityReference(string Name, bool IsHidden, int Slot);

/// <summary>Evolucion posible, con las condiciones que la habilitan.</summary>
internal sealed record DatasetEvolution(
    int Id,
    string Name,
    string DisplayName,
    IReadOnlyList<DatasetEvolutionRequirement> Requirements);

/// <summary>Condicion de evolucion. Solo un subconjunto de las propiedades tiene valor.</summary>
/// <param name="Trigger">Evento que dispara la evolucion.</param>
/// <param name="MinLevel">Nivel minimo necesario.</param>
/// <param name="Item">Objeto que se consume al evolucionar.</param>
/// <param name="HeldItem">Objeto que debe llevar encima al evolucionar.</param>
/// <param name="Location">Lugar donde debe producirse la evolucion.</param>
/// <param name="Gender">Genero requerido: <c>female</c> o <c>male</c>.</param>
/// <param name="KnownMove">Movimiento que debe conocer.</param>
/// <param name="KnownMoveType">Tipo que debe tener el movimiento conocido.</param>
/// <param name="TimeOfDay">Momento del dia requerido.</param>
/// <param name="MinHappiness">Felicidad minima.</param>
/// <param name="MinAffection">Afecto minimo.</param>
/// <param name="MinBeauty">Belleza minima.</param>
/// <param name="NeedsOverworldRain">Si requiere que este lloviendo en el mundo exterior.</param>
/// <param name="TurnUpsideDown">Si requiere mantener la consola boca abajo.</param>
/// <param name="RelativePhysicalStats">Comparacion de estadisticas fisicas: 1, 0 o -1.</param>
/// <param name="PartyType">Tipo de Pokemon que debe acompanar al grupo.</param>
/// <param name="TradeSpecies">Especie a la que hay que intercambiarlo.</param>
internal sealed record DatasetEvolutionRequirement(
    string Trigger,
    int? MinLevel = null,
    string? Item = null,
    string? HeldItem = null,
    string? Location = null,
    string? Gender = null,
    string? KnownMove = null,
    string? KnownMoveType = null,
    string? TimeOfDay = null,
    int? MinHappiness = null,
    int? MinAffection = null,
    int? MinBeauty = null,
    bool NeedsOverworldRain = false,
    bool TurnUpsideDown = false,
    int? RelativePhysicalStats = null,
    string? PartyType = null,
    string? TradeSpecies = null);

/// <summary>Direcciones de las imagenes de un Pokemon.</summary>
internal sealed record DatasetSprites(
    string OfficialArtwork,
    string OfficialArtworkShiny,
    string HomeArtwork,
    string FrontDefault,
    string FrontShiny,
    string PixelArt,
    string Spritesheet);
