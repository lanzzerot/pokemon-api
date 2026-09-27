using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Abstractions.Repositories;
using PokemonApi.Infrastructure.Data;
using PokemonApi.Infrastructure.Messaging;
using PokemonApi.Infrastructure.Persistence;

namespace PokemonApi.Infrastructure;

/// <summary>
/// Registro de dependencias de la capa de infraestructura.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registra el catalogo en memoria, los repositorios y el mediator con sus
    /// comportamientos.
    /// </summary>
    /// <param name="services">Coleccion de servicios.</param>
    /// <returns>La misma coleccion, para permitir el encadenamiento.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<PokemonDatasetLoader>();

        // El dataset se materializa la primera vez que se resuelve, y no al
        // registrar. Materializarlo aqui requeriria un ILogger que todavia no
        // existe, porque el contenedor se esta construyendo; resolverlo desde la
        // fabrica deja que cada peticion reciba el registrador real.
        //
        // Por eso el arranque debe forzar la resolucion (ver WarmUp): asi, un
        // recurso incrustado corrupto sigue haciendo fallar el proceso al
        // arrancar y no en mitad del trafico.
        services.AddSingleton(serviceProvider =>
        {
            var loader = serviceProvider.GetRequiredService<PokemonDatasetLoader>();
            return loader.Load();
        });

        services.AddSingleton<IPokemonRepository, InMemoryPokemonRepository>();
        services.AddSingleton<ICatalogRepository, InMemoryCatalogRepository>();
        services.AddScoped<ISender, Mediator>();

        // Los comportamientos se registran abiertos y en este orden: el primero
        // registrado es el mas externo de la cadena, de modo que el registro de
        // logs envuelve a la validacion y una peticion invalida queda anotada
        // antes de rechazarse.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }

    /// <summary>
    /// Fuerza la materializacion del catalogo y comprueba que el proceso puede
    /// arrancar con el dataset incrustado.
    /// </summary>
    /// <remarks>
    /// Se invoca una vez desde <c>Program.cs</c> antes de aceptar trafico. Sin
    /// esta llamada, el coste de la carga se pagaria en la primera peticion y un
    /// dataset corrupto se manifestaria como un error sporadico en lugar de como
    /// un fallo de despliegue limpio.
    /// </remarks>
    /// <param name="serviceProvider">Contenedor ya construido.</param>
    /// <exception cref="InvalidDataException">Si el recurso incrustado falta o es incoherente.</exception>
    public static void WarmUp(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _ = serviceProvider.GetRequiredService<PokemonDataSet>();
    }
}
