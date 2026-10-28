using Avalonia.Media.Imaging;
using UI.Iso;

namespace UI.Pixel;

/// <summary>Petits sprites du HUD du pack utilisés directement par les styles (App.axaml), sans cadre 9-slice.</summary>
public static class PackSkins
{
    /// <summary>Poignée du curseur de défilement vertical (hud_scroll_grip_v), centrée, jamais étirée.</summary>
    public static Bitmap GripV { get; } = Sprite("hud_scroll_grip_v");

    /// <summary>Poignée du curseur de défilement horizontal (hud_scroll_grip_h).</summary>
    public static Bitmap GripH { get; } = Sprite("hud_scroll_grip_h");

    /// <summary>Un sprite d'interface du pack dans un état (« on_normal »…), ou son image unique.</summary>
    public static Bitmap Sprite(string id, string? state = null) => PackLibrary.Png(PackLibrary.StateFile(PackLibrary.Asset(id), state)).ToBitmap();
}
