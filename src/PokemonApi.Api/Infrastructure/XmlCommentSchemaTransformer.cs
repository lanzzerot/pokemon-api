using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace PokemonApi.Api.Infrastructure;

/// <summary>
/// Añade a cada esquema de OpenAPI el resumen XML de su tipo y de sus
/// propiedades.
/// </summary>
/// <remarks>
/// Es el equivalente de la integracion de comentarios XML queoffercia
/// Swashbuckle y que .NET todavia no trae de serie. Sin el, la referencia
/// mostraria los nombres de las propiedades del detalle sin decir que miden, en
/// que unidad estan ni cuando aparecen.
/// </remarks>
/// <param name="comments">Indice de comentarios XML.</param>
public sealed class XmlCommentSchemaTransformer(XmlCommentIndex comments) : IOpenApiSchemaTransformer
{
    private readonly XmlCommentIndex _comments = comments;

    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        var type = context.JsonTypeInfo?.Type;

        if (type is null)
        {
            return Task.CompletedTask;
        }

        // El generador ya habia resuelto los <inheritdoc/>, asi que solo se
        // rellena lo que sigue vacio y nunca se pisa una descripcion explicita.
        schema.Description ??= _comments.GetTypeSummary(type);

        if (schema.Properties is { Count: > 0 } && context.JsonTypeInfo is { } jsonTypeInfo)
        {
            DescribeProperties(schema, jsonTypeInfo, type);
        }

        return Task.CompletedTask;
    }

    private void DescribeProperties(OpenApiSchema schema, JsonTypeInfo jsonTypeInfo, Type type)
    {
        foreach (var property in jsonTypeInfo.Properties)
        {
            // El esquema se indexa por el nombre con el que se serializa
            // (camelCase), mientras que el XML usa el nombre de la propiedad en
            // C#. AttributeProvider es el puente entre ambos: es el miembro de
            // CLR al que corresponde esa propiedad serializada.
            if (property.AttributeProvider is not MemberInfo member
                || !schema.Properties.TryGetValue(property.Name, out var propertySchema))
            {
                continue;
            }

            propertySchema.Description ??= _comments.GetPropertySummary(type, member.Name);
        }
    }
}
