using Microsoft.AspNetCore.Mvc;
using PokemonApi.Api.Infrastructure;
using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Common.Pagination;
using PokemonApi.Application.Features.Pokemon.GetById;
using PokemonApi.Application.Features.Pokemon.GetByName;
using PokemonApi.Application.Features.Pokemon.GetEvolutionChain;
using PokemonApi.Application.Features.Pokemon.List;
using PokemonApi.Domain.Enumerations;

namespace PokemonApi.Api.Endpoints;

/// <summary>
/// Endpoints de consulta de Pokemon.
/// </summary>
/// <remarks>
/// Cada endpoint se limita a traducir HTTP a un caso de uso y el resultado a una
/// respuesta. Ninguno contiene reglas de negocio: si un filtro necesita saber si
/// "fire" es un tipo valido, esa pregunta la responde el caso de uso, no el
/// endpoint.
/// </remarks>
public static class PokemonEndpoints
{
    /// <summary>Prefijo comun de todos los endpoints de Pokemon.</summary>
    private const string RoutePrefix = "/api/v1/pokemon";

    /// <summary>Registra los endpoints de Pokemon.</summary>
    /// <param name="app">Aplicacion web.</param>
    public static void MapPokemonEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup(RoutePrefix)
            .WithTags("Pokemon")
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireRateLimiting(RateLimitPolicies.Default);

        MapList(group);
        MapById(group);
        MapByName(group);
        MapEvolutionChain(group);
    }

    private static void MapList(RouteGroupBuilder group) => group
        .MapGet("/", async (
            ISender sender,
            CancellationToken cancellationToken,
            HttpContext httpContext,
            [FromQuery(Name = "name")] string? name,
            [FromQuery(Name = "types")] string[]? types,
            [FromQuery(Name = "abilities")] string[]? abilities,
            [FromQuery(Name = "generation")] string? generation,
            [FromQuery(Name = "generations")] string[]? generations,
            [FromQuery(Name = "regions")] string[]? regions,
            [FromQuery(Name = "eggGroups")] string[]? eggGroups,
            [FromQuery(Name = "habitats")] string[]? habitats,
            [FromQuery(Name = "rarity")] string? rarity,
            [FromQuery(Name = "minHeight")] decimal? minHeight,
            [FromQuery(Name = "maxHeight")] decimal? maxHeight,
            [FromQuery(Name = "minWeight")] decimal? minWeight,
            [FromQuery(Name = "maxWeight")] decimal? maxWeight,
            [FromQuery(Name = "minBaseExperience")] int? minBaseExperience,
            [FromQuery(Name = "minTotalStats")] int? minTotalStats,
            [FromQuery(Name = "maxTotalStats")] int? maxTotalStats,
            [FromQuery(Name = "minStat")] string? minStat,
            [FromQuery(Name = "minStatValue")] int? minStatValue,
            [FromQuery(Name = "sortBy")] string? sortBy,
            [FromQuery(Name = "sortDirection")] string? sortDirection,
            [FromQuery(Name = "page")] int? page,
            [FromQuery(Name = "pageSize")] int? pageSize) =>
        {
            var query = new ListPokemonQuery(
                name,
                types,
                abilities,
                generation,
                generations,
                regions,
                eggGroups,
                habitats,
                rarity,
                minHeight,
                maxHeight,
                minWeight,
                maxWeight,
                minBaseExperience,
                minTotalStats,
                maxTotalStats,
                minStat,
                minStatValue,
                sortBy,
                sortDirection,
                page,
                pageSize);

            var result = await sender
                .SendAsync(query, cancellationToken)
                .ConfigureAwait(false);

            return ProblemFactory.FromResult(result, httpContext);
        })
        .WithName("ListPokemon")
        .WithSummary("Lista Pokemon con filtros, orden y paginacion.")
        .WithDescription(
            "Todos los filtros son opcionales y se combinan con AND entre campos distintos y con OR " +
            "dentro del mismo campo. Los filtros multiples se expresan repitiendo el parametro o " +
            "separando valores por comas: `types=fire&types=water` equivale a `types=fire,water`. " +
            "`generation` acepta una sola generacion y `generations` varias; ambas aceptan el numero " +
            "(`3`) o el slug (`generation-iii`). " +
            "Los valores de `rarity`, `minStat`, `sortBy` y `sortDirection` se interpretan " +
            "sin distinguir mayusculas, y `sortDirection` admite `asc` y `desc`. " +
            "Las alturas se devuelven y se filtran en metros, y los pesos en kilogramos. " +
            "Un valor de catalogo desconocido (tipo, habilidad, region, habitat o grupo de huevos) " +
            "se rechaza con 400 en lugar de ignorarse. " +
            "Una pagina mas alla del final devuelve 200 con `items` vacio y `hasNextPage` en false.")
        .Produces<PageResponse<PokemonApi.Application.Features.Pokemon.Dtos.PokemonSummaryResponse>>(
            StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

    private static void MapById(RouteGroupBuilder group) => group
        .MapGet("/{id:int}", async (
            int id,
            ISender sender,
            CancellationToken cancellationToken,
            HttpContext httpContext) =>
        {
            var result = await sender
                .SendAsync(new GetPokemonByIdQuery(id), cancellationToken)
                .ConfigureAwait(false);

            return ProblemFactory.FromResult(result, httpContext);
        })
        .WithName("GetPokemonById")
        .WithSummary("Obtiene un Pokemon por su identificador de la Poke'dex.")
        .Produces<Application.Features.Pokemon.Dtos.PokemonDetailResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

    private static void MapByName(RouteGroupBuilder group) => group
        .MapGet("/by-name/{name}", async (
            string name,
            ISender sender,
            CancellationToken cancellationToken,
            HttpContext httpContext) =>
        {
            var result = await sender
                .SendAsync(new GetPokemonByNameQuery(name), cancellationToken)
                .ConfigureAwait(false);

            return ProblemFactory.FromResult(result, httpContext);
        })
        .WithName("GetPokemonByName")
        .WithSummary("Obtiene un Pokemon por su nombre canonico.")
        .WithDescription(
            "Se expone en `/by-name/{name}` y no en `/{name}` a proposito: así el nombre nunca compite " +
            "con las rutas literales del grupo y no hay ambiguedad que resolver en tiempo de ejecucion. " +
            "La busqueda no distingue mayusculas de minusculas.")
        .Produces<Application.Features.Pokemon.Dtos.PokemonDetailResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

    private static void MapEvolutionChain(RouteGroupBuilder group) => group
        .MapGet("/evolution-chain/{name}", async (
            string name,
            ISender sender,
            CancellationToken cancellationToken,
            HttpContext httpContext) =>
        {
            var result = await sender
                .SendAsync(new GetEvolutionChainQuery(name), cancellationToken)
                .ConfigureAwait(false);

            return ProblemFactory.FromResult(result, httpContext);
        })
        .WithName("GetEvolutionChain")
        .WithSummary("Obtiene la cadena evolutiva completa de un Pokemon.")
        .WithDescription(
            "Los miembros se devuelven de la forma base a la evolucion final. El orden se calcula " +
            "recorriendo la cadena, no por identificador, para que las evoluciones cruzadas " +
            "(por ejemplo Slowpoke a Slowking) aparezcan en su lugar correcto.")
        .Produces<EvolutionChainResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);
}
