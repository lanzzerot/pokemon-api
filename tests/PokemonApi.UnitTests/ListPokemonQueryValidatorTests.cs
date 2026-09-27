using Shouldly;
using PokemonApi.Application.Features.Pokemon.List;

namespace PokemonApi.UnitTests;

/// <summary>
/// Pruebas de las invariantes de <see cref="ListPokemonQuery"/> que no dependen
/// del contenido del catalogo.
/// </summary>
public sealed class ListPokemonQueryValidatorTests
{
    private readonly ListPokemonQueryValidator _validator = new();

    [Fact]
    public void An_empty_query_is_valid()
    {
        // Act
        var failures = _validator.Validate(new ListPokemonQuery());

        // Assert
        failures.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_name_filter_is_rejected(string name)
    {
        // Act
        var failures = _validator.Validate(new ListPokemonQuery(Name: name));

        // Assert
        failures.ShouldContainKey(nameof(ListPokemonQuery.Name));
    }

    [Fact]
    public void A_name_longer_than_the_limit_is_rejected()
    {
        // Act
        var failures = _validator.Validate(
            new ListPokemonQuery(Name: new string('a', ListPokemonLimits.MaxSearchLength + 1)));

        // Assert
        failures.ShouldContainKey(nameof(ListPokemonQuery.Name));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_page_below_one_is_rejected(int page)
    {
        // Act
        var failures = _validator.Validate(new ListPokemonQuery(Page: page));

        // Assert
        failures.ShouldContainKey(nameof(ListPokemonQuery.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void A_non_positive_page_size_is_rejected(int pageSize)
    {
        // Act
        var failures = _validator.Validate(new ListPokemonQuery(PageSize: pageSize));

        // Assert
        failures.ShouldContainKey(nameof(ListPokemonQuery.PageSize));
    }

    [Fact]
    public void A_page_size_above_the_maximum_is_rejected()
    {
        // Act
        var failures = _validator.Validate(
            new ListPokemonQuery(PageSize: ListPokemonLimits.MaxPageSize + 1));

        // Assert
        failures.ShouldContainKey(nameof(ListPokemonQuery.PageSize));
    }

    [Fact]
    public void A_page_size_at_the_maximum_is_accepted()
    {
        // Act
        var failures = _validator.Validate(
            new ListPokemonQuery(PageSize: ListPokemonLimits.MaxPageSize));

        // Assert
        failures.ShouldBeEmpty();
    }

    [Fact]
    public void Inverted_height_bounds_are_rejected()
    {
        // Act
        var failures = _validator.Validate(new ListPokemonQuery(MinHeight: 2m, MaxHeight: 1m));

        // Assert
        failures.ShouldContainKey(nameof(ListPokemonQuery.MinHeight));
    }

    [Fact]
    public void Inverted_weight_bounds_are_rejected()
    {
        // Act
        var failures = _validator.Validate(new ListPokemonQuery(MinWeight: 90m, MaxWeight: 10m));

        // Assert
        failures.ShouldContainKey(nameof(ListPokemonQuery.MinWeight));
    }

    [Fact]
    public void A_negative_height_is_rejected()
    {
        // Act
        var failures = _validator.Validate(new ListPokemonQuery(MinHeight: -0.1m));

        // Assert
        failures.ShouldContainKey(nameof(ListPokemonQuery.MinHeight));
    }

    [Fact]
    public void A_zero_maximum_height_is_rejected()
    {
        // Act: un maximo de cero no puede distancing nada, asi que es un error.
        var failures = _validator.Validate(new ListPokemonQuery(MaxHeight: 0m));

        // Assert
        failures.ShouldContainKey(nameof(ListPokemonQuery.MaxHeight));
    }

    [Fact]
    public void Inverted_total_stats_bounds_are_rejected()
    {
        // Act
        var failures = _validator.Validate(new ListPokemonQuery(MinTotalStats: 600, MaxTotalStats: 300));

        // Assert
        failures.ShouldContainKey(nameof(ListPokemonQuery.MinTotalStats));
    }

    [Fact]
    public void A_stat_threshold_without_a_value_is_rejected()
    {
        // Act
        var failures = _validator.Validate(new ListPokemonQuery(MinStat: "attack"));

        // Assert
        failures.ShouldContainKey(nameof(ListPokemonQuery.MinStatValue));
    }

    [Fact]
    public void A_stat_value_without_a_stat_is_rejected()
    {
        // Act
        var failures = _validator.Validate(new ListPokemonQuery(MinStatValue: 100));

        // Assert
        failures.ShouldContainKey(nameof(ListPokemonQuery.MinStat));
    }

    [Fact]
    public void A_negative_stat_value_is_rejected()
    {
        // Act
        var failures = _validator.Validate(new ListPokemonQuery(MinStat: "attack", MinStatValue: -1));

        // Assert
        failures.ShouldContainKey(nameof(ListPokemonQuery.MinStatValue));
    }

    [Fact]
    public void A_complete_stat_threshold_is_valid()
    {
        // Act
        var failures = _validator.Validate(new ListPokemonQuery(MinStat: "attack", MinStatValue: 100));

        // Assert
        failures.ShouldBeEmpty();
    }

    [Fact]
    public void Every_broken_rule_is_reported_at_once()
    {
        // Act: el cliente recibe todos los problemas de una vez, no solo el primero.
        var failures = _validator.Validate(new ListPokemonQuery(
            Name: "   ",
            Page: 0,
            PageSize: 1000,
            MinHeight: 5m,
            MaxHeight: 1m,
            MinWeight: 5m,
            MaxWeight: 1m,
            MinTotalStats: 600,
            MaxTotalStats: 100,
            MinStat: "speed"));

        // Assert
        failures.Keys.ShouldBe(
            [
                nameof(ListPokemonQuery.Name),
                nameof(ListPokemonQuery.Page),
                nameof(ListPokemonQuery.PageSize),
                nameof(ListPokemonQuery.MinHeight),
                nameof(ListPokemonQuery.MinWeight),
                nameof(ListPokemonQuery.MinTotalStats),
                nameof(ListPokemonQuery.MinStatValue),
            ],
            ignoreOrder: true);
    }
}
