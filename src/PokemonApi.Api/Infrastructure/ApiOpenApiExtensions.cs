using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;

namespace PokemonApi.Api.Infrastructure;

/// <summary>
/// Configura el documento OpenAPI que la API publica en <c>/openapi.json</c>.
/// </summary>
/// <remarks>
/// La documentacion ejecutable se genera con el generador incluido en .NET, sin
/// Swashbuckle ni ninguna otra dependencia. A cambio, las dos piezas que el
/// generador no trae (los comentarios XML y los valores admitidos de los
/// parametros de conjunto cerrado) se anaden aqui como transformadores, de modo
/// que el documento sigue siendo tan explicito como el que se publicaba antes.
/// </remarks>
public static class ApiOpenApiExtensions
{
    /// <summary>
    /// Registra el generador del documento y sus transformadores.
    /// </summary>
    /// <param name="services">Coleccion de servicios.</param>
    /// <returns>La misma coleccion, para permitir el encadenamiento.</returns>
    public static IServiceCollection AddApiOpenApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // La sobrecarga con IServiceProvider es explicita a proposito: pasar el
        // grupo de metodos directamente es ambiguo y no llega a registrar nada.
        services.TryAddSingleton(_ => XmlCommentIndex.Create());
        services.TryAddSingleton<XmlCommentSchemaTransformer>();
        services.TryAddSingleton<ClosedWorldParameterTransformer>();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Pokemon API",
                    Description =
                        "Catalogo de Pokemon de las nueve generaciones. Datos provistos por PokeAPI, " +
                        "con licencia BSD-3-Clause. La API es de solo lectura y no necesita autenticacion.",
                };

                return Task.CompletedTask;
            });

            options.AddSchemaTransformer<XmlCommentSchemaTransformer>();
            options.AddOperationTransformer<ClosedWorldParameterTransformer>();
            options.AddDocumentTransformer<DuplicateSchemaMerger>();

            // El generador numera los esquemas cuyo nombre simple colisiona
            // (AbilityResponse y AbilityResponse2). La implementacion estatica de
            // OpenApiOptions usa el espacio de nombres completo, que ya es
            // unico, asi que se asigna explicitamente en lugar de la que el
            // documento traeria por defecto.
            options.CreateSchemaReferenceId = OpenApiOptions.CreateDefaultSchemaReferenceId;
        });

        return services;
    }
}
