using PokemonApi.Application.Common.Pagination;
using PokemonApi.Application.Common.Queries;
using PokemonApi.Domain.Entities;

namespace PokemonApi.Application.Abstractions.Repositories;

/// <summary>
/// Fuente de lectura del catalogo de Pokemon.
/// </summary>
/// <remarks>
/// Vive en la capa de aplicacion y no en la de infraestructura: la inversion de
/// dependencias permite que los casos de uso se prueben con dobles de test sin
/// levantar base de datos ni cargar el dataset real.
/// </remarks>
public interface IPokemonRepository
{
    /// <summary>Devuelve un Pokemon por su identificador de la Poke&#x27;dex.</summary>
    /// <param name="id">Identificador de la Poke&#x27;dex.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El Pokemon, o <see langword="null"/> si no existe.</returns>
    Task<Pokemon?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Devuelve un Pokemon por su nombre canonico.</summary>
    /// <param name="name">Slug canonico del nombre (p. ej. <c>pikachu</c>).</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El Pokemon, o <see langword="null"/> si no existe.</returns>
    Task<Pokemon?> GetByNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Devuelve una pagina de Pokemon que cumplen el filtro indicado.
    /// </summary>
    /// <param name="query">Filtros, orden y paginacion solicitados.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La pagina de resultados y el total de coincidencias.</returns>
    Task<PagedResult<Pokemon>> SearchAsync(PokemonSearchQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Devuelve los Pokemon de una cadena evolutiva, con la forma base primero
    /// y el resto ordenados por identificador.
    /// </summary>
    /// <remarks>
    /// El orden importa: los casos de uso presentan la cadena como una progresion
    /// desde la forma base, y un orden derivado solo del identificador no la
    /// respectaria.
    /// </remarks>
    /// <param name="chainId">Identificador de la cadena evolutiva.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Los Pokemon que componen la cadena.</returns>
    Task<IReadOnlyList<Pokemon>> GetEvolutionChainAsync(int chainId, CancellationToken cancellationToken);
}
