using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Abstractions.Repositories;
using PokemonApi.Application.Common.Validation;
using PokemonApi.Application.Features.Pokemon.Dtos;
using PokemonApi.Domain.Common;

namespace PokemonApi.Application.Features.Pokemon.GetEvolutionChain;

/// <summary>
/// Peticion para obtener la cadena evolutiva completa de un Pokemon.
/// </summary>
/// <param name="Name">Slug canonico del nombre del Pokemon.</param>
public sealed record GetEvolutionChainQuery(string Name) : IRequest<Result<EvolutionChainResponse>>;

/// <summary>
/// Valida <see cref="GetEvolutionChainQuery"/>.
/// </summary>
public sealed class GetEvolutionChainQueryValidator : IValidator<GetEvolutionChainQuery>
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, string[]> Validate(GetEvolutionChainQuery request)
    {
        var failures = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (ValidationRules.IsMissing(request.Name))
        {
            failures[nameof(request.Name)] = ["The name is required."];
        }

        return failures;
    }
}

/// <summary>
/// Miembro de una cadena evolutiva: un Pokemon y su posicion dentro de ella.
/// </summary>
/// <param name="Id">Identificador de la Poke&#x27;dex del Pokemon.</param>
/// <param name="Name">Slug canonico del nombre.</param>
/// <param name="DisplayName">Nombre presentable.</param>
/// <param name="OfficialArtwork">URL de la ilustracion oficial.</param>
/// <param name="EvolutionOrder">
/// Posicion dentro de la cadena, empezando en 0 para la forma base.
/// </param>
public sealed record EvolutionChainMemberResponse(
    int Id,
    string Name,
    string DisplayName,
    string OfficialArtwork,
    int EvolutionOrder);

/// <summary>
/// Cadena evolutiva completa.
/// </summary>
/// <param name="ChainId">Identificador de la cadena.</param>
/// <param name="RootName">Slug de la forma base de la cadena.</param>
/// <param name="Members">Pokemon que componen la cadena, de la forma base a la final.</param>
public sealed record EvolutionChainResponse(
    int ChainId,
    string RootName,
    IReadOnlyList<EvolutionChainMemberResponse> Members);

/// <summary>
/// Obtiene la cadena evolutiva de un Pokemon.
/// </summary>
/// <param name="repository">Repositorio del catalogo.</param>
public sealed class GetEvolutionChainQueryHandler(IPokemonRepository repository)
    : IRequestHandler<GetEvolutionChainQuery, Result<EvolutionChainResponse>>
{
    private readonly IPokemonRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<EvolutionChainResponse>> HandleAsync(
        GetEvolutionChainQuery request,
        CancellationToken cancellationToken)
    {
        var pokemon = await _repository
            .GetByNameAsync(Domain.ValueObjects.Slug.Normalize(request.Name), cancellationToken)
            .ConfigureAwait(false);

        if (pokemon is null)
        {
            return Result<EvolutionChainResponse>.Failure(Error.NotFound(
                "pokemon.not_found",
                $"No Pokemon exists with the name '{request.Name}'."));
        }

        var chain = await _repository
            .GetEvolutionChainAsync(pokemon.EvolutionChainId, cancellationToken)
            .ConfigureAwait(false);

        if (chain.Count == 0)
        {
            // Un Pokemon sin cadena solo puede ocurrir si el dataset esta
            // desincronizado, y eso es un fallo tecnico, no un 404.
            return Result<EvolutionChainResponse>.Failure(Error.Unexpected(
                "evolution_chain.empty",
                $"Pokemon '{pokemon.Name.Value}' declares evolution chain {pokemon.EvolutionChainId}, " +
                "but that chain has no members."));
        }

        // La posicion dentro de la cadena se deduce de la profundidad de cada
        // eslabon, no del identificador, porque las evoluciones sideways
        // (Slowpoke -> Slowking) no siguen el orden de la Poke'dex.
        var root = chain.FirstOrDefault(p => p.EvolvesFrom is null) ?? chain[0];
        var orderById = BuildEvolutionOrder(chain, root);

        var members = chain
            .OrderBy(p => orderById.GetValueOrDefault(p.Id))
            .ThenBy(p => p.Id)
            .Select(p => new EvolutionChainMemberResponse(
                p.Id,
                p.Name.Value,
                p.DisplayName,
                p.Sprites.OfficialArtwork.ToString(),
                orderById.GetValueOrDefault(p.Id)))
            .ToArray();

        return Result<EvolutionChainResponse>.Success(
            new EvolutionChainResponse(pokemon.EvolutionChainId, root.Name.Value, members));
    }

    /// <summary>
    /// Calcula la profundidad de cada miembro de la cadena mediante un recorrido
    /// en anchura desde la forma base.
    /// </summary>
    /// <param name="chain">Miembros de la cadena.</param>
    /// <param name="baseForm">Miembro sin antecesor, desde el que arranca el recorrido.</param>
    /// <returns>Profundidad de cada Pokemon, indexada por identificador.</returns>
    private static Dictionary<int, int> BuildEvolutionOrder(
        IReadOnlyList<Domain.Entities.Pokemon> chain,
        Domain.Entities.Pokemon baseForm)
    {
        var order = new Dictionary<int, int>();
        var depth = new Dictionary<string, int>(StringComparer.Ordinal);

        // La forma base es el unico miembro sin antecesor dentro de la cadena. No
        // se parte de chain[0] porque el calculo de profundidad debe depender de
        // la estructura de la cadena y no del orden en que la entregue el
        // repositorio: si ese orden cambiara, todas las profundidades colapsarian
        // a cero.
        var frontier = new Queue<string>();
        frontier.Enqueue(baseForm.Name.Value);
        depth[baseForm.Name.Value] = 0;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            var currentDepth = depth[current];

            foreach (var next in chain
                         .Where(p => p.EvolvesFrom?.Value == current)
                         .Select(p => p.Name.Value)
                         .Distinct(StringComparer.Ordinal))
            {
                if (depth.ContainsKey(next))
                {
                    continue;
                }

                depth[next] = currentDepth + 1;
                frontier.Enqueue(next);
            }
        }

        foreach (var pokemon in chain)
        {
            order[pokemon.Id] = depth.GetValueOrDefault(pokemon.Name.Value);
        }

        return order;
    }
}
