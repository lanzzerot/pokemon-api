using PokemonApi.Application.Common;
using PokemonEntity = PokemonApi.Domain.Entities.Pokemon;

namespace PokemonApi.Application.Features.Pokemon.Dtos;

/// <summary>
/// Vista resumida de un Pokemon, usada en listados y resultados de busqueda.
/// </summary>
/// <remarks>
/// Deliberadamente excluye campos pesados (descripcion, evoluciones, sprites en
/// todos los formatos) para que listar mil Pokemon no exija descargar varios
/// megabytes. El detalle completo se obtiene en <c>GET /api/v1/pokemon/{id}</c>.
/// </remarks>
/// <param name="Id">Identificador de la Poke&#x27;dex nacional.</param>
/// <param name="Name">Slug canonico del nombre.</param>
/// <param name="DisplayName">Nombre presentable.</param>
/// <param name="Types">Tipos del Pokemon.</param>
/// <param name="Generation">Generacion de pertenencia.</param>
/// <param name="GenerationId">Identificador de la generacion.</param>
/// <param name="Region">Region de pertenencia.</param>
/// <param name="TotalStats">Suma de las seis estadisticas base.</param>
/// <param name="IsLegendary">Si es legendary.</param>
/// <param name="IsMythical">Si es mythical.</param>
/// <param name="OfficialArtwork">URL de la ilustracion oficial.</param>
public sealed record PokemonSummaryResponse(
    int Id,
    string Name,
    string DisplayName,
    IReadOnlyList<string> Types,
    string Generation,
    int GenerationId,
    string? Region,
    int TotalStats,
    bool IsLegendary,
    bool IsMythical,
    string OfficialArtwork)
{
    /// <summary>Convierte una entidad de dominio en su vista resumida.</summary>
    /// <param name="pokemon">Entidad de dominio.</param>
    /// <returns>La vista resumida.</returns>
    public static PokemonSummaryResponse FromDomain(PokemonEntity pokemon) => new(
        pokemon.Id,
        pokemon.Name.Value,
        pokemon.DisplayName,
        [.. pokemon.Types.Select(t => t.Value)],
        pokemon.Generation.ToSlug(),
        (int)pokemon.Generation,
        pokemon.Region?.Value,
        pokemon.Stats.Total,
        pokemon.IsLegendary,
        pokemon.IsMythical,
        pokemon.Sprites.OfficialArtwork.ToString());
}
