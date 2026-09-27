using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PokemonApi.Domain.Entities;
using PokemonApi.Infrastructure.Logging;

namespace PokemonApi.Infrastructure.Data;

/// <summary>
/// Lee el recurso <c>Data/pokemon.json</c> incrustado en el ensamblado y lo
/// convierte en una <see cref="PokemonDataSet"/>.
/// </summary>
/// <remarks>
/// <para>
/// Se ejecuta una unica vez, al arrancar la aplicacion. Leer desde el recurso
/// incrustado en lugar de desde el disco es deliberado: el artefacto publicado no
/// depende de ficheros externos, de rutas de contenido ni de permisos de lectura,
/// y desplegar se reduce a copiar una carpeta de binarios.
/// </para>
/// <para>
/// Los recuentos por tipo, habilidad, grupo de huevo y habitat no se almacenan
/// en el JSON: se derivan aqui recorriendo el catalogo, de modo que es
/// imposible que se desincronicen respecto a las Pokemon.
/// </para>
/// </remarks>
internal sealed class PokemonDatasetLoader(ILogger<PokemonDatasetLoader> logger)
{
    /// <summary>Nombre logico del recurso incrustado.</summary>
    public const string ResourceName = "PokemonApi.Infrastructure.Data.pokemon.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly ILogger<PokemonDatasetLoader> _logger = logger;

    /// <summary>Carga y materializa el catalogo completo.</summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La instantanea lista para servir peticiones.</returns>
    /// <exception cref="InvalidDataException">Si el recurso falta o es incoherente.</exception>
    public PokemonDataSet Load(CancellationToken cancellationToken = default)
    {
        var file = ReadResource(cancellationToken);
        var mapper = new PokemonDatasetMapper(file);
        var metadata = file.Metadata
            ?? throw new InvalidDataException("The embedded dataset has no metadata block.");

        var typeCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var abilityCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var eggGroupCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var habitatCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var pokemon = new List<Pokemon>(file.Pokemon.Count);

        foreach (var entry in file.Pokemon)
        {
            cancellationToken.ThrowIfCancellationRequested();
            pokemon.Add(mapper.MapPokemon(entry));

            Count(typeCounts, entry.Types);
            Count(abilityCounts, entry.Abilities.Select(ability => ability.Name));
            Count(eggGroupCounts, entry.EggGroups);
            Count(habitatCounts, entry.Habitat is null ? [] : [entry.Habitat]);
        }

        var generations = PokemonDatasetMapper.MapGenerations(file.Generations);

        var dataSet = new PokemonDataSet(
            metadata,
            [.. pokemon.OrderBy(entry => entry.Id)],
            generations,
            PokemonDatasetMapper.MapTypes(file.Types, typeCounts),
            PokemonDatasetMapper.MapAbilities(file.Abilities, abilityCounts),
            PokemonDatasetMapper.MapEggGroups(file.EggGroups),
            PokemonDatasetMapper.MapSimpleCatalog(file.Habitats, habitatCounts, "habitat"),
            BuildRegions(generations, pokemon));

        InfrastructureLog.DataSetLoaded(
            _logger,
            dataSet.Pokemon.Count,
            dataSet.Generations.Count,
            dataSet.Metadata.SchemaVersion,
            dataSet.Metadata.Source,
            dataSet.Metadata.SourceVersion,
            dataSet.Metadata.GeneratedAtUtc);

        return dataSet;
    }

    private static PokemonDatasetFile ReadResource(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = typeof(PokemonDatasetLoader).Assembly
            .GetManifestResourceStream(ResourceName)
            ?? throw new InvalidDataException(
                $"The embedded resource '{ResourceName}' is missing from " +
                $"{Assembly.GetExecutingAssembly().GetName().Name}. " +
                "Check the EmbeddedResource item in PokemonApi.Infrastructure.csproj.");

        return JsonSerializer.Deserialize<PokemonDatasetFile>(stream, SerializerOptions)
            ?? throw new InvalidDataException($"The embedded resource '{ResourceName}' is empty.");
    }

    /// <summary>
    /// Deriva el catalogo de regiones a partir de las generaciones: el dataset no
    /// lo almacena porque se deduce de la generacion a la que pertenece cada una.
    /// </summary>
    /// <param name="generations">Generaciones del catalogo.</param>
    /// <param name="pokemon">Pokemon materializados, para contrastar los recuentos.</param>
    /// <returns>Las regiones, en orden de aparicion.</returns>
    /// <exception cref="InvalidDataException">Si los recuentos no cuadran.</exception>
    private static IReadOnlyList<CatalogEntryInfo> BuildRegions(
        IReadOnlyList<Generation> generations,
        IReadOnlyList<Pokemon> pokemon)
    {
        var regionIds = new Dictionary<string, int>(StringComparer.Ordinal);
        var regions = new List<CatalogEntryInfo>();

        foreach (var generation in generations)
        {
            var slug = generation.Region.Value;

            if (!regionIds.TryGetValue(slug, out var id))
            {
                id = regionIds.Count + 1;
                regionIds[slug] = id;
                regions.Add(new CatalogEntryInfo(id, generation.Region, generation.RegionName, 0));
            }

            var index = id - 1;
            regions[index] = regions[index] with { PokemonCount = regions[index].PokemonCount + generation.PokemonCount };
        }

        // El total por generacion y el recuento real sobre el catalogo deben
        // coincidir. Si no coinciden, el dataset esta desincronizado y es mejor
        // fallar al arrancar que publicar recuentos contradictorios.
        var actual = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var entry in pokemon)
        {
            if (entry.Region is { } region)
            {
                actual[region.Value] = actual.GetValueOrDefault(region.Value) + 1;
            }
        }

        foreach (var region in regions)
        {
            if (actual.TryGetValue(region.Slug.Value, out var count) && count != region.PokemonCount)
            {
                throw new InvalidDataException(
                    $"Region '{region.Slug}' declares {region.PokemonCount} Pokemon across its generations, " +
                    $"but the catalogue contains {count}.");
            }
        }

        return regions;
    }

    private static void Count(Dictionary<string, int> counts, IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            counts[value] = counts.GetValueOrDefault(value) + 1;
        }
    }
}
