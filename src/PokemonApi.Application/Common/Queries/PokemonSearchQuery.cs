using PokemonApi.Domain.Enumerations;

namespace PokemonApi.Application.Common.Queries;

/// <summary>
/// Criterio de ordenamiento de un listado de Pokemon.
/// </summary>
/// <param name="Field">Campo por el que ordenar.</param>
/// <param name="Direction">Sentido del ordenamiento.</param>
public sealed record PokemonSortBy(PokemonSortField Field, SortDirection Direction);

/// <summary>
/// Umbral minimo para una estadistica concreta.
/// </summary>
/// <param name="Stat">Estadistica evaluada.</param>
/// <param name="Value">Valor minimo, inclusive.</param>
public sealed record StatThreshold(PokemonStat Stat, int Value);

/// <summary>
/// Criterios de busqueda sobre el catalogo de Pokemon.
/// </summary>
/// <remarks>
/// <para>
/// Se modela como un objeto inmutable con semantica de "AND" entre filtros: un
/// Pokemon debe cumplir todos los criterios indicados. Los valores de un mismo
/// campo se combinan con "OR", de modo que <c>type=fire,water</c> devuelve los
/// de fuego y los de agua.
/// </para>
/// <para>
/// Las propiedades anulables significan "sin restriction": un filtro que el
/// cliente no envia no debe reducir el conjunto de resultados.
/// </para>
/// </remarks>
/// <param name="Name">Fragmento del nombre. Coincidencia parcial, sin distinguir mayusculas.</param>
/// <param name="Types">Tipos que el Pokemon debe tener.</param>
/// <param name="Abilities">Habilidades que el Pokemon debe tener.</param>
/// <param name="Generations">Identificadores de las generaciones admitidas.</param>
/// <param name="Regions">Regiones de pertenencia admitidas.</param>
/// <param name="EggGroups">Grupos de huevo. El Pokemon debe pertenecer a alguno.</param>
/// <param name="Habitats">Habitats. El Pokemon debe pertenecer a alguno.</param>
/// <param name="Rarity">Rareza del Pokemon.</param>
/// <param name="MinHeight">Altura minima.</param>
/// <param name="MaxHeight">Altura maxima.</param>
/// <param name="MinWeight">Peso minimo.</param>
/// <param name="MaxWeight">Peso maximo.</param>
/// <param name="MinBaseExperience">Experiencia base minima.</param>
/// <param name="MinTotalStats">Suma de estadisticas minima.</param>
/// <param name="MaxTotalStats">Suma de estadisticas maxima.</param>
/// <param name="MinStat">Estadistica y valor minimo que debe alcanzar.</param>
/// <param name="SortBy">Criterio de ordenamiento. Si es <see langword="null"/> se usa el identificador.</param>
/// <param name="Page">Pagina solicitada, empezando en 1.</param>
/// <param name="PageSize">Cantidad de elementos por pagina.</param>
public sealed record PokemonSearchQuery(
    string? Name = null,
    IReadOnlyList<string>? Types = null,
    IReadOnlyList<string>? Abilities = null,
    IReadOnlyList<int>? Generations = null,
    IReadOnlyList<string>? Regions = null,
    IReadOnlyList<string>? EggGroups = null,
    IReadOnlyList<string>? Habitats = null,
    PokemonRarity? Rarity = null,
    decimal? MinHeight = null,
    decimal? MaxHeight = null,
    decimal? MinWeight = null,
    decimal? MaxWeight = null,
    int? MinBaseExperience = null,
    int? MinTotalStats = null,
    int? MaxTotalStats = null,
    StatThreshold? MinStat = null,
    PokemonSortBy? SortBy = null,
    int Page = 1,
    int PageSize = 20)
{
    /// <summary>Devuelve un filtro vacio con la paginacion indicada.</summary>
    /// <param name="page">Pagina solicitada.</param>
    /// <param name="pageSize">Cantidad de elementos por pagina.</param>
    /// <returns>Un filtro que no restringe el catalogo.</returns>
    public static PokemonSearchQuery Unfiltered(int page = 1, int pageSize = 20) =>
        new(SortBy: null, Page: page, PageSize: pageSize);
}
