namespace PokemonApi.Application.Abstractions.Messaging;

/// <summary>
/// Punto de entrada para ejecutar casos de uso.
/// </summary>
/// <remarks>
/// Los endpoints HTTP no llaman a los handlers directamente: envian la
/// peticion a traves del <see cref="ISender"/>. Gracias a esa indireccion,
/// los comportamientos transversales se aplican automaticamente y un endpoint no
/// necesita saber nada sobre la implementacion del caso de uso.
/// </remarks>
public interface ISender
{
    /// <summary>Envia una peticion a su handler.</summary>
    /// <typeparam name="TResponse">Tipo de la respuesta esperada.</typeparam>
    /// <param name="request">Peticion a ejecutar.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Resultado del caso de uso.</returns>
    Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
