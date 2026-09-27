namespace PokemonApi.Domain.ValueObjects;

/// <summary>
/// Unidades en las que puede expresarse una magnitud fisica de un Pokemon.
/// </summary>
public enum MeasureUnit
{
    /// <summary>Decimetros. Unidad en la que la fuente de datos almacena la altura.</summary>
    Decimetre = 0,

    /// <summary>Metros.</summary>
    Metre = 1,

    /// <summary>Hectogramos. Unidad en la que la fuente de datos almacena el peso.</summary>
    Hectogram = 2,

    /// <summary>Kilogramos.</summary>
    Kilogram = 3,
}
