namespace PokemonApi.Application.Abstractions.Messaging;

/// <summary>
/// Comportamiento transversal que envuelve la ejecucion de un handler.
/// </summary>
/// <typeparam name="TRequest">Tipo de la peticion.</typeparam>
/// <typeparam name="TResponse">Tipo de la respuesta.</typeparam>
/// <remarks>
/// Los comportamientos se encadenan en el orden de registro y el ultimo
/// registrado es el mas externo. Si uno de ellos no invoca al siguiente, la
/// peticion no llega al handler: es el mecanismo habitual para implementar
/// cache, validacion o cortocircuitos.
/// </remarks>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Ejecuta el comportamiento y continua la cadena.</summary>
    /// <param name="request">Peticion recibida.</param>
    /// <param name="next">Siguiente eslabon de la cadena.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Resultado del handler.</returns>
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}

/// <summary>
/// Delegado que representa el siguiente eslabon de la cadena de comportamientos.
/// </summary>
/// <typeparam name="TResponse">Tipo de la respuesta.</typeparam>
/// <param name="cancellationToken">Token de cancelacion.</param>
/// <returns>Resultado del handler.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken);
