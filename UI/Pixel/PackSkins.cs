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

    /// <summary>Main au-dessus de ce qui se clique (boutons).</summary>
    public static Avalonia.Input.Cursor ActionCursor => Cursor("cursor_action");

    /// <summary>Barre d'insertion au-dessus des champs de saisie.</summary>
    public static Avalonia.Input.Cursor TextCursor => Cursor("cursor_text");

    private static readonly System.Collections.Generic.Dictionary<string, Avalonia.Input.Cursor> Cursors = new();

    /// <summary>Un curseur du pack, dans son export ×2 (même dessin agrandi) avec son point actif.</summary>
    public static Avalonia.Input.Cursor Cursor(string id)
    {
        if (Cursors.TryGetValue(id, out var cursor))
            return cursor;
        var x2 = PackLibrary.Asset(id).GetProperty("technical_exports").GetProperty("x2");
        var hotspot = x2.GetProperty("hotspot");
        var bitmap = PackLibrary.Png(x2.GetProperty("file").GetString()!).ToBitmap();
        return Cursors[id] = new Avalonia.Input.Cursor(bitmap, new Avalonia.PixelPoint(hotspot[0].GetInt32(), hotspot[1].GetInt32()));
    }

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
