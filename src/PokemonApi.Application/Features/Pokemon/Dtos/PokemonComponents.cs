using PokemonApi.Domain.Enumerations;

namespace PokemonApi.Application.Features.Pokemon.Dtos;

/// <summary>
/// Estadisticas base de un Pokemon.
/// </summary>
/// <param name="Hp">Puntos de vida.</param>
/// <param name="Attack">Ataque fisico.</param>
/// <param name="Defense">Defensa fisica.</param>
/// <param name="SpecialAttack">Ataque especial.</param>
/// <param name="SpecialDefense">Defensa especial.</param>
/// <param name="Speed">Velocidad.</param>
/// <param name="Total">Suma de las seis estadisticas.</param>
public sealed record StatsResponse(
    int Hp,
    int Attack,
    int Defense,
    int SpecialAttack,
    int SpecialDefense,
    int Speed,
    int Total)
{
    /// <summary>Convierte las estadisticas de dominio en su representacion de API.</summary>
    /// <param name="stats">Estadisticas de dominio.</param>
    /// <returns>La representacion de API.</returns>
    public static StatsResponse FromDomain(Domain.ValueObjects.PokemonStats stats) => new(
        stats.Hp,
        stats.Attack,
        stats.Defense,
        stats.SpecialAttack,
        stats.SpecialDefense,
        stats.Speed,
        stats.Total);
}

/// <summary>
/// Habilidad de un Pokemon.
/// </summary>
/// <param name="Name">Slug canonico de la habilidad.</param>
/// <param name="DisplayName">Nombre presentable.</param>
/// <param name="IsHidden">Si es una habilidad oculta.</param>
/// <param name="Slot">Posicion que ocupa en la especie (1, 2 o 3 para la oculta).</param>
public sealed record AbilityResponse(
    string Name,
    string DisplayName,
    bool IsHidden,
    int Slot)
{
    /// <summary>Convierte una habilidad de dominio en su representacion de API.</summary>
    /// <param name="ability">Habilidad de dominio.</param>
    /// <returns>La representacion de API.</returns>
    public static AbilityResponse FromDomain(Domain.ValueObjects.PokemonAbility ability) => new(
        ability.Name.Value,
        ability.DisplayName,
        ability.IsHidden,
        ability.Slot);
}

/// <summary>
/// Condicion que habilita una evolucion.
/// </summary>
/// <param name="Trigger">Evento que dispara la evolucion.</param>
/// <param name="MinLevel">Nivel minimo necesario.</param>
/// <param name="Item">Objeto que se consume al evolucionar.</param>
/// <param name="HeldItem">Objeto que debe llevar encima.</param>
/// <param name="Location">Lugar donde debe producirse.</param>
/// <param name="Gender">Genero requerido: <c>female</c> o <c>male</c>.</param>
/// <param name="KnownMove">Movimiento que debe conocer.</param>
/// <param name="KnownMoveType">Tipo que debe tener el movimiento conocido.</param>
/// <param name="TimeOfDay">Momento del dia requerido.</param>
/// <param name="MinHappiness">Felicidad minima.</param>
/// <param name="MinAffection">Afecto minimo.</param>
/// <param name="MinBeauty">Belleza minima.</param>
/// <param name="NeedsOverworldRain">Si requiere que este lloviendo.</param>
/// <param name="TurnUpsideDown">Si requiere mantener la consola boca abajo.</param>
/// <param name="RelativePhysicalStats">
/// Comparacion de estadisticas exigida, expresada como texto legible
/// (<c>attack &gt; defense</c>, <c>attack = defense</c> o
/// <c>attack &lt; defense</c>). La fuente de datos la codifica como 1, 0 y -1.
/// </param>
/// <param name="PartyType">Tipo de Pokemon que debe acompanar al grupo.</param>
/// <param name="TradeSpecies">Especie a la que hay que intercambiarlo.</param>
public sealed record EvolutionRequirementResponse(
    string Trigger,
    int? MinLevel,
    string? Item,
    string? HeldItem,
    string? Location,
    string? Gender,
    string? KnownMove,
    string? KnownMoveType,
    string? TimeOfDay,
    int? MinHappiness,
    int? MinAffection,
    int? MinBeauty,
    bool NeedsOverworldRain,
    bool TurnUpsideDown,
    string? RelativePhysicalStats,
    string? PartyType,
    string? TradeSpecies)
{
    /// <summary>Convierte un requisito de dominio en su representacion de API.</summary>
    /// <param name="requirement">Requisito de dominio.</param>
    /// <returns>La representacion de API.</returns>
    public static EvolutionRequirementResponse FromDomain(Domain.ValueObjects.EvolutionRequirement requirement) => new(
        requirement.Trigger,
        requirement.MinLevel,
        requirement.Item?.Value,
        requirement.HeldItem?.Value,
        requirement.Location?.Value,
        requirement.Gender,
        requirement.KnownMove?.Value,
        requirement.KnownMoveType?.Value,
        requirement.TimeOfDay,
        requirement.MinHappiness,
        requirement.MinAffection,
        requirement.MinBeauty,
        requirement.NeedsOverworldRain,
        requirement.TurnUpsideDown,
        DescribeRelativePhysicalStats(requirement.RelativePhysicalStats),
        requirement.PartyType?.Value,
        requirement.TradeSpecies?.Value);

    /// <summary>
    /// Traduce la comparacion de estadisticas fisicas de la fuente de datos a un
    /// texto legible.
    /// </summary>
    /// <param name="value">Codigo original: 1, 0 o -1.</param>
    /// <returns>La comparacion en texto, o <see langword="null"/> si no aplica.</returns>
    private static string? DescribeRelativePhysicalStats(int? value) => value switch
    {
        1 => "attack > defense",
        0 => "attack = defense",
        -1 => "attack < defense",
        _ => null,
    };
}

/// <summary>
/// Evolucion posible a partir de un Pokemon.
/// </summary>
/// <param name="Id">Identificador de la Poke&#x27;dex del Pokemon resultante.</param>
/// <param name="Name">Slug del Pokemon resultante.</param>
/// <param name="DisplayName">Nombre presentable del Pokemon resultante.</param>
/// <param name="OfficialArtwork">URL de la ilustracion del Pokemon resultante.</param>
/// <param name="Requirements">
/// Condiciones que habilitan la evolucion. Puede haber mas de una cuando son
/// alternativas entre si.
/// </param>
public sealed record EvolutionResponse(
    int Id,
    string Name,
    string DisplayName,
    string OfficialArtwork,
    IReadOnlyList<EvolutionRequirementResponse> Requirements)
{
    /// <summary>Convierte una evolucion de dominio en su representacion de API.</summary>
    /// <param name="evolution">Evolucion de dominio.</param>
    /// <param name="officialArtwork">URL de la ilustracion del Pokemon resultante.</param>
    /// <returns>La representacion de API.</returns>
    public static EvolutionResponse FromDomain(
        Domain.ValueObjects.Evolution evolution,
        string officialArtwork) => new(
        evolution.TargetId,
        evolution.TargetName.Value,
        evolution.TargetDisplayName,
        officialArtwork,
        [.. evolution.Requirements.Select(EvolutionRequirementResponse.FromDomain)]);
}
