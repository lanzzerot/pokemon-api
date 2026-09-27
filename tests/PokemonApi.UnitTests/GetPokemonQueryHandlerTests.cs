using NSubstitute;
using Shouldly;
using PokemonApi.Application.Abstractions.Repositories;
using PokemonApi.Application.Features.Pokemon.GetById;
using PokemonApi.Application.Features.Pokemon.GetByName;
using PokemonApi.Domain.Common;
using PokemonApi.Domain.Enumerations;

namespace PokemonApi.UnitTests;

/// <summary>
/// Pruebas de los casos de uso que recuperan un Pokemon concreto.
/// </summary>
/// <remarks>
/// Lo que se verifica aqui es el contrato de errores: un identificador o nombre
/// inexistente debe traducirse a <c>pokemon.not_found</c> (404) y no a un
/// fallo generico, porque es la unica forma que tiene el cliente de
/// distinguir "no existe" de "algo se rompio".
/// </remarks>
public sealed class GetPokemonQueryHandlerTests
{
    private readonly IPokemonRepository _repository = Substitute.For<IPokemonRepository>();

    [Fact]
    public async Task ById_returns_the_pokemon_when_it_exists()
    {
        // Arrange
        var pikachu = TestData.Pokemon();
        _repository.GetByIdAsync(25, Arg.Any<CancellationToken>()).Returns(pikachu);
        var handler = new GetPokemonByIdQueryHandler(_repository);

        // Act
        var result = await handler.HandleAsync(new GetPokemonByIdQuery(25), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(25);
        result.Value.Name.ShouldBe("pikachu");
        result.Value.DisplayName.ShouldBe("Pikachu");
    }

    [Fact]
    public async Task ById_exposes_the_total_of_base_stats()
    {
        // Arrange
        _repository.GetByIdAsync(25, Arg.Any<CancellationToken>()).Returns(TestData.Pokemon());
        var handler = new GetPokemonByIdQueryHandler(_repository);

        // Act
        var result = await handler.HandleAsync(new GetPokemonByIdQuery(25), CancellationToken.None);

        // Assert
        result.Value.TotalStats.ShouldBe(320);
    }

    [Fact]
    public async Task ById_reports_not_found_for_an_unknown_id()
    {
        // Arrange
        _repository.GetByIdAsync(9999, Arg.Any<CancellationToken>()).Returns((Domain.Entities.Pokemon?)null);
        var handler = new GetPokemonByIdQueryHandler(_repository);

        // Act
        var result = await handler.HandleAsync(new GetPokemonByIdQuery(9999), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("pokemon.not_found");
    }

    [Fact]
    public async Task ByName_normalizes_the_requested_name_before_querying()
    {
        // Arrange
        _repository
            .GetByNameAsync("mr-mime", Arg.Any<CancellationToken>())
            .Returns(TestData.Pokemon(id: 122, name: "mr-mime"));
        var handler = new GetPokemonByNameQueryHandler(_repository);

        // Act
        var result = await handler.HandleAsync(new GetPokemonByNameQuery("  Mr__Mime  "), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("mr-mime");
        await _repository.Received(1).GetByNameAsync("mr-mime", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ByName_reports_not_found_for_an_unknown_name()
    {
        // Arrange
        _repository
            .GetByNameAsync("missingno", Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.Pokemon?)null);
        var handler = new GetPokemonByNameQueryHandler(_repository);

        // Act
        var result = await handler.HandleAsync(new GetPokemonByNameQuery("missingno"), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("pokemon.not_found");
    }

    [Fact]
    public async Task ByName_is_case_insensitive_because_the_name_is_normalized()
    {
        // Arrange
        _repository
            .GetByNameAsync("pikachu", Arg.Any<CancellationToken>())
            .Returns(TestData.Pokemon());
        var handler = new GetPokemonByNameQueryHandler(_repository);

        // Act
        var result = await handler.HandleAsync(new GetPokemonByNameQuery("PIKACHU"), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Detail_preserves_the_units_of_the_catalogue()
    {
        // Arrange
        _repository.GetByIdAsync(25, Arg.Any<CancellationToken>()).Returns(TestData.Pokemon());
        var handler = new GetPokemonByIdQueryHandler(_repository);

        // Act
        var result = await handler.HandleAsync(new GetPokemonByIdQuery(25), CancellationToken.None);

        // Assert: la API publica metros y kilogramos, no decimetros y hectogramos.
        result.Value.Height.Value.ShouldBe(0.4m);
        result.Value.Height.Unit.ShouldBe("metre");
        result.Value.Weight.Value.ShouldBe(6m);
        result.Value.Weight.Unit.ShouldBe("kilogram");
    }

    [Fact]
    public async Task Detail_exposes_the_generation_slug_and_region()
    {
        // Arrange
        _repository
            .GetByIdAsync(25, Arg.Any<CancellationToken>())
            .Returns(TestData.Pokemon(generation: PokemonGeneration.GenerationI));
        var handler = new GetPokemonByIdQueryHandler(_repository);

        // Act
        var result = await handler.HandleAsync(new GetPokemonByIdQuery(25), CancellationToken.None);

        // Assert
        result.Value.Generation.ShouldBe("generation-i");
        result.Value.Region.ShouldBe("kanto");
    }
}
