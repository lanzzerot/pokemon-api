using PokemonApi.Domain.Enumerations;
using PokemonApi.Domain.ValueObjects;

namespace PokemonApi.Domain.Entities;

/// <summary>
/// Generacion de Pokemon junto con la region a la que pertenece.
/// </summary>
/// <remarks>
/// Se declara con un constructor explicito en lugar de usar el constructor
/// primario de los <c>record</c> porque un parametro llamado <c>Slug</c>
/// ocultaria el tipo <see cref="Slug"/> en las firmas siguientes.
/// </remarks>
public sealed record Generation
{
    /// <summary>Inicializa una generacion del catalogo.</summary>
    /// <param name="id">Identificador de la generacion.</param>
    /// <param name="slug">Slug canonico (p. ej. <c>generation-i</c>).</param>
    /// <param name="name">Nombre presentable (p. ej. <c>Generation I</c>).</param>
    /// <param name="region">Slug de la region (p. ej. <c>kanto</c>).</param>
    /// <param name="regionName">Nombre presentable de la region (p. ej. <c>Kanto</c>).</param>
    /// <param name="pokemonCount">Numero de Pokemon de la generacion presentes en el catalogo.</param>
    public Generation(
        int id,
        Slug slug,
        string name,
        Slug region,
        string regionName,
        int pokemonCount)
    {
        Id = id;
        Slug = slug;
        Name = name;
        Region = region;
        RegionName = regionName;
        PokemonCount = pokemonCount;
    }

    /// <summary>Identificador de la generacion.</summary>
    public int Id { get; }

    /// <summary>Slug canonico, usado en las URLs de la API.</summary>
    public Slug Slug { get; }

    /// <summary>Nombre presentable (p. ej. <c>Generation I</c>).</summary>
    public string Name { get; }

    /// <summary>Slug de la region (p. ej. <c>kanto</c>).</summary>
    public Slug Region { get; }

    /// <summary>Nombre presentable de la region (p. ej. <c>Kanto</c>).</summary>
    public string RegionName { get; }

    /// <summary>Numero de Pokemon de la generacion presentes en el catalogo.</summary>
    public int PokemonCount { get; }

    /// <summary>Generacion a la que corresponde el identificador.</summary>
    /// <returns>La generacion como enumeracion del dominio.</returns>
    public PokemonGeneration ToEnum() => (PokemonGeneration)Id;
}
