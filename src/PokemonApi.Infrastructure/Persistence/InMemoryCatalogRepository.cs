using PokemonApi.Application.Abstractions.Repositories;
using PokemonApi.Domain.Entities;
using PokemonApi.Infrastructure.Data;

namespace PokemonApi.Infrastructure.Persistence;

/// <summary>
/// Implementacion de <see cref="ICatalogRepository"/> sobre la instantanea en
/// memoria.
/// </summary>
/// <remarks>
/// Los catalogos son inmutables durante toda la vida del proceso, asi que las
/// listas se devuelven tal cual, sin copia: un endpoint de catalogo no debe
/// pagar una asignacion por peticion para repetir exactamente los mismos datos.
/// La inmutabilidad del dominio es lo que hace que compartir las referencias sea
/// seguro.
/// </remarks>
/// <param name="dataSet">Instantanea del catalogo.</param>
public sealed class InMemoryCatalogRepository(PokemonDataSet dataSet) : ICatalogRepository
{
    private readonly PokemonDataSet _dataSet = dataSet;

    /// <inheritdoc />
    public Task<IReadOnlyList<Generation>> GetGenerationsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_dataSet.Generations);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PokemonTypeInfo>> GetTypesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_dataSet.Types);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AbilityInfo>> GetAbilitiesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_dataSet.Abilities);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<EggGroupInfo>> GetEggGroupsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_dataSet.EggGroups);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CatalogEntryInfo>> GetHabitatsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_dataSet.Habitats);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CatalogEntryInfo>> GetRegionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_dataSet.Regions);
    }
}
