namespace PokemonApi.Application.Features.Pokemon.Dtos;

/// <summary>
/// Magnitud fisica de un Pokemon con su unidad.
/// </summary>
/// <remarks>
/// Se publica en la unidad convencional de cada familia (metros y kilogramos) y
/// no en la unidad original del dataset (decimetros y hectogramos), para que el
/// cliente pueda usar el mismo numero que recibe en la respuesta como filtro de
/// <c>minHeight</c>, <c>maxHeight</c>, <c>minWeight</c> o <c>maxWeight</c>.
/// </remarks>
/// <param name="Value">Valor de la magnitud.</param>
/// <param name="Unit">Unidad en la que se expresa el valor.</param>
public readonly record struct MeasureResponse(decimal Value, string Unit)
{
    /// <summary>Convierte una magnitud de dominio en su representacion de API.</summary>
    /// <param name="measure">Magnitud de dominio.</param>
    /// <returns>La representacion de API, en metros o kilogramos.</returns>
    public static MeasureResponse FromDomain(Domain.ValueObjects.Measure measure)
    {
        var conventional = measure.ToConventionalUnit();

        return new MeasureResponse(conventional.Value, conventional.Unit.ToString().ToLowerInvariant());
    }
}

/// <summary>
/// Direcciones de las imagenes de un Pokemon.
/// </summary>
/// <param name="OfficialArtwork">Ilustracion oficial de alta resolucion.</param>
/// <param name="OfficialArtworkShiny">Ilustracion oficial de la variante brillante.</param>
/// <param name="HomeArtwork">Ilustracion de portada.</param>
/// <param name="FrontDefault">Sprite frontal, vista por defecto.</param>
/// <param name="FrontShiny">Sprite frontal de la variante brillante.</param>
/// <param name="PixelArt">Sprite de 96x96 px.</param>
/// <param name="Spritesheet">Hoja de sprites con todas las animaciones.</param>
public sealed record SpritesResponse(
    string OfficialArtwork,
    string OfficialArtworkShiny,
    string HomeArtwork,
    string FrontDefault,
    string FrontShiny,
    string PixelArt,
    string Spritesheet)
{
    /// <summary>Convierte los sprites de dominio en su representacion de API.</summary>
    /// <param name="sprites">Sprites de dominio.</param>
    /// <returns>La representacion de API.</returns>
    public static SpritesResponse FromDomain(Domain.ValueObjects.PokemonSprites sprites) => new(
        sprites.OfficialArtwork.ToString(),
        sprites.OfficialArtworkShiny.ToString(),
        sprites.HomeArtwork.ToString(),
        sprites.FrontDefault.ToString(),
        sprites.FrontShiny.ToString(),
        sprites.PixelArt.ToString(),
        sprites.Spritesheet.ToString());
}
