using PokemonApi.Application.Common.Validation;

namespace PokemonApi.Application.Common.Pagination;

/// <summary>
/// Pagina una solicitud del cliente.
/// </summary>
/// <param name="Page">Pagina solicitada, empezando en 1.</param>
/// <param name="PageSize">Cantidad de elementos por pagina.</param>
/// <param name="MaxPageSize">Tope maximo admitted por pagina, definido por el servidor.</param>
public readonly record struct PageRequest(int Page, int PageSize, int MaxPageSize)
{
    /// <summary>Valor minimo de pagina.</summary>
    public const int FirstPage = 1;

    /// <summary>Cantidad de elementos por pagina cuando el cliente no indica ninguna.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Devuelve una solicitud de pagina con los valores por defecto.</summary>
    /// <param name="maxPageSize">Tope maximo admitido por pagina.</param>
    /// <returns>La primera pagina con el tamano por defecto.</returns>
    public static PageRequest Default(int maxPageSize) => new(FirstPage, DefaultPageSize, maxPageSize);

    /// <summary>Normaliza y valida los parametros de paginacion recibidos.</summary>
    /// <param name="page">Pagina solicitada, o <see langword="null"/> para usar la primera.</param>
    /// <param name="pageSize">Tamano de pagina, o <see langword="null"/> para usar el valor por defecto.</param>
    /// <param name="maxPageSize">Tope maximo admitido por pagina.</param>
    /// <returns>La solicitud normalizada, o un error de validacion.</returns>
    public static Domain.Common.Result<PageRequest> Create(int? page, int? pageSize, int maxPageSize)
    {
        var failures = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (page is < FirstPage)
        {
            failures[nameof(page)] = [$"The page must be greater than or equal to {FirstPage}."];
        }

        if (pageSize is <= 0)
        {
            failures[nameof(pageSize)] = ["The page size must be greater than zero."];
        }
        else if (pageSize > maxPageSize)
        {
            failures[nameof(pageSize)] = [$"The page size must be at most {maxPageSize}."];
        }

        return failures.Count > 0
            ? Domain.Common.Result<PageRequest>.Failure(
                Domain.Common.Error.Validation("pagination.invalid", "The pagination parameters are invalid."))
            : Domain.Common.Result<PageRequest>.Success(
                new PageRequest(page ?? FirstPage, pageSize ?? DefaultPageSize, maxPageSize));
    }

    /// <summary>Cantidad de elementos a omitir para alcanzar la primera fila de la pagina.</summary>
    public int Skip => (Page - FirstPage) * PageSize;
}
