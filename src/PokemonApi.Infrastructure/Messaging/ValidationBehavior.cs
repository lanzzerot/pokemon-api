using Microsoft.Extensions.DependencyInjection;
using PokemonApi.Application.Abstractions.Messaging;
using PokemonApi.Application.Common.Validation;

namespace PokemonApi.Infrastructure.Messaging;

/// <summary>
/// Ejecuta los <see cref="IValidator{TRequest}"/> registrados para la peticion
/// antes de invocar al handler.
/// </summary>
/// <remarks>
/// <para>
/// La validacion vive en la capa de aplicacion, pero se ejecuta aqui, en la
/// cadena, para que ningun endpoint pueda saltarsela: basta con enviar la
/// peticion a traves de <see cref="ISender"/>.
/// </para>
/// <para>
/// Un comportamiento abierto no puede resolver
/// <c>IEnumerable&lt;IValidator&lt;TRequest&gt;&gt;</c> de forma estatica, asi que
/// se le inyecta el contenedor y se resuelve el servicio ya cerrado. La
/// alternativa habitual, un comportamiento cerrado escrito a mano por peticion,
/// se repite tantas veces como casos de uso.
/// </para>
/// <para>
/// Las reglas se ejecutan en dos fases porque no todas dependen solo de la
/// peticion: las invariantes se comprueban con los validadores sincronos y los
/// valores de catalogo con los que consultan los repositorios. Ambas fases se
/// ejecutan siempre, aunque la primera ya haya fallado, para que el cliente
/// reciba en un unico 400 el informe completo en lugar de descubrir los
/// problemas de uno en uno.
/// </para>
/// </remarks>
/// <typeparam name="TRequest">Tipo de la peticion validada.</typeparam>
/// <typeparam name="TResponse">Tipo de la respuesta.</typeparam>
/// <param name="serviceProvider">Contenedor de inyeccion de dependencias.</param>
public sealed class ValidationBehavior<TRequest, TResponse>(IServiceProvider serviceProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validators = _serviceProvider.GetServices<IValidator<TRequest>>().ToArray();
        var asyncValidators = _serviceProvider.GetServices<IAsyncValidator<TRequest>>().ToArray();

        if (validators.Length == 0 && asyncValidators.Length == 0)
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        // Se acumulan los fallos de todos los validadores para que el cliente
        // reciba el informe completo en una sola respuesta, en lugar de
        // descubrir los problemas de uno en uno.
        var failures = new Dictionary<string, string[]>(StringComparer.Ordinal);

        foreach (var validator in validators)
        {
            Merge(failures, validator.Validate(request));
        }

        foreach (var validator in asyncValidators)
        {
            Merge(failures, await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false));
        }

        return failures.Count > 0
            ? throw new ValidationException(failures)
            : await next(cancellationToken).ConfigureAwait(false);
    }

    private static void Merge(
        Dictionary<string, string[]> failures,
        IReadOnlyDictionary<string, string[]> found)
    {
        foreach (var (field, messages) in found)
        {
            failures[field] = failures.TryGetValue(field, out var existing)
                ? [.. existing, .. messages]
                : messages;
        }
    }
}
