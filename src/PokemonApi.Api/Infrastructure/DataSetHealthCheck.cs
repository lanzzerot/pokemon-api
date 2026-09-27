using Microsoft.Extensions.Diagnostics.HealthChecks;
using PokemonApi.Infrastructure.Data;

namespace PokemonApi.Api.Infrastructure;

/// <summary>
/// Comprueba que el catalogo esta materializado y es coherente.
/// </summary>
/// <remarks>
/// El dataset se carga al arrancar, asi que para que este check devuelva
/// <see cref="HealthCheckResult.Unhealthy"/> habria que haber corrupto el proceso
/// despues de arrancar. Su valor real es el diagnostico: distingue "no hay datos"
/// de "los hay pero el endpoint no responde", que es lo que se suele buscar al
/// integrar un balanceador.
/// </remarks>
public sealed class DataSetHealthCheck(PokemonDataSet dataSet) : IHealthCheck
{
    private readonly PokemonDataSet _dataSet = dataSet;

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var pokemonCount = _dataSet.Pokemon.Count;
        var generationCount = _dataSet.Generations.Count;

        if (pokemonCount == 0 || generationCount == 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "The dataset is loaded but empty.",
                data: new Dictionary<string, object>
                {
                    ["pokemonCount"] = pokemonCount,
                    ["generationCount"] = generationCount,
                }));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"The dataset holds {pokemonCount} Pokemon across {generationCount} generations.",
            data: new Dictionary<string, object>
            {
                ["pokemonCount"] = pokemonCount,
                ["generationCount"] = generationCount,
                ["typeCount"] = _dataSet.Types.Count,
                ["abilityCount"] = _dataSet.Abilities.Count,
                ["source"] = _dataSet.Metadata.Source,
                ["sourceVersion"] = _dataSet.Metadata.SourceVersion,
                ["generatedAtUtc"] = _dataSet.Metadata.GeneratedAtUtc,
            }));
    }
}
