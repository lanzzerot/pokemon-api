using System.Globalization;
using PokemonApi.Domain.Enumerations;

namespace PokemonApi.Application.Common;

/// <summary>
/// Conversiones entre <see cref="PokemonGeneration"/> y su representacion
/// textual.
/// </summary>
public static class PokemonGenerationExtensions
{
    /// <summary>
    /// Devuelve el slug de la generacion tal y como aparece en las URLs de la
    /// API: <c>generation-i</c>, <c>generation-ii</c>, ..., <c>generation-ix</c>.
    /// </summary>
    /// <param name="generation">Generacion del dominio.</param>
    /// <returns>El slug de la generacion.</returns>
    public static string ToSlug(this PokemonGeneration generation) =>
        $"generation-{ToRomanNumeral((int)generation)}";

    /// <summary>
    /// Intenta convertir un texto en una generacion, aceptando tanto el numero
    /// (<c>3</c>) como el slug (<c>generation-iii</c>).
    /// </summary>
    /// <param name="value">Texto de origen.</param>
    /// <param name="generation">Generacion interpretada.</param>
    /// <returns><see langword="true"/> si el texto corresponde a una generacion valida.</returns>
    public static bool TryParse(string? value, out PokemonGeneration generation)
    {
        generation = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var text = value.Trim();

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            && Enum.IsDefined((PokemonGeneration)id))
        {
            generation = (PokemonGeneration)id;
            return true;
        }

        var normalized = text.ToLowerInvariant().Replace("_", "-", StringComparison.Ordinal);

        foreach (var candidate in Enum.GetValues<PokemonGeneration>())
        {
            if (string.Equals(candidate.ToSlug(), normalized, StringComparison.Ordinal))
            {
                generation = candidate;
                return true;
            }
        }

        return false;
    }

    private static string ToRomanNumeral(int value) => value switch
    {
        1 => "i",
        2 => "ii",
        3 => "iii",
        4 => "iv",
        5 => "v",
        6 => "vi",
        7 => "vii",
        8 => "viii",
        9 => "ix",
        _ => value.ToString(CultureInfo.InvariantCulture),
    };
}
