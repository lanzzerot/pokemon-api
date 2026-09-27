using NSubstitute;
using Shouldly;
using PokemonApi.Application.Abstractions.Repositories;
using PokemonApi.Application.Common.Queries;
using PokemonApi.Application.Common.Validation;
using PokemonApi.Application.Features.Pokemon.List;
using PokemonApi.Domain.Enumerations;
using SortDirection = PokemonApi.Domain.Enumerations.SortDirection;

namespace PokemonApi.UnitTests;

/// <summary>
/// Pruebas de la traduccion de la peticion del cliente a la consulta interna.
/// </summary>
/// <remarks>
/// Es la capa mas sensible a regresiones: decide que se considera un valor
/// valido de catalogo, convierte las unidades de entrada y normaliza los campos
/// de Closed World. Cualquier cambio aqui cambia el contrato publico.
/// </remarks>
public sealed class PokemonSearchQueryBuilderTests
{
    private readonly ICatalogRepository _catalog = Substitute.For<ICatalogRepository>();
    private readonly PokemonSearchQueryBuilder _builder;

    /// <summary>Inicializa las pruebas con un catalogo de prueba.</summary>
    public PokemonSearchQueryBuilderTests()
    {
        _catalog.GetTypesAsync(Arg.Any<CancellationToken>()).Returns(TestData.Types);
        _catalog.GetAbilitiesAsync(Arg.Any<CancellationToken>()).Returns(TestData.Abilities);
        _catalog.GetGenerationsAsync(Arg.Any<CancellationToken>()).Returns(TestData.Generations);
        _catalog.GetRegionsAsync(Arg.Any<CancellationToken>()).Returns(TestData.Regions);
        _catalog.GetEggGroupsAsync(Arg.Any<CancellationToken>()).Returns(TestData.EggGroups);
        _catalog.GetHabitatsAsync(Arg.Any<CancellationToken>()).Returns(TestData.Habitats);

        _builder = new PokemonSearchQueryBuilder(_catalog);
    }

    [Fact]
    public async Task An_empty_request_produces_the_default_query()
    {
        // Act
        var result = await _builder.BuildAsync(new ListPokemonQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var query = result.Value;
        query.Name.ShouldBeNull();
        query.Types.ShouldBeNull();
        query.Abilities.ShouldBeNull();
        query.Generations.ShouldBeNull();
        query.Regions.ShouldBeNull();
        query.EggGroups.ShouldBeNull();
        query.Habitats.ShouldBeNull();
        query.Rarity.ShouldBeNull();
        query.MinStat.ShouldBeNull();
        query.SortBy.ShouldBe(new PokemonSortBy(PokemonSortField.Id, SortDirection.Ascending));
        query.Page.ShouldBe(ListPokemonLimits.DefaultPage);
        query.PageSize.ShouldBe(ListPokemonLimits.DefaultPageSize);
    }

    [Fact]
    public async Task The_name_filter_is_normalized()
    {
        // Act
        var result = await _builder.BuildAsync(
            new ListPokemonQuery(Name: "  Mr__Mime  "), CancellationToken.None);

        // Assert
        result.Value.Name.ShouldBe("mr-mime");
    }

    [Theory]
    [InlineData("fire")]
    [InlineData("FIRE")]
    [InlineData("Fire")]
    public async Task Known_types_are_accepted_regardless_of_case(string type)
    {
        // Act
        var result = await _builder.BuildAsync(
            new ListPokemonQuery(Types: [type]), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Types.ShouldBe(["fire"]);
    }

    [Fact]
    public async Task An_unknown_type_is_rejected_naming_the_field()
    {
        // Act
        var exception = await Should.ThrowAsync<ValidationException>(() => _builder.BuildAsync(
            new ListPokemonQuery(Types: ["plastic"]), CancellationToken.None));

        // Assert
        exception.Failures.ShouldContainKey(nameof(ListPokemonQuery.Types));
    }

    [Fact]
    public async Task A_partially_valid_type_list_is_rejected_as_a_whole()
    {
        // Act: devolver solo los validos daria al cliente la sensacion de haber
        // filtrado por "fire,plastic" cuando en realidad se filtro por "fire".
        var exception = await Should.ThrowAsync<ValidationException>(() => _builder.BuildAsync(
            new ListPokemonQuery(Types: ["fire", "plastic"]), CancellationToken.None));

        // Assert
        exception.Failures.Keys.ShouldBe([nameof(ListPokemonQuery.Types)]);
    }

    [Fact]
    public async Task Comma_separated_values_are_split()
    {
        // Act
        var result = await _builder.BuildAsync(
            new ListPokemonQuery(Types: ["fire,water"]), CancellationToken.None);

        // Assert
        result.Value.Types.ShouldBe(["fire", "water"]);
    }

    [Fact]
    public async Task Repeated_parameters_are_combined()
    {
        // Act
        var result = await _builder.BuildAsync(
            new ListPokemonQuery(Types: ["fire", "water"]), CancellationToken.None);

        // Assert
        result.Value.Types.ShouldBe(["fire", "water"]);
    }

    [Fact]
    public async Task Duplicated_values_are_collapsed()
    {
        // Act
        var result = await _builder.BuildAsync(
            new ListPokemonQuery(Types: ["fire", "FIRE", "fire"]), CancellationToken.None);

        // Assert
        result.Value.Types.ShouldBe(["fire"]);
    }

    [Fact]
    public async Task Every_catalog_filter_is_validated()
    {
        // Act
        var exception = await Should.ThrowAsync<ValidationException>(() => _builder.BuildAsync(
            new ListPokemonQuery(
                Abilities: ["blazing-destiny"],
                Regions: ["mars"],
                EggGroups: ["astral"],
                Habitats: ["lunar"]),
            CancellationToken.None));

        // Assert: cada filtro se valida contra su propio catalogo.
        exception.Failures.Keys.ShouldBe(
            [
                nameof(ListPokemonQuery.Abilities),
                nameof(ListPokemonQuery.Regions),
                nameof(ListPokemonQuery.EggGroups),
                nameof(ListPokemonQuery.Habitats),
            ],
            ignoreOrder: true);
    }

    [Fact]
    public async Task Known_ability_region_egg_group_and_habitat_are_accepted()
    {
        // Act
        var result = await _builder.BuildAsync(
            new ListPokemonQuery(
                Abilities: ["static"],
                Regions: ["kanto"],
                EggGroups: ["monster"],
                Habitats: ["forest"]),
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Abilities.ShouldBe(["static"]);
        result.Value.Regions.ShouldBe(["kanto"]);
        result.Value.EggGroups.ShouldBe(["monster"]);
        result.Value.Habitats.ShouldBe(["forest"]);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("generation-i")]
    [InlineData("GENERATION-I")]
    [InlineData("generation_i")]
    public async Task A_generation_is_accepted_as_a_number_or_as_a_slug(string generation)
    {
        // Act
        var result = await _builder.BuildAsync(
            new ListPokemonQuery(Generation: generation), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Generations.ShouldBe([1]);
    }

    [Fact]
    public async Task Generation_and_generations_are_merged_into_one_filter()
    {
        // Act
        var result = await _builder.BuildAsync(
            new ListPokemonQuery(Generation: "1", Generations: ["generation-iii"]), CancellationToken.None);

        // Assert
        result.Value.Generations.ShouldBe([1, 3], ignoreOrder: true);
    }

    [Fact]
    public async Task An_unparseable_generation_is_rejected()
    {
        // Act
        var exception = await Should.ThrowAsync<ValidationException>(() => _builder.BuildAsync(
            new ListPokemonQuery(Generation: "generation-x"), CancellationToken.None));

        // Assert
        exception.Failures.ShouldContainKey(nameof(ListPokemonQuery.Generation));
    }

    [Fact]
    public async Task A_generation_absent_from_the_catalogue_is_rejected()
    {
        // Act: el enumerado reconoce la IX generacion, pero el catalogo de prueba
        // solo tiene I-III, asi que el valor existe y aun asi no es filtrable.
        var exception = await Should.ThrowAsync<ValidationException>(() => _builder.BuildAsync(
            new ListPokemonQuery(Generation: "9"), CancellationToken.None));

        // Assert
        exception.Failures.ShouldContainKey(nameof(ListPokemonQuery.Generation));
    }

    [Fact]
    public async Task Heights_are_converted_from_metres_to_decimetres()
    {
        // Act
        var result = await _builder.BuildAsync(
            new ListPokemonQuery(MinHeight: 1.5m, MaxHeight: 20m), CancellationToken.None);

        // Assert
        result.Value.MinHeight.ShouldBe(15m);
        result.Value.MaxHeight.ShouldBe(200m);
    }

    [Fact]
    public async Task Weights_are_converted_from_kilograms_to_hectograms()
    {
        // Act
        var result = await _builder.BuildAsync(
            new ListPokemonQuery(MinWeight: 6m, MaxWeight: 100m), CancellationToken.None);

        // Assert
        result.Value.MinWeight.ShouldBe(60m);
        result.Value.MaxWeight.ShouldBe(1000m);
    }

    [Theory]
    [InlineData("common", PokemonRarity.Common)]
    [InlineData("LEGENDARY", PokemonRarity.Legendary)]
    [InlineData("Mythical", PokemonRarity.Mythical)]
    public async Task Rarity_is_parsed_ignoring_case(string raw, PokemonRarity expected)
    {
        // Act
        var result = await _builder.BuildAsync(
            new ListPokemonQuery(Rarity: raw), CancellationToken.None);

        // Assert
        result.Value.Rarity.ShouldBe(expected);
    }

    [Theory]
    [InlineData("hp", PokemonStat.Hp)]
    [InlineData("Attack", PokemonStat.Attack)]
    [InlineData("SPECIALATTACK", PokemonStat.SpecialAttack)]
    [InlineData("specialAttack", PokemonStat.SpecialAttack)]
    [InlineData("special_attack", PokemonStat.SpecialAttack)]
    public async Task Min_stat_is_parsed_ignoring_case(string raw, PokemonStat expected)
    {
        // Act
        var result = await _builder.BuildAsync(
            new ListPokemonQuery(MinStat: raw, MinStatValue: 90), CancellationToken.None);

        // Assert
        result.Value.MinStat.ShouldBe(new StatThreshold(expected, 90));
    }

    [Fact]
    public async Task A_stat_without_a_value_yields_no_threshold()
    {
        // Act: el validator ya rechaza este caso; el builder no debe fallar.
        var result = await _builder.BuildAsync(
            new ListPokemonQuery(MinStat: "attack"), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.MinStat.ShouldBeNull();
    }

    [Theory]
    [InlineData("id", PokemonSortField.Id)]
    [InlineData("name", PokemonSortField.Name)]
    [InlineData("TotalStats", PokemonSortField.TotalStats)]
    [InlineData("baseexperience", PokemonSortField.BaseExperience)]
    public async Task Sort_field_is_parsed_ignoring_case(string raw, PokemonSortField expected)
    {
        // Act
        var result = await _builder.BuildAsync(
            new ListPokemonQuery(SortBy: raw), CancellationToken.None);

        // Assert
        result.Value.SortBy!.Field.ShouldBe(expected);
    }

    [Theory]
    [InlineData("asc", SortDirection.Ascending)]
    [InlineData("ASC", SortDirection.Ascending)]
    [InlineData("ascending", SortDirection.Ascending)]
    [InlineData("desc", SortDirection.Descending)]
    [InlineData("Desc", SortDirection.Descending)]
    [InlineData("descending", SortDirection.Descending)]
    public async Task Sort_direction_accepts_short_and_long_forms(string raw, SortDirection expected)
    {
        // Act
        var result = await _builder.BuildAsync(
            new ListPokemonQuery(SortDirection: raw), CancellationToken.None);

        // Assert
        result.Value.SortBy!.Direction.ShouldBe(expected);
    }

    [Fact]
    public async Task An_unknown_sort_field_is_rejected()
    {
        // Act
        var exception = await Should.ThrowAsync<ValidationException>(() => _builder.BuildAsync(
            new ListPokemonQuery(SortBy: "colour"), CancellationToken.None));

        // Assert
        exception.Failures.ShouldContainKey(nameof(ListPokemonQuery.SortBy));
    }

    [Fact]
    public async Task An_unknown_sort_direction_is_rejected()
    {
        // Act
        var exception = await Should.ThrowAsync<ValidationException>(() => _builder.BuildAsync(
            new ListPokemonQuery(SortDirection: "sideways"), CancellationToken.None));

        // Assert
        exception.Failures.ShouldContainKey(nameof(ListPokemonQuery.SortDirection));
    }

    [Fact]
    public async Task Several_invalid_filters_are_reported_together()
    {
        // Act: el cliente recibe todos los campos problematicos de una vez.
        var exception = await Should.ThrowAsync<ValidationException>(() => _builder.BuildAsync(
            new ListPokemonQuery(Types: ["plastic"], SortBy: "colour", Rarity: "unique"),
            CancellationToken.None));

        // Assert
        exception.Failures.Keys.ShouldBe(
            [
                nameof(ListPokemonQuery.Types),
                nameof(ListPokemonQuery.SortBy),
                nameof(ListPokemonQuery.Rarity),
            ],
            ignoreOrder: true);
    }

    [Fact]
    public async Task Pagination_values_are_carried_over()
    {
        // Act
        var result = await _builder.BuildAsync(
            new ListPokemonQuery(Page: 4, PageSize: 5), CancellationToken.None);

        // Assert
        result.Value.Page.ShouldBe(4);
        result.Value.PageSize.ShouldBe(5);
    }

    [Fact]
    public async Task Null_filters_do_not_trigger_catalog_lookups()
    {
        // Act
        await _builder.BuildAsync(new ListPokemonQuery(Page: 2), CancellationToken.None);

        // Assert: pedir el catalogo sin necesidad costaria una lectura por filtro.
        await _catalog.DidNotReceive().GetTypesAsync(Arg.Any<CancellationToken>());
        await _catalog.DidNotReceive().GetAbilitiesAsync(Arg.Any<CancellationToken>());
        await _catalog.DidNotReceive().GetGenerationsAsync(Arg.Any<CancellationToken>());
        await _catalog.DidNotReceive().GetRegionsAsync(Arg.Any<CancellationToken>());
        await _catalog.DidNotReceive().GetEggGroupsAsync(Arg.Any<CancellationToken>());
        await _catalog.DidNotReceive().GetHabitatsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validating_reports_the_failures_instead_of_throwing()
    {
        // Act: la cadena de validacion necesita la lista de fallos para juntarla con
        // los de los otros validadores, no una excepcion.
        var failures = await _builder.ValidateAsync(
            new ListPokemonQuery(Types: ["plastic"], SortBy: "colour"),
            CancellationToken.None);

        // Assert
        failures.Keys.ShouldBe(
            [nameof(ListPokemonQuery.Types), nameof(ListPokemonQuery.SortBy)],
            ignoreOrder: true);
    }

    [Fact]
    public async Task Validating_a_valid_request_reports_nothing()
    {
        // Act
        var failures = await _builder.ValidateAsync(
            new ListPokemonQuery(Types: ["Fire", "water,ghost"], Generation: "3"),
            CancellationToken.None);

        // Assert
        failures.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_catalogue_is_read_once_per_builder()
    {
        // Act: la validacion y la traduccion recorren los mismos filtros, asi que
        // los conjuntos de valores admitidos se cachean.
        await _builder.ValidateAsync(new ListPokemonQuery(Types: ["fire"]), CancellationToken.None);
        await _builder.ValidateAsync(new ListPokemonQuery(Types: ["water"]), CancellationToken.None);
        await _builder.BuildAsync(new ListPokemonQuery(Types: ["ghost"]), CancellationToken.None);

        // Assert
        await _catalog.Received(1).GetTypesAsync(Arg.Any<CancellationToken>());
    }
}
