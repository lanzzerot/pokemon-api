using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace PokemonApi.IntegrationTests;

/// <summary>
/// Pruebas de los endpoints de detalle, busqueda por nombre y cadena evolutiva.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class PokemonDetailEndpointTests(PokemonApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private HttpClient Client => factory.CreateClient();

    [Fact]
    public async Task Detail_by_id_returns_the_complete_pokemon()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/pokemon/25");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("id").GetInt32().ShouldBe(25);
        body.GetProperty("name").GetString().ShouldBe("pikachu");
        body.GetProperty("displayName").GetString().ShouldBe("Pikachu");
        body.GetProperty("generation").GetString().ShouldBe("generation-i");
        body.GetProperty("region").GetString().ShouldBe("kanto");
        body.GetProperty("types").EnumerateArray().Select(t => t.GetString()).ShouldBe(["electric"]);
        body.GetProperty("stats").GetProperty("hp").GetInt32().ShouldBe(35);
        body.GetProperty("evolvesTo").GetArrayLength().ShouldBe(1);
        body.GetProperty("evolutionChainId").GetInt32().ShouldBe(10);
    }

    [Fact]
    public async Task Detail_exposes_the_total_of_base_stats()
    {
        // Act
        var body = await ReadDetailAsync("/api/v1/pokemon/25");

        // Assert: el total permite ordenar y filtrar con el mismo dato que se lee.
        body.GetProperty("totalStats").GetInt32().ShouldBe(
            body.GetProperty("stats").GetProperty("hp").GetInt32()
            + body.GetProperty("stats").GetProperty("attack").GetInt32()
            + body.GetProperty("stats").GetProperty("defense").GetInt32()
            + body.GetProperty("stats").GetProperty("specialAttack").GetInt32()
            + body.GetProperty("stats").GetProperty("specialDefense").GetInt32()
            + body.GetProperty("stats").GetProperty("speed").GetInt32());
    }

    [Fact]
    public async Task Detail_by_name_is_case_insensitive()
    {
        // Act
        var lower = await ReadDetailAsync("/api/v1/pokemon/by-name/pikachu");
        var upper = await ReadDetailAsync("/api/v1/pokemon/by-name/Pikachu");

        // Assert
        lower.GetProperty("id").GetInt32().ShouldBe(upper.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Detail_by_name_normalizes_separators()
    {
        // Act
        var body = await ReadDetailAsync("/api/v1/pokemon/by-name/mr-mime");

        // Assert
        body.GetProperty("id").GetInt32().ShouldBe(122);
        body.GetProperty("name").GetString().ShouldBe("mr-mime");
    }

    [Fact]
    public async Task Detail_publishes_absolute_urls_for_every_sprite()
    {
        // Act
        var body = await ReadDetailAsync("/api/v1/pokemon/25");

        // Assert
        var sprites = body.GetProperty("sprites");
        foreach (var name in new[]
                 {
                     "officialArtwork", "officialArtworkShiny", "homeArtwork",
                     "frontDefault", "frontShiny", "pixelArt", "spritesheet",
                 })
        {
            var url = sprites.GetProperty(name).GetString();
            url.ShouldNotBeNullOrWhiteSpace();
            Uri.TryCreate(url, UriKind.Absolute, out _).ShouldBeTrue($"'{name}' debe ser una URL absoluta: {url}");
        }
    }

    [Fact]
    public async Task An_unknown_id_returns_a_problem_details_404()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/pokemon/999999");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        body.GetProperty("title").GetString().ShouldBe("Resource not found.");
        body.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task An_unknown_name_returns_a_problem_details_404()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/pokemon/by-name/missingno");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task An_unknown_evolution_chain_returns_404()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/pokemon/evolution-chain/missingno");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_non_numeric_id_does_not_match_the_route()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/pokemon/abc");

        // Assert: la ruta restringe a enteros, asi que un texto no es un 400 de
        // validacion sino un recurso inexistente.
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task The_evolution_chain_starts_at_the_earliest_form()
    {
        // Act: la cadena 10 no empieza en Pikachu, sino en Pichu, su preevolucion.
        var body = await ReadEvolutionChainAsync("pikachu");

        // Assert
        body.GetProperty("chainId").GetInt32().ShouldBe(10);
        body.GetProperty("rootName").GetString().ShouldBe("pichu");

        var members = body.GetProperty("members").EnumerateArray().ToArray();
        members[0].GetProperty("name").GetString().ShouldBe("pichu");
        members[0].GetProperty("evolutionOrder").GetInt32().ShouldBe(0);
        members[1].GetProperty("name").GetString().ShouldBe("pikachu");
        members[1].GetProperty("evolutionOrder").GetInt32().ShouldBe(1);
        members[2].GetProperty("name").GetString().ShouldBe("raichu");
        members[2].GetProperty("evolutionOrder").GetInt32().ShouldBe(2);
    }

    [Fact]
    public async Task A_multi_stage_chain_numbers_its_stages_in_order()
    {
        // Act
        var body = await ReadEvolutionChainAsync("bulbasaur");

        // Assert
        var orders = body.GetProperty("members").EnumerateArray()
            .Select(m => m.GetProperty("evolutionOrder").GetInt32())
            .ToArray();

        orders.ShouldBe([0, 1, 2]);
    }

    [Fact]
    public async Task The_evolution_chain_is_the_same_from_any_member()
    {
        // Act
        var fromBase = await ReadEvolutionChainAsync("bulbasaur");
        var fromFinal = await ReadEvolutionChainAsync("venusaur");

        // Assert
        fromBase.GetProperty("chainId").GetInt32().ShouldBe(fromFinal.GetProperty("chainId").GetInt32());
        fromBase.GetProperty("rootName").GetString().ShouldBe(fromFinal.GetProperty("rootName").GetString());
    }

    [Fact]
    public async Task A_gender_dependent_evolution_publishes_the_required_gender()
    {
        // Act: Kirlia evoluciona a Gardevoir siempre y a Gallade solo si es macho.
        var body = await ReadDetailAsync("/api/v1/pokemon/281");

        // Assert
        var gallade = body.GetProperty("evolvesTo").EnumerateArray()
            .Single(e => e.GetProperty("name").GetString() == "gallade");

        gallade.GetProperty("requirements").EnumerateArray()
            .Select(requirement => requirement.GetProperty("gender").GetString())
            .ShouldBe(["male"]);
    }

    [Fact]
    public async Task An_evolution_dependent_on_physical_stats_publishes_the_comparison()
    {
        // Act: Tyrogue evoluciona a Hitmontop cuando ataque y defensa empatan, y a
        // Hitmonlee o Hitmonchan segun cual sea mayor. El dataset lo guarda como
        // 1, -1 y 0, y la API lo publica como texto legible.
        var body = await ReadDetailAsync("/api/v1/pokemon/236");

        // Assert
        var evolutions = body.GetProperty("evolvesTo").EnumerateArray().ToArray();

        static string? Comparison(JsonElement evolution) => evolution.GetProperty("requirements")
            .EnumerateArray()
            .Select(r => r.GetProperty("relativePhysicalStats").GetString())
            .SingleOrDefault();

        Comparison(evolutions.Single(e => e.GetProperty("name").GetString() == "hitmontop"))
            .ShouldBe("attack = defense");
        Comparison(evolutions.Single(e => e.GetProperty("name").GetString() == "hitmonlee"))
            .ShouldBe("attack > defense");
        Comparison(evolutions.Single(e => e.GetProperty("name").GetString() == "hitmonchan"))
            .ShouldBe("attack < defense");
    }

    private async Task<JsonElement> ReadDetailAsync(string url)
    {
        var response = await Client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Json);
    }

    private async Task<JsonElement> ReadEvolutionChainAsync(string name)
    {
        var response = await Client.GetAsync($"/api/v1/pokemon/evolution-chain/{name}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Json);
    }
}
