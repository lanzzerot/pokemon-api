using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using PokemonApi.Domain.Enumerations;

namespace PokemonApi.Api.Infrastructure;

/// <summary>
/// Anota con los valores admitidos los parametros de consulta que se reciben
/// como texto pero representan un conjunto cerrado.
/// </summary>
/// <remarks>
/// Los endpoints aceptan <c>?rarity=legendary</c> en lugar de
/// <c>?rarity=Legendary</c> porque el enlace automatico de los
/// <see langword="enum"/> del framework solo reconoce el nombre del miembro con
/// mayusculas y minusculas exactas. A cambio de esa comodidad, estos parametros
/// se declaran como <see cref="string"/> y se interpretan en la capa de
/// aplicacion, que devuelve ademas un error que enumera los valores validos.
/// <para>
/// Este transformador recupera en el documento la informacion que se perderia:
/// sin el, la referencia mostraria un <c>string</c> libre y el cliente tendria
/// que leer la descripcion del endpoint para descubrir las opciones.
/// </para>
/// </remarks>
public sealed class ClosedWorldParameterTransformer : IOpenApiOperationTransformer
{
    private static readonly IReadOnlyDictionary<string, Type> EnumParameters =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["sortBy"] = typeof(PokemonSortField),
            ["sortDirection"] = typeof(SortDirection),
            ["rarity"] = typeof(PokemonRarity),
            ["minStat"] = typeof(PokemonStat),
        };

    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (operation.Parameters is null)
        {
            return Task.CompletedTask;
        }

        foreach (var parameter in operation.Parameters)
        {
            if (parameter.In != ParameterLocation.Query
                || parameter.Name is null
                || !EnumParameters.TryGetValue(parameter.Name, out var enumType))
            {
                continue;
            }

            parameter.Schema ??= new OpenApiSchema { Type = "string" };
            parameter.Schema.Enum =
            [
                .. AllowedValues(parameter.Name, enumType)
                    .Select(name => (IOpenApiAny)new OpenApiString(name)),
            ];
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Valores que se documentan para un parametro: los del enumerado mas las
    /// abreviaturas que el endpoint acepta ademas.
    /// </summary>
    /// <param name="parameterName">Nombre del parametro de consulta.</param>
    /// <param name="enumType">Enumerado que representa.</param>
    /// <returns>Los valores admitidos, sin repetir.</returns>
    private static IEnumerable<string> AllowedValues(string parameterName, Type enumType)
    {
        var names = Enum.GetNames(enumType).ToList();

        // El endpoint acepta `asc` y `desc` como sinonimos de los nombres largos.
        if (enumType == typeof(SortDirection))
        {
            names.AddRange(["asc", "desc"]);
        }

        return names.Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
