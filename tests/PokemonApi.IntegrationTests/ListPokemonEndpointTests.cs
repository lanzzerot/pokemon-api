using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace PokemonApi.IntegrationTests;

/// <summary>
/// Pruebas de los endpoints de listado contra el catalogo real.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ListPokemonEndpointTests(PokemonApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private HttpClient Client => factory.CreateClient();

    [Fact]
    public async Task Listing_returns_the_default_page()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/pokemon");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");
        body.GetProperty("items").GetArrayLength().ShouldBe(20);
        body.GetProperty("page").GetInt32().ShouldBe(1);
        body.GetProperty("pageSize").GetInt32().ShouldBe(20);
        body.GetProperty("totalCount").GetInt32().ShouldBeGreaterThan(1000);
        body.GetProperty("hasNextPage").GetBoolean().ShouldBeTrue();
        body.GetProperty("hasPreviousPage").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task An_empty_result_set_is_still_a_successful_page()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/pokemon?name=zzzznotapokemon");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        // Assert: 404 seria un error de contracto; un filtro que no encuentra nada
        // es un resultado legitimo.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("items").GetArrayLength().ShouldBe(0);
        body.GetProperty("totalCount").GetInt32().ShouldBe(0);
        body.GetProperty("hasNextPage").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task A_page_beyond_the_end_is_empty_and_carries_no_next_page()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/pokemon?page=9999");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("items").GetArrayLength().ShouldBe(0);
        body.GetProperty("hasNextPage").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Pagination_does_not_repeat_or_lose_items_across_pages()
    {
        // Arrange
        var client = Client;

        // Act
        var first = await ReadNamesAsync(client, "/api/v1/pokemon?page=1&pageSize=5");
        var second = await ReadNamesAsync(client, "/api/v1/pokemon?page=2&pageSize=5");

        // Assert
        first.Count.ShouldBe(5);
        second.Count.ShouldBe(5);
        first.Intersect(second).ShouldBeEmpty();
    }

    [Fact]
    public async Task Types_can_be_repeated_or_comma_separated()
    {
        // Arrange
        var client = Client;

        // Act: las dos sintaxis que anuncia la documentacion deben equivaler.
        var repeated = await ReadIdsAsync(client, "/api/v1/pokemon?types=fire&types=flying&pageSize=100");
        var commaSeparated = await ReadIdsAsync(client, "/api/v1/pokemon?types=fire,flying&pageSize=100");

        // Assert
        repeated.ShouldNotBeEmpty();
        commaSeparated.ShouldBe(repeated);
    }

    [Fact]
    public async Task A_multiple_type_filter_is_an_or_within_the_field()
    {
        // Act
        var items = await ReadAllAsync(Client, "/api/v1/pokemon?types=fire,water&pageSize=100");

        // Assert: basta con cumplir uno de los dos tipos.
        items.ShouldNotBeEmpty();

        var typeLists = items
            .Select(item => item.GetProperty("types").EnumerateArray().Select(type => type.GetString()).ToArray())
            .ToArray();

        typeLists.ShouldAllBe(types => types.Contains("fire") || types.Contains("water"));
    }

    [Fact]
    public async Task Sorting_by_weight_descending_matches_the_detail_endpoint()
    {
        // Arrange: el resumen de listado no publica medidas, asi que el orden se
        // contrasta con el detalle, que si las expone.
        var body = await ReadPageAsync("/api/v1/pokemon?sortBy=weight&sortDirection=desc&pageSize=3");
        var ids = body.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("id").GetInt32())
            .ToArray();

        // Act
        var weights = new List<decimal>();
        foreach (var id in ids)
        {
            var detail = await Client.GetAsync($"/api/v1/pokemon/{id}");
            detail.StatusCode.ShouldBe(HttpStatusCode.OK);
            weights.Add((await detail.Content.ReadFromJsonAsync<JsonElement>(Json))
                .GetProperty("weight").GetProperty("value").GetDecimal());
        }

        // Assert
        weights.OrderByDescending(weight => weight).ShouldBe(weights);
    }

    [Fact]
    public async Task The_summary_shape_stays_lean()
    {
        // Act: el listado es deliberadamente ligero; las medidas, el genero y las
        // evoluciones se piden en el detalle.
        var items = await ReadAllAsync(Client, "/api/v1/pokemon?pageSize=1");

        // Assert
        var summary = items[0];
        summary.GetProperty("id").ValueKind.ShouldBe(JsonValueKind.Number);
        summary.GetProperty("name").ValueKind.ShouldBe(JsonValueKind.String);
        summary.GetProperty("types").ValueKind.ShouldBe(JsonValueKind.Array);
        summary.GetProperty("totalStats").ValueKind.ShouldBe(JsonValueKind.Number);
        summary.TryGetProperty("height", out _).ShouldBeFalse();
        summary.TryGetProperty("description", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task A_height_filter_is_answered_in_metres()
    {
        // Act: el filtro se expresa en metros aunque internamente se compare en
        // decimetros, de modo que el valor publicado y el filtrado coinciden.
        var items = await ReadAllAsync(Client, "/api/v1/pokemon?minHeight=1.5&pageSize=5");

        // Assert
        items.ShouldNotBeEmpty();
        var heights = items
            .Select(item => item.GetProperty("id").GetInt32())
            .ToArray();

        heights.OrderBy(id => id).ShouldBe(heights);

        foreach (var id in heights)
        {
            var detail = await Client.GetAsync($"/api/v1/pokemon/{id}");
            var body = await detail.Content.ReadFromJsonAsync<JsonElement>(Json);
            body.GetProperty("height").GetProperty("value").GetDecimal().ShouldBeGreaterThanOrEqualTo(1.5m);
        }
    }

    private async Task<JsonElement> ReadPageAsync(string url)
    {
        var response = await Client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Json);
    }

    [Fact]
    public async Task Sort_direction_accepts_the_short_form()
    {
        // Act
        var ascending = await ReadAllAsync(Client, "/api/v1/pokemon?sortBy=id&sortDirection=asc&pageSize=3");
        var descending = await ReadAllAsync(Client, "/api/v1/pokemon?sortBy=id&sortDirection=desc&pageSize=3");

        // Assert
        ascending.Select(p => p.GetProperty("id").GetInt32()).ShouldBe([1, 2, 3]);
        descending[0].GetProperty("id").GetInt32().ShouldBeGreaterThan(ascending[2].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Enum_parameters_are_case_insensitive()
    {
        // Act
        var lower = await ReadAllAsync(Client, "/api/v1/pokemon?rarity=legendary&pageSize=5");
        var upper = await ReadAllAsync(Client, "/api/v1/pokemon?rarity=LEGENDARY&pageSize=5");

        // Assert
        lower.Count.ShouldBe(5);
        upper.Select(p => p.GetProperty("id").GetInt32())
            .ShouldBe(lower.Select(p => p.GetProperty("id").GetInt32()));
    }

    [Fact]
    public async Task Legendary_filter_returns_only_legendaries()
    {
        // Act
        var items = await ReadAllAsync(Client, "/api/v1/pokemon?rarity=legendary&pageSize=100");

        // Assert
        items.ShouldNotBeEmpty();
        items.ShouldAllBe(item => item.GetProperty("isLegendary").GetBoolean());
    }

    [Fact]
    public async Task A_name_fragment_filters_by_prefix()
    {
        // Act
        var items = await ReadAllAsync(Client, "/api/v1/pokemon?name=pika");

        // Assert
        items.ShouldNotBeEmpty();
        items.ShouldAllBe(item => item.GetProperty("name").GetString()!.StartsWith("pika", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Total_stats_filter_and_sorting_agree()
    {
        // Act
        var items = await ReadAllAsync(Client, "/api/v1/pokemon?minTotalStats=600&sortBy=totalStats&sortDirection=desc&pageSize=5");

        // Assert
        var totals = items.Select(item => item.GetProperty("totalStats").GetInt32()).ToArray();
        totals.ShouldAllBe(total => total >= 600);
        totals.OrderByDescending(total => total).ShouldBe(totals);
    }

    [Fact]
    public async Task Height_and_weight_are_published_in_conventional_units()
    {
        // Act
        var body = await ReadPageAsync("/api/v1/pokemon?pageSize=1");
        var id = body.GetProperty("items")[0].GetProperty("id").GetInt32();
        var detail = await (await Client.GetAsync($"/api/v1/pokemon/{id}"))
            .Content.ReadFromJsonAsync<JsonElement>(Json);

        // Assert: el dataset guarda decimetros y hectogramos, y la API publica
        // metros y kilogramos, que son las unidades que espera quien consulta.
        detail.GetProperty("height").GetProperty("unit").GetString().ShouldBe("metre");
        detail.GetProperty("weight").GetProperty("unit").GetString().ShouldBe("kilogram");
    }

    private static async Task<List<JsonElement>> ReadAllAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return [.. body.GetProperty("items").EnumerateArray()];
    }

    private static async Task<List<string>> ReadNamesAsync(HttpClient client, string url)
    {
        var items = await ReadAllAsync(client, url);
        return [.. items.Select(item => item.GetProperty("name").GetString()!)];
    }

    private static async Task<List<int>> ReadIdsAsync(HttpClient client, string url)
    {
        var items = await ReadAllAsync(client, url);
        return [.. items.Select(item => item.GetProperty("id").GetInt32())];
    }
}
