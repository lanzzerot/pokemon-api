namespace PokemonApi.Api.Infrastructure;

/// <summary>
/// Nombres de las politicas de limitacion de peticiones.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>Limite por defecto para el resto de endpoints.</summary>
    public const string Default = "default";

    /// <summary>Limite mas alto para los catalogos, que son baratos y estables.</summary>
    public const string Catalog = "catalog";

    /// <summary>
    /// Duracion de la ventana de limitacion.
    /// </summary>
    /// <remarks>
    /// Se declara aqui y no en cada politica porque es tambien el valor que se
    /// anuncia en la cabecera <c>Retry-After</c> al rechazar una peticion. Que
    /// ambos coincidan evita anunciar un tiempo que no es real.
    /// </remarks>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <summary>Peticiones por ventana en la politica por defecto.</summary>
    public const int DefaultPermitLimit = 120;

    /// <summary>Peticiones por ventana en la politica de catalogos.</summary>
    public const int CatalogPermitLimit = 300;
}
