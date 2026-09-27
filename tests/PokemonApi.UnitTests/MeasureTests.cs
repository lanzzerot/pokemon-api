using Shouldly;
using PokemonApi.Domain.Enumerations;
using PokemonApi.Domain.ValueObjects;

namespace PokemonApi.UnitTests;

/// <summary>
/// Pruebas de <see cref="Measure"/>, que conserva las unidades del dataset
/// (decimetros y hectogramos) y las convierte a las unidades convencionales de
/// la API (metros y kilogramos).
/// </summary>
public sealed class MeasureTests
{
    [Fact]
    public void FromDecimetres_keeps_the_source_unit()
    {
        // Act
        var height = Measure.FromDecimetres(4);

        // Assert
        height.Value.ShouldBe(4m);
        height.Unit.ShouldBe(MeasureUnit.Decimetre);
    }

    [Fact]
    public void FromHectograms_keeps_the_source_unit()
    {
        // Act
        var weight = Measure.FromHectograms(60);

        // Assert
        weight.Value.ShouldBe(60m);
        weight.Unit.ShouldBe(MeasureUnit.Hectogram);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(4, 0.4)]
    [InlineData(17, 1.7)]
    [InlineData(107, 10.7)]
    public void Decimetres_convert_to_metres(int decimetres, double expectedMetres)
    {
        // Act
        var metres = Measure.FromDecimetres(decimetres).ConvertTo(MeasureUnit.Metre);

        // Assert
        metres.Unit.ShouldBe(MeasureUnit.Metre);
        metres.Value.ShouldBe((decimal)expectedMetres);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(60, 6)]
    [InlineData(905, 90.5)]
    [InlineData(999, 99.9)]
    public void Hectograms_convert_to_kilograms(int hectograms, double expectedKilograms)
    {
        // Act
        var kilograms = Measure.FromHectograms(hectograms).ConvertTo(MeasureUnit.Kilogram);

        // Assert
        kilograms.Unit.ShouldBe(MeasureUnit.Kilogram);
        kilograms.Value.ShouldBe((decimal)expectedKilograms);
    }

    [Fact]
    public void Conversion_is_reversible()
    {
        // Act
        var roundTrip = Measure.FromDecimetres(17)
            .ConvertTo(MeasureUnit.Metre)
            .ConvertTo(MeasureUnit.Decimetre);

        // Assert
        roundTrip.Value.ShouldBe(17m);
        roundTrip.Unit.ShouldBe(MeasureUnit.Decimetre);
    }

    [Fact]
    public void Converting_to_the_same_unit_is_a_no_op()
    {
        // Arrange
        var height = Measure.FromDecimetres(4);

        // Act
        var result = height.ConvertTo(MeasureUnit.Decimetre);

        // Assert
        result.ShouldBe(height);
    }

    [Fact]
    public void ToConventionalUnit_returns_metres_for_heights()
    {
        // Act
        var result = Measure.FromDecimetres(17).ToConventionalUnit();

        // Assert
        result.Unit.ShouldBe(MeasureUnit.Metre);
        result.Value.ShouldBe(1.7m);
    }

    [Fact]
    public void ToConventionalUnit_returns_kilograms_for_weights()
    {
        // Act
        var result = Measure.FromHectograms(905).ToConventionalUnit();

        // Assert
        result.Unit.ShouldBe(MeasureUnit.Kilogram);
        result.Value.ShouldBe(90.5m);
    }

    [Fact]
    public void Converting_between_length_and_mass_throws()
    {
        // Arrange
        var height = Measure.FromDecimetres(4);

        // Act & Assert
        Should.Throw<ArgumentException>(() => height.ConvertTo(MeasureUnit.Kilogram));
    }

    [Fact]
    public void Measures_of_different_units_compare_by_physical_magnitude()
    {
        // Act & Assert: 17 dm = 1.7 m, por lo que 1.7 m es menor que 2 m.
        (Measure.FromDecimetres(17) < Measure.FromDecimetres(20)).ShouldBeTrue();
        (Measure.FromDecimetres(17) <= Measure.FromDecimetres(17)).ShouldBeTrue();
        (Measure.FromDecimetres(20) > Measure.FromDecimetres(17)).ShouldBeTrue();
        (Measure.FromDecimetres(17) >= Measure.FromDecimetres(17)).ShouldBeTrue();
    }

    [Fact]
    public void ToString_renders_the_value_in_its_own_unit()
    {
        // Act & Assert: un coma decimal seria una trampa para un cliente de otra
        // region, de ahi el uso explicito de la cultura invariante.
        Measure.FromDecimetres(17).ToString().ShouldBe("17 decimetre");
        Measure.FromHectograms(905).ToString().ShouldBe("905 hectogram");
    }

    [Fact]
    public void ToString_renders_the_conventional_unit_after_conversion()
    {
        // Act & Assert
        Measure.FromDecimetres(17).ToConventionalUnit().ToString().ShouldBe("1.7 metre");
        Measure.FromHectograms(905).ToConventionalUnit().ToString().ShouldBe("90.5 kilogram");
    }
}
