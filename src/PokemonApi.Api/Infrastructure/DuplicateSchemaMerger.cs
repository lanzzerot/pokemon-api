using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace PokemonApi.Api.Infrastructure;

/// <summary>
/// Fusiona los esquemas duplicados que el generador crea para un mismo tipo.
/// </summary>
/// <remarks>
/// Los registros con constructor posicional producen un esquema por cada
/// parametro: <c>PokemonDetailResponse</c> declara <c>Height</c> y <c>Weight</c>
/// del tipo <c>MeasureResponse</c>, y el documento acababa publicando
/// <c>MeasureResponse</c> y <c>MeasureResponse2</c> con la misma forma pero
/// distintas descripciones.
/// <para>
/// Dos esquemas con la misma forma son el mismo tipo, asi que se conserva el
/// primero y se reescriben las referencias del resto. La descripcion del
/// esquema se ignora al comparar porque es justo lo que los distingue, y no
/// dice nada del tipo: lo que explica la altura y el peso es la descripcion de
/// cada propiedad, que no se toca.
/// </para>
/// </remarks>
public sealed class DuplicateSchemaMerger : IOpenApiDocumentTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (MergeDuplicates(document) is not { Count: > 0 } renames)
        {
            return Task.CompletedTask;
        }

        Rewrite(document.Paths, renames);
        Rewrite(document.Components, renames);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Calcula a donde se redirige cada esquema duplicado y elimina el original
    /// de la lista de componentes.
    /// </summary>
    /// <param name="document">Documento a compactar.</param>
    /// <returns>Los nombres duplicados y el nombre que los sustituye.</returns>
    private static IReadOnlyDictionary<string, string> MergeDuplicates(OpenApiDocument document)
    {
        var schemas = document.Components?.Schemas;

        if (schemas is null)
        {
            return new Dictionary<string, string>();
        }

        var canonicalByShape = new Dictionary<string, string>(StringComparer.Ordinal);
        var renames = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (name, schema) in schemas.ToArray())
        {
            if (schema is not OpenApiSchema definition)
            {
                continue;
            }

            var shape = Shape(definition);

            if (canonicalByShape.TryGetValue(shape, out var canonical))
            {
                renames[name] = canonical;
            }
            else
            {
                canonicalByShape[shape] = name;
            }
        }

        foreach (var (duplicate, canonical) in renames)
        {
            var schema = schemas[duplicate];

            schemas.Remove(duplicate);
            Rewrite(schema, renames);
        }

        return renames;
    }

    /// <summary>Huella de un esquema, sin su descripcion.</summary>
    /// <param name="schema">Esquema a comparar.</param>
    /// <returns>Una cadena igual para dos esquemas de la misma forma.</returns>
    private static string Shape(OpenApiSchema schema)
    {
        // Se serializa a la version 3.0 porque es la que entiende la
        // biblioteca de OpenAPI: la huella solo tiene que ser comparable entre
        // esquemas, no publicable.
        var json = JsonNode.Parse(schema.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0));

        json?.AsObject().Remove("description");

        return json?.ToJsonString() ?? string.Empty;
    }

    private static void Rewrite(OpenApiComponents? components, IReadOnlyDictionary<string, string> renames)
    {
        if (components is null)
        {
            return;
        }

        foreach (var schema in components.Schemas?.Values.OfType<OpenApiSchema>() ?? [])
        {
            Rewrite(schema, renames);
        }

        foreach (var parameter in components.Parameters?.Values.OfType<OpenApiParameter>() ?? [])
        {
            Rewrite(parameter, renames);
        }

        foreach (var response in components.Responses?.Values.OfType<OpenApiResponse>() ?? [])
        {
            Rewrite(response, renames);
        }

        foreach (var request in components.RequestBodies?.Values.OfType<OpenApiRequestBody>() ?? [])
        {
            Rewrite(request, renames);
        }
    }

    private static void Rewrite(OpenApiPaths? paths, IReadOnlyDictionary<string, string> renames)
    {
        if (paths is null)
        {
            return;
        }

        foreach (var path in paths.Values.OfType<OpenApiPathItem>())
        {
            foreach (var operation in path.Operations.Values.OfType<OpenApiOperation>())
            {
                foreach (var parameter in operation.Parameters ?? [])
                {
                    Rewrite(parameter, renames);
                }

                Rewrite(operation.RequestBody, renames);

                foreach (var response in operation.Responses?.Values.OfType<OpenApiResponse>() ?? [])
                {
                    Rewrite(response, renames);
                }
            }
        }
    }

    private static void Rewrite(OpenApiRequestBody? body, IReadOnlyDictionary<string, string> renames)
    {
        if (body is null)
        {
            return;
        }

        foreach (var content in body.Content?.Values.OfType<OpenApiMediaType>() ?? [])
        {
            Rewrite(content, renames);
        }
    }

    private static void Rewrite(OpenApiResponse? response, IReadOnlyDictionary<string, string> renames)
    {
        if (response is null)
        {
            return;
        }

        foreach (var content in response.Content?.Values.OfType<OpenApiMediaType>() ?? [])
        {
            Rewrite(content, renames);
        }
    }

    private static void Rewrite(OpenApiParameter? parameter, IReadOnlyDictionary<string, string> renames)
    {
        if (parameter is null)
        {
            return;
        }

        Rewrite(parameter.Schema, renames);

        foreach (var content in parameter.Content?.Values.OfType<OpenApiMediaType>() ?? [])
        {
            Rewrite(content, renames);
        }
    }

    private static void Rewrite(OpenApiMediaType? mediaType, IReadOnlyDictionary<string, string> renames)
    {
        if (mediaType is null)
        {
            return;
        }

        Rewrite(mediaType.Schema, renames);

        foreach (var encoding in mediaType.Encoding?.Values.OfType<OpenApiEncoding>() ?? [])
        {
            foreach (var header in encoding.Headers?.Values.OfType<OpenApiHeader>() ?? [])
            {
                Rewrite(header, renames);
            }
        }
    }

    private static void Rewrite(OpenApiHeader? header, IReadOnlyDictionary<string, string> renames)
    {
        if (header is not null)
        {
            Rewrite(header.Schema, renames);
        }
    }

    private static void Rewrite(OpenApiSchema? schema, IReadOnlyDictionary<string, string> renames)
    {
        if (schema is null)
        {
            return;
        }

        if (schema.Reference?.Id is { } id && renames.TryGetValue(id, out var canonical))
        {
            schema.Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = canonical };
        }

        foreach (var property in schema.Properties?.Values.OfType<OpenApiSchema>() ?? [])
        {
            Rewrite(property, renames);
        }

        Rewrite(schema.Items, renames);
        Rewrite(schema.AdditionalProperties, renames);
        Rewrite(schema.Not, renames);

        foreach (var composition in schema.AllOf ?? [])
        {
            Rewrite(composition, renames);
        }

        foreach (var composition in schema.AnyOf ?? [])
        {
            Rewrite(composition, renames);
        }

        foreach (var composition in schema.OneOf ?? [])
        {
            Rewrite(composition, renames);
        }
    }
}
