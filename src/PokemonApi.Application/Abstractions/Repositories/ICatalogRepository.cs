using PokemonApi.Domain.Entities;

namespace PokemonApi.Application.Abstractions.Repositories;

/// <summary>
/// Fuente de lectura de los catalogos auxiliares (generaciones, tipos,
/// habilidades, grupos de huevo).
/// </summary>
/// <remarks>
/// Se separa de <see cref="IPokemonRepository"/> porque los catalogos son
/// practicamente estaticos: se consultan por separado, se cachean de forma
/// independiente y no admiten filtros.
/// </remarks>
public interface ICatalogRepository
{
    /// <summary>Devuelve las generaciones presentes en el catalogo.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Las generaciones ordenadas por identificador.</returns>
    Task<IReadOnlyList<Generation>> GetGenerationsAsync(CancellationToken cancellationToken);

    /// <summary>Devuelve los tipos presentes en el catalogo.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Los tipos con su recuento de Pokemon.</returns>
    Task<IReadOnlyList<PokemonTypeInfo>> GetTypesAsync(CancellationToken cancellationToken);

    /// <summary>Devuelve las habilidades presentes en el catalogo.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Las habilidades con su recuento de Pokemon.</returns>
    Task<IReadOnlyList<AbilityInfo>> GetAbilitiesAsync(CancellationToken cancellationToken);

    /// <summary>Devuelve los grupos de huevo presentes en el catalogo.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Los grupos de huevo.</returns>
    Task<IReadOnlyList<EggGroupInfo>> GetEggGroupsAsync(CancellationToken cancellationToken);

    /// <summary>Devuelve los habitats presentes en el catalogo.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Los habitats, ordenados alfabeticamente.</returns>
    Task<IReadOnlyList<CatalogEntryInfo>> GetHabitatsAsync(CancellationToken cancellationToken);

    /// <summary>Devuelve las regiones presentes en el catalogo.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Las regiones, ordenadas alfabeticamente.</returns>
    Task<IReadOnlyList<CatalogEntryInfo>> GetRegionsAsync(CancellationToken cancellationToken);
}
