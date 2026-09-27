using PokemonApi.Domain.Entities;

namespace PokemonApi.Application.Features.Pokemon.Dtos;

/// <summary>
/// Vista completa de un Pokemon.
/// </summary>
/// <param name="Id">Identificador de la Poke&#x27;dex nacional.</param>
/// <param name="Name">Slug canonico del nombre.</param>
/// <param name="DisplayName">Nombre presentable.</param>
/// <param name="Genus">Categoria a la que pertenece.</param>
/// <param name="Description">Descripcion de la Poke&#x27;dex.</param>
/// <param name="Generation">Generacion de pertenencia.</param>
/// <param name="GenerationId">Identificador de la generacion.</param>
/// <param name="Region">Region de pertenencia.</param>
/// <param name="Types">Tipos del Pokemon.</param>
/// <param name="Height">Altura y su unidad.</param>
/// <param name="Weight">Peso y su unidad.</param>
/// <param name="BaseExperience">Experiencia base concedida al ser capturado.</param>
/// <param name="Stats">Estadisticas base de combate.</param>
/// <param name="TotalStats">Suma de las seis estadisticas base.</param>
/// <param name="Abilities">Habilidades que puede poseer.</param>
/// <param name="IsLegendary">Si es legendary.</param>
/// <param name="IsMythical">Si es mythical.</param>
/// <param name="IsBaby">Si es una forma bebe.</param>
/// <param name="CaptureRate">Tasa de captura, de 0 a 255.</param>
/// <param name="BaseHappiness">Felicidad base.</param>
/// <param name="GenderRate">Probabilidad de ser hembra, de 0 a 8. -1 indica genero indeterminado.</param>
/// <param name="HasGenderDifferences">Si su forma hembra difiere visualmente.</param>
/// <param name="HatchCounter">Pasos necesarios para eclosionar del huevo.</param>
/// <param name="GrowthRate">Velocidad de crecimiento.</param>
/// <param name="EggGroups">Grupos de huevo.</param>
/// <param name="Color">Color predominante.</param>
/// <param name="Habitat">Habitat natural, o <see langword="null"/> si no tiene.</param>
/// <param name="Shape">Silueta.</param>
/// <param name="EvolvesFrom">Pokemon del que evoluciona, o <see langword="null"/> si es una forma base.</param>
/// <param name="EvolutionChainId">Identificador de la cadena evolutiva.</param>
/// <param name="EvolvesTo">Evoluciones posibles.</param>
/// <param name="Sprites">Direcciones de las imagenes disponibles.</param>
public sealed record PokemonDetailResponse(
    int Id,
    string Name,
    string DisplayName,
    string? Genus,
    string? Description,
    string Generation,
    int GenerationId,
    string? Region,
    IReadOnlyList<string> Types,
    MeasureResponse Height,
    MeasureResponse Weight,
    int? BaseExperience,
    StatsResponse Stats,
    int TotalStats,
    IReadOnlyList<AbilityResponse> Abilities,
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
    IReadOnlyList<EvolutionResponse> EvolvesTo,
    SpritesResponse Sprites)
{
    /// <summary>
    /// Convierte una entidad de dominio en su vista completa.
    /// </summary>
    /// <param name="pokemon">Entidad de dominio.</param>
    /// <param name="evolutionArtworks">
    /// Ilustraciones de los Pokemon destino de las evoluciones, indexadas por
    /// identificador. Se reciben aparte porque el repositorio de evoluciones es
    /// quien las conoce y evita un N+1 al construir la respuesta.
    /// </param>
    /// <returns>La vista completa.</returns>
    public static PokemonDetailResponse FromDomain(
        Domain.Entities.Pokemon pokemon,
        IReadOnlyDictionary<int, string> evolutionArtworks)
    {
        ArgumentNullException.ThrowIfNull(pokemon);
        ArgumentNullException.ThrowIfNull(evolutionArtworks);

        return new PokemonDetailResponse(
            pokemon.Id,
            pokemon.Name.Value,
            pokemon.DisplayName,
            pokemon.Genus,
            pokemon.Description,
            PokemonSummaryResponse.FromDomain(pokemon).Generation,
            (int)pokemon.Generation,
            pokemon.Region?.Value,
            [.. pokemon.Types.Select(t => t.Value)],
            MeasureResponse.FromDomain(pokemon.Height),
            MeasureResponse.FromDomain(pokemon.Weight),
            pokemon.BaseExperience,
            StatsResponse.FromDomain(pokemon.Stats),
            pokemon.Stats.Total,
            [.. pokemon.Abilities.Select(AbilityResponse.FromDomain)],
            pokemon.IsLegendary,
            pokemon.IsMythical,
            pokemon.IsBaby,
            pokemon.CaptureRate,
            pokemon.BaseHappiness,
            pokemon.GenderRate,
            pokemon.HasGenderDifferences,
            pokemon.HatchCounter,
            pokemon.GrowthRate.Value,
            [.. pokemon.EggGroups.Select(g => g.Value)],
            pokemon.Color.Value,
            pokemon.Habitat?.Value,
            pokemon.Shape.Value,
            pokemon.EvolvesFrom?.Value,
            pokemon.EvolutionChainId,
            [.. pokemon.EvolvesTo.Select(e => EvolutionResponse.FromDomain(
                e,
                evolutionArtworks.TryGetValue(e.TargetId, out var artwork) ? artwork : string.Empty))],
            SpritesResponse.FromDomain(pokemon.Sprites));
    }
}
