using PokemonApi.Application.Common.Validation;

namespace PokemonApi.Application.Features.Pokemon.List;

/// <summary>
/// Valida los parametros de <see cref="ListPokemonQuery"/>.
/// </summary>
/// <remarks>
/// Los filtros de tipo, generacion, region, etc. se validan contra el catalogo
/// en el handler, porque alli se dispone del listado real de valores validos.
/// Aqui solo se comprueban las invariantes que no dependen de los datos.
/// </remarks>
public sealed class ListPokemonQueryValidator : IValidator<ListPokemonQuery>
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, string[]> Validate(ListPokemonQuery request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var failures = new Dictionary<string, string[]>(StringComparer.Ordinal);

        ValidateName(request, failures);
        ValidatePagination(request, failures);
        ValidateHeight(request, failures);
        ValidateWeight(request, failures);
        ValidateTotalStats(request, failures);
        ValidateStatThreshold(request, failures);

        return failures;
    }

    private static void ValidateName(ListPokemonQuery request, Dictionary<string, string[]> failures)
    {
        if (request.Name is null)
        {
            return;
        }

        if (ValidationRules.IsMissing(request.Name))
        {
            failures[nameof(request.Name)] = ["The name filter cannot be empty. Omit it to search by name prefix."];
        }
        else if (request.Name.Length > ListPokemonLimits.MaxSearchLength)
        {
            failures[nameof(request.Name)] =
                [$"The name filter must be at most {ListPokemonLimits.MaxSearchLength} characters long."];
        }
    }

    private static void ValidatePagination(ListPokemonQuery request, Dictionary<string, string[]> failures)
    {
        if (request.Page is < 1)
        {
            failures[nameof(request.Page)] = ["The page must be greater than or equal to 1."];
        }

        if (request.PageSize is <= 0)
        {
            failures[nameof(request.PageSize)] = ["The page size must be greater than zero."];
        }
        else if (request.PageSize > ListPokemonLimits.MaxPageSize)
        {
            failures[nameof(request.PageSize)] =
                [$"The page size must be at most {ListPokemonLimits.MaxPageSize}."];
        }
    }

    private static void ValidateHeight(ListPokemonQuery request, Dictionary<string, string[]> failures)
    {
        if (request.MinHeight is < 0)
        {
            failures[nameof(request.MinHeight)] = ["The minimum height cannot be negative."];
        }

        if (request.MaxHeight is <= 0)
        {
            failures[nameof(request.MaxHeight)] = ["The maximum height must be greater than zero."];
        }

        if (request.MinHeight.HasValue && request.MaxHeight.HasValue && request.MinHeight > request.MaxHeight)
        {
            failures[nameof(request.MinHeight)] = ["The minimum height cannot be greater than the maximum height."];
        }
    }

    private static void ValidateWeight(ListPokemonQuery request, Dictionary<string, string[]> failures)
    {
        if (request.MinWeight is < 0)
        {
            failures[nameof(request.MinWeight)] = ["The minimum weight cannot be negative."];
        }

        if (request.MaxWeight is <= 0)
        {
            failures[nameof(request.MaxWeight)] = ["The maximum weight must be greater than zero."];
        }

        if (request.MinWeight.HasValue && request.MaxWeight.HasValue && request.MinWeight > request.MaxWeight)
        {
            failures[nameof(request.MinWeight)] = ["The minimum weight cannot be greater than the maximum weight."];
        }
    }

    private static void ValidateTotalStats(ListPokemonQuery request, Dictionary<string, string[]> failures)
    {
        if (request.MinTotalStats is < 0)
        {
            failures[nameof(request.MinTotalStats)] = ["The minimum total stats cannot be negative."];
        }

        if (request.MaxTotalStats is < 0)
        {
            failures[nameof(request.MaxTotalStats)] = ["The maximum total stats cannot be negative."];
        }

        if (request.MinTotalStats.HasValue && request.MaxTotalStats.HasValue
            && request.MinTotalStats > request.MaxTotalStats)
        {
            failures[nameof(request.MinTotalStats)] =
                ["The minimum total stats cannot be greater than the maximum total stats."];
        }
    }

    private static void ValidateStatThreshold(ListPokemonQuery request, Dictionary<string, string[]> failures)
    {
        var hasStat = !string.IsNullOrWhiteSpace(request.MinStat);

        if (!hasStat)
        {
            if (request.MinStatValue.HasValue)
            {
                failures[nameof(request.MinStat)] = ["A minimum stat value requires a stat to be specified."];
            }

            return;
        }

        if (request.MinStatValue is null)
        {
            failures[nameof(request.MinStatValue)] = ["A minimum stat requires a value to be specified."];
        }
        else if (request.MinStatValue is < 0)
        {
            failures[nameof(request.MinStatValue)] = ["The minimum stat value cannot be negative."];
        }
    }
}
