using PokemonApi.Application.Common.Validation;

namespace PokemonApi.Application.Features.Pokemon.List;

/// <summary>
/// Valida los filtros de <see cref="ListPokemonQuery"/> que dependen del
/// catalogo.
/// </summary>
/// <remarks>
/// <para>
/// Un tipo, una habilidad o una generacion solo existen si aparecen en los
/// datos, asi que comprobarlos exige consultar el catalogo. Esa comprobacion no
/// cabe en un <see cref="IValidator{TRequest}"/> sincrono, y por eso se ejecuta
/// como <see cref="IAsyncValidator{TRequest}"/>.
/// </para>
/// <para>
/// Vive en la cadena de validacion, y no solo en el builder, por una razon que
/// el cliente percibe: si un 400 no puede reunir a la vez los errores
/// estructurales y los de catalogo, un <c>page=0</c> junto a un
/// <c>types=plastic</c> obligaria a descubrir los problemas de uno en uno. Al
/// ejecutarse ambas fases siempre, un unico 400 nombra todos los campos que
/// corregir.
/// </para>
/// <para>
/// Las reglas no se duplican: se delegan en
/// <see cref="PokemonSearchQueryBuilder.ValidateAsync"/>, que es el unico sitio
/// donde se decide que valores de catalogo admite cada filtro.
/// </para>
/// </remarks>
/// <param name="queryBuilder">Traductor de la peticion, que conoce el catalogo.</param>
public sealed class ListPokemonCatalogValidator(PokemonSearchQueryBuilder queryBuilder)
    : IAsyncValidator<ListPokemonQuery>
{
    private readonly PokemonSearchQueryBuilder _queryBuilder = queryBuilder;

    /// <inheritdoc />
    public ValueTask<IReadOnlyDictionary<string, string[]>> ValidateAsync(
        ListPokemonQuery request,
        CancellationToken cancellationToken) =>
        _queryBuilder.ValidateAsync(request, cancellationToken);
}
