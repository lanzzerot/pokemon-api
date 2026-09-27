using PokemonApi.Application.Common.Pagination;

namespace PokemonApi.Application.Common.Pagination;

/// <summary>
/// Envoltura de una pagina de resultados junto con sus metadatos de
/// paginacion, tal y como se expone en la respuesta HTTP.
/// </summary>
/// <typeparam name="T">Tipo de los elementos.</typeparam>
/// <param name="Items">Elementos de la pagina actual.</param>
/// <param name="Page">Numero de la pagina actual, empezando en 1.</param>
/// <param name="PageSize">Cantidad de elementos por pagina.</param>
/// <param name="TotalCount">Total de elementos que cumplen el filtro.</param>
/// <param name="TotalPages">Total de paginas disponibles.</param>
/// <param name="HasPreviousPage">Si existe una pagina anterior.</param>
/// <param name="HasNextPage">Si existe una pagina posterior.</param>
public sealed record PageResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    // CA1000 (no declarar miembros estaticos en tipos genericos) no aplica a las
    // fabricas de la respuesta: son el punto de entrada natural al tipo y
    // extraerlas a una clase no generica obligaria a repetir el tipo en cada
    // llamada de cada endpoint.
#pragma warning disable CA1000

    /// <summary>Convierte un resultado paginado interno en su representacion de API.</summary>
    /// <param name="result">Resultado paginado interno.</param>
    /// <returns>La representacion de API.</returns>
    public static PageResponse<T> FromResult(PagedResult<T> result) => new(
        result.Items,
        result.Page,
        result.PageSize,
        result.TotalCount,
        result.TotalPages,
        result.HasPreviousPage,
        result.HasNextPage);

    /// <summary>Crea una respuesta de pagina vacia.</summary>
    /// <param name="page">Pagina solicitada.</param>
    /// <param name="pageSize">Tamano de pagina.</param>
    /// <returns>Una respuesta sin elementos.</returns>
    public static PageResponse<T> Empty(int page, int pageSize) => new(
        [],
        page,
        pageSize,
        0,
        0,
        page > 1,
        false);

#pragma warning restore CA1000
}
