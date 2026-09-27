using System.Diagnostics.CodeAnalysis;

namespace PokemonApi.Domain.Common;

/// <summary>
/// Resultado de una operacion que puede fallar sin lanzar excepciones.
/// </summary>
/// <remarks>
/// Se usa para los fallos esperados (validacion, recurso inexistente), que son
/// parte del flujo normal de negocio. Los errores de programacion y los fallos
/// tecnicos siguen propagandose como excepciones.
/// </remarks>
public class Result
{
    /// <summary>Inicializa un resultado con el estado y el error indicados.</summary>
    /// <param name="isSuccess">Indica si la operacion fue exitosa.</param>
    /// <param name="error">Error asociado; es <see langword="null"/> si la operacion fue exitosa.</param>
    /// <exception cref="ArgumentException">Si <paramref name="isSuccess"/> es <see langword="true"/> y hay error, o al reves.</exception>
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess == (error is not null))
        {
            throw new ArgumentException(
                isSuccess ? "A successful result cannot carry an error." : "A failed result must carry an error.",
                nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Indica si la operacion fue exitosa.</summary>
    public bool IsSuccess { get; }

    /// <summary>Indica si la operacion fallo.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Error asociado al fallo, o <see langword="null"/> si la operacion fue exitosa.</summary>
    public Error? Error { get; }

    /// <summary>Crea un resultado exitoso sin valor.</summary>
    public static Result Success() => new(true, null);

    /// <summary>Crea un resultado fallido.</summary>
    public static Result Failure(Error error) => new(false, error);
}

/// <summary>
/// Resultado de una operacion fallible que ademas produce un valor.
/// </summary>
/// <typeparam name="TValue">Tipo del valor produced en caso de exito.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(bool isSuccess, TValue? value, Error? error)
        : base(isSuccess, error) => _value = value;

    /// <summary>
    /// Valor producido por la operacion.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se invoca sobre un resultado fallido.</exception>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    /// <summary>Intenta obtener el valor sin lanzar excepcion.</summary>
    /// <param name="value">Valor producido, si la operacion fue exitosa.</param>
    /// <returns><see langword="true"/> si la operacion fue exitosa.</returns>
    public bool TryGetValue([NotNullWhen(true)] out TValue? value)
    {
        value = _value;
        return IsSuccess;
    }

    // CA1000 (no declarar miembros estaticos en tipos genericos) no aplica a las
    // fabricas de Result: `Result<T>.Success(...)` y `Result<T>.Failure(...)` son
    // la raison d'etre del patron y moverlos a un tipo helper obligaria al
    // llamante a perder la inferencia de tipo en cada construccion de resultado.
#pragma warning disable CA1000

    /// <summary>Crea un resultado exitoso con el valor indicado.</summary>
    public static Result<TValue> Success(TValue value) => new(true, value, null);

    /// <summary>Crea un resultado fallido.</summary>
    public static new Result<TValue> Failure(Error error) => new(false, default, error);

#pragma warning restore CA1000
}
