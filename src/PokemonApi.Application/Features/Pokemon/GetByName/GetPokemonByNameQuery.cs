using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Common.Validation;
using PokemonApi.Application.Features.Pokemon.Dtos;
using PokemonApi.Domain.Common;

namespace PokemonApi.Application.Features.Pokemon.GetByName;

/// <summary>
/// Peticion para obtener un Pokemon por su nombre canonico.
/// </summary>
/// <param name="Name">
/// Nombre del Pokemon. Se normaliza antes de buscar, de modo que
/// <c>Pikachu</c>, <c>PIKACHU</c> y <c>Mr. Mime</c> funcionan igual que sus
/// formas canonicas.
/// </param>
public sealed record GetPokemonByNameQuery(string Name) : IRequest<Result<PokemonDetailResponse>>;

/// <summary>
/// Valida <see cref="GetPokemonByNameQuery"/>.
/// </summary>
public sealed class GetPokemonByNameQueryValidator : IValidator<GetPokemonByNameQuery>
{
    /// <summary>Longitud maxima admitida para un nombre.</summary>
    public const int MaxNameLength = 60;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string[]> Validate(GetPokemonByNameQuery request)
    {
        var failures = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (ValidationRules.IsMissing(request.Name))
        {
            failures[nameof(request.Name)] = ["The name is required."];
        }
        else if (request.Name.Length > MaxNameLength)
        {
            failures[nameof(request.Name)] = [$"The name must be at most {MaxNameLength} characters long."];
        }

        return failures;
    }
}
