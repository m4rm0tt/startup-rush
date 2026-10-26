using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace UI.Pixel;

/// <summary>
/// Les images du jeu tirées des packs (voir ASSET_USAGE.md) : chaque planche est chargée une seule fois,
/// chaque sprite est un rectangle de la planche, découpé une seule fois puis gardé en cache.
/// </summary>
public static class Sprites
{
    private const string Office = "avares://UI/Assets/Sprites/office.png";
    private const string Prompts = "avares://UI/Assets/Ui/input_prompts.png";

    // 2dPig Pixel Office (CC0), recoloré en Endesga 32 par tools/build_assets.py
    private static readonly Dictionary<string, PixelRect> OfficeRects = new()
    {
        ["clouds"] = new(0, 0, 256, 36),
        ["table"] = new(85, 47, 26, 16),
        ["plant"] = new(170, 65, 14, 19),
        ["sofa0"] = new(119, 66, 33, 15),
        ["sofa1"] = new(120, 83, 33, 16),
        ["sofa2"] = new(120, 102, 33, 16),
        ["sofa3"] = new(120, 121, 33, 16),
        ["whiteboard"] = new(84, 70, 26, 20),
        ["calendar"] = new(234, 81, 17, 11),
        ["coffeeMachine"] = new(159, 91, 7, 11),
        ["mug"] = new(169, 95, 8, 7),
        ["painting"] = new(239, 94, 11, 8),
        ["clock"] = new(159, 108, 19, 6),
        ["printer"] = new(233, 106, 15, 19),
        ["vending"] = new(159, 123, 24, 34),
        ["drinks"] = new(184, 126, 24, 31),
        ["cat0"] = new(65, 129, 16, 13),
        ["cat1"] = new(82, 129, 16, 13), // frame dérivée : la queue relevée
        ["dog"] = new(59, 146, 24, 11),
        ["bin"] = new(116, 143, 9, 14),
        ["cooler"] = new(147, 140, 9, 17),
        ["employee0"] = new(2, 105, 15, 23),
        ["employee1"] = new(19, 104, 19, 24),
        ["employee2"] = new(40, 107, 13, 21),
        ["employee3"] = new(3, 132, 17, 23),
        ["employee4"] = new(22, 132, 17, 23),
        ["binderRed"] = new(211, 119, 11, 8),
        ["binderBlue"] = new(211, 129, 11, 8),
        ["binderGreen"] = new(211, 140, 11, 8),
    };

    public const int EmployeeLooks = 5;

    private static readonly Dictionary<string, Bitmap> Sheets = new();
    private static readonly Dictionary<string, CroppedBitmap> Cache = new();

    private static Bitmap Sheet(string uri)
    {
        if (!Sheets.TryGetValue(uri, out var bitmap))
        {
            using var stream = AssetLoader.Open(new Uri(uri));
            bitmap = new Bitmap(stream);
            Sheets[uri] = bitmap;
        }
        return bitmap;
    }

    /// <summary>Un sprite de la planche de bureau, à sa taille d'origine en pixels.</summary>
    public static CroppedBitmap Get(string name)
    {
        if (!Cache.TryGetValue(name, out var sprite))
        {
            sprite = new CroppedBitmap(Sheet(Office), OfficeRects[name]);
            Cache[name] = sprite;
        }
        return sprite;
    }

    public static PixelSize Size(string name) => OfficeRects[name].Size;

    /// <summary>Une touche du clavier (Kenney Input Prompts Pixel, tuiles de 16 px).</summary>
    public static CroppedBitmap Key(int tile, int tilesWide = 1, int tilesHigh = 1)
    {
        string key = $"key{tile}x{tilesWide}x{tilesHigh}";
        if (!Cache.TryGetValue(key, out var sprite))
        {
            const int columns = 34;
            var rect = new PixelRect(tile % columns * 16, tile / columns * 16, tilesWide * 16, tilesHigh * 16);
            sprite = new CroppedBitmap(Sheet(Prompts), rect);
            Cache[key] = sprite;
        }
        return sprite;
    }

    /// <summary>Un cadre 9-slice recoloré (Kenney Pixel UI Pack).</summary>
    public static Bitmap Skin(string name) => Sheet($"avares://UI/Assets/Ui/skins/{name}.png");
}

/// <summary>Les touches affichées dans l'interface, pour qu'on sache toujours quoi appuyer.</summary>
public static class Keys
{
    public static CroppedBitmap Escape => Sprites.Key(17);
    public static CroppedBitmap F11 => Sprites.Key(28);
    public static CroppedBitmap One => Sprites.Key(51);
    public static CroppedBitmap Two => Sprites.Key(52);
    public static CroppedBitmap Three => Sprites.Key(53);
    public static CroppedBitmap Four => Sprites.Key(54);
    public static CroppedBitmap R => Sprites.Key(88);
    public static CroppedBitmap Space => Sprites.Key(153);
    public static CroppedBitmap Enter => Sprites.Key(100, 2, 2);
    public static CroppedBitmap Click => Sprites.Key(578);

    public static CroppedBitmap Number(int index) => Sprites.Key(51 + Math.Clamp(index, 0, 8));
}
