using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Shouldly;

namespace PokemonApi.IntegrationTests;

/// <summary>
/// Pruebas del documento de OpenAPI, que es parte del contrato público: si el
/// esquema no coincide con lo que la API acepta, el cliente se integra mal.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class OpenApiTests(PokemonApiFactory factory)
{
    private const string DocumentPath = "/openapi.json";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Sufijo numerico que el generador añade al desempatar dos esquemas con el
    /// mismo nombre: <c>AbilityResponse</c> y <c>AbilityResponse2</c>.
    /// </summary>
    private static readonly Regex NumberedSchema = new(@"^[A-Za-z]\w*\d+$", RegexOptions.Compiled);

    private HttpClient Client => factory.CreateClient();

    [Fact]
    public async Task The_document_is_served()
    {
        // Act
        var response = await Client.GetAsync(DocumentPath);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    [Fact]
    public async Task The_document_describes_itself()
    {
        // Act
        var document = await ReadDocumentAsync();

        // Assert: sin esto el cliente no sabe a quien preguntar ni con que version.
        var info = document.GetProperty("info");
        info.GetProperty("title").GetString().ShouldBe("Pokemon API");
        info.GetProperty("version").GetString().ShouldBe("v1");
        document.GetProperty("openapi").GetString().ShouldStartWith("3.");
    }

    [Fact]
    public async Task Every_route_is_documented()
    {
        // Act
        var paths = await ReadPathsAsync();

        // Assert
        paths.TryGetProperty("/api/v1/pokemon", out _).ShouldBeTrue();
        paths.TryGetProperty("/api/v1/pokemon/{id}", out _).ShouldBeTrue();
        paths.TryGetProperty("/api/v1/pokemon/by-name/{name}", out _).ShouldBeTrue();
        paths.TryGetProperty("/api/v1/pokemon/evolution-chain/{name}", out _).ShouldBeTrue();
        paths.TryGetProperty("/api/v1/generations", out _).ShouldBeTrue();
        paths.TryGetProperty("/api/v1/types", out _).ShouldBeTrue();
        paths.TryGetProperty("/api/v1/abilities", out _).ShouldBeTrue();
        paths.TryGetProperty("/api/v1/egg-groups", out _).ShouldBeTrue();
        paths.TryGetProperty("/api/v1/habitats", out _).ShouldBeTrue();
        paths.TryGetProperty("/api/v1/regions", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task The_documentation_website_is_not_part_of_the_contract()
    {
        // Act
        var paths = await ReadPathsAsync();

        // Assert: /docs documenta la API, no se documenta a si misma.
        paths.EnumerateObject().ShouldNotContain(path => path.Name.StartsWith("/docs", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Closed_world_parameters_publish_their_allowed_values()
    {
        // Act
        var parameters = await ListParametersAsync();

        // Assert: sin esto el cliente no descubre los valores admitidos.
        var sortBy = FindParameter(parameters, "sortBy");
        AllowedValues(sortBy).ShouldBe(
            ["Id", "Name", "Height", "Weight", "TotalStats", "BaseExperience"],
            ignoreOrder: true);

        var sortDirection = FindParameter(parameters, "sortDirection");
        AllowedValues(sortDirection).ShouldBe(["Ascending", "Descending", "asc", "desc"], ignoreOrder: true);

        var rarity = FindParameter(parameters, "rarity");
        AllowedValues(rarity).ShouldBe(["Common", "Legendary", "Mythical"], ignoreOrder: true);

        var minStat = FindParameter(parameters, "minStat");
        AllowedValues(minStat).ShouldBe(
            ["Hp", "Attack", "Defense", "SpecialAttack", "SpecialDefense", "Speed"],
            ignoreOrder: true);
    }

    [Fact]
    public async Task Catalog_filters_are_documented_with_their_plural_names()
    {
        // Act
        var names = (await ListParametersAsync())
            .Select(p => p.GetProperty("name").GetString())
            .ToArray();

        // Assert: los nombres en plural son los que la API acepta de verdad.
        names.ShouldContain("types");
        names.ShouldContain("abilities");
        names.ShouldContain("generations");
        names.ShouldContain("regions");
        names.ShouldContain("eggGroups");
        names.ShouldContain("habitats");
        names.ShouldNotContain("type");
        names.ShouldNotContain("ability");
        names.ShouldNotContain("region");
    }

    [Fact]
    public async Task Numeric_filters_are_documented_with_their_type()
    {
        // Act
        var parameters = await ListParametersAsync();

        // Assert: los filtros de magnitud admiten decimales y los de estadistica
        // son enteros.
        FindParameter(parameters, "minHeight").GetProperty("schema").GetProperty("type").GetString()
            .ShouldBe("number");
        FindParameter(parameters, "minTotalStats").GetProperty("schema").GetProperty("type").GetString()
            .ShouldBe("integer");
        FindParameter(parameters, "page").GetProperty("schema").GetProperty("type").GetString()
            .ShouldBe("integer");
    }

    [Fact]
    public async Task Schemas_are_described_with_their_xml_comments()
    {
        // Act
        var document = await ReadDocumentAsync();
        var schemas = document.GetProperty("components").GetProperty("schemas");

        // Assert: el generador de .NET no lee los XML, el transformador si. Sin
        // esto el esquema es una lista de nombres de propiedad sin explicar.
        schemas.GetProperty("PokemonDetailResponse").GetProperty("description").GetString()
            .ShouldBe("Vista completa de un Pokemon.");

        var height = schemas.GetProperty("PokemonDetailResponse")
            .GetProperty("properties")
            .GetProperty("height");

        height.GetProperty("description").GetString().ShouldBe("Altura y su unidad.");
    }

    [Fact]
    public async Task Colliding_schema_names_are_disambiguated_by_layer()
    {
        // Act
        var document = await ReadDocumentAsync();
        var names = document.GetProperty("components").GetProperty("schemas")
            .EnumerateObject()
            .Select(schema => schema.Name)
            .ToArray();

        // Assert: AbilityResponse existe en la API y en la aplicacion, y el
        // sufijo numerico del generador parece un error de tecleo.
        names.ShouldNotContain(name => NumberedSchema.IsMatch(name));
        names.ShouldContain("AbilityResponseApi");
        names.ShouldContain("AbilityResponseApplication");
    }

    private static JsonElement FindParameter(JsonElement[] parameters, string name) => parameters
        .Single(p => p.GetProperty("name").GetString() == name);

    private static List<string> AllowedValues(JsonElement parameter) =>
    [
        .. parameter.GetProperty("schema").GetProperty("enum").EnumerateArray()
            .Select(v => v.GetString()!)
    ];

    private async Task<JsonElement[]> ListParametersAsync() => (await ReadDocumentAsync())
        .GetProperty("paths")
        .GetProperty("/api/v1/pokemon")
        .GetProperty("get")
        .GetProperty("parameters")
        .EnumerateArray()
        .ToArray();

    private async Task<JsonElement> ReadDocumentAsync()
    {
        var response = await Client.GetAsync(DocumentPath);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await response.Content.ReadFromJsonAsync<JsonElement>(Json);
    }

    private async Task<JsonElement> ReadPathsAsync() => (await ReadDocumentAsync()).GetProperty("paths");
}
