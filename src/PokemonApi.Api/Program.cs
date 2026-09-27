using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using PokemonApi.Api.Endpoints;
using PokemonApi.Api.Infrastructure;
using PokemonApi.Application;
using PokemonApi.Application.Common.Validation;
using PokemonApi.Infrastructure;
using PokemonApi.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Composicion de dependencias.
//
// El orden importa: primero las capas de abajo (infraestructura y aplicacion),
// despues las concerns transversales de HTTP, y por ultimo los endpoints.
// ---------------------------------------------------------------------------

builder.Services.AddInfrastructure();
builder.Services.AddApplication();
builder.Services.AddApiOpenApi();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        // El traceId es lo que permite correlacionar el 400 que ve el cliente con
        // la linea exacta del log del servidor.
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        if (context.Exception is ValidationException validation)
        {
            context.ProblemDetails.Extensions["errors"] = validation.Failures;
        }
    };
});

// El framework evalua los manejadores de excepcion en orden INVERSO al de
// registro, asi que el mas especifico se declara el ultimo. Este orden produce
// BindFailure -> Validation -> Unexpected, que es la cadena de responsabilidad
// buscada: del error mas cercano al cliente al mas generico.
builder.Services.AddExceptionHandler<UnexpectedExceptionHandler>();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<MalformedRequestExceptionHandler>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        // Se anuncia la duracion de la ventana configurada, que es exactamente
        // lo que tardara el limitador en reponer el cupo. Es el mismo valor que
        // usan las dos politicas, asi que nunca se anuncia un plazo que no se
        // cumple.
        var retryAfter = RateLimitPolicies.Window;
        var retryAfterSeconds = (int)retryAfter.TotalSeconds;

        context.HttpContext.Response.Headers.RetryAfter =
            retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

        await ProblemFactory
            .WriteRateLimitedAsync(context.HttpContext, retryAfter, cancellationToken)
            .ConfigureAwait(false);
    };

    // Un limite global por IP. Se particiona por IP porque es una API publica
    // sin autenticacion, y se separan las lecturas de catalogo (baratas y
    // repetidas) de las paginadas (mas caras), de modo que un listado intenso no
    // consuma el cupo de los catalogos.
    options.AddPolicy(RateLimitPolicies.Default, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = RateLimitPolicies.DefaultPermitLimit,
                Window = RateLimitPolicies.Window,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));

    options.AddPolicy(RateLimitPolicies.Catalog, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = RateLimitPolicies.CatalogPermitLimit,
                Window = RateLimitPolicies.Window,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
});

builder.Services.AddHealthChecks()
    .AddCheck<DataSetHealthCheck>("dataset");

var app = builder.Build();

// Materializa el dataset antes de aceptar trafico: si el recurso incrustado
// estuviera corrupto, el proceso falla aqui y no con un 500 sporadico.
InfrastructureServiceCollectionExtensions.WarmUp(app.Services);

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseRateLimiter();

app.MapOpenApi("/openapi.json");

app.MapPokemonEndpoints();
app.MapCatalogEndpoints();
app.MapDocumentationEndpoints();
app.MapHealthChecks("/health");

app.Run();

/// <summary>
/// Punto de entrada de la API.
/// </summary>
/// <remarks>
/// Se declara como <c>public partial class Program</c> porque las sentencias de
/// nivel superior generan una clase <c>Program</c> interna, y
/// <c>WebApplicationFactory</c> necesita poder referenciarla para arrancar la
/// aplicacion desde los tests de integracion.
/// </remarks>
public partial class Program;
