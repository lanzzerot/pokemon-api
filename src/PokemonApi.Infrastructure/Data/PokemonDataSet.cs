using PokemonApi.Domain.Entities;

namespace PokemonApi.Infrastructure.Data;

/// <summary>
/// Procedencia del dataset, expuesta por la API y por el endpoint de salud.
/// </summary>
/// <remarks>
/// Existe como tipo publico y no como el record interno del JSON para que el
/// contrato de la API no dependa de la forma del recurso embebido: cambiar el
/// formato del dataset no debe cambiar el contrato published de la API.
/// </remarks>
/// <param name="Source">Nombre de la fuente de datos.</param>
/// <param name="SourceVersion">Version de la fuente de datos.</param>
/// <param name="SourceUrl">Sitio web de la fuente de datos.</param>
/// <param name="License">Licencia de los datos.</param>
/// <param name="Attribution">Atribucion que debe mostrarse a los consumidores.</param>
/// <param name="GeneratedAtUtc">Momento de generacion del recurso, en UTC.</param>
/// <param name="PokemonCount">Numero de Pokemon incluidos.</param>
/// <param name="GenerationCount">Numero de generaciones incluidas.</param>
/// <param name="EvolutionCount">Numero de relaciones evolutivas incluidas.</param>
/// <param name="SchemaVersion">Version del formato del dataset.</param>
public sealed record DatasetMetadataView(
    string Source,
    string SourceVersion,
    string SourceUrl,
    string License,
    string Attribution,
    DateTimeOffset GeneratedAtUtc,
    int PokemonCount,
    int GenerationCount,
    int EvolutionCount,
    string SchemaVersion)
{
    /// <summary>Proyecta la vista publica desde el record interno del dataset.</summary>
    /// <param name="metadata">Metadatos internos.</param>
    /// <returns>La vista expuesta por la API.</returns>
    internal static DatasetMetadataView From(DatasetMetadata metadata) => new(
        metadata.Source,
        metadata.SourceVersion,
        metadata.SourceUrl,
        metadata.License,
        metadata.Attribution,
        metadata.GeneratedAtUtc,
        metadata.PokemonCount,
        metadata.GenerationCount,
        metadata.EvolutionCount,
        metadata.SchemaVersion);
}

/// <summary>
/// Catalogo de Pokemon completo, materializado en memoria y de solo lectura.
/// </summary>
/// <remarks>
/// <para>
/// Es la unica fuente de datos en tiempo de ejecucion. El JSON se analiza una
/// vez durante el arranque y a partir de ahi todas las peticiones se resuelven
/// contra estas estructuras, sin disco, sin red y sin bloqueos: buscar sobre mil
/// entidades en memoria cuesta microsegundos y hace innecesaria una base de
/// datos para una API de solo lectura.
/// </para>
/// <para>
/// Todos los indices son <see cref="Dictionary{TKey, TValue}"/> y
/// <see cref="IReadOnlyList{T}"/> construidos una sola vez y nunca modificados,
/// de modo que las peticiones concurrentes solo leen. Se documenta como
/// invariante, no como recomendacion: es la razon de que la instancia se cree una
/// vez y se comparta como singleton.
/// </para>
/// </remarks>
public sealed class PokemonDataSet
{
    private readonly IReadOnlyDictionary<int, Pokemon> _byId;
    private readonly IReadOnlyDictionary<string, Pokemon> _byName;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<Pokemon>> _byEvolutionChain;
    private readonly IReadOnlyList<Pokemon> _orderedById;

    /// <summary>Inicializa la instantanea a partir de las entidades ya validadas.</summary>
    /// <param name="metadata">Procedencia del dataset.</param>
    /// <param name="pokemon">Entidades de dominio.</param>
    /// <param name="generations">Generaciones del catalogo.</param>
    /// <param name="types">Tipos con su recuento.</param>
    /// <param name="abilities">Habilidades con su recuento.</param>
    /// <param name="eggGroups">Grupos de huevo.</param>
    /// <param name="habitats">Habitats con su recuento.</param>
    /// <param name="regions">Regiones con su recuento.</param>
    internal PokemonDataSet(
        DatasetMetadata metadata,
        IReadOnlyList<Pokemon> pokemon,
        IReadOnlyList<Generation> generations,
        IReadOnlyList<PokemonTypeInfo> types,
        IReadOnlyList<AbilityInfo> abilities,
        IReadOnlyList<EggGroupInfo> eggGroups,
        IReadOnlyList<CatalogEntryInfo> habitats,
        IReadOnlyList<CatalogEntryInfo> regions)
    {
        Metadata = DatasetMetadataView.From(metadata);
        Pokemon = pokemon;
        Generations = generations;
        Types = types;
        Abilities = abilities;
        EggGroups = eggGroups;
        Habitats = habitats;
        Regions = regions;

        _orderedById = pokemon;
        _byId = pokemon.ToDictionary(p => p.Id);

        // El indice por nombre es lo que hace que GET /pokemon/{name} no
        // dependa de recorrer el catalogo. El comparador es ordinal porque los
        // slugs ya estan normalizados a minusculas: en ese alfabeto, comparar
        // ordinalmente y comparar con culture invariante dan el mismo resultado,
        // y el ordinal no depende de los datos de globalization del host.
        var byName = new Dictionary<string, Pokemon>(pokemon.Count, StringComparer.Ordinal);

        foreach (var entry in pokemon)
        {
            byName[entry.Name.Value] = entry;
        }

        _byName = byName;

        // La cadena evolutiva se indexa con la forma base primero y el resto por
        // identificador: el consumidor de la cadena espera ese orden y asi el
        // resultado es determinista entre llamadas.
        _byEvolutionChain = pokemon
            .GroupBy(p => p.EvolutionChainId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Pokemon>)
                    [.. group
                        .OrderBy(p => p.EvolvesFrom is null ? 0 : 1)
                        .ThenBy(p => p.Id)]);
    }

    /// <summary>Procedencia y trazabilidad del dataset cargado.</summary>
    public DatasetMetadataView Metadata { get; }

    /// <summary>Todos los Pokemon, ordenados por identificador.</summary>
    public IReadOnlyList<Pokemon> Pokemon { get; }

    /// <summary>Generaciones del catalogo, ordenadas por identificador.</summary>
    public IReadOnlyList<Generation> Generations { get; }

    /// <summary>Tipos del catalogo, con su recuento de Pokemon.</summary>
    public IReadOnlyList<PokemonTypeInfo> Types { get; }

    /// <summary>Habilidades del catalogo, con su recuento de Pokemon.</summary>
    public IReadOnlyList<AbilityInfo> Abilities { get; }

    /// <summary>Grupos de huevo del catalogo.</summary>
    public IReadOnlyList<EggGroupInfo> EggGroups { get; }

    /// <summary>Habitats del catalogo.</summary>
    public IReadOnlyList<CatalogEntryInfo> Habitats { get; }

    /// <summary>Regiones del catalogo.</summary>
    public IReadOnlyList<CatalogEntryInfo> Regions { get; }

    /// <summary>
    /// Busca un Pokemon por identificador.
    /// </summary>
    /// <param name="id">Identificador de la Poke&#x27;dex.</param>
    /// <returns>El Pokemon, o <see langword="null"/> si no existe.</returns>
    public Pokemon? FindById(int id) => _byId.GetValueOrDefault(id);

    /// <summary>
    /// Busca un Pokemon por su slug canonico.
    /// </summary>
    /// <param name="slug">Slug canonico del nombre.</param>
    /// <returns>El Pokemon, o <see langword="null"/> si no existe.</returns>
    public Pokemon? FindBySlug(string slug) =>
        _byName.TryGetValue(slug, out var pokemon) ? pokemon : null;

    /// <summary>
    /// Devuelve la cadena evolutiva indicada, con la forma base primero.
    /// </summary>
    /// <param name="chainId">Identificador de la cadena.</param>
    /// <returns>Los Pokemon de la cadena; vacia si no existe.</returns>
    public IReadOnlyList<Pokemon> FindEvolutionChain(int chainId) =>
        _byEvolutionChain.TryGetValue(chainId, out var chain) ? chain : [];

    /// <summary>
    /// Devuelve todos los Pokemon en orden de identificador, la posicion por
    /// defecto del listado.
    /// </summary>
    /// <returns>La secuencia completa, sin copia.</returns>
    public IReadOnlyList<Pokemon> OrderedById() => _orderedById;
}
