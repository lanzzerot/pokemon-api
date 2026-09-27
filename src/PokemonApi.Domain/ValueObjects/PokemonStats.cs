using PokemonApi.Domain.Enumerations;

namespace PokemonApi.Domain.ValueObjects;

/// <summary>
/// Las seis estadisticas base de un Pokemon, junto con su suma.
/// </summary>
/// <remarks>
/// Es un value object inmutable: dos Pokemon con las mismas estadisticas son
/// estadisticamente iguales. La suma se calcula una unica vez en la
/// construccion porque se usa en el filtro y en el ordenamiento por
/// rendimiento, que son las operaciones mas frecuentes de la API.
/// </remarks>
public sealed record PokemonStats
{
    /// <summary>Valor minimo admisible para una estadistica base.</summary>
    public const int MinValue = 1;

    /// <summary>Valor maximo admisible para una estadistica base.</summary>
    public const int MaxValue = 1000;

    private PokemonStats(int hp, int attack, int defense, int specialAttack, int specialDefense, int speed)
    {
        Hp = hp;
        Attack = attack;
        Defense = defense;
        SpecialAttack = specialAttack;
        SpecialDefense = specialDefense;
        Speed = speed;
        Total = hp + attack + defense + specialAttack + specialDefense + speed;
    }

    /// <summary>Puntos de vida.</summary>
    public int Hp { get; }

    /// <summary>Ataque fisico.</summary>
    public int Attack { get; }

    /// <summary>Defensa fisica.</summary>
    public int Defense { get; }

    /// <summary>Ataque especial.</summary>
    public int SpecialAttack { get; }

    /// <summary>Defensa especial.</summary>
    public int SpecialDefense { get; }

    /// <summary>Velocidad.</summary>
    public int Speed { get; }

    /// <summary>Suma de las seis estadisticas base.</summary>
    public int Total { get; }

    /// <summary>Devuelve el valor de una estadistica concreta.</summary>
    /// <param name="stat">Estadistica solicitada.</param>
    /// <returns>El valor base de la estadistica.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="stat"/> no esta definido.</exception>
    public int this[PokemonStat stat] => stat switch
    {
        PokemonStat.Hp => Hp,
        PokemonStat.Attack => Attack,
        PokemonStat.Defense => Defense,
        PokemonStat.SpecialAttack => SpecialAttack,
        PokemonStat.SpecialDefense => SpecialDefense,
        PokemonStat.Speed => Speed,
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, "Unknown Pokemon stat."),
    };

    /// <summary>
    /// Crea un conjunto de estadisticas validando los rangos de cada valor.
    /// </summary>
    /// <param name="hp">Puntos de vida.</param>
    /// <param name="attack">Ataque fisico.</param>
    /// <param name="defense">Defensa fisica.</param>
    /// <param name="specialAttack">Ataque especial.</param>
    /// <param name="specialDefense">Defensa especial.</param>
    /// <param name="speed">Velocidad.</param>
    /// <exception cref="ArgumentOutOfRangeException">Si algun valor esta fuera de rango.</exception>
    public static PokemonStats Create(
        int hp,
        int attack,
        int defense,
        int specialAttack,
        int specialDefense,
        int speed)
    {
        Validate(hp, nameof(hp));
        Validate(attack, nameof(attack));
        Validate(defense, nameof(defense));
        Validate(specialAttack, nameof(specialAttack));
        Validate(specialDefense, nameof(specialDefense));
        Validate(speed, nameof(speed));

        return new PokemonStats(hp, attack, defense, specialAttack, specialDefense, speed);
    }

    /// <summary>
    /// Indica si <paramref name="other"/> supera a este Pokemon en todas sus
    /// estadisticas. Se usa para el filtro "<c>dominates</c>".
    /// </summary>
    /// <param name="other">Pokemon de referencia.</param>
    /// <returns><see langword="true"/> si todas las estadisticas son estrictamente mayores.</returns>
    public bool Dominates(PokemonStats other) =>
        Hp > other.Hp
        && Attack > other.Attack
        && Defense > other.Defense
        && SpecialAttack > other.SpecialAttack
        && SpecialDefense > other.SpecialDefense
        && Speed > other.Speed;

    private static void Validate(int value, string parameterName)
    {
        if (value is < MinValue or > MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"A base stat must be between {MinValue} and {MaxValue}.");
        }
    }
}
