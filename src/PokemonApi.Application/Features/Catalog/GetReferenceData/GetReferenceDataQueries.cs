using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Abstractions.Repositories;
using PokemonApi.Domain.Common;
using PokemonApi.Domain.Entities;

namespace PokemonApi.Application.Features.Catalog.GetReferenceData;

/// <summary>
/// Representacion de API de una entrada de catalogo simple.
/// </summary>
/// <param name="Id">Identificador de la entrada.</param>
/// <param name="Slug">Slug canonico.</param>
/// <param name="Name">Nombre presentable.</param>
/// <param name="PokemonCount">Numero de Pokemon asociados a la entrada.</param>
public sealed record CatalogEntryResponse(
    int Id,
    string Slug,
    string Name,
    int PokemonCount)
{
    /// <summary>Convierte una entrada de dominio en su representacion de API.</summary>
    /// <param name="entry">Entrada de dominio.</param>
    /// <returns>La representacion de API.</returns>
    public static CatalogEntryResponse FromDomain(CatalogEntryInfo entry) =>
        new(entry.Id, entry.Slug.Value, entry.Name, entry.PokemonCount);
}

/// <summary>
/// Representacion de API de un grupo de huevo.
/// </summary>
/// <param name="Id">Identificador del grupo.</param>
/// <param name="Slug">Slug canonico.</param>
/// <param name="Name">Nombre presentable.</param>
public sealed record EggGroupResponse(
    int Id,
    string Slug,
    string Name)
{
    /// <summary>Convierte un grupo de huevo de dominio en su representacion de API.</summary>
    /// <param name="group">Grupo de huevo de dominio.</param>
    /// <returns>La representacion de API.</returns>
    public static EggGroupResponse FromDomain(EggGroupInfo group) =>
        new(group.Id, group.Slug.Value, group.Name);
}

/// <summary>
/// Peticion para listar los habitats presentes en el catalogo.
/// </summary>
public sealed record GetHabitatsQuery : IRequest<Result<IReadOnlyList<CatalogEntryResponse>>>;

/// <summary>
/// Obtiene los habitats presentes en el catalogo.
/// </summary>
/// <param name="catalog">Repositorio de catalogos.</param>
public sealed class GetHabitatsQueryHandler(ICatalogRepository catalog)
    : IRequestHandler<GetHabitatsQuery, Result<IReadOnlyList<CatalogEntryResponse>>>
{
    private readonly ICatalogRepository _catalog = catalog;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CatalogEntryResponse>>> HandleAsync(
        GetHabitatsQuery request,
        CancellationToken cancellationToken)
    {
        var habitats = await _catalog.GetHabitatsAsync(cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<CatalogEntryResponse>>.Success(
            [.. habitats.Select(CatalogEntryResponse.FromDomain)]);
    }
}

/// <summary>
/// Peticion para listar las regiones presentes en el catalogo.
/// </summary>
public sealed record GetRegionsQuery : IRequest<Result<IReadOnlyList<CatalogEntryResponse>>>;

/// <summary>
/// Obtiene las regiones presentes en el catalogo.
/// </summary>
/// <param name="catalog">Repositorio de catalogos.</param>
public sealed class GetRegionsQueryHandler(ICatalogRepository catalog)
    : IRequestHandler<GetRegionsQuery, Result<IReadOnlyList<CatalogEntryResponse>>>
{
    private readonly ICatalogRepository _catalog = catalog;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CatalogEntryResponse>>> HandleAsync(
        GetRegionsQuery request,
        CancellationToken cancellationToken)
    {
        var regions = await _catalog.GetRegionsAsync(cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<CatalogEntryResponse>>.Success(
            [.. regions.Select(CatalogEntryResponse.FromDomain)]);
    }
}

/// <summary>
/// Peticion para listar los grupos de huevo del catalogo.
/// </summary>
public sealed record GetEggGroupsQuery : IRequest<Result<IReadOnlyList<EggGroupResponse>>>;

/// <summary>
/// Obtiene los grupos de huevo del catalogo.
/// </summary>
/// <param name="catalog">Repositorio de catalogos.</param>
public sealed class GetEggGroupsQueryHandler(ICatalogRepository catalog)
    : IRequestHandler<GetEggGroupsQuery, Result<IReadOnlyList<EggGroupResponse>>>
{
    private readonly ICatalogRepository _catalog = catalog;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<EggGroupResponse>>> HandleAsync(
        GetEggGroupsQuery request,
        CancellationToken cancellationToken)
    {
        var eggGroups = await _catalog.GetEggGroupsAsync(cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<EggGroupResponse>>.Success(
            [.. eggGroups.Select(EggGroupResponse.FromDomain)]);
    }
}
