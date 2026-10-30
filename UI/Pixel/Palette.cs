using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace UI.Pixel;

/// <summary>
/// Les couleurs de l'interface (thème « Papier » du pack) et l'ancienne palette Endesga 32, gardée pour les couleurs de projet.
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

    // Thème « Papier » du pack (assets/hud/theme_tokens.json). Les noms viennent de l'ancienne interface sombre et gardent
    // leur rôle : Snow = texte principal (encre), Fog = texte discret, Night/Slate/Steel = papiers de plus en plus foncés,
    // Gold/Lemon = accents (soleil, sarcelle), Leaf/Ember/Blood/Sky = succès, attention, danger, information.
    public static readonly IBrush Ink = Brush("#2b2128");
    public static readonly IBrush Night = Brush("#efe4cc");
    public static readonly IBrush Slate = Brush("#dccdb1");
    public static readonly IBrush Steel = Brush("#c2ae94");
    public static readonly IBrush Fog = Brush("#6f5f5d");
    public static readonly IBrush Cloud = Brush("#4a3a42");
    public static readonly IBrush Snow = Brush("#2b2128");
    public static readonly IBrush Gold = Brush("#c9922e");
    public static readonly IBrush Lemon = Brush("#275a66");
    public static readonly IBrush Leaf = Brush("#437d40");
    public static readonly IBrush Forest = Brush("#437d40");
    public static readonly IBrush Blood = Brush("#a23a34");
    public static readonly IBrush Sky = Brush("#4670a6");
    public static readonly IBrush Ice = Brush("#357c82");
    public static readonly IBrush Plum = Brush("#8f3d39");
    public static readonly IBrush Ember = Brush("#c05843");
    public static readonly IBrush Paper = Brush("#fbf5e6");

    public static readonly IBrush Wine = Brush("#a22633");
    public static readonly IBrush Navy = Brush("#124e89");
    public static readonly IBrush Teal = Brush("#265c42");
    public static readonly IBrush Sand = Brush("#ead4aa");
    public static readonly IBrush Rust = Brush("#be4a2f");
    public static readonly IBrush Brown = Brush("#733e39");

    /// <summary>
    /// Les accents servent aussi aux barres, aux jauges et aux fonds ; en texte, ils doivent rester lisibles sur tous les papiers
    /// (contraste WCAG AA d'au moins 4,5:1 sur le plus foncé, Slate). Même teinte, plus sombre : l'or passait à 2,2:1 sur le
    /// papier, l'alerte à 3,5:1, le vert, le bleu et la sarcelle à environ 4:1.
    /// </summary>
    private static readonly Dictionary<IBrush, IBrush> TextTones = new(ReferenceEqualityComparer.Instance)
    {
        [Gold] = Brush("#72531a"),
        [Ember] = Brush("#904030"),
        [Leaf] = Brush("#356232"),
        [Forest] = Brush("#356232"),
        [Sky] = Brush("#385a86"),
        [Ice] = Brush("#296065"),
        [Fog] = Brush("#615351"),
        [Blood] = Brush("#9a3732"),
    };

    /// <summary>La couleur à donner à un texte écrit dans cette couleur (son ton lisible pour un accent, sinon elle-même).</summary>
    public static IBrush ForText(IBrush brush) => TextTones.TryGetValue(brush, out var tone) ? tone : brush;

    public static readonly IBrush White = Brush("#ffffff");

    private static double Luminance(Color c)
    {
        static double Channel(byte v) => v / 255.0 is var x && x <= 0.03928 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4);
        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }

    /// <summary>Rapport de contraste WCAG entre deux couleurs (1 à 21).</summary>
    public static double Contrast(Color a, Color b)
    {
        double la = Luminance(a), lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    /// <summary>L'encre ou le blanc, celui qui se lit le mieux sur ce fond (bouton, badge, puce du pack).</summary>
    public static IBrush ReadableOn(Color background) =>
        Contrast(((ISolidColorBrush)Ink).Color, background) >= Contrast(Avalonia.Media.Colors.White, background) ? Ink : White;

    /// <summary>Une couleur de la palette par son indice (0 à 31).</summary>
    public static Color At(int index)
    {
        uint c = Colors[Math.Clamp(index, 0, Colors.Length - 1)];
        return Color.FromRgb((byte)(c >> 16), (byte)(c >> 8), (byte)c);
    }

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
