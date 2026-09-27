using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Abstractions.Repositories;
using PokemonApi.Application.Common;
using PokemonApi.Application.Common.Validation;
using PokemonApi.Application.Common.Queries;
using PokemonApi.Application.Features.Pokemon.Dtos;
using PokemonApi.Domain.Enumerations;
using PokemonApi.Domain.ValueObjects;

namespace PokemonApi.Application.Features.Pokemon.List;

/// <summary>
/// Aplica los filtros de <see cref="ListPokemonQuery"/> sobre el catalogo.
/// </summary>
/// <remarks>
/// Este servicio es el unico lugar donde se decide como se traducen los
/// parametros de la peticion a la representacion interna del catalogo: la altura
/// se pide en metros pero el dominio la almacena en decimetros, y los filtros de
/// catalogo se validan contra los valores realmente presentes en los datos.
/// El handler queda reducido a orquestar la llamada.
/// </remarks>
/// <param name="catalog">Catalogo de tipos, generaciones, habilidades y habitats validos.</param>
public sealed class PokemonSearchQueryBuilder(ICatalogRepository catalog)
{
    // El dataset no cambia mientras el proceso viva, asi que los conjuntos de
    // valores admitidos se resuelven una sola vez. Se cachean aqui, y no en el
    // repositorio, porque quien los consume es el filtro de closed world: sin
    // esta cache cada peticion construiria seis colecciones para comprobar si
    // 'fire' es un tipo, y la validacion recorre los mismos filtros dos veces
    // (una al validar y otra al traducir).
    //
    // El repositorio se recibe como parametro de las funciones auxiliares, y no
    // como miembro, porque un inicializador de campo todavia no puede invocar
    // metodos de instancia.
    private readonly Lazy<Task<IReadOnlySet<string>>> _typeSlugs =
        new(() => SlugsAsync(catalog, static c => c.GetTypesAsync(default), static t => t.Slug.Value));

    private readonly Lazy<Task<IReadOnlySet<string>>> _abilitySlugs =
        new(() => SlugsAsync(catalog, static c => c.GetAbilitiesAsync(default), static a => a.Slug.Value));

    private readonly Lazy<Task<IReadOnlySet<string>>> _regionSlugs =
        new(() => SlugsAsync(catalog, static c => c.GetRegionsAsync(default), static r => r.Slug.Value));

    private readonly Lazy<Task<IReadOnlySet<string>>> _eggGroupSlugs =
        new(() => SlugsAsync(catalog, static c => c.GetEggGroupsAsync(default), static g => g.Slug.Value));

    private readonly Lazy<Task<IReadOnlySet<string>>> _habitatSlugs =
        new(() => SlugsAsync(catalog, static c => c.GetHabitatsAsync(default), static h => h.Slug.Value));

    private readonly Lazy<Task<IReadOnlyDictionary<int, string>>> _generationsById =
        new(() => BuildGenerationsByIdAsync(catalog));

    private readonly ICatalogRepository _catalog = catalog;

    /// <summary>
    /// Comprueba los filtros que dependen del catalogo sin traducir la peticion.
    /// </summary>
    /// <remarks>
    /// Se expone como <see cref="IAsyncValidator{TRequest}"/> para que un cliente
    /// que Combine un filtro de catalogo con un error de paginacion reciba los
    /// dos en el mismo 400. Comparte toda la logica con
    /// <see cref="BuildAsync"/>: si las reglas estuvieran en dos sitios, un
    /// filtro podria aceptarse al validar y rechazarse al traducir.
    /// </remarks>
    /// <param name="request">Peticion del cliente.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Los fallos indexados por nombre de campo; vacio si la peticion es valida.</returns>
    public async ValueTask<IReadOnlyDictionary<string, string[]>> ValidateAsync(
        ListPokemonQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (_, failures) = await ResolveAsync(request, cancellationToken).ConfigureAwait(false);

        return failures;
    }

    /// <summary>
    /// Traduce la peticion del cliente a un <see cref="PokemonSearchQuery"/>
    /// normalizado, rechazando los valores de catalogo desconocidos.
    /// </summary>
    /// <param name="request">Peticion del cliente.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La consulta normalizada.</returns>
    /// <exception cref="ValidationException">
    /// Si algun filtro no cumple las reglas del validador o usa un valor que no
    /// existe en el catalogo. Los mensajes llegan al cliente en el
    /// <c>ProblemDetails</c>, indexados por nombre de campo.
    /// </exception>
    public async Task<Domain.Common.Result<PokemonSearchQuery>> BuildAsync(
        ListPokemonQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (resolved, failures) = await ResolveAsync(request, cancellationToken).ConfigureAwait(false);

        if (failures.Count > 0)
        {
            // Los mensajes por campo se propagan como ValidationException, que es
            // la via que ya usan los validadores de la peticion. Devolver aqui un
            // Error generico desperdiciaria informacion: el cliente sabria que
            // algo falla, pero no que parametro corregir ni que valores validos
            // admite ese campo.
            //
            // La cadena de validacion ya habria rechazado la peticion antes de
            // llegar aqui, de modo que este camino es una invariante: si alguien
            // invoca el builder sin pasar por la cadena, el fallo se sigue
            // reportando con el mismo detalle por campo.
            throw new ValidationException(failures);
        }

        return Domain.Common.Result<PokemonSearchQuery>.Success(new PokemonSearchQuery(
            Name: Normalize(request.Name),
            Types: resolved.Types,
            Abilities: resolved.Abilities,
            Generations: resolved.GenerationIds,
            Regions: resolved.Regions,
            EggGroups: resolved.EggGroups,
            Habitats: resolved.Habitats,
            Rarity: resolved.Rarity,
            MinHeight: ToDecimetres(request.MinHeight),
            MaxHeight: ToDecimetres(request.MaxHeight),
            MinWeight: ToHectograms(request.MinWeight),
            MaxWeight: ToHectograms(request.MaxWeight),
            MinBaseExperience: request.MinBaseExperience,
            MinTotalStats: request.MinTotalStats,
            MaxTotalStats: request.MaxTotalStats,
            MinStat: BuildStatThreshold(resolved.MinStat, request.MinStatValue),
            SortBy: new PokemonSortBy(
                resolved.SortBy ?? PokemonSortField.Id,
                resolved.SortDirection ?? SortDirection.Ascending),
            Page: request.Page ?? ListPokemonLimits.DefaultPage,
            PageSize: request.PageSize ?? ListPokemonLimits.DefaultPageSize));
    }

    /// <summary>
    /// Resuelve todos los filtros contra el catalogo y anota los desconocidos.
    /// </summary>
    /// <remarks>
    /// No falla nunca: devuelve los valores interpreted y, en paralelo, los
    /// errores encontrados. Quien llama decide que hacer con ellos, que es lo que
    /// permite usar la misma resolucion tanto para validar como para construir la
    /// consulta.
    /// </remarks>
    private async Task<(Resolution Resolved, IReadOnlyDictionary<string, string[]> Failures)> ResolveAsync(
        ListPokemonQuery request,
        CancellationToken cancellationToken)
    {
        var failures = new Dictionary<string, string[]>(StringComparer.Ordinal);

        var types = await ResolveSlugsAsync(
            request.Types, _typeSlugs, nameof(request.Types), failures).ConfigureAwait(false);
        var abilities = await ResolveSlugsAsync(
            request.Abilities, _abilitySlugs, nameof(request.Abilities), failures).ConfigureAwait(false);
        var regions = await ResolveSlugsAsync(
            request.Regions, _regionSlugs, nameof(request.Regions), failures).ConfigureAwait(false);
        var eggGroups = await ResolveSlugsAsync(
            request.EggGroups, _eggGroupSlugs, nameof(request.EggGroups), failures).ConfigureAwait(false);
        var habitats = await ResolveSlugsAsync(
            request.Habitats, _habitatSlugs, nameof(request.Habitats), failures).ConfigureAwait(false);

        // `generation` y `generations` son el mismo filtro con distinta cardinalidad:
        // se combinan en una unica lista de identificadores.
        var generationIds = await ResolveGenerationIdsAsync(request, failures).ConfigureAwait(false);

        // Los campos de Closed World llegan como texto: se interpretan aqui, sin
        // distinguir mayusculas, y se anotan los valores desconocidos.
        var rarity = ParseEnum<PokemonRarity>(request.Rarity, nameof(request.Rarity), failures);
        var minStat = ParseEnum<PokemonStat>(request.MinStat, nameof(request.MinStat), failures);
        var sortBy = ParseEnum<PokemonSortField>(request.SortBy, nameof(request.SortBy), failures);
        var sortDirection = ParseSortDirection(request.SortDirection, failures);

        return (
            new Resolution(types, abilities, regions, eggGroups, habitats, generationIds, rarity, minStat, sortBy, sortDirection),
            failures);
    }

    /// <summary>
    /// Interpreta el sentido de ordenamiento admitiendo las abreviaturas
    /// habituales <c>asc</c> y <c>desc</c>.
    /// </summary>
    /// <remarks>
    /// Los nombres del enumerado (<c>Ascending</c> y <c>Descending</c>) no son lo
    /// que un cliente espera escribir en una cadena de consulta, y <c>desc</c> no
    /// llegaria ni a interpretarse como abreviatura. Se aceptan las dos formas.
    /// </remarks>
    /// <param name="raw">Valor recibido del cliente.</param>
    /// <param name="failures">Errores acumulados.</param>
    /// <returns>El sentido interpretado, o <see langword="null"/> si falta o no es valido.</returns>
    private static SortDirection? ParseSortDirection(
        string? raw,
        Dictionary<string, string[]> failures)
    {
        var trimmed = raw?.Trim();

        if (string.Equals(trimmed, "asc", StringComparison.OrdinalIgnoreCase))
        {
            return SortDirection.Ascending;
        }

        if (string.Equals(trimmed, "desc", StringComparison.OrdinalIgnoreCase))
        {
            return SortDirection.Descending;
        }

        return ParseEnum<SortDirection>(raw, nameof(ListPokemonQuery.SortDirection), failures);
    }

    /// <summary>
    /// Interpreta un valor de <see cref="Enum"/> recibido como texto, sin
    /// distinguir mayusculas, guiones bajos ni guiones, y anota el error si no
    /// existe.
    /// </summary>
    /// <remarks>
    /// Los separadores se ignoran porque el cliente escribe estos campos en una
    /// cadena de consulta, donde <c>specialAttack</c>, <c>special_attack</c> y
    /// <c>special-attack</c> designan lo mismo y solo la primera forma es la que
    /// publica el catalogo de Swagger.
    /// </remarks>
    /// <typeparam name="TEnum">Conjunto de valores admitidos.</typeparam>
    /// <param name="raw">Valor recibido del cliente.</param>
    /// <param name="fieldName">Nombre del campo, usado en el mensaje de error.</param>
    /// <param name="failures">Errores acumulados.</param>
    /// <returns>El valor interpretado, o <see langword="null"/> si falta o no es valido.</returns>
    private static TEnum? ParseEnum<TEnum>(
        string? raw,
        string fieldName,
        Dictionary<string, string[]> failures)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var candidate = raw
            .Trim()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        if (Enum.TryParse(candidate, ignoreCase: true, out TEnum parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        AddFailure(
            failures,
            fieldName,
            $"'{raw}' is not a valid value. Allowed values: {string.Join(", ", Enum.GetNames<TEnum>())}.");

        return null;
    }

    /// <summary>
    /// Normaliza y valida una lista de slugs contra el conjunto de valores
    /// admitidos por un filtro, anotando los desconocidos en
    /// <paramref name="failures"/>.
    /// </summary>
    /// <remarks>
    /// Acepta las dos sintaxis que anuncia la documentacion: el parametro
    /// repetido (<c>types=fire&amp;types=water</c>, que el framework ya entrega
    /// separado) y los valores unidos por comas (<c>types=fire,water</c>), que
    /// llegan como un unico elemento y hay que desglosar.
    /// </remarks>
    private static async Task<IReadOnlyList<string>?> ResolveSlugsAsync(
        string[]? requested,
        Lazy<Task<IReadOnlySet<string>>> knownSlugs,
        string fieldName,
        Dictionary<string, string[]> failures)
    {
        if (requested is not { Length: > 0 })
        {
            return null;
        }

        var known = await knownSlugs.Value.ConfigureAwait(false);
        var resolved = new List<string>(requested.Length);

        foreach (var value in requested)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var candidate = Slug.Normalize(part);

                if (known.Contains(candidate))
                {
                    resolved.Add(candidate);
                }
                else
                {
                    AddFailure(failures, fieldName, $"'{part}' is not a valid value for this filter.");
                }
            }
        }

        return resolved.Count > 0 ? [.. resolved.Distinct()] : null;
    }

    /// <summary>
    /// Resuelve <c>generation</c> y <c>generations</c> a identificadores,
    /// admitiendo tanto el numero (<c>3</c>) como el slug (<c>generation-iii</c>).
    /// </summary>
    private async Task<IReadOnlyList<int>?> ResolveGenerationIdsAsync(
        ListPokemonQuery request,
        Dictionary<string, string[]> failures)
    {
        var requested = new List<string>();

        if (request.Generation is { } single && !string.IsNullOrWhiteSpace(single))
        {
            requested.Add(single);
        }

        if (request.Generations is { Length: > 0 } multiple)
        {
            requested.AddRange(multiple);
        }

        if (requested.Count == 0)
        {
            return null;
        }

        var byId = await _generationsById.Value.ConfigureAwait(false);
        var resolved = new List<int>(requested.Count);

        foreach (var value in requested)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (!PokemonGenerationExtensions.TryParse(value, out var generation))
            {
                AddFailure(failures, nameof(ListPokemonQuery.Generation), $"'{value}' is not a valid generation.");
                continue;
            }

            if (byId.ContainsKey((int)generation))
            {
                resolved.Add((int)generation);
            }
            else
            {
                AddFailure(failures, nameof(ListPokemonQuery.Generation),
                    $"Generation '{value}' is not present in the catalogue.");
            }
        }

        return resolved.Count > 0 ? resolved.Distinct().ToArray() : null;
    }

    private static async Task<IReadOnlySet<string>> SlugsAsync<TEntry>(
        ICatalogRepository catalog,
        Func<ICatalogRepository, Task<IReadOnlyList<TEntry>>> read,
        Func<TEntry, string> select)
    {
        var entries = await read(catalog).ConfigureAwait(false);

        return new HashSet<string>(entries.Select(select), StringComparer.Ordinal);
    }

    private static async Task<IReadOnlyDictionary<int, string>> BuildGenerationsByIdAsync(ICatalogRepository catalog)
    {
        var generations = await catalog.GetGenerationsAsync(default).ConfigureAwait(false);

        return generations.ToDictionary(generation => generation.Id, generation => generation.Slug.Value);
    }

    private static void AddFailure(Dictionary<string, string[]> failures, string field, string message)
    {
        failures[field] = [.. failures.GetValueOrDefault(field) ?? [], message];
    }

    private static string? Normalize(string? name) =>
        string.IsNullOrWhiteSpace(name) ? null : Slug.Normalize(name);

    private static decimal? ToDecimetres(decimal? metres) => metres * 10m;

    private static decimal? ToHectograms(decimal? kilograms) => kilograms * 10m;

    private static StatThreshold? BuildStatThreshold(PokemonStat? stat, int? value) =>
        stat is { } selected && value is { } minimum
            ? new StatThreshold(selected, minimum)
            : null;

    /// <summary>
    /// Valores de los filtros ya interpretados contra el catalogo.
    /// </summary>
    /// <param name="Types">Slugs de tipo admitidos.</param>
    /// <param name="Abilities">Slugs de habilidad admitidos.</param>
    /// <param name="Regions">Slugs de region admitidos.</param>
    /// <param name="EggGroups">Slugs de grupo de huevos admitidos.</param>
    /// <param name="Habitats">Slugs de habitat admitidos.</param>
    /// <param name="GenerationIds">Generaciones seleccionadas.</param>
    /// <param name="Rarity">Rareza interpretada, si se envio.</param>
    /// <param name="MinStat">Estadistica minima interpretada, si se envio.</param>
    /// <param name="SortBy">Campo de orden interpretado, si se envio.</param>
    /// <param name="SortDirection">Sentido de orden interpretado, si se envio.</param>
    /// <remarks>
    /// Todos los campos admiten <see langword="null"/>, que significa "filtro no
    /// solicitado" y no "filtro vacio": un valor desconocido tambien produce
    /// <see langword="null"/>, pero acompañado de su error en
    /// <c>Failures</c>.
    /// </remarks>
    private sealed record Resolution(
        IReadOnlyList<string>? Types,
        IReadOnlyList<string>? Abilities,
        IReadOnlyList<string>? Regions,
        IReadOnlyList<string>? EggGroups,
        IReadOnlyList<string>? Habitats,
        IReadOnlyList<int>? GenerationIds,
        PokemonRarity? Rarity,
        PokemonStat? MinStat,
        PokemonSortField? SortBy,
        SortDirection? SortDirection);
}
