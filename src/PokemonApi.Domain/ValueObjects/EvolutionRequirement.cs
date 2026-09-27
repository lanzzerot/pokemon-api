namespace PokemonApi.Domain.ValueObjects;

/// <summary>
/// Condicion que debe cumplirse para que un Pokemon evolucione.
/// </summary>
/// <remarks>
/// <para>
/// Solo un subconjunto de las propiedades tiene valor: las demas quedan a
/// <see langword="null"/> porque no aplican a esa transicion. Esta
/// representacion espeja la realidad de los juegos, donde un Pokemon puede
/// evolucionar por nivel, por objeto, por intercambio o por una combinacion de
/// condiciones.
/// </para>
/// <para>
/// El mismo par de Pokemon puede admitir varias condiciones excluyentes entre
/// si; ese caso se modela con varias instancias en
/// <see cref="Evolution.Requirements"/>.
/// </para>
/// </remarks>
/// <param name="Trigger">Evento que dispara la evolucion (p. ej. <c>level-up</c>, <c>use-item</c>, <c>trade</c>).</param>
/// <param name="MinLevel">Nivel minimo necesario.</param>
/// <param name="Item">Objeto que se consume al evolucionar.</param>
/// <param name="HeldItem">Objeto que debe llevar encima al evolucionar.</param>
/// <param name="Location">Lugar donde debe producirse la evolucion.</param>
/// <param name="Gender">Genero requerido: <c>female</c> o <c>male</c>.</param>
/// <param name="KnownMove">Movimiento que debe conocer.</param>
/// <param name="KnownMoveType">Tipo que debe tener el movimiento conocido.</param>
/// <param name="TimeOfDay">Momento del dia requerido (<c>day</c> o <c>night</c>).</param>
/// <param name="MinHappiness">Felicidad minima.</param>
/// <param name="MinAffection">Afecto minimo.</param>
/// <param name="MinBeauty">Belleza minima.</param>
/// <param name="NeedsOverworldRain">Si requiere que este lloviendo en el mundo exterior.</param>
/// <param name="TurnUpsideDown">Si requiere mantener la consola boca abajo.</param>
/// <param name="RelativePhysicalStats">
/// Comparacion de estadisticas fisicas exigida. La fuente de datos la expresa
/// como un entero: <c>1</c> si el ataque debe superar a la defensa, <c>0</c> si
/// deben ser iguales y <c>-1</c> si la defensa debe superar al ataque.
/// </param>
/// <param name="PartyType">Tipo de Pokemon que debe acompanar al grupo.</param>
/// <param name="TradeSpecies">Especie a la que hay que intercambiarlo.</param>
public sealed record EvolutionRequirement(
    string Trigger,
    int? MinLevel = null,
    Slug? Item = null,
    Slug? HeldItem = null,
    Slug? Location = null,
    string? Gender = null,
    Slug? KnownMove = null,
    Slug? KnownMoveType = null,
    string? TimeOfDay = null,
    int? MinHappiness = null,
    int? MinAffection = null,
    int? MinBeauty = null,
    bool NeedsOverworldRain = false,
    bool TurnUpsideDown = false,
    int? RelativePhysicalStats = null,
    Slug? PartyType = null,
    Slug? TradeSpecies = null);
