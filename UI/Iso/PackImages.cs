using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace UI.Iso;

// Portage du compositeur de référence du pack (assets/StartupRushOriginalAssets/tools/csharp/StartupRushReference/Program.cs) :
// PNG RGBA 8 bits, composition « over », recoloration par masque et motifs muraux, mêmes opérations que la référence.

/// <summary>Une image RGBA 8 bits non prémultipliée.</summary>
public sealed class Img
{
    public readonly int W, H;
    public readonly byte[] P;

    public Img(int w, int h)
    {
        W = w;
        H = h;
        P = new byte[w * h * 4];
    }

    public Img Clone()
    {
        var c = new Img(W, H);
        Buffer.BlockCopy(P, 0, c.P, 0, P.Length);
        return c;
    }

    /// <summary>Le bitmap Avalonia correspondant (BGRA prémultiplié), à garder en cache.</summary>
    public Bitmap ToBitmap()
    {
        var bitmap = new WriteableBitmap(new PixelSize(Math.Max(1, W), Math.Max(1, H)), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var buffer = bitmap.Lock();
        var row = new byte[W * 4];
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                int i = (y * W + x) * 4;
                int a = P[i + 3];
                row[x * 4] = (byte)(P[i + 2] * a / 255);
                row[x * 4 + 1] = (byte)(P[i + 1] * a / 255);
                row[x * 4 + 2] = (byte)(P[i] * a / 255);
                row[x * 4 + 3] = (byte)a;
            }
            System.Runtime.InteropServices.Marshal.Copy(row, 0, buffer.Address + y * buffer.RowBytes, row.Length);
        }
        return bitmap;
    }
}

public static class Png
{
    private static int BE32(byte[] d, int p) => (d[p] << 24) | (d[p + 1] << 16) | (d[p + 2] << 8) | d[p + 3];

    public static Img Load(byte[] data, string name)
    {
        int pos = 8, w = 0, h = 0, depth = 0, ctype = 0, inter = 0;
        var idat = new MemoryStream();
        while (pos < data.Length)
        {
            int len = BE32(data, pos);
            string type = System.Text.Encoding.ASCII.GetString(data, pos + 4, 4);
            int d = pos + 8;
            if (type == "IHDR")
            {
                w = BE32(data, d);
                h = BE32(data, d + 4);
                depth = data[d + 8];
                ctype = data[d + 9];
                inter = data[d + 12];
            }
            else if (type == "IDAT")
                idat.Write(data, d, len);
            else if (type == "IEND")
                break;
            pos = d + len + 4;
        }
        if (depth != 8 || ctype != 6 || inter != 0)
            throw new NotSupportedException($"PNG attendu RGBA 8 bits : {name}");
        idat.Position = 0;
        var raw = new MemoryStream();
        using (var z = new ZLibStream(idat, CompressionMode.Decompress))
            z.CopyTo(raw);
        byte[] r = raw.ToArray();
        var img = new Img(w, h);
        int stride = w * 4, ip = 0;
        var prev = new byte[stride];
        var cur = new byte[stride];
        for (int y = 0; y < h; y++)
        {
            int f = r[ip++];
            Buffer.BlockCopy(r, ip, cur, 0, stride);
            ip += stride;
            for (int x = 0; x < stride; x++)
            {
                int a = x >= 4 ? cur[x - 4] : 0, b = prev[x], c = x >= 4 ? prev[x - 4] : 0, v = cur[x];
                switch (f)
                {
                    case 1: v += a; break;
                    case 2: v += b; break;
                    case 3: v += (a + b) >> 1; break;
                    case 4:
                        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
                        v += pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
                        break;
                }
                cur[x] = (byte)v;
            }
            Buffer.BlockCopy(cur, 0, img.P, y * stride, stride);
            (prev, cur) = (cur, prev);
        }
        return img;
    }
}

/// <summary>Composition alpha « over » et tampon d'ombre (maximum des alphas), comme la référence.</summary>
public static class Blend
{
    public static void Over(Img dst, Img src, int x, int y, float opacity = 1f)
    {
        int x0 = Math.Max(0, x), y0 = Math.Max(0, y), x1 = Math.Min(dst.W, x + src.W), y1 = Math.Min(dst.H, y + src.H);
        for (int yy = y0; yy < y1; yy++)
            for (int xx = x0; xx < x1; xx++)
            {
                int si = ((yy - y) * src.W + (xx - x)) * 4, di = (yy * dst.W + xx) * 4;
                if (src.P[si + 3] == 0)
                    continue;
                float sa = src.P[si + 3] / 255f * opacity, da = dst.P[di + 3] / 255f;
                float oa = sa + da * (1f - sa);
                for (int k = 0; k < 3; k++)
                {
                    float v = oa > 0 ? (src.P[si + k] * sa + dst.P[di + k] * da * (1f - sa)) / Math.Max(oa, 1e-6f) : 0f;
                    dst.P[di + k] = (byte)Math.Clamp(MathF.Round(v), 0, 255);
                }
                dst.P[di + 3] = (byte)Math.Clamp(MathF.Round(oa * 255f), 0, 255);
            }
    }

    public static void StampMax(byte[] buf, int bw, int bh, Img src, int x, int y)
    {
        int x0 = Math.Max(0, x), y0 = Math.Max(0, y), x1 = Math.Min(bw, x + src.W), y1 = Math.Min(bh, y + src.H);
        for (int yy = y0; yy < y1; yy++)
            for (int xx = x0; xx < x1; xx++)
            {
                byte a = src.P[((yy - y) * src.W + (xx - x)) * 4 + 3];
                if (a > buf[yy * bw + xx])
                    buf[yy * bw + xx] = a;
            }
    }
}

/// <summary>
/// Le pack embarqué dans l'exécutable : manifeste, plans de pièces et PNG (sprites et masques). Chaque PNG est décodé une
/// seule fois ; chaque recoloration est gardée en cache par (fichier, couleurs).
/// </summary>
public static class PackLibrary
{
    private static readonly Dictionary<string, string> Resources = Assembly.GetExecutingAssembly().GetManifestResourceNames()
        .Where(n => n.StartsWith("pack/"))
        .ToDictionary(n => n.Replace('\\', '/')["pack/".Length..], n => n);

    private static readonly Lazy<JsonDocument> Manifest = new(() => JsonDocument.Parse(Read("manifest.json")));
    private static readonly Dictionary<string, JsonElement> AssetsById = new();
    private static readonly Dictionary<string, int> Channels = new();
    private static readonly Dictionary<int, string> ChannelNames = new();
    private static readonly Dictionary<string, byte[][]> Ramps = new();
    private static readonly Dictionary<string, Img> Images = new();
    private static readonly Dictionary<string, Img> Recolored = new();
    private static readonly Dictionary<string, JsonDocument> Rooms = new();
    private static bool _loaded;

    public static (byte R, byte G, byte B) ShadowColor { get; private set; }

    private static byte[] Read(string path)
    {
        if (!Resources.TryGetValue(path, out var name))
            throw new FileNotFoundException($"Ressource du pack absente : {path}");
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
            return;
        _loaded = true;
        var root = Manifest.Value.RootElement;
        foreach (var a in root.GetProperty("assets").EnumerateArray())
            AssetsById[a.GetProperty("id").GetString()!] = a;
        foreach (var c in root.GetProperty("channels").GetProperty("ids").EnumerateObject())
        {
            Channels[c.Name] = c.Value.GetInt32();
            ChannelNames[c.Value.GetInt32()] = c.Name;
        }
        foreach (var r in root.GetProperty("palette").GetProperty("ramps").EnumerateObject())
            Ramps[r.Name] = r.Value.EnumerateArray().Select(h => Hex(h.GetString()!)).ToArray();
        var shadow = Hex(root.GetProperty("palette").GetProperty("fixed").GetProperty("shadow").GetString()!);
        ShadowColor = (shadow[0], shadow[1], shadow[2]);
    }

    private static byte[] Hex(string s) =>
        [Convert.ToByte(s.Substring(1, 2), 16), Convert.ToByte(s.Substring(3, 2), 16), Convert.ToByte(s.Substring(5, 2), 16)];

    public static bool Has(string id)
    {
        EnsureLoaded();
        return AssetsById.ContainsKey(id);
    }

    public static JsonElement Asset(string id)
    {
        EnsureLoaded();
        return AssetsById[id];
    }

    public static JsonElement Room(string planId)
    {
        if (!Rooms.TryGetValue(planId, out var document))
            Rooms[planId] = document = JsonDocument.Parse(Read($"rooms/{planId}.json"));
        return document.RootElement;
    }

    public static Img Png(string file)
    {
        EnsureLoaded();
        if (!Images.TryGetValue(file, out var img))
            Images[file] = img = UI.Iso.Png.Load(Read(file), file);
        return img;
    }

    private static string Key(string file, IReadOnlyDictionary<string, string> swatches) =>
        file + "|" + string.Join(",", swatches.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}"));

    /// <summary>Un calque du pack, recoloré par son masque si des couleurs sont demandées.</summary>
    public static Img Layer(JsonElement layer, IReadOnlyDictionary<string, string> swatches)
    {
        string file = layer.GetProperty("file").GetString()!;
        if (swatches.Count == 0 || !layer.TryGetProperty("mask", out var mask))
            return Png(file);
        string key = Key(file, swatches);
        if (!Recolored.TryGetValue(key, out var img))
            Recolored[key] = img = Recolor(Png(file), Png(mask.GetString()!), swatches);
        return img;
    }

    public static Img Recolor(Img rgba, Img mask, IReadOnlyDictionary<string, string> sw)
    {
        EnsureLoaded();
        var o = rgba.Clone();
        foreach (var kv in sw)
        {
            if (!Channels.TryGetValue(kv.Key, out int c) || !Ramps.TryGetValue(kv.Value, out var pal))
                continue;
            for (int i = 0; i < o.P.Length; i += 4)
                if (mask.P[i + 3] > 0 && mask.P[i] == c)
                {
                    var col = pal[Math.Min(5, (int)mask.P[i + 1])];
                    o.P[i] = col[0];
                    o.P[i + 1] = col[1];
                    o.P[i + 2] = col[2];
                }
        }
        return o;
    }

    /// <summary>Motif mural : calque (canal, delta + 8) appliqué aux pixels marqués B &amp; 1, puis recoloration (COLOR_SYSTEM.md §5).</summary>
    public static Img WallPattern(Img rgba, Img mask, Img ov, IReadOnlyDictionary<string, string> sw, List<(int dx, int dy)> offsets)
    {
        EnsureLoaded();
        var o = rgba.Clone();
        int n = rgba.W * rgba.H;
        var ch = new int[n];
        var idx = new int[n];
        var done = new bool[n];
        for (int i = 0; i < n; i++)
        {
            ch[i] = mask.P[i * 4];
            idx[i] = mask.P[i * 4 + 1];
        }
        foreach (var (dx, dy) in offsets)
            for (int y = 0; y < rgba.H; y++)
                for (int x = 0; x < rgba.W; x++)
                {
                    int i = y * rgba.W + x;
                    if (done[i] || mask.P[i * 4 + 3] == 0 || (mask.P[i * 4 + 2] & 1) == 0)
                        continue;
                    int oy = y - dy, ox = x - dx;
                    if (oy < 0 || oy >= ov.H || ox < 0 || ox >= ov.W)
                        continue;
                    int oi = (oy * ov.W + ox) * 4;
                    if (ov.P[oi + 3] == 0)
                        continue;
                    ch[i] = ov.P[oi];
                    idx[i] = Math.Clamp(idx[i] + ov.P[oi + 1] - 8, 0, 5);
                    done[i] = true;
                }
        for (int i = 0; i < n; i++)
        {
            if (mask.P[i * 4 + 3] == 0 || !ChannelNames.TryGetValue(ch[i], out var cname))
                continue;
            string? ramp = sw.TryGetValue(cname, out var r0) ? r0 : cname == "wall2" && sw.TryGetValue("wall", out var r1) ? r1 : null;
            if (ramp == null || !Ramps.TryGetValue(ramp, out var pal))
                continue;
            var col = pal[idx[i]];
            o.P[i * 4] = col[0];
            o.P[i * 4 + 1] = col[1];
            o.P[i * 4 + 2] = col[2];
        }
        return o;
    }

    /// <summary>L'orientation demandée d'un asset, ou la première disponible.</summary>
    public static JsonElement Orient(JsonElement asset, string facing)
    {
        var os = asset.GetProperty("orientations");
        return os.TryGetProperty(facing, out var o) ? o : os.EnumerateObject().First().Value;
    }

    public static (int X, int Y) Anchor(JsonElement orientation)
    {
        var a = orientation.GetProperty("anchor");
        return (a[0].GetInt32(), a[1].GetInt32());
    }

    /// <summary>La nuance représentative d'une rampe de la palette (celle du milieu), pour les cases de couleur.</summary>
    public static Avalonia.Media.Color RampColor(string ramp)
    {
        EnsureLoaded();
        var shades = Ramps[ramp];
        var c = shades[shades.Length / 2];
        return Avalonia.Media.Color.FromRgb(c[0], c[1], c[2]);
    }

    /// <summary>Le fichier d'un composant d'interface dans un état (« hover », « on_normal »…), ou son fichier unique.</summary>
    public static string StateFile(JsonElement asset, string? state)
    {
        if (asset.TryGetProperty("states", out var states))
        {
            if (state != null && states.TryGetProperty(state, out var st))
                return st.GetProperty("file").GetString()!;
            if (!asset.TryGetProperty("file", out _))
                return states.EnumerateObject().First().Value.GetProperty("file").GetString()!;
        }
        return asset.GetProperty("file").GetString()!;
    }

    private static readonly Dictionary<string, Bitmap> Icons = new();

    /// <summary>Une icône 16 × 16 du pack (blanche), teintée par multiplication avec une couleur du thème.</summary>
    public static Bitmap Icon(string id, string hex)
    {
        string key = id + hex;
        if (Icons.TryGetValue(key, out var bitmap))
            return bitmap;
        var img = Png(Asset(id).GetProperty("file").GetString()!).Clone();
        var c = Hex(hex);
        for (int i = 0; i < img.P.Length; i += 4)
            for (int k = 0; k < 3; k++)
                img.P[i + k] = (byte)(img.P[i + k] * c[k] / 255);
        return Icons[key] = img.ToBitmap();
    }

    private static readonly Dictionary<string, Bitmap> Thumbnails = new();

    /// <summary>Vignette d'un asset (son calque de base dans l'orientation par défaut), pour les catalogues.</summary>
    public static Bitmap Thumbnail(string id)
    {
        if (Thumbnails.TryGetValue(id, out var bitmap))
            return bitmap;
        var asset = Asset(id);
        string file = asset.TryGetProperty("file", out var f) && f.ValueKind == JsonValueKind.String
            ? f.GetString()!
            : Orient(asset, "se").GetProperty("layers").GetProperty("base").GetProperty("file").GetString()!;
        return Thumbnails[id] = Png(file).ToBitmap();
    }
}
