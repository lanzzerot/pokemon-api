using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using PokemonApi.Application.Abstractions.Messaging;

namespace PokemonApi.Infrastructure.Messaging;

/// <summary>
/// Ejecutor no generico de una peticion concreta.
/// </summary>
/// <remarks>
/// Existe para que el mediator pueda cachear, por cada tipo de peticion, un
/// ejecutor ya cerrado sobre los tipos <c>TRequest</c> y <c>TResponse</c>. Sin
/// este indireccion, resolver el tipo de respuesta exigiria reflexion en cada
/// peticion; con el, el coste se paga una sola vez por tipo.
/// </remarks>
internal interface IRequestExecutor
{
    /// <summary>Ejecuta la peticion a traves de la cadena de comportamientos.</summary>
    /// <param name="serviceProvider">Contenedor de inyeccion de dependencias.</param>
    /// <param name="request">Peticion recibida.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La respuesta del handler.</returns>
    Task<object?> ExecuteAsync(
        IServiceProvider serviceProvider,
        object request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Implementacion de <see cref="ISender"/> que localiza el handler de cada
/// peticion en el contenedor de inyeccion de dependencias y lo ejecuta a traves
/// de la cadena de comportamientos transversales.
/// </summary>
/// <remarks>
/// <para>
/// Es un mediator propio, deliberadamente minimo, en lugar de una libreria
/// externa. La API solo necesita resolver un unico handler por tipo de peticion
/// y ejecutar una lista de comportamientos: implementar esas dos ideas en unas
/// pocas decenas de lineas evita sumar una dependencia externa y deja el flujo de
/// ejecucion completamente visible para quien lea el repositorio.
/// </para>
/// <para>
/// Los ejecutores se cachean por tipo de peticion en un diccionario concurrente,
/// de modo que la reflexion necesaria para cerrar los tipos genericos ocurre una
/// unica vez por tipo, no en cada peticion.
/// </para>
/// </remarks>
public sealed class Mediator(IServiceProvider serviceProvider) : ISender
{
    private static readonly ConcurrentDictionary<Type, IRequestExecutor> Executors = new();

    /// <inheritdoc />
    public Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var executor = Executors.GetOrAdd(
            request.GetType(),
            static requestType => CreateExecutor(requestType));

        return ExecuteAsync<TResponse>(serviceProvider, executor, request, cancellationToken);
    }

    private static async Task<TResponse> ExecuteAsync<TResponse>(
        IServiceProvider serviceProvider,
        IRequestExecutor executor,
        IRequest<TResponse> request,
        CancellationToken cancellationToken)
    {
        var response = await executor
            .ExecuteAsync(serviceProvider, request, cancellationToken)
            .ConfigureAwait(false);

        return (TResponse)response!;
    }

    private static IRequestExecutor CreateExecutor(Type requestType)
    {
        // TResponse solo se conoce a traves de la interfaz IRequest<TResponse>
        // que implementa la peticion, de modo que se localiza por reflexion y se
        // cierra el tipo generico con MakeGenericType.
        var requestContract = requestType
            .GetInterfaces()
            .FirstOrDefault(contract =>
                contract.IsGenericType
                && contract.GetGenericTypeDefinition() == typeof(IRequest<>));

        if (requestContract is null)
        {
            throw new InvalidOperationException(
                $"'{requestType.Name}' does not implement IRequest<TResponse>.");
        }

        var responseType = requestContract.GetGenericArguments()[0];
        var executorType = typeof(RequestExecutor<,>).MakeGenericType(requestType, responseType);

        return (IRequestExecutor)Activator.CreateInstance(executorType)!;
    }

    /// <summary>
    /// Ejecutor cerrado sobre una combinacion concreta de peticion y respuesta.
    /// </summary>
    /// <typeparam name="TRequest">Tipo de la peticion.</typeparam>
    /// <typeparam name="TResponse">Tipo de la respuesta.</typeparam>
    private sealed class RequestExecutor<TRequest, TResponse> : IRequestExecutor
        where TRequest : IRequest<TResponse>
    {
        public async Task<object?> ExecuteAsync(
            IServiceProvider serviceProvider,
            object request,
            CancellationToken cancellationToken)
        {
            var typedRequest = (TRequest)request;

            var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>()
                ?? throw new InvalidOperationException(
                    $"No handler is registered for '{typeof(TRequest).Name}'. " +
                    "Did you forget to call AddApplication()?");

            // El ultimo eslabon de la cadena es el propio handler.
            RequestHandlerDelegate<TResponse> pipeline =
                token => handler.HandleAsync(typedRequest, token);

            // Se recorre en orden inverso al de registro para que el primer
            // comportamiento registrado sea el mas externo y, por tanto, el
            // primero que ve la peticion.
            foreach (var behavior in serviceProvider
                         .GetServices<IPipelineBehavior<TRequest, TResponse>>()
                         .Reverse())
            {
                var current = pipeline;
                var next = behavior;

                pipeline = token => next.HandleAsync(typedRequest, current, token);
            }

            return await pipeline(cancellationToken).ConfigureAwait(false);
        }
    }
}
