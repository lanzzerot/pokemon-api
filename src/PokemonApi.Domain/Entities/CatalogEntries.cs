using PokemonApi.Domain.ValueObjects;

namespace PokemonApi.Domain.Entities;

/// <summary>
/// Tipo de Pokemon (Fuego, Agua, Electrico...).
/// </summary>
/// <param name="Id">Identificador del tipo.</param>
/// <param name="Slug">Slug canonico del tipo.</param>
/// <param name="Name">Nombre presentable del tipo.</param>
/// <param name="PokemonCount">Numero de Pokemon de este tipo en el catalogo.</param>
public sealed record PokemonTypeInfo(
    int Id,
    Slug Slug,
    string Name,
    int PokemonCount);

/// <summary>
/// Habilidad del catalogo, con su descripcion resumida.
/// </summary>
/// <param name="Id">Identificador de la habilidad.</param>
/// <param name="Slug">Slug canonico de la habilidad.</param>
/// <param name="Name">Nombre presentable de la habilidad.</param>
/// <param name="IsMainSeries">Si la habilidad pertenece a la serie principal de juegos.</param>
/// <param name="ShortEffect">Descripcion resumida de su efecto.</param>
/// <param name="PokemonCount">Numero de Pokemon que poseen esta habilidad en el catalogo.</param>
public sealed record AbilityInfo(
    int Id,
    Slug Slug,
    string Name,
    bool IsMainSeries,
    string? ShortEffect,
    int PokemonCount);

/// <summary>
/// Grupo de huevo: la categoria de compatibilidad reproductiva de una especie.
/// </summary>
/// <param name="Id">Identificador del grupo.</param>
/// <param name="Slug">Slug canonico del grupo.</param>
/// <param name="Name">Nombre presentable del grupo.</param>
public sealed record EggGroupInfo(
    int Id,
    Slug Slug,
    string Name);

/// <summary>
/// Entrada de un catalogo simple (habitats, regiones, formas, colores), sin
/// metadatos adicionales.
/// </summary>
/// <param name="Id">Identificador de la entrada.</param>
/// <param name="Slug">Slug canonico de la entrada.</param>
/// <param name="Name">Nombre presentable de la entrada.</param>
/// <param name="PokemonCount">Numero de Pokemon asociados a la entrada.</param>
public sealed record CatalogEntryInfo(
    int Id,
    Slug Slug,
    string Name,
    int PokemonCount);
