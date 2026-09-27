using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Abstractions.Repositories;
using PokemonApi.Application.Features.Pokemon.Dtos;
using PokemonApi.Domain.Common;
using PokemonApi.Domain.Entities;

namespace PokemonApi.Application.Features.Pokemon.GetById;

/// <summary>
/// Obtiene un Pokemon por su identificador de la Poke&#x27;dex.
/// </summary>
/// <param name="repository">Repositorio del catalogo.</param>
public sealed class GetPokemonByIdQueryHandler(IPokemonRepository repository)
    : IRequestHandler<GetPokemonByIdQuery, Result<PokemonDetailResponse>>
{
    private readonly IPokemonRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<PokemonDetailResponse>> HandleAsync(
        GetPokemonByIdQuery request,
        CancellationToken cancellationToken)
    {
        var pokemon = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (pokemon is null)
        {
            return Result<PokemonDetailResponse>.Failure(Error.NotFound(
                "pokemon.not_found",
                $"No Pokemon exists with id {request.Id}."));
        }

        // La respuesta incluye las evoluciones con su ilustracion. Recuperar la
        // cadena evolutiva en la misma consulta evita un N+1 al montar el DTO.
        var chain = await _repository
            .GetEvolutionChainAsync(pokemon.EvolutionChainId, cancellationToken)
            .ConfigureAwait(false);

        var artworks = chain.ToDictionary(p => p.Id, p => p.Sprites.OfficialArtwork.ToString());

        return Result<PokemonDetailResponse>.Success(
            PokemonDetailResponse.FromDomain(pokemon, artworks));
    }
}
