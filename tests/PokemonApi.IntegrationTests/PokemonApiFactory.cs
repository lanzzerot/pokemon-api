using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace PokemonApi.IntegrationTests;

/// <summary>
/// Arranca la API completa en memoria contra el dataset real.
/// </summary>
/// <remarks>
/// Se usa el dataset de verdad, no un doble, porque lo que interesa verificar en
/// las pruebas de integracion es precisamente que el JSON incrustado se carga y
/// se traduce sin errores: un doble aqui no detectaria un cambio en el mapeo del
/// dataset ni en las unidades.
/// <para>
/// El entorno es Development paraReproducir la configuracion de trabajo y tener
/// acceso al documento de OpenAPI, que solo se publica en ese entorno.
/// </para>
/// </remarks>
public sealed class PokemonApiFactory : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Development);
    }
}

/// <summary>
/// Grupo de pruebas que comparten una unica instancia de la API.
/// </summary>
/// <remarks>
/// Compartir la instancia evita materializar el dataset una vez por prueba. Se
/// declara como coleccion porque xUnit ejecuta en paralelo las clases de una
/// misma coleccion solo si no comparten ella, y arrancar la API en paralelo
/// multiplicaria el coste sin aportar cobertura.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<PokemonApiFactory>
{
    /// <summary>Nombre de la coleccion.</summary>
    public const string Name = "pokemon-api";
}
