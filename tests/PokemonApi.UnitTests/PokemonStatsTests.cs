using Shouldly;
using PokemonApi.Domain.Enumerations;
using PokemonApi.Domain.ValueObjects;

namespace PokemonApi.UnitTests;

/// <summary>
/// Pruebas de <see cref="PokemonStats"/>, en particular del total derivado que
/// expone el detalle de cada Pokemon.
/// </summary>
public sealed class PokemonStatsTests
{
    [Fact]
    public void Create_computes_the_total()
    {
        // Act
        var stats = PokemonStats.Create(35, 55, 40, 50, 50, 90);

        // Assert
        stats.Total.ShouldBe(320);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Create_rejects_values_outside_the_allowed_range(int invalid)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(
            () => PokemonStats.Create(invalid, 55, 40, 50, 50, 90));
    }

    [Fact]
    public void Indexer_returns_each_stat()
    {
        // Arrange
        var stats = PokemonStats.Create(35, 55, 40, 50, 50, 90);

        // Assert
        stats[PokemonStat.Hp].ShouldBe(35);
        stats[PokemonStat.Attack].ShouldBe(55);
        stats[PokemonStat.Defense].ShouldBe(40);
        stats[PokemonStat.SpecialAttack].ShouldBe(50);
        stats[PokemonStat.SpecialDefense].ShouldBe(50);
        stats[PokemonStat.Speed].ShouldBe(90);
    }

    [Fact]
    public void Indexer_throws_for_an_undefined_stat()
    {
        // Arrange
        var stats = PokemonStats.Create(35, 55, 40, 50, 50, 90);

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => stats[(PokemonStat)99]);
    }

    [Fact]
    public void Dominates_requires_every_stat_to_be_greater()
    {
        // Arrange
        var superior = PokemonStats.Create(100, 100, 100, 100, 100, 100);
        var inferior = PokemonStats.Create(80, 90, 70, 95, 60, 70);

        // Assert
        superior.Dominates(inferior).ShouldBeTrue();
        inferior.Dominates(superior).ShouldBeFalse();
    }

    [Fact]
    public void Dominates_is_false_when_a_single_stat_ties()
    {
        // Arrange: right supera a left en cinco estadisticas y empata en la sexta.
        var left = PokemonStats.Create(100, 100, 100, 100, 100, 100);
        var right = PokemonStats.Create(90, 90, 90, 90, 90, 100);

        // Assert: la definicion exige que TODAS sean estrictamente mayores, asi
        // que el empate bloquea la dominacion en ambos sentidos.
        right.Dominates(left).ShouldBeFalse();
        left.Dominates(right).ShouldBeFalse();
    }

    [Fact]
    public void Dominates_is_false_for_equal_stats()
    {
        // Arrange
        var left = PokemonStats.Create(50, 50, 50, 50, 50, 50);
        var right = PokemonStats.Create(50, 50, 50, 50, 50, 50);

        // Assert
        left.Dominates(right).ShouldBeFalse();
        right.Dominates(left).ShouldBeFalse();
    }

    [Fact]
    public void Stats_with_the_same_values_are_equal()
    {
        // Act & Assert
        PokemonStats.Create(35, 55, 40, 50, 50, 90)
            .ShouldBe(PokemonStats.Create(35, 55, 40, 50, 50, 90));
    }
}
