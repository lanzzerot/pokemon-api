using Microsoft.AspNetCore.Mvc;
using PokemonApi.Api.Infrastructure;
using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Features.Catalog.GetAbilities;
using PokemonApi.Application.Features.Catalog.GetGenerations;
using PokemonApi.Application.Features.Catalog.GetReferenceData;
using PokemonApi.Application.Features.Catalog.GetTypes;

namespace PokemonApi.Api.Endpoints;

/// <summary>
/// Endpoints de los catalogos de referencia.
/// </summary>
/// <remarks>
/// Son endpoints separados y no un unico <c>/metadata</c> porque cada catalogo se
/// consume de forma independiente: un cliente que solo necesita el mapa de tipos
/// no deberia descargar 367 habilidades para obtener el resto.
/// </remarks>
public static class CatalogEndpoints
{
    /// <summary>Prefijo comun de los endpoints de catalogo.</summary>
    private const string RoutePrefix = "/api/v1";

    /// <summary>Registra los endpoints de catalogo.</summary>
    /// <param name="app">Aplicacion web.</param>
    public static void MapCatalogEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup(RoutePrefix)
            .WithTags("Catalog")
            .RequireRateLimiting(RateLimitPolicies.Catalog)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        MapGenerations(group);
        MapTypes(group);
        MapAbilities(group);
        MapSimpleCatalogs(group);
    }

    private static void MapGenerations(RouteGroupBuilder group) => group
        .MapGet("/generations", async (ISender sender, CancellationToken cancellationToken, HttpContext httpContext) =>
        {
            var result = await sender
                .SendAsync(new GetGenerationsQuery(), cancellationToken)
                .ConfigureAwait(false);

            return ProblemFactory.FromResult(result, httpContext);
        })
        .WithName("ListGenerations")
        .WithSummary("Lista las generaciones presentes en el catalogo.")
        .WithDescription("Devuelve las nueve generaciones con su region y el numero de Pokemon de cada una.")
        .Produces<IReadOnlyList<GenerationResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

    private static void MapTypes(RouteGroupBuilder group) => group
        .MapGet("/types", async (ISender sender, CancellationToken cancellationToken, HttpContext httpContext) =>
        {
            var result = await sender
                .SendAsync(new GetTypesQuery(), cancellationToken)
                .ConfigureAwait(false);

            return ProblemFactory.FromResult(result, httpContext);
        })
        .WithName("ListTypes")
        .WithSummary("Lista los tipos de Pokemon presentes en el catalogo.")
        .WithDescription(
            "El recuento de cada tipo incluye tanto las formas base como las evoluciones, porque el " +
            "catalogo contiene Pokemon, no especies.")
        .Produces<IReadOnlyList<TypeResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

    private static void MapAbilities(RouteGroupBuilder group) => group
        .MapGet("/abilities", async (
            ISender sender,
            CancellationToken cancellationToken,
            HttpContext httpContext,
            [FromQuery(Name = "name")] string? name,
            [FromQuery(Name = "isMainSeries")] bool? isMainSeries) =>
        {
            var result = await sender
                .SendAsync(new GetAbilitiesQuery(name, isMainSeries), cancellationToken)
                .ConfigureAwait(false);

            return ProblemFactory.FromResult(result, httpContext);
        })
        .WithName("ListAbilities")
        .WithSummary("Lista las habilidades del catalogo, con filtro opcional.")
        .WithDescription(
            "El filtro `name` busca tanto en el nombre de la habilidad como en la descripcion de su " +
            "efecto, de modo que tambien sirve para encontrar habilidades por lo que hacen.")
        .Produces<IReadOnlyList<Application.Features.Catalog.GetAbilities.AbilityResponse>>(
            StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

    private static void MapSimpleCatalogs(RouteGroupBuilder group)
    {
        group
            .MapGet("/egg-groups", async (ISender sender, CancellationToken cancellationToken, HttpContext httpContext) =>
            {
                var result = await sender
                    .SendAsync(new GetEggGroupsQuery(), cancellationToken)
                    .ConfigureAwait(false);

                return ProblemFactory.FromResult(result, httpContext);
            })
            .WithName("ListEggGroups")
            .WithSummary("Lista los grupos de huevo del catalogo.")
            .Produces<IReadOnlyList<EggGroupResponse>>(StatusCodes.Status200OK);

        group
            .MapGet("/habitats", async (ISender sender, CancellationToken cancellationToken, HttpContext httpContext) =>
            {
                var result = await sender
                    .SendAsync(new GetHabitatsQuery(), cancellationToken)
                    .ConfigureAwait(false);

                return ProblemFactory.FromResult(result, httpContext);
            })
            .WithName("ListHabitats")
            .WithSummary("Lista los habitats naturales presentes en el catalogo.")
            .WithDescription("Cada habitat incluye cuantos Pokemon lo tienen asignado.")
            .Produces<IReadOnlyList<CatalogEntryResponse>>(StatusCodes.Status200OK);

        group
            .MapGet("/regions", async (ISender sender, CancellationToken cancellationToken, HttpContext httpContext) =>
            {
                var result = await sender
                    .SendAsync(new GetRegionsQuery(), cancellationToken)
                    .ConfigureAwait(false);

                return ProblemFactory.FromResult(result, httpContext);
            })
            .WithName("ListRegions")
            .WithSummary("Lista las regiones del catalogo.")
            .WithDescription("Las regiones se derivan de las generaciones a las que pertenecen.")
            .Produces<IReadOnlyList<CatalogEntryResponse>>(StatusCodes.Status200OK);
    }
}
