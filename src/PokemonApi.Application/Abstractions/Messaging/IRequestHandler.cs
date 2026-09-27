namespace PokemonApi.Application.Abstractions.Messaging;

/// <summary>
/// Manejador de una peticion.
/// </summary>
/// <typeparam name="TRequest">Tipo de la peticion.</typeparam>
/// <typeparam name="TResponse">Tipo de la respuesta.</typeparam>
/// <remarks>
/// Se registra en el contenedor de inyeccion de dependencias con ciclo de vida
/// <see cref="Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped"/>,
/// por lo que puede depender de otros servicios con el mismo ciclo de vida.
/// </remarks>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Ejecuta el caso de uso.</summary>
    /// <param name="request">Peticion recibida.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Resultado del caso de uso.</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
