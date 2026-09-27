using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Common.Pagination;
using PokemonApi.Application.Common.Queries;
using PokemonApi.Application.Common.Validation;
using PokemonApi.Application.Features.Pokemon.Dtos;
using PokemonApi.Domain.Common;
using PokemonApi.Domain.Enumerations;

namespace PokemonApi.Application.Features.Pokemon.List;

/// <summary>
/// Peticion para listar Pokemon con filtros, orden y paginacion.
/// </summary>
/// <remarks>
/// Todos los filtros son opcionales. Un filtro multiplojo se expresa separado
/// por comas (<c>types=fire,water</c>) o repitiendo el parametro
/// (<c>types=fire&amp;types=water</c>), y se interpreta como "OR" dentro del
/// campo; entre campos distintos se combinan con "AND".
/// <para>
/// Los campos que Closed World admiten (rareza, estadistica, orden y sentido)
/// viajan como texto y se interpretan sin distinguir mayusculas: el framework
/// solo enlaza los <see langword="enum"/> respetando el caso exacto del nombre
/// del miembro, lo que obligaria al cliente a escribir
/// <c>?sortBy=TotalStats</c>. Aceptarlos como texto devuelve ademas un mensaje
/// de error que enumera los valores validos.
/// </para>
/// </remarks>
/// <param name="Name">Fragmento del nombre.</param>
/// <param name="Types">Tipos que el Pokemon debe tener.</param>
/// <param name="Abilities">Habilidades que el Pokemon debe tener.</param>
/// <param name="Generation">
/// Generacion de pertenencia. Acepta tanto el numero (<c>3</c>) como el slug
/// (<c>generation-iii</c>).
/// </param>
/// <param name="Generations">Generaciones de pertenencia admitidas, con el mismo formato.</param>
/// <param name="Regions">Regiones de pertenencia admitidas.</param>
/// <param name="EggGroups">Grupos de huevo.</param>
/// <param name="Habitats">Habitats naturales.</param>
/// <param name="Rarity">Rareza: <c>common</c>, <c>legendary</c> o <c>mythical</c>.</param>
/// <param name="MinHeight">Altura minima.</param>
/// <param name="MaxHeight">Altura maxima.</param>
/// <param name="MinWeight">Peso minimo.</param>
/// <param name="MaxWeight">Peso maximo.</param>
/// <param name="MinBaseExperience">Experiencia base minima.</param>
/// <param name="MinTotalStats">Suma de estadisticas minima.</param>
/// <param name="MaxTotalStats">Suma de estadisticas maxima.</param>
/// <param name="MinStat">
/// Estadistica que debe alcanzar un valor minimo: <c>hp</c>, <c>attack</c>,
/// <c>defense</c>, <c>specialAttack</c>, <c>specialDefense</c> o <c>speed</c>.
/// </param>
/// <param name="MinStatValue">Valor minimo de <paramref name="MinStat"/>.</param>
/// <param name="SortBy">
/// Campo de ordenamiento: <c>id</c>, <c>name</c>, <c>height</c>, <c>weight</c>,
/// <c>totalStats</c> o <c>baseExperience</c>.
/// </param>
/// <param name="SortDirection">
/// Sentido del ordenamiento: <c>asc</c> o <c>desc</c>. Tambien se aceptan los
/// nombres largos <c>ascending</c> y <c>descending</c>.
/// </param>
/// <param name="Page">Pagina solicitada.</param>
/// <param name="PageSize">Cantidad de elementos por pagina.</param>
public sealed record ListPokemonQuery(
    string? Name = null,
    string[]? Types = null,
    string[]? Abilities = null,
    string? Generation = null,
    string[]? Generations = null,
    string[]? Regions = null,
    string[]? EggGroups = null,
    string[]? Habitats = null,
    string? Rarity = null,
    decimal? MinHeight = null,
    decimal? MaxHeight = null,
    decimal? MinWeight = null,
    decimal? MaxWeight = null,
    int? MinBaseExperience = null,
    int? MinTotalStats = null,
    int? MaxTotalStats = null,
    string? MinStat = null,
    int? MinStatValue = null,
    string? SortBy = null,
    string? SortDirection = null,
    int? Page = null,
    int? PageSize = null)
    : IRequest<Result<PageResponse<PokemonSummaryResponse>>>;

/// <summary>
/// Limites de paginacion aplicados a <see cref="ListPokemonQuery"/>.
/// </summary>
public static class ListPokemonLimits
{
    /// <summary>Numero de pagina por defecto.</summary>
    public const int DefaultPage = 1;

    /// <summary>Cantidad de elementos por pagina cuando el cliente no indica.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Tope maximo de elementos por pagina, decidido por el servidor.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Longitud maxima del fragmento de busqueda por nombre.</summary>
    public const int MaxSearchLength = 60;

    /// <summary>Altura maxima admitida en metros.</summary>
    public const decimal MaxHeightInMetres = 200m;

    /// <summary>Peso maximo admitido en kilogramos.</summary>
    public const decimal MaxWeightInKilograms = 1000m;
}
