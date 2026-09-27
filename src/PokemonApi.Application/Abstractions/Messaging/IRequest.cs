namespace PokemonApi.Application.Abstractions.Messaging;

/// <summary>
/// Peticion que produce un <typeparamref name="TResponse"/> cuando se envia a
/// travers del <see cref="ISender"/>.
/// </summary>
/// <typeparam name="TResponse">Tipo de la respuesta del caso de uso.</typeparam>
/// <remarks>
/// Los casos de uso se modelan como mensajes con un unico handler, de modo que
/// anadir comportamiento transversal (validacion, logging, cache) no obliga a
/// tocar la logica de negocio.
/// </remarks>
public interface IRequest<out TResponse>;
