using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using PokemonApi.Domain.Common;

namespace PokemonApi.Domain.ValueObjects;

/// <summary>
/// Identificador de texto canonico empleado por el dominio para tipos,
/// habilidades, grupos de huevo, habitats, colores y formas.
/// </summary>
/// <remarks>
/// <para>
/// La fuente de datos utiliza slugs en kebab-case (<c>special-attack</c>,
/// <c>thunder-stone</c>). Centralizar esa normalizacion en un value object
/// evita comparaciones por cadenas sueltas repartidas por el codigo y permite
/// rejechar entradas invalidas en el limite del sistema.
/// </para>
/// <para>
/// Es un <c>record struct</c>: no tiene identidad propia, dos slugs con el
/// mismo valor son el mismo valor, y su implementacion dentro de una entidad
/// se compara por su contenido.
/// </para>
/// </remarks>
public readonly partial record struct Slug : IComparable<Slug>
{
    /// <summary>Longitud maxima admitida para un slug.</summary>
    public const int MaxLength = 60;

    private Slug(string value) => Value = value;

    /// <summary>Valor canonico del slug, siempre en kebab-case.</summary>
    public string Value { get; }

    /// <summary>
    /// Crea un slug a partir de un valor arbitrario, normalizandolo.
    /// </summary>
    /// <param name="value">Texto de origen (se normaliza a minusculas y guiones simples).</param>
    /// <returns>El slug canonico, o un error de validacion si queda vacio.</returns>
    public static Result<Slug> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<Slug>.Failure(Error.Validation(
                "slug.empty",
                "The identifier must be a non-empty string."));
        }

        var normalized = Normalize(value);
        if (normalized.Length == 0)
        {
            // Tras normalizar, una entrada como "???" o "--" se queda sin
            // ningun caracter util: aceptarla produciria un slug vacio que se
            // propagaria al resto del dominio como un identificador valido.
            return Result<Slug>.Failure(Error.Validation(
                "slug.empty",
                "The identifier must contain at least one letter or digit."));
        }

        if (normalized.Length > MaxLength)
        {
            return Result<Slug>.Failure(Error.Validation(
                "slug.too_long",
                $"The identifier must be at most {MaxLength} characters long."));
        }

        return Result<Slug>.Success(new Slug(normalized));
    }

    /// <summary>
    /// Intenta crear un slug sin propagar el resultado como excepcion.
    /// </summary>
    /// <param name="value">Texto de origen.</param>
    /// <param name="slug">Slug canonico, o <see langword="default"/> si la entrada no es valida.</param>
    /// <returns><see langword="true"/> si el valor de origen es valido.</returns>
    public static bool TryCreate(string? value, out Slug slug)
    {
        var result = Create(value);
        slug = result.TryGetValue(out var created) ? created : default;
        return result.IsSuccess;
    }

    /// <summary>
    /// Normaliza un texto arbitrario a kebab-case: minusculas, espacios y
    /// separadores convertidos en guiones simples.
    /// </summary>
    /// <param name="value">Texto de origen.</param>
    /// <returns>Texto normalizado; puede quedar vacio si la entrada solo contenia separadores.</returns>
    public static string Normalize(string value) => NonConsecutiveSeparators()
        .Replace(value.Trim().ToLowerInvariant(), "-")
        .Trim('-');

    /// <inheritdoc />
    public int CompareTo(Slug other) => string.CompareOrdinal(Value, other.Value);

    /// <summary>Compara dos slugs por su valor canonico.</summary>
    /// <param name="left">Primer slug.</param>
    /// <param name="right">Segundo slug.</param>
    /// <returns><see langword="true"/> si <paramref name="left"/> es menor.</returns>
    public static bool operator <(Slug left, Slug right) => left.CompareTo(right) < 0;

    /// <summary>Compara dos slugs por su valor canonico.</summary>
    /// <param name="left">Primer slug.</param>
    /// <param name="right">Segundo slug.</param>
    /// <returns><see langword="true"/> si <paramref name="left"/> es menor o igual.</returns>
    public static bool operator <=(Slug left, Slug right) => left.CompareTo(right) <= 0;

    /// <summary>Compara dos slugs por su valor canonico.</summary>
    /// <param name="left">Primer slug.</param>
    /// <param name="right">Segundo slug.</param>
    /// <returns><see langword="true"/> si <paramref name="left"/> es mayor.</returns>
    public static bool operator >(Slug left, Slug right) => left.CompareTo(right) > 0;

    /// <summary>Compara dos slugs por su valor canonico.</summary>
    /// <param name="left">Primer slug.</param>
    /// <param name="right">Segundo slug.</param>
    /// <returns><see langword="true"/> si <paramref name="left"/> es mayor o igual.</returns>
    public static bool operator >=(Slug left, Slug right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Permite usar el valor del slug donde se espera un <see cref="string"/>.</summary>
    /// <param name="slug">Slug de origen.</param>
    public static implicit operator string(Slug slug) => slug.Value;

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonConsecutiveSeparators();
}
