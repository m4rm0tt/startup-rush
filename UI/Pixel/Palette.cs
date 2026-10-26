using System;
using Avalonia.Media;
using Core.roguelike;

namespace UI.Pixel;

/// <summary>
/// La palette Endesga 32 : toute la scène est ramenée à ces 32 couleurs,
/// et l'interface n'en utilise pas d'autres. C'est ce qui donne un style cohérent.
/// </summary>
public static class Palette
{
    public static readonly uint[] Colors =
    [
        0xbe4a2f, 0xd77643, 0xead4aa, 0xe4a672, 0xb86f50, 0x733e39, 0x3e2731, 0xa22633,
        0xe43b44, 0xf77622, 0xfeae34, 0xfee761, 0x63c74d, 0x3e8948, 0x265c42, 0x193c3e,
        0x124e89, 0x0099db, 0x2ce8f5, 0xffffff, 0xc0cbdc, 0x8b9bb4, 0x5a6988, 0x3a4466,
        0x262b44, 0x181425, 0xff0044, 0x68386c, 0xb55088, 0xf6757a, 0xe8b796, 0xc28569,
    ];

    public static readonly IBrush Ink = Brush("#181425");
    public static readonly IBrush Night = Brush("#262b44");
    public static readonly IBrush Slate = Brush("#3a4466");
    public static readonly IBrush Steel = Brush("#5a6988");
    public static readonly IBrush Fog = Brush("#8b9bb4");
    public static readonly IBrush Cloud = Brush("#c0cbdc");
    public static readonly IBrush Snow = Brush("#ffffff");
    public static readonly IBrush Gold = Brush("#feae34");
    public static readonly IBrush Lemon = Brush("#fee761");
    public static readonly IBrush Leaf = Brush("#63c74d");
    public static readonly IBrush Forest = Brush("#3e8948");
    public static readonly IBrush Blood = Brush("#e43b44");
    public static readonly IBrush Sky = Brush("#0099db");
    public static readonly IBrush Ice = Brush("#2ce8f5");
    public static readonly IBrush Plum = Brush("#b55088");
    public static readonly IBrush Ember = Brush("#f77622");

    public static IBrush Rarity(Rarity rarity) => rarity switch
    {
        Core.roguelike.Rarity.Common => Cloud,
        Core.roguelike.Rarity.Rare => Sky,
        Core.roguelike.Rarity.Epic => Plum,
        Core.roguelike.Rarity.Legendary => Gold,
        _ => Blood
    };

    public static string RarityName(Rarity rarity) => rarity switch
    {
        Core.roguelike.Rarity.Common => "COMMUN",
        Core.roguelike.Rarity.Rare => "RARE",
        Core.roguelike.Rarity.Epic => "ÉPIQUE",
        Core.roguelike.Rarity.Legendary => "LÉGENDAIRE",
        _ => "MALÉDICTION"
    };

    public static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));

    // Table de correspondance RGB 15 bits → couleur de la palette la plus proche (calculée une seule fois)
    private static readonly Lazy<uint[]> Lookup = new(BuildLookup);

    /// <summary>Couleur de la palette la plus proche, en tenant compte de la sensibilité de l'œil.</summary>
    public static uint Nearest(int r, int g, int b) => Lookup.Value[(r >> 3 << 10) | (g >> 3 << 5) | (b >> 3)];

    private static uint[] BuildLookup()
    {
        var table = new uint[32 * 32 * 32];
        for (int r = 0; r < 32; r++)
        for (int g = 0; g < 32; g++)
        for (int b = 0; b < 32; b++)
        {
            int rr = r << 3 | 4, gg = g << 3 | 4, bb = b << 3 | 4;
            uint best = Colors[0];
            double bestDistance = double.MaxValue;
            foreach (uint c in Colors)
            {
                int dr = rr - (int)(c >> 16 & 0xff), dg = gg - (int)(c >> 8 & 0xff), db = bb - (int)(c & 0xff);
                double distance = 0.3 * dr * dr + 0.59 * dg * dg + 0.11 * db * db;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = c;
                }
            }
            table[r << 10 | g << 5 | b] = best;
        }
        return table;
    }
}
