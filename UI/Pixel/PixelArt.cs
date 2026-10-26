using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace UI.Pixel;

/// <summary>
/// Transforme de l'ASCII art en image : chaque lettre est une couleur de la palette, « . » est transparent.
/// Toutes les icônes et tous les portraits du jeu sont dessinés comme ça, sans fichier externe.
/// </summary>
public static class PixelArt
{
    public static readonly Dictionary<char, uint> Inks = new()
    {
        ['K'] = 0x181425, ['k'] = 0x3a4466, ['g'] = 0x8b9bb4, ['c'] = 0xc0cbdc, ['w'] = 0xffffff,
        ['Y'] = 0xfeae34, ['y'] = 0xfee761, ['O'] = 0xf77622, ['R'] = 0xe43b44, ['r'] = 0xa22633,
        ['G'] = 0x63c74d, ['D'] = 0x3e8948, ['B'] = 0x0099db, ['b'] = 0x124e89, ['C'] = 0x2ce8f5,
        ['P'] = 0xb55088, ['p'] = 0x68386c, ['F'] = 0xf6757a, ['S'] = 0xe8b796, ['s'] = 0xc28569,
        ['N'] = 0x733e39, ['n'] = 0xb86f50, ['T'] = 0xead4aa, ['E'] = 0xe4a672, ['V'] = 0x3e2731,
        ['Z'] = 0x5a6988, ['z'] = 0x262b44,
    };

    public static WriteableBitmap FromAscii(IReadOnlyList<string> rows, IReadOnlyDictionary<char, uint>? extraInks = null)
    {
        int width = rows.Max(r => r.Length);
        int height = rows.Count;
        var pixels = new int[width * height];

        for (int y = 0; y < height; y++)
        for (int x = 0; x < rows[y].Length; x++)
        {
            char ink = rows[y][x];
            uint color;
            if (extraInks != null && extraInks.TryGetValue(ink, out var extra))
                color = extra;
            else if (!Inks.TryGetValue(ink, out color))
                continue; // transparent

            pixels[y * width + x] = unchecked((int)(0xff000000 | color));
        }

        return FromPixels(pixels, width, height);
    }

    public static WriteableBitmap FromPixels(int[] pixels, int width, int height)
    {
        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96),
            PixelFormat.Bgra8888, AlphaFormat.Premul);
        Write(bitmap, pixels, width, height);
        return bitmap;
    }

    public static void Write(WriteableBitmap bitmap, int[] pixels, int width, int height)
    {
        using var frame = bitmap.Lock();
        for (int y = 0; y < height; y++)
            Marshal.Copy(pixels, y * width, frame.Address + y * frame.RowBytes, width);
    }

    public static void Read(ILockedFramebuffer frame, int[] pixels, int width, int height)
    {
        for (int y = 0; y < height; y++)
            Marshal.Copy(frame.Address + y * frame.RowBytes, pixels, y * width, width);
    }

    public static void WriteTo(ILockedFramebuffer frame, int[] pixels, int width, int height)
    {
        for (int y = 0; y < height; y++)
            Marshal.Copy(pixels, y * width, frame.Address + y * frame.RowBytes, width);
    }
}
