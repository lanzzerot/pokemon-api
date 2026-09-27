using System.Diagnostics.CodeAnalysis;

namespace PokemonApi.Application.Common.Validation;

/// <summary>
/// Error de validacion de una peticion de entrada.
/// </summary>
/// <remarks>
/// Se propaga como excepcion y se convierte en un <c>ProblemDetails</c> con
/// codigo HTTP 400. Se prefiere a un tipo de retorno validado porque la
/// validacion ocurre en el borde del sistema, no en el centro del dominio, y
/// anadir el caso de fallo a cada firma ensuciaria los casos de uso sin
/// aportar claridad.
/// </remarks>
public sealed class ValidationException : Exception
{
    /// <summary>Inicializa la excepcion a partir de los fallos detectados.</summary>
    /// <param name="failures">Fallos de validacion, indexados por el nombre del campo.</param>
    public ValidationException(IReadOnlyDictionary<string, string[]> failures)
        : base("One or more validation errors occurred.")
    {
        ArgumentNullException.ThrowIfNull(failures);
        Failures = failures;
    }

    /// <summary>Fallos detectados, indexados por el nombre del campo.</summary>
    public IReadOnlyDictionary<string, string[]> Failures { get; }
}

/// <summary>
/// Valida una peticion antes de que llegue al handler.
/// </summary>
/// <typeparam name="TRequest">Tipo de la peticion validada.</typeparam>
public interface IValidator<in TRequest>
{
    /// <summary>Valida la peticion.</summary>
    /// <param name="request">Peticion a validar.</param>
    /// <returns>
    /// Un diccionario de fallos indexado por el nombre del campo. Un
    /// diccionario vacio significa que la peticion es valida.
    /// </returns>
    IReadOnlyDictionary<string, string[]> Validate(TRequest request);
}

/// <summary>
/// Valida una peticion antes de que llegue al handler, cuando comprobar la
/// validez exige consultar el catalogo.
/// </summary>
/// <remarks>
/// Separado de <see cref="IValidator{TRequest}"/> porque un filtro como
/// <c>types=fire</c> solo puede juzgarse consultando los tipos existentes, y esa
/// consulta es asincrona. Forzar la comprobacion dentro de un
/// <see cref="IValidator{TRequest}"/> obligaria a bloquear el hilo o a cambiar el
/// contrato de los repositorios, que es justo lo que se queria evitar.
/// </remarks>
/// <typeparam name="TRequest">Tipo de la peticion validada.</typeparam>
public interface IAsyncValidator<in TRequest>
{
    /// <summary>Valida la peticion consultando las fuentes de datos necesarias.</summary>
    /// <param name="request">Peticion a validar.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>
    /// Un diccionario de fallos indexado por el nombre del campo. Un
    /// diccionario vacio significa que la peticion es valida.
    /// </returns>
    ValueTask<IReadOnlyDictionary<string, string[]>> ValidateAsync(
        TRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Extensiones para componer reglas de validacion de forma declarativa.
/// </summary>
public static class ValidationRules
{
    /// <summary>Indica que un valor obligatorio no fue proporcionado.</summary>
    /// <param name="value">Valor recibido.</param>
    /// <returns><see langword="true"/> si el valor falta.</returns>
    public static bool IsMissing([NotNullWhen(false)] string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>Indica que un entero no cabe en el rango permitido.</summary>
    /// <param name="value">Valor recibido.</param>
    /// <param name="min">Minimo permitido, inclusive.</param>
    /// <param name="max">Maximo permitido, inclusive.</param>
    /// <returns><see langword="true"/> si el valor esta fuera de rango.</returns>
    public static bool IsOutOfRange(int value, int min, int max) => value < min || value > max;
}
