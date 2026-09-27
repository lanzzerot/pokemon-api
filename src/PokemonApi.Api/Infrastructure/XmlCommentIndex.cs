using System.Reflection;
using System.Xml.Linq;
using PokemonApi.Application;
using PokemonApi.Domain.Common;

namespace PokemonApi.Api.Infrastructure;

/// <summary>
/// Indice de los comentarios XML de los tres ensamblados que forman la API.
/// </summary>
/// <remarks>
/// <para>
/// El generador nativo de OpenAPI de .NET no lee los archivos de documentacion
/// XML, y en este proyecto el resumen de cada tipo y cada propiedad es
/// precisamente la mitad del valor de la referencia. Sin este indice, el
/// esquema de <c>PokemonDetailResponse</c> seria una lista de nombres de
/// propiedad sin explicar, que es justo lo que el cliente no quiere.
/// </para>
/// <para>
/// Se indexa en memoria al arrancar y no en cada peticion: el documento se
/// genera una vez y sus entradas se reciclan para todos los esquemas.
/// </para>
/// </remarks>
public sealed class XmlCommentIndex
{
    private readonly Dictionary<string, string> _summaries;

    private XmlCommentIndex(Dictionary<string, string> summaries)
    {
        _summaries = summaries;
    }

    /// <summary>
    /// Crea el indice leyendo los archivos de documentacion de la API.
    /// </summary>
    /// <remarks>
    /// Se recorren solo los tres ensamblados propios. Los XML del framework y de
    /// los paquetes tambien estan en la carpeta de salida, y sus miembros no
    /// interesan: lo que se documenta aqui es el contrato de esta API.
    /// </remarks>
    /// <returns>El indice; vacio si los archivos no estan, para no impedir el arranque.</returns>
    public static XmlCommentIndex Create()
    {
        var summaries = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var assembly in DocumentedAssemblies())
        {
            var path = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");

            if (File.Exists(path))
            {
                Add(summaries, path);
            }
        }

        return new XmlCommentIndex(summaries);
    }

    /// <summary>
    /// Recupera el resumen de un tipo.
    /// </summary>
    /// <param name="type">Tipo documentado.</param>
    /// <returns>El resumen, o <see langword="null"/> si el tipo no lo tiene.</returns>
    public string? GetTypeSummary(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return Lookup($"T:{MemberPath(type)}");
    }

    /// <summary>
    /// Recupera el resumen de una propiedad.
    /// </summary>
    /// <param name="declaringType">Tipo que declara la propiedad.</param>
    /// <param name="propertyName">
    /// Nombre de la propiedad en C#, no el nombre con el que se serializa.
    /// </param>
    /// <returns>El resumen, o <see langword="null"/> si la propiedad no lo tiene.</returns>
    public string? GetPropertySummary(Type declaringType, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(declaringType);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        return Lookup($"P:{MemberPath(declaringType)}.{propertyName}");
    }

    /// <summary>
    /// Traduce un tipo al identificador de miembro que usa el XML.
    /// </summary>
    /// <remarks>
    /// El identificador separa los tipos anidados con punto, mientras que
    /// <see cref="Type.FullName"/> los separa con <c>+</c>. Los tipos genericos
    /// aparecen ademas con el numero de parametros de tipo detras de una
    /// acotacion grave, y con los argumentos ya cerrados entre corchetes: solo
    /// interesa el arity, porque el documento XML documenta la definicion.
    /// </remarks>
    /// <param name="type">Tipo a traducir.</param>
    /// <returns>La ruta del tipo tal y como aparece en el archivo XML.</returns>
    private static string MemberPath(Type type)
    {
        var path = type.FullName ?? type.Name;
        var arguments = path.IndexOf('[');

        if (arguments >= 0)
        {
            path = path[..arguments];
        }

        var arity = type.IsGenericType
            ? "$" + type.GetGenericArguments().Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : string.Empty;

        return path.Replace('+', '.') + arity;
    }

    private static IEnumerable<Assembly> DocumentedAssemblies()
    {
        yield return Assembly.GetExecutingAssembly();
        yield return typeof(ApplicationServiceCollectionExtensions).Assembly;
        yield return typeof(Result).Assembly;
    }

    private static void Add(Dictionary<string, string> summaries, string path)
    {
        var document = XDocument.Load(path);

        foreach (var member in document.Descendants("member"))
        {
            var name = member.Attribute("name")?.Value;
            var summary = member.Element("summary") is { } element
                ? Normalize(element.Value)
                : null;

            // Sin resumen no hay nada que aportar. El comentario heredado ya viene
            // resuelto en el XML, y un resumen vacio o solo con marcas de formato
            // no le dice nada al cliente.
            if (name is not null && !string.IsNullOrEmpty(summary))
            {
                summaries[name] = summary;
            }
        }
    }

    private static string Normalize(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private string? Lookup(string member) => _summaries.GetValueOrDefault(member);
}
