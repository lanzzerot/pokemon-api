using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using PokemonApi.Api.Infrastructure;

namespace PokemonApi.IntegrationTests;

/// <summary>
/// Pruebas del limitador de peticiones.
/// </summary>
/// <remarks>
/// Esta clase usa su propia fabrica y no la coleccion compartida: el limitador
/// guarda el cupo por IP y, sharing la instancia, las peticiones de esta prueba
/// consumirian el cupo de las demas y las haria fallar de forma intermitente.
/// </remarks>
public sealed class RateLimitingTests : IDisposable
{
    private readonly PokemonApiFactory _factory = new();

    [Fact]
    public async Task The_limit_is_enforced_with_429_and_a_retry_after_header()
    {
        // Arrange: se agotan las peticiones del endpoint, cuyo cupo es de 120.
        using var client = _factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < RateLimitPolicies.DefaultPermitLimit + 10; attempt++)
        {
            var response = await client.GetAsync("/api/v1/pokemon?pageSize=1");
            statuses.Add(response.StatusCode);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                // Act
                var problem = await response.Content.ReadFromJsonAsync<JsonElement>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));

                // Assert
                response.Headers.RetryAfter!.Delta!.Value.ShouldBe(RateLimitPolicies.Window);
                problem.GetProperty("title").GetString().ShouldBe("Too many requests.");
                break;
            }
        }

        // Assert: el limite se alcanzo de verdad dentro del margen calculado.
        statuses.ShouldContain(HttpStatusCode.OK);
        statuses.ShouldContain(HttpStatusCode.TooManyRequests);
    }

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();
}
