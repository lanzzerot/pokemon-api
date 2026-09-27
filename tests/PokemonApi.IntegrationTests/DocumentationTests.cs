using System.Net;
using Shouldly;

namespace PokemonApi.IntegrationTests;

/// <summary>
/// Pruebas de la web de documentacion, sus recursos incrustados y sus
/// tipos MIME.
/// </summary>
/// <remarks>
/// La pagina se sirve desde el ensamblado y sin CDN, asi que el riesgo real no
/// es que devuelva un 404 sino que lo devuelva con un tipo MIME que el
/// navegador rechaza. Un <c>text/plain</c> en un <c>&lt;script&gt;</c> no se
/// ejecuta, y la pagina pareceria rota sin que nada falle en el servidor.
/// </remarks>
[Collection(ApiCollection.Name)]
public sealed class DocumentationTests(PokemonApiFactory factory)
{
    private HttpClient Client => factory.CreateClient();

    public static TheoryData<string, string> Assets => new()
    {
        { "/docs", "text/html" },
        { "/docs/docs.css", "text/css" },
        { "/docs/docs.js", "text/javascript" },
        { "/docs/pokemon-api-logo.svg", "image/svg+xml" },
    };

    [Theory]
    [MemberData(nameof(Assets))]
    public async Task The_asset_is_served_with_its_own_content_type(string path, string mediaType)
    {
        // Act
        var response = await Client.GetAsync(path);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(mediaType);
        (await response.Content.ReadAsStringAsync()).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task The_page_is_served_in_every_environment()
    {
        // Act: la documentacion es parte del producto, no una herramienta de
        // desarrollo, asi que no se esconde detras de IsDevelopment.
        var response = await Client.GetAsync("/docs");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.CharSet.ShouldBe("utf-8");
    }

    [Fact]
    public async Task The_page_explains_the_api_without_swagger()
    {
        // Act
        var html = await Client.GetStringAsync("/docs");

        // Assert: la guia es contenido propio, y el enlace al documento OpenAPI
        // es lo que conecta la pagina con la referencia generada.
        html.ShouldContain("Orden y paginación");
        html.ShouldContain("urn:pokemon-api:error:request.validation_failed");
        html.ShouldContain("openapi.json");
        html.ShouldNotContain("swagger");
    }

    [Fact]
    public async Task The_page_does_not_depend_on_anything_external()
    {
        // Act
        var html = await Client.GetStringAsync("/docs");

        // Assert: sin CDN la documentacion funciona sin salida a internet.
        html.ShouldNotContain("//cdn.");
        html.ShouldNotContain("https://unpkg");
        html.ShouldNotContain("googleapis");
        html.ShouldNotContain("http://localhost:5199");
    }

    [Fact]
    public async Task The_assets_are_served_from_the_own_routes()
    {
        // Act: el script declara sus recursos con rutas relativas a /docs.
        var html = await Client.GetStringAsync("/docs");

        // Assert
        html.ShouldContain("href=\"/docs/docs.css\"");
        html.ShouldContain("src=\"/docs/docs.js\"");
        html.ShouldContain("href=\"/docs/pokemon-api-logo.svg\"");
        html.ShouldContain("src=\"/docs/pokemon-api-logo.svg\"");
        html.ShouldContain("href=\"/openapi.json\"");

        var script = await Client.GetStringAsync("/docs/docs.js");
        script.ShouldContain("/openapi.json");
    }
}
