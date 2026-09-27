using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Abstractions.Repositories;
using PokemonApi.Application.Features.Pokemon.Dtos;
using PokemonApi.Domain.Common;
using PokemonApi.Domain.ValueObjects;

namespace PokemonApi.Application.Features.Pokemon.GetByName;

/// <summary>
/// Obtiene un Pokemon por su nombre canonico.
/// </summary>
/// <param name="repository">Repositorio del catalogo.</param>
public sealed class GetPokemonByNameQueryHandler(IPokemonRepository repository)
    : IRequestHandler<GetPokemonByNameQuery, Result<PokemonDetailResponse>>
{
    private readonly IPokemonRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<PokemonDetailResponse>> HandleAsync(
        GetPokemonByNameQuery request,
        CancellationToken cancellationToken)
    {
        // El nombre se normaliza a kebab-case para que las consultas del cliente
        // no dependan de como escribe la persona: "Mr. Mime" y "mr-mime" son
        // la misma peticion.
        var pokemon = await _repository
            .GetByNameAsync(Slug.Normalize(request.Name), cancellationToken)
            .ConfigureAwait(false);

        if (pokemon is null)
        {
            return Result<PokemonDetailResponse>.Failure(Error.NotFound(
                "pokemon.not_found",
                $"No Pokemon exists with the name '{request.Name}'."));
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
