using System.Reflection;

namespace PokemonApi.Api.Endpoints;

/// <summary>
/// Endpoints de la documentacion publica.
/// </summary>
/// <remarks>
/// La web se sirve desde los recursos incrustados del propio ensamblado, igual
/// que el dataset: no hay carpeta que desplegar, ni CDN que pueda caerse, ni
/// archivos que se queden atras respecto al binario que se esta ejecutando.
/// <para>
/// El <c>Content-Type</c> se declara de forma explicita porque un recurso
/// incrustado no deduce la extension, y el navegador rechazaria un
/// <c>text/plain</c> cargado desde un <c>&lt;script&gt;</c>. Los assets se
/// cachean cinco minutos: son pequenos y solo cambian al desplegar.
/// </para>
/// <para>
/// No se aplica politica de limitacion. La pagina pide un recurso y el documento
/// OpenAPI uno mas, mientras que los limites de la API existen para proteger el
/// catalogo de un cliente que lo recorre entero.
/// </para>
/// </remarks>
public static class DocumentationEndpoints
{
    /// <summary>Ruta de la documentacion.</summary>
    private const string RoutePrefix = "/docs";

    /// <summary>Tipos MIME de los recursos, indexados por recurso.</summary>
    private static readonly IReadOnlyDictionary<string, string> ContentTypes =
        new Dictionary<string, string>
        {
            ["index.html"] = "text/html; charset=utf-8",
            ["docs.css"] = "text/css; charset=utf-8",
            ["docs.js"] = "text/javascript; charset=utf-8",
            ["pokemon-api-logo.svg"] = "image/svg+xml",
        };

    /// <summary>
    /// Los recursos se leen una sola vez del ensamblado y se sirven de
    /// memoria, que ademas es donde ya estan en el caso normal.
    /// </summary>
    private static readonly Lazy<IReadOnlyDictionary<string, byte[]>> Assets =
        new(LoadAssets, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Registra los endpoints de documentacion.</summary>
    /// <param name="app">Aplicacion web.</param>
    public static void MapDocumentationEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/", () => Serve("index.html"))
            .ExcludeFromDescription()
            .AddEndpointFilter(CacheForAShortTimeAsync)
            .WithName("GetHomeDocumentation");

        // La pagina se sirve en /docs y los recursos en /docs/{recurso}. El HTML
        // usa rutas absolutas para que ambos formatos de la URL funcionen.
        var group = app.MapGroup(RoutePrefix)
            .ExcludeFromDescription()
            .AddEndpointFilter(CacheForAShortTimeAsync);

        group.MapGet("/", () => Serve("index.html")).WithName("GetDocumentation");
        group.MapGet("/docs.css", () => Serve("docs.css"));
        group.MapGet("/docs.js", () => Serve("docs.js"));
        group.MapGet("/pokemon-api-logo.svg", () => Serve("pokemon-api-logo.svg"));
    }

    /// <summary>Devuelve un recurso de la documentacion.</summary>
    /// <param name="name">Nombre del recurso incrustado.</param>
    /// <returns>El recurso con su tipo MIME.</returns>
    /// <exception cref="InvalidOperationException">El recurso no esta incrustado.</exception>
    private static IResult Serve(string name)
    {
        if (!Assets.Value.TryGetValue(name, out var content))
        {
            throw new InvalidOperationException(
                $"El recurso incrustado '{name}' no existe. Comprueba la declaracion " +
                "EmbeddedResource del proyecto Api.");
        }

        return Results.Bytes(content, ContentTypes[name]);
    }

    /// <summary>Carga los recursos incrustados de la documentacion.</summary>
    /// <returns>Contenido de cada recurso, indexado por nombre.</returns>
    private static IReadOnlyDictionary<string, byte[]> LoadAssets()
    {
        var assembly = Assembly.GetExecutingAssembly();

        return ContentTypes.Keys.ToDictionary(
            name => name,
            name =>
            {
                var stream = assembly.GetManifestResourceStream($"PokemonApi.Api.Docs.{name}")
                    ?? throw new InvalidOperationException(
                        $"El recurso incrustado 'PokemonApi.Api.Docs.{name}' no esta incrustado.");

                using (stream)
                {
                    using var buffer = new MemoryStream();

                    stream.CopyTo(buffer);

                    return buffer.ToArray();
                }
            });
    }

    /// <summary>
    /// Anade una cache corta a la respuesta. Se ejecuta antes de que el
    /// resultado escriba la respuesta, que es cuando todavia se pueden tocar las
    /// cabeceras.
    /// </summary>
    /// <param name="context">Contexto del filtro.</param>
    /// <param name="next">Siguiente filtro de la cadena.</param>
    /// <returns>El resultado del endpoint.</returns>
    private static async ValueTask<object?> CacheForAShortTimeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var result = await next(context);

        context.HttpContext.Response.Headers.CacheControl = "public, max-age=300";

        return result;
    }
}
