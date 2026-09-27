using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Abstractions.Repositories;
using PokemonApi.Domain.Common;
using PokemonApi.Domain.Entities;

namespace PokemonApi.Application.Features.Catalog.GetTypes;

/// <summary>
/// Representacion de API de un tipo de Pokemon.
/// </summary>
/// <param name="Id">Identificador del tipo.</param>
/// <param name="Slug">Slug canonico.</param>
/// <param name="Name">Nombre presentable.</param>
/// <param name="PokemonCount">Numero de Pokemon del catalogo que poseen el tipo.</param>
public sealed record TypeResponse(
    int Id,
    string Slug,
    string Name,
    int PokemonCount)
{
    /// <summary>Convierte un tipo de dominio en su representacion de API.</summary>
    /// <param name="type">Tipo de dominio.</param>
    /// <returns>La representacion de API.</returns>
    public static TypeResponse FromDomain(PokemonTypeInfo type) =>
        new(type.Id, type.Slug.Value, type.Name, type.PokemonCount);
}

/// <summary>
/// Peticion para listar los tipos de Pokemon del catalogo.
/// </summary>
public sealed record GetTypesQuery : IRequest<Result<IReadOnlyList<TypeResponse>>>;

/// <summary>
/// Obtiene los tipos de Pokemon del catalogo.
/// </summary>
/// <param name="catalog">Repositorio de catalogos.</param>
public sealed class GetTypesQueryHandler(ICatalogRepository catalog)
    : IRequestHandler<GetTypesQuery, Result<IReadOnlyList<TypeResponse>>>
{
    private readonly ICatalogRepository _catalog = catalog;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TypeResponse>>> HandleAsync(
        GetTypesQuery request,
        CancellationToken cancellationToken)
    {
        var types = await _catalog.GetTypesAsync(cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<TypeResponse>>.Success(
            [.. types.Select(TypeResponse.FromDomain)]);
    }
}
