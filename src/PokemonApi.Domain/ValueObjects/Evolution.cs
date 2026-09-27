namespace PokemonApi.Domain.ValueObjects;

/// <summary>
/// Evolucion posible a partir de un Pokemon concreto.
/// </summary>
/// <param name="TargetId">Identificador de la Pokédex del Pokemon resultante.</param>
/// <param name="TargetName">Slug del Pokemon resultante.</param>
/// <param name="TargetDisplayName">Nombre presentable del Pokemon resultante.</param>
/// <param name="Requirements">
/// Conjunto de condiciones que habilitan la evolucion. Puede contener mas de
/// una entrada cuando las condiciones son alternativas entre si, como en el
/// caso de Eevee, que evoluciona a Glaceon con cualquier piedra de hielo o con
/// un movimiento de tipo hielo.
/// </param>
public sealed record Evolution(
    int TargetId,
    Slug TargetName,
    string TargetDisplayName,
    IReadOnlyList<EvolutionRequirement> Requirements);
