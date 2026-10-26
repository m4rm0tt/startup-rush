using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;

namespace UI.Pixel;

/// <summary>
/// Portraits 16×16 des personnages : un même visage, décliné en cheveux, peau, tenue et accessoires.
/// </summary>
public static class Portraits
{
    // H cheveux, S peau, s ombre, E yeux, M bouche, T haut, t col / cravate
    private static readonly string[] Face =
    [
        "................",
        "....HHHHHHHH....",
        "...HHHHHHHHHH...",
        "..HHHHHHHHHHHH..",
        "..HHSSSSSSSSHH..",
        "..HSSSSSSSSSSH..",
        "..HSEESSSSEESH..",
        "..SSEESSSSEESS..",
        "..SSSSSSSSSSSS..",
        "...SSSSssSSSS...",
        "...SSSMMMMSSS...",
        "....SSSSSSSS....",
        ".....sSSSSs.....",
        "..TTTTTttTTTTT..",
        ".TTTTTTttTTTTTT.",
        ".TTTTTTttTTTTTT.",
    ];

    private record Look(uint Hair, uint Skin, uint Shade, uint Top, uint Collar, bool Glasses = false, bool Messy = false);

    private static readonly Dictionary<string, Look> Looks = new()
    {
        ["dev"] = new(0x733e39, 0xe8b796, 0xc28569, 0x5a6988, 0x3a4466, Messy: true),
        ["hacker"] = new(0x181425, 0xc28569, 0xb86f50, 0x68386c, 0x181425, Glasses: true),
        ["business"] = new(0xfee761, 0xe8b796, 0xc28569, 0x124e89, 0xe43b44),
        ["heir"] = new(0xfeae34, 0xead4aa, 0xe4a672, 0xf6757a, 0xffffff),
    };

    private static readonly Dictionary<string, WriteableBitmap> Cache = new();

    public static WriteableBitmap Get(string characterId)
    {
        if (Cache.TryGetValue(characterId, out var bitmap))
            return bitmap;

        var look = Looks.GetValueOrDefault(characterId, Looks["dev"]);
        var rows = Face.ToArray();

        // Cheveux en bataille : des mèches qui dépassent
        if (look.Messy)
            rows[0] = "...H.H..H.HH....";

        // Lunettes : un contour sombre autour des yeux
        if (look.Glasses)
        {
            rows[5] = "..HKKKKSSKKKKH..";
            rows[6] = "..HKEEKKKKEEKH..";
            rows[7] = "..SKKKKSSKKKKS..";
        }

        var inks = new Dictionary<char, uint>
        {
            ['H'] = look.Hair, ['S'] = look.Skin, ['s'] = look.Shade, ['E'] = 0x181425, ['M'] = 0xa22633,
            ['T'] = look.Top, ['t'] = look.Collar, ['K'] = 0x181425,
        };

        bitmap = PixelArt.FromAscii(rows, inks);
        Cache[characterId] = bitmap;
        return bitmap;
    }
}
