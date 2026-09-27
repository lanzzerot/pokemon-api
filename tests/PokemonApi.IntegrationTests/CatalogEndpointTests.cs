using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace PokemonApi.IntegrationTests;

/// <summary>
/// Pruebas de los catalogos auxiliares y de los errores de validacion.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class CatalogEndpointTests(PokemonApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private HttpClient Client => factory.CreateClient();

    [Theory]
    [InlineData("/api/v1/generations", 9)]
    [InlineData("/api/v1/types", 21)]
    [InlineData("/api/v1/abilities", 367)]
    [InlineData("/api/v1/egg-groups", 15)]
    [InlineData("/api/v1/habitats", 9)]
    [InlineData("/api/v1/regions", 9)]
    public async Task Catalog_returns_a_flat_array(string url, int expectedCount)
    {
        // Act
        var response = await Client.GetAsync(url);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        // Assert: los catalogos no son paginados, devuelven el array directamente.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ValueKind.ShouldBe(JsonValueKind.Array);
        body.GetArrayLength().ShouldBe(expectedCount);
    }

    [Fact]
    public async Task Catalog_entries_carry_a_slug_a_name_and_a_count()
    {
        // Act
        var body = await ReadArrayAsync("/api/v1/types");

        // Assert
        var first = body[0];
        first.GetProperty("id").GetInt32().ShouldBeGreaterThan(0);
        first.GetProperty("slug").GetString().ShouldNotBeNullOrWhiteSpace();
        first.GetProperty("name").GetString().ShouldNotBeNullOrWhiteSpace();
        first.GetProperty("pokemonCount").GetInt32().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Generations_expose_their_region()
    {
        // Act
        var body = await ReadArrayAsync("/api/v1/generations");

        // Assert
        var kanto = body.Single(g => g.GetProperty("slug").GetString() == "generation-i");
        kanto.GetProperty("region").GetString().ShouldBe("kanto");
        kanto.GetProperty("name").GetString().ShouldBe("Generation I");
        kanto.GetProperty("pokemonCount").GetInt32().ShouldBe(151);
    }

    [Fact]
    public async Task Health_reports_the_dataset_as_healthy()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Theory]
    [InlineData("/api/v1/pokemon?page=0")]
    [InlineData("/api/v1/pokemon?pageSize=0")]
    [InlineData("/api/v1/pokemon?pageSize=101")]
    [InlineData("/api/v1/pokemon?minHeight=-1")]
    [InlineData("/api/v1/pokemon?maxHeight=0")]
    [InlineData("/api/v1/pokemon?minHeight=5&maxHeight=1")]
    [InlineData("/api/v1/pokemon?minWeight=5&maxWeight=1")]
    [InlineData("/api/v1/pokemon?minTotalStats=600&maxTotalStats=100")]
    [InlineData("/api/v1/pokemon?minStat=attack")]
    [InlineData("/api/v1/pokemon?minStatValue=100")]
    [InlineData("/api/v1/pokemon?name=")]
    [InlineData("/api/v1/pokemon?sortDirection=sideways")]
    public async Task A_broken_filter_returns_400_with_the_offending_fields(string url)
    {
        // Act
        var response = await Client.GetAsync(url);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        body.GetProperty("title").GetString().ShouldBe("Invalid request.");
        body.GetProperty("errors").EnumerateObject().ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("/api/v1/pokemon?types=plastic")]
    [InlineData("/api/v1/pokemon?abilities=superstrength")]
    [InlineData("/api/v1/pokemon?regions=atlantis")]
    [InlineData("/api/v1/pokemon?eggGroups=astral")]
    [InlineData("/api/v1/pokemon?habitats=lunar")]
    [InlineData("/api/v1/pokemon?generation=generation-x")]
    [InlineData("/api/v1/pokemon?rarity=unique")]
    [InlineData("/api/v1/pokemon?minStat=armour")]
    [InlineData("/api/v1/pokemon?sortBy=colour")]
    public async Task A_value_outside_the_catalogue_returns_400(string url)
    {
        // Act
        var response = await Client.GetAsync(url);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        // Assert: devolver 200 con cero resultados ocultaria un error del cliente.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("errors").EnumerateObject().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task An_out_of_catalogue_enum_lists_the_allowed_values()
    {
        // Act: el mensaje enumera los valores validos para que el cliente pueda
        // corregirse sin consultar la documentacion.
        var response = await Client.GetAsync("/api/v1/pokemon?rarity=unique");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        var messages = body.GetProperty("errors").GetProperty("Rarity").EnumerateArray()
            .Select(m => m.GetString())
            .ToArray();

        // Assert
        var message = messages.ShouldHaveSingleItem() ?? string.Empty;
        message.ShouldContain("Common");
        message.ShouldContain("Legendary");
        message.ShouldContain("Mythical");
    }

    [Fact]
    public async Task A_400_names_every_broken_field()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/pokemon?page=0&pageSize=0&types=plastic");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        // Assert
        var errors = body.GetProperty("errors");
        errors.EnumerateObject().Select(p => p.Name)
            .ShouldBe(["Page", "PageSize", "Types"], ignoreOrder: true);
    }

    [Fact]
    public async Task A_malformed_value_that_cannot_be_bound_returns_400()
    {
        // Act: un entero donde se espera texto no se puede enlazar; el error debe
        // ser un 400 explicito y no el 500 de una excepcion sin controlar, y debe
        // nombrar el parametro culpable.
        var response = await Client.GetAsync("/api/v1/pokemon?minBaseExperience=not-a-number");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        body.GetProperty("errors").TryGetProperty("minBaseExperience", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Problem_details_carry_a_trace_id()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/pokemon?page=0");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        // Assert: es lo que permite correlacionar el 400 con el log del servidor.
        body.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    private async Task<List<JsonElement>> ReadArrayAsync(string url)
    {
        var response = await Client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return [.. (await response.Content.ReadFromJsonAsync<JsonElement>(Json)).EnumerateArray()];
    }
}
