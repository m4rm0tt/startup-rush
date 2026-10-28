using Avalonia.Media.Imaging;
using UI.Iso;

namespace UI.Pixel;

/// <summary>Petits sprites du HUD du pack utilisés directement par les styles (App.axaml), sans cadre 9-slice.</summary>
public static class PackSkins
{
    // Déclaré en premier : les propriétés statiques plus bas s'en servent dès l'initialisation de la classe
    private static readonly System.Collections.Generic.Dictionary<string, Bitmap> Cache = new();

    /// <summary>Poignée du curseur de défilement vertical (hud_scroll_grip_v), centrée, jamais étirée.</summary>
    public static Bitmap GripV { get; } = Sprite("hud_scroll_grip_v");

    /// <summary>Poignée du curseur de défilement horizontal (hud_scroll_grip_h).</summary>
    public static Bitmap GripH { get; } = Sprite("hud_scroll_grip_h");

    /// <summary>Une pièce annexe d'un composant (« extras » du manifeste : pointe de bulle, bouts de bandeau…).</summary>
    public static Bitmap Extra(string id, string part)
    {
        string key = id + "+" + part;
        if (!Cache.TryGetValue(key, out var bitmap))
            Cache[key] = bitmap = PackLibrary.Png(PackLibrary.Asset(id).GetProperty("extras").GetProperty(part).GetProperty("file").GetString()!).ToBitmap();
        return bitmap;
    }

    /// <summary>Un sprite d'interface du pack dans un état (« on_normal »…), ou son image unique (décodé une fois).</summary>
    public static Bitmap Sprite(string id, string? state = null)
    {
        string key = id + ":" + state;
        if (!Cache.TryGetValue(key, out var bitmap))
            Cache[key] = bitmap = PackLibrary.Png(PackLibrary.StateFile(PackLibrary.Asset(id), state)).ToBitmap();
        return bitmap;
    }
}
