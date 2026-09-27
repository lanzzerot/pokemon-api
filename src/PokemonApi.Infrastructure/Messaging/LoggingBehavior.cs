using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Domain.Common;
using PokemonApi.Infrastructure.Logging;

namespace PokemonApi.Infrastructure.Messaging;

/// <summary>
/// Registra el inicio y el final de cada peticion, con su resultado.
/// </summary>
/// <remarks>
/// <para>
/// Se registra como comportamiento abierto (<c>IPipelineBehavior&lt;,&gt;</c>),
/// de modo que el mismo tipo sirve para todos los casos de uso sin codigo
/// repetido.
/// </para>
/// <para>
/// El nivel de log se elige segun el resultado, y para eso no hace falta conocer
/// el tipo concreto de la respuesta: todos los casos de uso devuelven un
/// <see cref="Result"/>, que es donde vive el indicador de exito.
/// </para>
/// </remarks>
/// <typeparam name="TRequest">Tipo de la peticion.</typeparam>
/// <typeparam name="TResponse">Tipo de la respuesta.</typeparam>
/// <param name="logger">Registrador asociado al comportamiento.</param>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger = logger;

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startTimestamp = Stopwatch.GetTimestamp();

        InfrastructureLog.RequestStarted(_logger, requestName);

        try
        {
            var response = await next(cancellationToken).ConfigureAwait(false);
            var elapsed = ElapsedMilliseconds(startTimestamp);

            if (response is Result { IsSuccess: false })
            {
                InfrastructureLog.RequestCompletedWithDomainError(_logger, requestName, elapsed);
            }
            else
            {
                InfrastructureLog.RequestHandled(_logger, requestName, elapsed);
            }

            return response;
        }
        catch (Exception exception)
        {
            InfrastructureLog.RequestFailed(
                _logger,
                exception,
                requestName,
                ElapsedMilliseconds(startTimestamp));

            throw;
        }
    }

    private static double ElapsedMilliseconds(long startTimestamp) =>
        Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
}
