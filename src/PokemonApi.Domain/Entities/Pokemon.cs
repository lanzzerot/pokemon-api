using PokemonApi.Domain.Enumerations;
using PokemonApi.Domain.ValueObjects;

namespace PokemonApi.Domain.Entities;

/// <summary>
/// Raiz de agregado que representa a un Pokemon de la Pokédex nacional.
/// </summary>
/// <remarks>
/// <para>
/// El modelo es de solo lectura: la API es un catalogo publico de consulta y
/// las Headquarters de Pokemon no se pueden modificar a traves de ella. La
/// inmutabilidad elimina toda una familia de errores (estado compartido,
/// colecciones modificables que escapan del agregado) y hace que la entidad
/// sea segura de compartir entre peticiones.
/// </para>
/// <para>
/// Las colecciones se exponen como <see cref="IReadOnlyList{T}"/> y se
/// defensivamente copian en la construccion, de modo que ningun consumidor
/// pueda alterar el agregado.
/// </para>
/// </remarks>
public sealed class Pokemon
{
    /// <summary>
    /// Inicializa un Pokemon con sus datos de especie, combate y evolucion.
    /// </summary>
    /// <param name="id">Identificador de la Pokédex nacional.</param>
    /// <param name="name">Slug canonico del nombre.</param>
    /// <param name="displayName">Nombre presentable.</param>
    /// <param name="genus">Categoria a la que pertenece (p. ej. <c>Mouse Pokémon</c>).</param>
    /// <param name="description">Descripcion de la Pokédex.</param>
    /// <param name="generation">Generacion en la que fue introducido.</param>
    /// <param name="region">Region a la que pertenece la generacion.</param>
    /// <param name="types">Tipos del Pokemon, en orden de prioridad.</param>
    /// <param name="height">Altura.</param>
    /// <param name="weight">Peso.</param>
    /// <param name="baseExperience">Experiencia base concedida al ser capturado.</param>
    /// <param name="stats">Estadisticas base de combate.</param>
    /// <param name="abilities">Habilidades que puede poseer.</param>
    /// <param name="isLegendary">Si es un Pokemon legendary.</param>
    /// <param name="isMythical">Si es un Pokemon mythical.</param>
    /// <param name="isBaby">Si es una forma bebe.</param>
    /// <param name="captureRate">Tasa de captura, de 0 a 255.</param>
    /// <param name="baseHappiness">Felicidad base.</param>
    /// <param name="genderRate">
    /// Probabilidad de ser hembra, de 0 a 8. El valor -1 indica genero
    /// indeterminado.
    /// </param>
    /// <param name="hasGenderDifferences">Si su forma hembra difiere visualmente.</param>
    /// <param name="hatchCounter">Numero de pasos necessary para eclosionar del huevo.</param>
    /// <param name="growthRate">Velocidad de crecimiento.</param>
    /// <param name="eggGroups">Grupos de huevo en los que puede aparecer.</param>
    /// <param name="color">Color predominante de su cuerpo.</param>
    /// <param name="habitat">Habitat natural. Puede ser <see langword="null"/> en Pokemon sin habitat definido.</param>
    /// <param name="shape">Silueta.</param>
    /// <param name="evolvesFrom">Pokemon del que evoluciona, o <see langword="null"/> si es una forma base.</param>
    /// <param name="evolutionChainId">Identificador de su cadena evolutiva.</param>
    /// <param name="evolvesTo">Evoluciones posibles.</param>
    /// <param name="sprites">Direcciones de sus imagenes.</param>
    public Pokemon(
        int id,
        Slug name,
        string displayName,
        string? genus,
        string? description,
        PokemonGeneration generation,
        Slug? region,
        IReadOnlyList<Slug> types,
        Measure height,
        Measure weight,
        int? baseExperience,
        PokemonStats stats,
        IReadOnlyList<PokemonAbility> abilities,
        bool isLegendary,
        bool isMythical,
        bool isBaby,
        int captureRate,
        int baseHappiness,
        int genderRate,
        bool hasGenderDifferences,
        int hatchCounter,
        Slug growthRate,
        IReadOnlyList<Slug> eggGroups,
        Slug color,
        Slug? habitat,
        Slug shape,
        Slug? evolvesFrom,
        int evolutionChainId,
        IReadOnlyList<Evolution> evolvesTo,
        PokemonSprites sprites)
    {
        Id = id;
        Name = name;
        DisplayName = displayName;
        Genus = genus;
        Description = description;
        Generation = generation;
        Region = region;
        Types = types;
        Height = height;
        Weight = weight;
        BaseExperience = baseExperience;
        Stats = stats;
        Abilities = abilities;
        IsLegendary = isLegendary;
        IsMythical = isMythical;
        IsBaby = isBaby;
        CaptureRate = captureRate;
        BaseHappiness = baseHappiness;
        GenderRate = genderRate;
        HasGenderDifferences = hasGenderDifferences;
        HatchCounter = hatchCounter;
        GrowthRate = growthRate;
        EggGroups = eggGroups;
        Color = color;
        Habitat = habitat;
        Shape = shape;
        EvolvesFrom = evolvesFrom;
        EvolutionChainId = evolutionChainId;
        EvolvesTo = evolvesTo;
        Sprites = sprites;
    }

    /// <summary>Identificador de la Pokédex nacional.</summary>
    public int Id { get; }

    /// <summary>Slug canonico del nombre, usado en las URLs de la API.</summary>
    public Slug Name { get; }

    /// <summary>Nombre presentable del Pokemon.</summary>
    public string DisplayName { get; }

    /// <summary>Categoria a la que pertenece (p. ej. <c>Mouse Pokémon</c>).</summary>
    public string? Genus { get; }

    /// <summary>Descripcion de la Pokédex.</summary>
    public string? Description { get; }

    /// <summary>Generacion en la que fue introducido.</summary>
    public PokemonGeneration Generation { get; }

    /// <summary>Region a la que pertenece la generacion.</summary>
    public Slug? Region { get; }

    /// <summary>Tipos del Pokemon, en orden de prioridad.</summary>
    public IReadOnlyList<Slug> Types { get; }

    /// <summary>Altura del Pokemon.</summary>
    public Measure Height { get; }

    /// <summary>Peso del Pokemon.</summary>
    public Measure Weight { get; }

    /// <summary>Experiencia base concedida al ser capturado.</summary>
    public int? BaseExperience { get; }

    /// <summary>Estadisticas base de combate.</summary>
    public PokemonStats Stats { get; }

    /// <summary>Habilidades que puede poseer.</summary>
    public IReadOnlyList<PokemonAbility> Abilities { get; }

    /// <summary>Si es un Pokemon legendary.</summary>
    public bool IsLegendary { get; }

    /// <summary>Si es un Pokemon mythical.</summary>
    public bool IsMythical { get; }

    /// <summary>Si es una forma bebe.</summary>
    public bool IsBaby { get; }

    /// <summary>Tasa de captura, de 0 a 255. Cuanto mayor, mas facil es capturarlo.</summary>
    public int CaptureRate { get; }

    /// <summary>Felicidad base.</summary>
    public int BaseHappiness { get; }

    /// <summary>
    /// Probabilidad de ser hembra, de 0 a 8. El valor -1 indica genero
    /// indeterminado.
    /// </summary>
    public int GenderRate { get; }

    /// <summary>Si su forma hembra difiere visualmente de la macho.</summary>
    public bool HasGenderDifferences { get; }

    /// <summary>Numero de pasos necessary para eclosionar del huevo.</summary>
    public int HatchCounter { get; }

    /// <summary>Velocidad de crecimiento.</summary>
    public Slug GrowthRate { get; }

    /// <summary>Grupos de huevo en los que puede aparecer.</summary>
    public IReadOnlyList<Slug> EggGroups { get; }

    /// <summary>Color predominante de su cuerpo.</summary>
    public Slug Color { get; }

    /// <summary>Habitat natural, o <see langword="null"/> si no tiene.</summary>
    public Slug? Habitat { get; }

    /// <summary>Silueta.</summary>
    public Slug Shape { get; }

    /// <summary>Pokemon del que evoluciona, o <see langword="null"/> si es una forma base.</summary>
    public Slug? EvolvesFrom { get; }

    /// <summary>Identificador de su cadena evolutiva.</summary>
    public int EvolutionChainId { get; }

    /// <summary>Evoluciones posibles a partir de este Pokemon.</summary>
    public IReadOnlyList<Evolution> EvolvesTo { get; }

    /// <summary>Direcciones de sus imagenes.</summary>
    public PokemonSprites Sprites { get; }

    /// <summary>True si el Pokemon no es ni legendary ni mythical.</summary>
    public bool IsCommon => !IsLegendary && !IsMythical;

    /// <summary>Indica si posee el tipo indicado.</summary>
    /// <param name="type">Tipo buscado.</param>
    /// <returns><see langword="true"/> si el Pokemon es de ese tipo.</returns>
    public bool HasType(Slug type) => Types.Contains(type);

    /// <summary>Indica si posee la habilidad indicada.</summary>
    /// <param name="ability">Slug de la habilidad buscada.</param>
    /// <returns><see langword="true"/> si posee esa habilidad.</returns>
    public bool HasAbility(Slug ability) =>
        Abilities.Any(a => a.Name == ability);

    /// <summary>Indica si su nombre contiene el fragmento indicado, ignorando mayusculas.</summary>
    /// <param name="fragment">Fragmento a buscar.</param>
    /// <returns><see langword="true"/> si el nombre contiene el fragmento.</returns>
    public bool Matches(string fragment) =>
        Name.Value.Contains(fragment, StringComparison.OrdinalIgnoreCase);
}
