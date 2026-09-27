using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Abstractions.Repositories;
using PokemonApi.Application.Common.Pagination;
using PokemonApi.Domain.Common;
using PokemonApi.Domain.Entities;

namespace PokemonApi.Application.Features.Catalog.GetGenerations;

/// <summary>
/// Representacion de API de una generacion.
/// </summary>
/// <param name="Id">Identificador de la generacion.</param>
/// <param name="Slug">Slug canonico.</param>
/// <param name="Name">Nombre presentable.</param>
/// <param name="Region">Slug de la region.</param>
/// <param name="RegionName">Nombre presentable de la region.</param>
/// <param name="PokemonCount">Numero de Pokemon de la generacion.</param>
public sealed record GenerationResponse(
    int Id,
    string Slug,
    string Name,
    string Region,
    string RegionName,
    int PokemonCount)
{
    /// <summary>Convierte una generacion de dominio en su representacion de API.</summary>
    /// <param name="generation">Generacion de dominio.</param>
    /// <returns>La representacion de API.</returns>
    public static GenerationResponse FromDomain(Generation generation) => new(
        generation.Id,
        generation.Slug.Value,
        generation.Name,
        generation.Region.Value,
        generation.RegionName,
        generation.PokemonCount);
}

/// <summary>
/// Peticion para listar las generaciones del catalogo.
/// </summary>
public sealed record GetGenerationsQuery : IRequest<Result<IReadOnlyList<GenerationResponse>>>;

/// <summary>
/// Obtiene las generaciones del catalogo.
/// </summary>
/// <param name="catalog">Repositorio de catalogos.</param>
public sealed class GetGenerationsQueryHandler(ICatalogRepository catalog)
    : IRequestHandler<GetGenerationsQuery, Result<IReadOnlyList<GenerationResponse>>>
{
    private readonly ICatalogRepository _catalog = catalog;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GenerationResponse>>> HandleAsync(
        GetGenerationsQuery request,
        CancellationToken cancellationToken)
    {
        var generations = await _catalog.GetGenerationsAsync(cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<GenerationResponse>>.Success(
            [.. generations.Select(GenerationResponse.FromDomain)]);
    }
}
