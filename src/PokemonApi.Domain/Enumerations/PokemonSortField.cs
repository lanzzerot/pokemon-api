namespace PokemonApi.Domain.Enumerations;

/// <summary>
/// Campo por el que se puede ordenar un listado de Pokemon.
/// </summary>
public enum PokemonSortField
{
    /// <summary>Identificador de la Pokédex nacional (orden estable por defecto).</summary>
    Id = 1,

    /// <summary>Nombre alfabetico.</summary>
    Name = 2,

    /// <summary>Altura.</summary>
    Height = 3,

    /// <summary>Peso.</summary>
    Weight = 4,

    /// <summary>Suma de las seis estadisticas base.</summary>
    TotalStats = 5,

    /// <summary>Experiencia base concedida al ser capturado.</summary>
    BaseExperience = 6,
}

/// <summary>
/// Sentido del ordenamiento.
/// </summary>
public enum SortDirection
{
    /// <summary>Orden ascendente.</summary>
    Ascending = 1,

    /// <summary>Orden descendente.</summary>
    Descending = 2,
}
