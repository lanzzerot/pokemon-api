using NSubstitute;
using Shouldly;
using PokemonApi.Application.Abstractions.Repositories;
using PokemonApi.Application.Common.Queries;
using PokemonApi.Application.Common.Validation;
using PokemonApi.Application.Common.Pagination;
using PokemonApi.Application.Features.Pokemon.List;
using PokemonApi.Domain.Common;
using PokemonApi.Domain.Entities;
using PokemonApi.Domain.ValueObjects;
using SortDirection = PokemonApi.Domain.Enumerations.SortDirection;

namespace PokemonApi.UnitTests;

/// <summary>
/// Pruebas de la orquestacion de <see cref="ListPokemonQueryHandler"/>.
/// </summary>
public sealed class ListPokemonQueryHandlerTests
{
    private readonly IPokemonRepository _repository = Substitute.For<IPokemonRepository>();
    private readonly ICatalogRepository _catalog = Substitute.For<ICatalogRepository>();
    private readonly ListPokemonQueryHandler _handler;

    /// <summary>Inicializa las pruebas con un catalogo de prueba.</summary>
    public ListPokemonQueryHandlerTests()
    {
        _catalog.GetTypesAsync(Arg.Any<CancellationToken>()).Returns(TestData.Types);
        _catalog.GetAbilitiesAsync(Arg.Any<CancellationToken>()).Returns(TestData.Abilities);
        _catalog.GetGenerationsAsync(Arg.Any<CancellationToken>()).Returns(TestData.Generations);
        _catalog.GetRegionsAsync(Arg.Any<CancellationToken>()).Returns(TestData.Regions);
        _catalog.GetEggGroupsAsync(Arg.Any<CancellationToken>()).Returns(TestData.EggGroups);
        _catalog.GetHabitatsAsync(Arg.Any<CancellationToken>()).Returns(TestData.Habitats);

        _handler = new ListPokemonQueryHandler(
            _repository,
            new PokemonSearchQueryBuilder(_catalog));
    }

    [Fact]
    public async Task An_invalid_filter_never_reaches_the_repository()
    {
        // Act
        await Should.ThrowAsync<ValidationException>(() => _handler.HandleAsync(
            new ListPokemonQuery(Types: ["plastic"]), CancellationToken.None));

        // Assert: consultar el repositorio con un filtro invalido daria una
        // respuesta vacia que el cliente interpretaria como "no hay Pokemon".
        await _repository.DidNotReceive().SearchAsync(Arg.Any<PokemonSearchQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_valid_request_is_translated_into_a_repository_query()
    {
        // Arrange
        _repository
            .SearchAsync(Arg.Any<PokemonSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Pokemon>([TestData.Pokemon()], 1, 20, 1));

        // Act
        var result = await _handler.HandleAsync(
            new ListPokemonQuery(Types: ["fire"], SortBy: "name", SortDirection: "asc"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _repository.Received(1).SearchAsync(
            Arg.Is<PokemonSearchQuery>(q =>
                q.Types != null && q.Types.Contains("fire")
                && q.SortBy == new PokemonSortBy(PokemonApi.Domain.Enumerations.PokemonSortField.Name, SortDirection.Ascending)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Results_are_mapped_to_the_summary_shape()
    {
        // Arrange
        _repository
            .SearchAsync(Arg.Any<PokemonSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Pokemon>([TestData.Pokemon()], 1, 20, 1));

        // Act
        var result = await _handler.HandleAsync(new ListPokemonQuery(), CancellationToken.None);

        // Assert
        var summary = result.Value.Items.ShouldHaveSingleItem();
        summary.Id.ShouldBe(25);
        summary.Name.ShouldBe("pikachu");
        summary.DisplayName.ShouldBe("Pikachu");
        summary.Generation.ShouldBe("generation-i");
        summary.Region.ShouldBe("kanto");
        summary.TotalStats.ShouldBe(320);
        summary.IsLegendary.ShouldBeFalse();
        summary.IsMythical.ShouldBeFalse();
        summary.OfficialArtwork.ShouldBe("https://example.test/artwork/25.png");
    }

    [Fact]
    public async Task Pagination_metadata_comes_from_the_repository()
    {
        // Arrange
        _repository
            .SearchAsync(Arg.Any<PokemonSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Pokemon>([TestData.Pokemon()], 2, 20, 45));

        // Act
        var result = await _handler.HandleAsync(
            new ListPokemonQuery(Page: 2, PageSize: 20), CancellationToken.None);

        // Assert
        result.Value.Page.ShouldBe(2);
        result.Value.PageSize.ShouldBe(20);
        result.Value.TotalCount.ShouldBe(45);
        result.Value.HasPreviousPage.ShouldBeTrue();
        result.Value.HasNextPage.ShouldBeTrue();
    }

    [Fact]
    public async Task An_empty_catalog_produces_an_empty_page()
    {
        // Arrange
        _repository
            .SearchAsync(Arg.Any<PokemonSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Pokemon>([], 1, 20, 0));

        // Act
        var result = await _handler.HandleAsync(new ListPokemonQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
        result.Value.HasNextPage.ShouldBeFalse();
        result.Value.HasPreviousPage.ShouldBeFalse();
    }
}

/// <summary>
/// Pruebas de la ordenacion de la cadena evolutiva.
/// </summary>
public sealed class GetEvolutionChainQueryHandlerTests
{
    private readonly IPokemonRepository _repository = Substitute.For<IPokemonRepository>();

    [Fact]
    public async Task Members_are_ordered_from_the_base_form()
    {
        // Arrange: la cadena llega desordenada y con una evolucion lateral, que es
        // justamente el caso que un orden por identificador resolveria mal.
        var chain = new[]
        {
            TestData.Pokemon(id: 26, name: "raichu", evolvesFrom: TestData.SlugOf("pikachu"), chainId: 10),
            TestData.Pokemon(id: 25, name: "pikachu", chainId: 10),
            TestData.Pokemon(id: 27, name: "raichu-alola", evolvesFrom: TestData.SlugOf("pikachu"), chainId: 10),
        };

        _repository
            .GetByNameAsync("pikachu", Arg.Any<CancellationToken>())
            .Returns(TestData.Pokemon(id: 25, name: "pikachu", chainId: 10));
        _repository.GetEvolutionChainAsync(10, Arg.Any<CancellationToken>()).Returns(chain);

        var handler = new PokemonApi.Application.Features.Pokemon.GetEvolutionChain
            .GetEvolutionChainQueryHandler(_repository);

        // Act
        var result = await handler.HandleAsync(
            new PokemonApi.Application.Features.Pokemon.GetEvolutionChain.GetEvolutionChainQuery("pikachu"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ChainId.ShouldBe(10);
        result.Value.RootName.ShouldBe("pikachu");
        result.Value.Members[0].Name.ShouldBe("pikachu");
        result.Value.Members[0].EvolutionOrder.ShouldBe(0);
        result.Value.Members[1].EvolutionOrder.ShouldBe(1);
        result.Value.Members[2].EvolutionOrder.ShouldBe(1);
    }

    [Fact]
    public async Task An_unknown_name_is_reported_as_not_found()
    {
        // Arrange
        _repository
            .GetByNameAsync("missingno", Arg.Any<CancellationToken>())
            .Returns((Pokemon?)null);

        var handler = new PokemonApi.Application.Features.Pokemon.GetEvolutionChain
            .GetEvolutionChainQueryHandler(_repository);

        // Act
        var result = await handler.HandleAsync(
            new PokemonApi.Application.Features.Pokemon.GetEvolutionChain.GetEvolutionChainQuery("missingno"),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task An_empty_chain_is_a_technical_error_not_a_not_found()
    {
        // Arrange: el dataset declara una cadena y no la tiene, lo que es una
        // inconsistencia de datos y no una peticion incorrecta del cliente.
        _repository
            .GetByNameAsync("pikachu", Arg.Any<CancellationToken>())
            .Returns(TestData.Pokemon());
        _repository.GetEvolutionChainAsync(10, Arg.Any<CancellationToken>()).Returns([]);

        var handler = new PokemonApi.Application.Features.Pokemon.GetEvolutionChain
            .GetEvolutionChainQueryHandler(_repository);

        // Act
        var result = await handler.HandleAsync(
            new PokemonApi.Application.Features.Pokemon.GetEvolutionChain.GetEvolutionChainQuery("pikachu"),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.Unexpected);
        result.Error.Code.ShouldBe("evolution_chain.empty");
    }
}
