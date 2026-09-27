using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Abstractions.Repositories;
using PokemonApi.Application.Common.Pagination;
using PokemonApi.Application.Features.Pokemon.Dtos;
using PokemonApi.Domain.Common;

namespace PokemonApi.Application.Features.Pokemon.List;

/// <summary>
/// Lista Pokemon aplicando filtros, orden y paginacion.
/// </summary>
/// <param name="repository">Repositorio del catalogo.</param>
/// <param name="queryBuilder">Traduce la peticion a un filtro de dominio.</param>
public sealed class ListPokemonQueryHandler(
    IPokemonRepository repository,
    PokemonSearchQueryBuilder queryBuilder)
    : IRequestHandler<ListPokemonQuery, Result<PageResponse<PokemonSummaryResponse>>>
{
    private readonly IPokemonRepository _repository = repository;
    private readonly PokemonSearchQueryBuilder _queryBuilder = queryBuilder;

    /// <inheritdoc />
    public async Task<Result<PageResponse<PokemonSummaryResponse>>> HandleAsync(
        ListPokemonQuery request,
        CancellationToken cancellationToken)
    {
        var query = await _queryBuilder.BuildAsync(request, cancellationToken).ConfigureAwait(false);

        if (query.IsFailure)
        {
            return Result<PageResponse<PokemonSummaryResponse>>.Failure(query.Error!);
        }

        var page = await _repository.SearchAsync(query.Value, cancellationToken).ConfigureAwait(false);

        return Result<PageResponse<PokemonSummaryResponse>>.Success(
            PageResponse<PokemonSummaryResponse>.FromResult(new PagedResult<PokemonSummaryResponse>(
                [.. page.Items.Select(PokemonSummaryResponse.FromDomain)],
                page.Page,
                page.PageSize,
                page.TotalCount)));
    }
}
