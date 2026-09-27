using PokemonApi.Application.Abstractions.Repositories;
using PokemonApi.Application.Common.Pagination;
using PokemonApi.Application.Common.Queries;
using PokemonApi.Domain.Entities;
using PokemonApi.Domain.Enumerations;
using PokemonApi.Domain.ValueObjects;
using PokemonApi.Infrastructure.Data;

namespace PokemonApi.Infrastructure.Persistence;

/// <summary>
/// Implementacion de <see cref="IPokemonRepository"/> sobre la instantanea en
/// memoria.
/// </summary>
/// <remarks>
/// <para>
/// No mantiene estado: todo lo que necesita ya esta materializado en
/// <see cref="PokemonDataSet"/>. Se registra como singleton, igual que la
/// instantanea, porque no hay nada por-request que construir.
/// </para>
/// <para>
/// Los metodos son <c>async</c> sin <c>await</c> porque la interfaz se define
/// asi para no atar los casos de uso a esta decision: si manana el catalogo pasa
/// a vivir en Postgres, los handlers no cambian una sola linea. No es una
/// optimizacion prematura sino una decision de frontera, y el coste es
/// despreciable porque la tarea ya esta completada al devolverse.
/// </para>
/// </remarks>
public sealed class InMemoryPokemonRepository(PokemonDataSet dataSet) : IPokemonRepository
{
    private readonly PokemonDataSet _dataSet = dataSet;

    /// <inheritdoc />
    public Task<Pokemon?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_dataSet.FindById(id));
    }

    /// <inheritdoc />
    public Task<Pokemon?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_dataSet.FindBySlug(name));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Pokemon>> GetEvolutionChainAsync(
        int chainId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_dataSet.FindEvolutionChain(chainId));
    }

    /// <inheritdoc />
    public Task<PagedResult<Pokemon>> SearchAsync(
        PokemonSearchQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        // Los criterios se precalculan a HashSet una sola vez por peticion: sin
        // ello, cada elemento recorreria los filtros buscando en un array, y el
        // coste creceria con el producto del numero de filtros por el de valores.
        var typeFilter = LookupSet(query.Types);
        var abilityFilter = LookupSet(query.Abilities);
        var regionFilter = LookupSet(query.Regions);
        var eggGroupFilter = LookupSet(query.EggGroups);
        var habitatFilter = LookupSet(query.Habitats);
        var generationFilter = query.Generations is { Count: > 0 } generations
            ? generations.ToHashSet()
            : null;

        IEnumerable<Pokemon> results = _dataSet.OrderedById();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            results = results.Where(pokemon => pokemon.Matches(query.Name));
        }

        if (typeFilter is not null)
        {
            results = results.Where(pokemon => pokemon.Types.Any(type => typeFilter.Contains(type.Value)));
        }

        if (abilityFilter is not null)
        {
            results = results.Where(pokemon =>
                pokemon.Abilities.Any(ability => abilityFilter.Contains(ability.Name.Value)));
        }

        if (generationFilter is not null)
        {
            results = results.Where(pokemon => generationFilter.Contains((int)pokemon.Generation));
        }

        if (regionFilter is not null)
        {
            results = results.Where(pokemon =>
                pokemon.Region is { } region && regionFilter.Contains(region.Value));
        }

        if (eggGroupFilter is not null)
        {
            results = results.Where(pokemon =>
                pokemon.EggGroups.Any(group => eggGroupFilter.Contains(group.Value)));
        }

        if (habitatFilter is not null)
        {
            results = results.Where(pokemon =>
                pokemon.Habitat is { } habitat && habitatFilter.Contains(habitat.Value));
        }

        if (query.Rarity is { } rarity)
        {
            results = results.Where(pokemon => rarity switch
            {
                PokemonRarity.Common => pokemon.IsCommon,
                PokemonRarity.Legendary => pokemon.IsLegendary,
                PokemonRarity.Mythical => pokemon.IsMythical,
                _ => false,
            });
        }

        // El filtro viaja ya convertido a las unidades del dataset: el constructor
        // de la consulta multiplica por 10 los metros y kilogramos que pide el
        // cliente. Comparar aqui contra los valores en metros o kilogramos
        // aplicaria una segunda conversion y descartaria todos los resultados
        // (una altura de 15 decimetros nunca es >= 15 metros).
        if (query.MinHeight is { } minHeight)
        {
            results = results.Where(pokemon => pokemon.Height.Value >= minHeight);
        }

        if (query.MaxHeight is { } maxHeight)
        {
            results = results.Where(pokemon => pokemon.Height.Value <= maxHeight);
        }

        if (query.MinWeight is { } minWeight)
        {
            results = results.Where(pokemon => pokemon.Weight.Value >= minWeight);
        }

        if (query.MaxWeight is { } maxWeight)
        {
            results = results.Where(pokemon => pokemon.Weight.Value <= maxWeight);
        }

        if (query.MinBaseExperience is { } minExperience)
        {
            results = results.Where(pokemon => pokemon.BaseExperience >= minExperience);
        }

        if (query.MinTotalStats is { } minTotal)
        {
            results = results.Where(pokemon => pokemon.Stats.Total >= minTotal);
        }

        if (query.MaxTotalStats is { } maxTotal)
        {
            results = results.Where(pokemon => pokemon.Stats.Total <= maxTotal);
        }

        if (query.MinStat is { } threshold)
        {
            results = results.Where(pokemon => pokemon.Stats[threshold.Stat] >= threshold.Value);
        }

        var page = PagedResult<Pokemon>.Create(
            Sort(results, query.SortBy),
            query.Page,
            query.PageSize);

        return Task.FromResult(page);
    }

    /// <summary>
    /// Ordena la secuencia con el criterio pedido, appendiendo siempre el
    /// identificador como desempate.
    /// </summary>
    /// <remarks>
    /// El desempate no es decorativo: sin el, dos Pokemon con la misma
    /// estadistica pueden cambiar de posicion entre peticiones y la paginacion
    /// devolveria elementos repetidos o los saltaria. Con el, el orden es total y
    /// por tanto reproducible.
    /// </remarks>
    private static IEnumerable<Pokemon> Sort(IEnumerable<Pokemon> source, PokemonSortBy? sortBy)
    {
        if (sortBy is null)
        {
            return source;
        }

        var comparer = Comparer<Pokemon>.Create((left, right) =>
        {
            var comparison = sortBy.Field switch
            {
                PokemonSortField.Name => string.CompareOrdinal(left.Name.Value, right.Name.Value),
                PokemonSortField.Height => left.Height.CompareTo(right.Height),
                PokemonSortField.Weight => left.Weight.CompareTo(right.Weight),
                PokemonSortField.TotalStats => left.Stats.Total.CompareTo(right.Stats.Total),
                PokemonSortField.BaseExperience =>
                    SortKey(left).CompareTo(SortKey(right)),
                _ => left.Id.CompareTo(right.Id),
            };

            return sortBy.Direction == SortDirection.Descending
                ? -comparison
                : comparison;
        });

        return source.OrderBy(pokemon => pokemon, comparer);
    }

    /// <summary>
    /// Valor de ordenacion de la experiencia base. Los Pokemon sin experiencia
    /// conocida se colocan por debajo de cualquier valor real en lugar de
    /// tratarse como cero, para que no se mezclen con los que tienen una
    /// experiencia baja de verdad.
    /// </summary>
    private static int SortKey(Pokemon pokemon) => pokemon.BaseExperience ?? -1;

    /// <summary>
    /// Prepara un conjunto de valores para busquedas de complejidad constante, o
    /// <see langword="null"/> si el filtro no restringe.
    /// </summary>
    /// <remarks>
    /// Devolver <see langword="null"/> en lugar de un conjunto vacio permite
    /// distinguir "no se filtro por este campo" de "se filtro y no coincidio
    /// nada", que en este modelo se combinan con AND.
    /// </remarks>
    private static HashSet<string>? LookupSet(IReadOnlyList<string>? values) =>
        values is { Count: > 0 } ? new HashSet<string>(values, StringComparer.Ordinal) : null;
}
