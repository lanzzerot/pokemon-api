using System.Globalization;

namespace PokemonApi.Domain.ValueObjects;

/// <summary>
/// Magnitud fisica expresada en una unidad concreta.
/// </summary>
/// <remarks>
/// La fuente de datos almacena la altura en decimetros y el peso en
/// hectogramos, que son unidades poco intuitivas para quien consume la API.
/// Este value object conserva la convencion original y ofrece conversiones,
/// de modo que cada consumidor elige la presentacion que necesita.
/// </remarks>
public readonly record struct Measure : IComparable<Measure>
{
    private static readonly decimal DecimetresPerMetre = 10m;
    private static readonly decimal HectogramsPerKilogram = 10m;

    private Measure(decimal value, MeasureUnit unit) => (Value, Unit) = (value, unit);

    /// <summary>Magnitud medida, en la unidad indicada por <see cref="Unit"/>.</summary>
    public decimal Value { get; }

    /// <summary>Unidad en la que se expresa <see cref="Value"/>.</summary>
    public MeasureUnit Unit { get; }

    /// <summary>Crea una altura a partir de decimetros, la unidad de origen.</summary>
    /// <param name="decimetres">Altura en decimetros.</param>
    public static Measure FromDecimetres(int decimetres) => new(decimetres, MeasureUnit.Decimetre);

    /// <summary>Crea un peso a partir de hectogramos, la unidad de origen.</summary>
    /// <param name="hectograms">Peso en hectogramos.</param>
    public static Measure FromHectograms(int hectograms) => new(hectograms, MeasureUnit.Hectogram);

    /// <summary>
    /// Convierte la magnitud a otra unidad compatible.
    /// </summary>
    /// <param name="unit">Unidad de destino.</param>
    /// <returns>La magnitud expresada en <paramref name="unit"/>.</returns>
    /// <exception cref="ArgumentException">Si la unidad solicitada no es compatible.</exception>
    public Measure ConvertTo(MeasureUnit unit)
    {
        if (unit == Unit)
        {
            return this;
        }

        return (Unit, unit) switch
        {
            (MeasureUnit.Decimetre, MeasureUnit.Metre) => new(Value / DecimetresPerMetre, unit),
            (MeasureUnit.Metre, MeasureUnit.Decimetre) => new(Value * DecimetresPerMetre, unit),
            (MeasureUnit.Hectogram, MeasureUnit.Kilogram) => new(Value / HectogramsPerKilogram, unit),
            (MeasureUnit.Kilogram, MeasureUnit.Hectogram) => new(Value * HectogramsPerKilogram, unit),
            _ => throw new ArgumentException(
                $"Cannot convert {Unit} to {unit}.",
                nameof(unit)),
        };
    }

    /// <summary>Convierte la magnitud a la unidad predeterminada de su familia.</summary>
    /// <returns>Metros para alturas y kilogramos para pesos.</returns>
    public Measure ToConventionalUnit() => Unit is MeasureUnit.Decimetre or MeasureUnit.Metre
        ? ConvertTo(MeasureUnit.Metre)
        : ConvertTo(MeasureUnit.Kilogram);

    /// <summary>Compara dos magnitudes, conviertiendolas a una misma unidad.</summary>
    /// <param name="left">Primera magnitud.</param>
    /// <param name="right">Segunda magnitud.</param>
    /// <returns><see langword="true"/> si <paramref name="left"/> es menor.</returns>
    public static bool operator <(Measure left, Measure right) => left.CompareTo(right) < 0;

    /// <summary>Compara dos magnitudes, conviertiendolas a una misma unidad.</summary>
    /// <param name="left">Primera magnitud.</param>
    /// <param name="right">Segunda magnitud.</param>
    /// <returns><see langword="true"/> si <paramref name="left"/> es menor o igual.</returns>
    public static bool operator <=(Measure left, Measure right) => left.CompareTo(right) <= 0;

    /// <summary>Compara dos magnitudes, conviertiendolas a una misma unidad.</summary>
    /// <param name="left">Primera magnitud.</param>
    /// <param name="right">Segunda magnitud.</param>
    /// <returns><see langword="true"/> si <paramref name="left"/> es mayor.</returns>
    public static bool operator >(Measure left, Measure right) => left.CompareTo(right) > 0;

    /// <summary>Compara dos magnitudes, conviertiendolas a una misma unidad.</summary>
    /// <param name="left">Primera magnitud.</param>
    /// <param name="right">Segunda magnitud.</param>
    /// <returns><see langword="true"/> si <paramref name="left"/> es mayor o igual.</returns>
    public static bool operator >=(Measure left, Measure right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public int CompareTo(Measure other) => Value.CompareTo(other.ConvertTo(Unit).Value);

    /// <inheritdoc />
    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"{Value.ToString("0.##", CultureInfo.InvariantCulture)} {Unit.ToString().ToLowerInvariant()}");
}
