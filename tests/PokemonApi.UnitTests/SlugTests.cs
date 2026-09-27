using Shouldly;
using PokemonApi.Domain.Common;
using PokemonApi.Domain.ValueObjects;

namespace PokemonApi.UnitTests;

/// <summary>
/// Pruebas del value object <see cref="Slug"/>, que normaliza los nombres que
/// llegan del dataset y del cliente.
/// </summary>
public sealed class SlugTests
{
    [Theory]
    [InlineData("pikachu", "pikachu")]
    [InlineData("Pikachu", "pikachu")]
    [InlineData("  Pikachu  ", "pikachu")]
    [InlineData("deoxys", "deoxys")]
    [InlineData("mr-mime", "mr-mime")]
    [InlineData("mr. Mime", "mr-mime")]
    [InlineData("  Mr__Mime  ", "mr-mime")]
    [InlineData("Farfetch’d", "farfetch-d")]
    [InlineData("Type: Null", "type-null")]
    [InlineData("Ho-Oh", "ho-oh")]
    [InlineData("japanese: pysuka", "japanese-pysuka")]
    [InlineData("Nidoran♀", "nidoran")]
    public void Create_normalizes_the_value(string input, string expected)
    {
        // Act
        var result = Slug.Create(input);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("???")]
    [InlineData("¡")]
    public void Create_rejects_values_without_letters_or_digits(string? input)
    {
        // Act
        var result = Slug.Create(input);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void Normalize_is_case_insensitive_and_idempotent()
    {
        // Act
        var once = Slug.Normalize("  Mr__Mime  ");
        var twice = Slug.Normalize(once);

        // Assert
        once.ShouldBe("mr-mime");
        twice.ShouldBe(once);
    }

    [Fact]
    public void Slugs_are_ordered_alphabetically()
    {
        // Arrange: dos slugs iguales pero construidos por separado, para que la
        // comparacion sea entre objetos distintos y no una autorreferencia.
        var abra = Slug.Create("abra").Value;
        var bulbasaur = Slug.Create("bulbasaur").Value;
        var charizard = Slug.Create("charizard").Value;
        var sameAsBulbasaur = Slug.Create("Bulbasaur").Value;

        // Assert
        (abra < bulbasaur).ShouldBeTrue();
        (bulbasaur < charizard).ShouldBeTrue();
        (bulbasaur <= sameAsBulbasaur).ShouldBeTrue();
        (charizard > abra).ShouldBeTrue();
        (bulbasaur >= sameAsBulbasaur).ShouldBeTrue();
    }

    [Fact]
    public void ToString_and_implicit_conversion_return_the_normalized_value()
    {
        // Arrange
        var slug = Slug.Create("PiKaChU").Value;

        // Assert
        slug.ToString().ShouldBe("pikachu");
        ((string)slug).ShouldBe("pikachu");
    }
}
