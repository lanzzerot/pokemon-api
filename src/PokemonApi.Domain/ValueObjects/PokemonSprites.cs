namespace PokemonApi.Domain.ValueObjects;

/// <summary>
/// URLs de las imagenes disponibles de un Pokemon.
/// </summary>
/// <remarks>
/// Se exponen como datos porque la API no descarga imagenes: entrega las
/// direcciones y es el cliente quien decide si y cuando obtenerlas. Las URLs
/// apuntan al repositorio publico de sprites de PokeAPI.
/// </remarks>
/// <param name="OfficialArtwork">Ilustracion oficial de alta resolucion.</param>
/// <param name="OfficialArtworkShiny">Ilustracion oficial de la variante brillante.</param>
/// <param name="HomeArtwork">Ilustracion de portada.</param>
/// <param name="FrontDefault">Sprite frontal, vista por defecto.</param>
/// <param name="FrontShiny">Sprite frontal de la variante brillante.</param>
/// <param name="PixelArt">Sprite de 96x96 px.</param>
/// <param name="Spritesheet">Hoja de sprites con todas las animaciones.</param>
public sealed record PokemonSprites(
    Uri OfficialArtwork,
    Uri OfficialArtworkShiny,
    Uri HomeArtwork,
    Uri FrontDefault,
    Uri FrontShiny,
    Uri PixelArt,
    Uri Spritesheet);
