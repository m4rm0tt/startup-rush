using System;
using System.Collections.Generic;
using System.Text.Json;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace UI.Pixel;

/// <summary>Un sprite de l'atlas de mobilier : son rectangle et l'emprise au sol qu'il représente.</summary>
public record AtlasSprite(string Name, PixelRect Rect, int FootprintWidth, int FootprintHeight, string Source);

/// <summary>
/// L'atlas de la scène (meubles et personnages), produit par tools/build_assets.py avec son manifeste JSON.
/// L'image est décodée une fois ; chaque sprite est découpé une fois puis gardé en cache.
/// </summary>
public static class Atlas
{
    private const string ImageUri = "avares://UI/Assets/Sprites/furniture.png";
    private const string ManifestUri = "avares://UI/Assets/Sprites/furniture.json";
    private const string PromptsUri = "avares://UI/Assets/Ui/input_prompts.png";

    private static Bitmap? _image;
    private static Dictionary<string, AtlasSprite>? _sprites;
    private static readonly Dictionary<string, CroppedBitmap> Cache = new();
    private static readonly Dictionary<string, Bitmap> Sheets = new();

    public static Bitmap Image => _image ??= Load(ImageUri);

    public static IReadOnlyDictionary<string, AtlasSprite> Sprites => _sprites ??= LoadManifest();

    private static Bitmap Load(string uri)
    {
        using var stream = AssetLoader.Open(new Uri(uri));
        return new Bitmap(stream);
    }

    private static Dictionary<string, AtlasSprite> LoadManifest()
    {
        using var stream = AssetLoader.Open(new Uri(ManifestUri));
        using var document = JsonDocument.Parse(stream);
        var result = new Dictionary<string, AtlasSprite>();
        foreach (var entry in document.RootElement.EnumerateObject())
        {
            var v = entry.Value;
            var footprint = v.GetProperty("footprint");
            result[entry.Name] = new AtlasSprite(entry.Name,
                new PixelRect(v.GetProperty("x").GetInt32(), v.GetProperty("y").GetInt32(), v.GetProperty("w").GetInt32(), v.GetProperty("h").GetInt32()),
                footprint[0].GetInt32(), footprint[1].GetInt32(), v.GetProperty("source").GetString() ?? "");
        }
        return result;
    }

    public static bool Has(string name) => Sprites.ContainsKey(name);

    public static AtlasSprite? Sprite(string name) => Sprites.TryGetValue(name, out var s) ? s : null;

    /// <summary>Le sprite découpé, pour l'afficher dans un contrôle Image (catalogue, inspecteur).</summary>
    public static CroppedBitmap? Cropped(string name)
    {
        if (Cache.TryGetValue(name, out var cropped))
            return cropped;
        var sprite = Sprite(name);
        if (sprite == null)
            return null;
        cropped = new CroppedBitmap(Image, sprite.Rect);
        Cache[name] = cropped;
        return cropped;
    }

    /// <summary>Une touche du clavier (Kenney Input Prompts Pixel, tuiles de 16 px).</summary>
    public static CroppedBitmap Key(int tile, int tilesWide = 1, int tilesHigh = 1)
    {
        string key = $"key{tile}x{tilesWide}x{tilesHigh}";
        if (!Cache.TryGetValue(key, out var sprite))
        {
            const int columns = 34;
            if (!Sheets.TryGetValue(PromptsUri, out var sheet))
                Sheets[PromptsUri] = sheet = Load(PromptsUri);
            sprite = new CroppedBitmap(sheet, new PixelRect(tile % columns * 16, tile / columns * 16, tilesWide * 16, tilesHigh * 16));
            Cache[key] = sprite;
        }
        return sprite;
    }

    /// <summary>Un cadre 9-slice recoloré (Kenney Pixel UI Pack).</summary>
    public static Bitmap Skin(string name)
    {
        string uri = $"avares://UI/Assets/Ui/skins/{name}.png";
        if (!Sheets.TryGetValue(uri, out var bitmap))
            Sheets[uri] = bitmap = Load(uri);
        return bitmap;
    }
}
