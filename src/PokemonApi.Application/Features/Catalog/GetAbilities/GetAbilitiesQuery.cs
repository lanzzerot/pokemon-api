using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Abstractions.Repositories;
using PokemonApi.Domain.Common;
using PokemonApi.Domain.Entities;

namespace PokemonApi.Application.Features.Catalog.GetAbilities;

/// <summary>
/// Representacion de API de una habilidad.
/// </summary>
/// <param name="Id">Identificador de la habilidad.</param>
/// <param name="Slug">Slug canonico.</param>
/// <param name="Name">Nombre presentable.</param>
/// <param name="IsMainSeries">Si pertenece a la serie principal de juegos.</param>
/// <param name="ShortEffect">Descripcion resumida de su efecto.</param>
/// <param name="PokemonCount">Numero de Pokemon del catalogo que poseen la habilidad.</param>
public sealed record AbilityResponse(
    int Id,
    string Slug,
    string Name,
    bool IsMainSeries,
    string? ShortEffect,
    int PokemonCount)
{
    /// <summary>Convierte una habilidad de dominio en su representacion de API.</summary>
    /// <param name="ability">Habilidad de dominio.</param>
    /// <returns>La representacion de API.</returns>
    public static AbilityResponse FromDomain(AbilityInfo ability) => new(
        ability.Id,
        ability.Slug.Value,
        ability.Name,
        ability.IsMainSeries,
        ability.ShortEffect,
        ability.PokemonCount);
}

/// <summary>
/// Peticion para listar las habilidades del catalogo.
/// </summary>
/// <param name="Name">Filtro opcional por nombre o descripcion.</param>
/// <param name="IsMainSeries">Filtro opcional por pertenencia a la serie principal.</param>
public sealed record GetAbilitiesQuery(
    string? Name = null,
    bool? IsMainSeries = null) : IRequest<Result<IReadOnlyList<AbilityResponse>>>;

/// <summary>
/// Obtiene las habilidades del catalogo.
/// </summary>
/// <param name="catalog">Repositorio de catalogos.</param>
public sealed class GetAbilitiesQueryHandler(ICatalogRepository catalog)
    : IRequestHandler<GetAbilitiesQuery, Result<IReadOnlyList<AbilityResponse>>>
{
    private readonly ICatalogRepository _catalog = catalog;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AbilityResponse>>> HandleAsync(
        GetAbilitiesQuery request,
        CancellationToken cancellationToken)
    {
        var abilities = await _catalog.GetAbilitiesAsync(cancellationToken).ConfigureAwait(false);

        IEnumerable<AbilityInfo> query = abilities;

        if (request.IsMainSeries is { } isMainSeries)
        {
            query = query.Where(a => a.IsMainSeries == isMainSeries);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var term = request.Name.Trim();
            query = query.Where(a =>
                a.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (a.ShortEffect?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return Result<IReadOnlyList<AbilityResponse>>.Success(
            [.. query.Select(AbilityResponse.FromDomain)]);
    }
}
