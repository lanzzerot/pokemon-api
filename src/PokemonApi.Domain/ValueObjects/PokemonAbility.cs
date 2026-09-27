namespace PokemonApi.Domain.ValueObjects;

/// <summary>
/// Habilidad que un Pokemon puede poseer.
/// </summary>
/// <param name="Name">Slug canonico de la habilidad (p. ej. <c>static</c>).</param>
/// <param name="DisplayName">Nombre presentable (p. ej. <c>Static</c>).</param>
/// <param name="IsHidden">
/// Indica si es una habilidad oculta. Las habilidades ocultas no aparecen en
/// la lista del Pokemon y se aprenden al evolucionar, lo que las convierte en
/// un rasgo caracteristico de la especie.
/// </param>
/// <param name="Slot">
/// Posicion que ocupa en la especie: 1 la primera, 2 la segunda y 3 la oculta.
/// </param>
public sealed record PokemonAbility(Slug Name, string DisplayName, bool IsHidden, int Slot);
