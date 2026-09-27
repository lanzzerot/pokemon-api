using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Common.Validation;
using PokemonApi.Application.Features.Catalog.GetAbilities;
using PokemonApi.Application.Features.Catalog.GetGenerations;
using PokemonApi.Application.Features.Catalog.GetReferenceData;
using PokemonApi.Application.Features.Catalog.GetTypes;
using PokemonApi.Application.Features.Pokemon.GetById;
using PokemonApi.Application.Features.Pokemon.GetByName;
using PokemonApi.Application.Features.Pokemon.GetEvolutionChain;
using PokemonApi.Application.Features.Pokemon.List;

namespace PokemonApi.Application;

/// <summary>
/// Registro de dependencias de la capa de aplicacion.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registra los casos de uso, sus validadores y los servicios de soporte.
    /// </summary>
    /// <param name="services">Coleccion de servicios.</param>
    /// <returns>La misma coleccion, para permitir el encadenamiento.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = Assembly.GetExecutingAssembly();

        RegisterImplementationsOf(services, assembly, typeof(IRequestHandler<,>));
        RegisterImplementationsOf(services, assembly, typeof(IValidator<>));
        RegisterImplementationsOf(services, assembly, typeof(IAsyncValidator<>));
        RegisterImplementationsOf(services, assembly, typeof(ISender));

        // Singleton: el builder solo depende del catalogo, que es inmutable, y
        // cachea los conjuntos de valores admitidos. Compartirlo entre peticiones
        // evita reconstruir esos conjuntos en cada una.
        services.AddSingleton<PokemonSearchQueryBuilder>();

        return services;
    }

    /// <summary>
    /// Registra con ciclo de vida Scoped todas las implementaciones concretas
    /// de <paramref name="serviceDefinition"/> que hay en el ensamblado.
    /// </summary>
    /// <remarks>
    /// Se usa la convencion "una implementacion por contrato" en lugar de un
    /// registro manual: añadir un caso de uso nuevo no obliga a tocar esta clase,
    /// y un olvido se detecta inmediatamente en los tests de integracion porque
    /// el endpoint responde 500 en lugar de 404.
    /// </remarks>
    private static void RegisterImplementationsOf(
        IServiceCollection services,
        Assembly assembly,
        Type serviceDefinition)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(serviceDefinition);

        // No se puede comparar el contrato abierto con la implementacion mediante
        // IsAssignableFrom: un tipo generico abierto nunca es asignable desde una
        // clase cerrada. Se enumeran las interfaces que cada tipo implementa y se
        // seleccionan las cuya definicion generica coincide con el contrato, lo que
        // ademas registra cada implementacion en todos los contratos que satisfaga.
        var registrations = assembly
            .GetExportedTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type
                .GetInterfaces()
                .Where(service => service.IsGenericType
                    && service.GetGenericTypeDefinition() == serviceDefinition)
                .Select(service => new { Service = service, Implementation = type }))
            .ToArray();

        foreach (var registration in registrations)
        {
            services.AddScoped(registration.Service, registration.Implementation);
        }
    }
}
