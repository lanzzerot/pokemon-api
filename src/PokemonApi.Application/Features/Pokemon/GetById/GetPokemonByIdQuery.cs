using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Common.Validation;
using PokemonApi.Application.Features.Pokemon.Dtos;
using PokemonApi.Domain.Common;

namespace PokemonApi.Application.Features.Pokemon.GetById;

/// <summary>
/// Peticion para obtener un Pokemon por su identificador de la Poke&#x27;dex.
/// </summary>
/// <param name="Id">Identificador de la Poke&#x27;dex.</param>
public sealed record GetPokemonByIdQuery(int Id) : IRequest<Result<PokemonDetailResponse>>;

/// <summary>
/// Valida <see cref="GetPokemonByIdQuery"/>.
/// </summary>
public sealed class GetPokemonByIdQueryValidator : IValidator<GetPokemonByIdQuery>
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, string[]> Validate(GetPokemonByIdQuery request)
    {
        var failures = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (ValidationRules.IsOutOfRange(request.Id, 1, int.MaxValue))
        {
            failures[nameof(request.Id)] = ["The id must be greater than zero."];
        }

        return failures;
    }
}
