namespace PokemonApi.Application.Common.Pagination;

/// <summary>
/// Pagina de resultados junto con los metadatos que necesita el cliente para
/// navegar el resto del catalogo.
/// </summary>
/// <remarks>
/// Se agrupan <c>Items</c>, <c>Page</c>, <c>PageSize</c> y <c>TotalCount</c>
/// en un unico objeto para que ningun endpoint pueda olvidarse de devolver los
/// metadatos de paginacion.
/// </remarks>
/// <typeparam name="T">Tipo de los elementos devueltos.</typeparam>
/// <param name="Items">Elementos de la pagina actual.</param>
/// <param name="Page">Numero de la pagina actual, empezando en 1.</param>
/// <param name="PageSize">Cantidad maxima de elementos por pagina.</param>
/// <param name="TotalCount">Total de elementos que cumplen el filtro, en todas las paginas.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    // CA1000 (no declarar miembros estaticos en tipos genericos) no aplica a las
    // fabricas de pagina: son el punto de entrada natural al tipo y extraerlas
    // a una clase no generica obligaria a repetir el tipo en cada llamada.
#pragma warning disable CA1000

    /// <summary>Crea una pagina vacia.</summary>
    public static PagedResult<T> Empty(int page, int pageSize) => new([], page, pageSize, 0);

    /// <summary>
    /// Crea una pagina a partir de la secuencia completa de resultados que
    /// cumplen el filtro.
    /// </summary>
    /// <remarks>
    /// La secuencia se materializa una sola vez porque hacen falta tanto el
    /// <see cref="TotalCount"/> como los elementos de la pagina: recorrerla dos
    /// veces obligaria a reevaluar todos los filtros, que es la parte cara de
    /// una consulta de catalogo.
    /// </remarks>
    /// <param name="source">Todos los resultados, ya filtrados y ordenados.</param>
    /// <param name="page">Pagina solicitada, empezando en 1.</param>
    /// <param name="pageSize">Cantidad de elementos por pagina.</param>
    /// <returns>La pagina correspondiente.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Si la pagina o el tamano no son validos.</exception>
    public static PagedResult<T> Create(IEnumerable<T> source, int page, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var materialised = source as IReadOnlyList<T> ?? [.. source];
        var totalCount = materialised.Count;
        var skip = (page - 1) * pageSize;

        // Pedir una pagina mas alla del final no es un error: devuelve una pagina
        // vacia que conserva el total, para que el cliente pueda ver que se ha
        // salido del rango.
        var items = skip >= totalCount ? [] : materialised.Skip(skip).Take(pageSize).ToArray();

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }

#pragma warning restore CA1000

    /// <summary>Total de paginas necesarias para recorrer todos los resultados.</summary>
    /// <remarks>
    /// Es cero cuando no hay resultados, de modo que el cliente puede distinguir
    /// "sin resultados" de "resultados parciales".
    /// </remarks>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Indica si existe una pagina anterior.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Indica si existe una pagina posterior.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Numero de elementos de la pagina actual.</summary>
    public int Count => Items.Count;
}
