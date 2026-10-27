// StartupRushReference : portage C# du compositeur de reference (tools/srgen/scene.py, rooms.py, compose.py).
// Aucune dependance externe : PNG lu/ecrit avec ZLibStream, manifeste lu avec System.Text.Json.
// Usage : dotnet run -- <racine_du_pack> <room_id> <sortie.png> [png_python_a_comparer]
// Ce programme n'est pas le jeu : il montre et verifie, en C#, l'algorithme d'assemblage decrit dans
// INTEGRATION_GUIDE.md (projection, tri topologique, couches, ombres, sockets, recoloration, motifs).
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace StartupRushReference;

public sealed class Img
{
    public readonly int W, H;
    public readonly byte[] P;   // RGBA 8 bits, non premultiplie
    public Img(int w, int h) { W = w; H = h; P = new byte[w * h * 4]; }
    public Img Clone() { var c = new Img(W, H); Buffer.BlockCopy(P, 0, c.P, 0, P.Length); return c; }
}

public static class Png
{
    static readonly byte[] Sig = { 137, 80, 78, 71, 13, 10, 26, 10 };
    static readonly uint[] CrcTable = MakeCrc();

    static uint[] MakeCrc()
    {
        var t = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            t[n] = c;
        }
        return t;
    }

    static int BE32(byte[] d, int p) => (d[p] << 24) | (d[p + 1] << 16) | (d[p + 2] << 8) | d[p + 3];

    public static Img Load(string path)
    {
        var data = File.ReadAllBytes(path);
        int pos = 8, w = 0, h = 0, depth = 0, ctype = 0, inter = 0;
        var idat = new MemoryStream();
        while (pos < data.Length)
        {
            int len = BE32(data, pos);
            string type = System.Text.Encoding.ASCII.GetString(data, pos + 4, 4);
            int d = pos + 8;
            if (type == "IHDR") { w = BE32(data, d); h = BE32(data, d + 4); depth = data[d + 8]; ctype = data[d + 9]; inter = data[d + 12]; }
            else if (type == "IDAT") idat.Write(data, d, len);
            else if (type == "IEND") break;
            pos = d + len + 4;
        }
        if (depth != 8 || ctype != 6 || inter != 0) throw new NotSupportedException($"PNG attendu RGBA 8 bits : {path}");
        idat.Position = 0;
        var raw = new MemoryStream();
        using (var z = new ZLibStream(idat, CompressionMode.Decompress)) z.CopyTo(raw);
        byte[] r = raw.ToArray();
        var img = new Img(w, h);
        int stride = w * 4, ip = 0;
        var prev = new byte[stride];
        var cur = new byte[stride];
        for (int y = 0; y < h; y++)
        {
            int f = r[ip++];
            Buffer.BlockCopy(r, ip, cur, 0, stride); ip += stride;
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
                        v += (pa <= pb && pa <= pc) ? a : (pb <= pc ? b : c); break;
                }
                cur[x] = (byte)v;
            }
            Buffer.BlockCopy(cur, 0, img.P, y * stride, stride);
            (prev, cur) = (cur, prev);
        }
        return img;
    }

    public static void Save(Img img, string path)
    {
        var raw = new MemoryStream();
        for (int y = 0; y < img.H; y++) { raw.WriteByte(0); raw.Write(img.P, y * img.W * 4, img.W * 4); }
        var comp = new MemoryStream();
        using (var z = new ZLibStream(comp, CompressionLevel.Optimal, true)) { raw.Position = 0; raw.CopyTo(z); }
        using var fs = File.Create(path);
        fs.Write(Sig);
        var ihdr = new byte[13];
        W32(ihdr, 0, img.W); W32(ihdr, 4, img.H); ihdr[8] = 8; ihdr[9] = 6;
        Chunk(fs, "IHDR", ihdr); Chunk(fs, "sRGB", new byte[] { 0 }); Chunk(fs, "IDAT", comp.ToArray()); Chunk(fs, "IEND", Array.Empty<byte>());
    }

    static void W32(byte[] d, int p, int v) { d[p] = (byte)(v >> 24); d[p + 1] = (byte)(v >> 16); d[p + 2] = (byte)(v >> 8); d[p + 3] = (byte)v; }

    static void Chunk(Stream s, string type, byte[] data)
    {
        var len = new byte[4]; W32(len, 0, data.Length); s.Write(len);
        var t = System.Text.Encoding.ASCII.GetBytes(type); s.Write(t); s.Write(data);
        uint c = 0xFFFFFFFFu;
        foreach (var b in t) c = CrcTable[(c ^ b) & 0xFF] ^ (c >> 8);
        foreach (var b in data) c = CrcTable[(c ^ b) & 0xFF] ^ (c >> 8);
        c ^= 0xFFFFFFFFu;
        var cb = new byte[4]; W32(cb, 0, (int)c); s.Write(cb);
    }
}

/// <summary>Composition alpha « over » (memes operations float32 que la reference Python).</summary>
public static class Blend
{
    public static void Over(Img dst, Img src, int x, int y, float opacity = 1f)
    {
        int x0 = Math.Max(0, x), y0 = Math.Max(0, y), x1 = Math.Min(dst.W, x + src.W), y1 = Math.Min(dst.H, y + src.H);
        for (int yy = y0; yy < y1; yy++)
            for (int xx = x0; xx < x1; xx++)
            {
                int si = ((yy - y) * src.W + (xx - x)) * 4, di = (yy * dst.W + xx) * 4;
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
                if (a > buf[yy * bw + xx]) buf[yy * bw + xx] = a;
            }
    }
}

/// <summary>Manifeste + cache des PNG + recoloration (COLOR_SYSTEM.md §4 et §5).</summary>
public sealed class Library
{
    readonly string root;
    public readonly Dictionary<string, JsonElement> Assets = new();
    public readonly Dictionary<string, int> Channels = new();
    public readonly Dictionary<int, string> ChannelNames = new();
    public readonly Dictionary<string, byte[][]> Ramps = new();
    readonly Dictionary<string, Img> cache = new();
    public readonly (byte r, byte g, byte b) ShadowColor;

    public Library(string root)
    {
        this.root = root;
        var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "manifest.json")));
        foreach (var a in doc.RootElement.GetProperty("assets").EnumerateArray()) Assets[a.GetProperty("id").GetString()!] = a;
        foreach (var c in doc.RootElement.GetProperty("channels").GetProperty("ids").EnumerateObject())
        { Channels[c.Name] = c.Value.GetInt32(); ChannelNames[c.Value.GetInt32()] = c.Name; }
        foreach (var r in doc.RootElement.GetProperty("palette").GetProperty("ramps").EnumerateObject())
            Ramps[r.Name] = r.Value.EnumerateArray().Select(h => Hex(h.GetString()!)).ToArray();
        var sc = Hex(doc.RootElement.GetProperty("palette").GetProperty("fixed").GetProperty("shadow").GetString()!);
        ShadowColor = (sc[0], sc[1], sc[2]);
    }

    static byte[] Hex(string s) => new[] { Convert.ToByte(s.Substring(1, 2), 16), Convert.ToByte(s.Substring(3, 2), 16), Convert.ToByte(s.Substring(5, 2), 16) };

    public Img Png(string rel)
    {
        if (!cache.TryGetValue(rel, out var img)) cache[rel] = img = StartupRushReference.Png.Load(Path.Combine(root, rel));
        return img;
    }

    public JsonElement Get(string id) => Assets[id];

    public Img Recolor(Img rgba, Img mask, IReadOnlyDictionary<string, string> sw)
    {
        var o = rgba.Clone();
        foreach (var kv in sw)
        {
            if (!Channels.TryGetValue(kv.Key, out int c) || !Ramps.TryGetValue(kv.Value, out var pal)) continue;
            for (int i = 0; i < o.P.Length; i += 4)
                if (mask.P[i + 3] > 0 && mask.P[i] == c)
                {
                    var col = pal[Math.Min(5, (int)mask.P[i + 1])];
                    o.P[i] = col[0]; o.P[i + 1] = col[1]; o.P[i + 2] = col[2];
                }
        }
        return o;
    }

    /// <summary>Motif mural : calque (canal, delta+8) applique aux pixels marques B&1, puis recoloration.</summary>
    public Img ApplyWallPattern(Img rgba, Img mask, Img ov, IReadOnlyDictionary<string, string> sw, List<(int dx, int dy)> offsets)
    {
        var o = rgba.Clone();
        int n = rgba.W * rgba.H;
        var ch = new int[n]; var idx = new int[n]; var done = new bool[n];
        for (int i = 0; i < n; i++) { ch[i] = mask.P[i * 4]; idx[i] = mask.P[i * 4 + 1]; }
        foreach (var (dx, dy) in offsets)
            for (int y = 0; y < rgba.H; y++)
                for (int x = 0; x < rgba.W; x++)
                {
                    int i = y * rgba.W + x;
                    if (done[i] || mask.P[i * 4 + 3] == 0 || (mask.P[i * 4 + 2] & 1) == 0) continue;
                    int oy = y - dy, ox = x - dx;
                    if (oy < 0 || oy >= ov.H || ox < 0 || ox >= ov.W) continue;
                    int oi = (oy * ov.W + ox) * 4;
                    if (ov.P[oi + 3] == 0) continue;
                    ch[i] = ov.P[oi];
                    idx[i] = Math.Clamp(idx[i] + ov.P[oi + 1] - 8, 0, 5);
                    done[i] = true;
                }
        for (int i = 0; i < n; i++)
        {
            if (mask.P[i * 4 + 3] == 0) continue;
            string cname = ChannelNames[ch[i]];
            string? ramp = null;
            if (sw.TryGetValue(cname, out var r0)) ramp = r0;
            else if (cname == "wall2" && sw.TryGetValue("wall", out var r1)) ramp = r1;
            if (ramp == null || !Ramps.TryGetValue(ramp, out var pal)) continue;
            var col = pal[idx[i]];
            o.P[i * 4] = col[0]; o.P[i * 4 + 1] = col[1]; o.P[i * 4 + 2] = col[2];
        }
        return o;
    }
}

public sealed class Node
{
    public string Aid = "", Facing = "";
    public int Sx, Sy;
    public double Depth, SeatDepth;
    public Dictionary<string, string> Swatches = new();
    public List<Node> Before = new(), After = new(), Chairs = new();
    public JsonElement? Character;
    public bool IsChair;
}

/// <summary>Assemblage d'une piece (ordre de rendu : INTEGRATION_GUIDE.md §4).</summary>
public sealed class Scene
{
    readonly Library lib;
    readonly int ox, oy;
    readonly List<(Img img, int x, int y)> floor = new(), edges = new(), walls = new(), wallItems = new();
    readonly List<((double u0, double u1, double v0, double v1) fp, Node node)> groups = new();

    public Scene(Library lib, int ox, int oy) { this.lib = lib; this.ox = ox; this.oy = oy; }

    (double x, double y) Tile(double u, double v) => (ox + (u - v) * 32, oy + (u + v) * 16);

    static JsonElement Orient(JsonElement e, string f)
    {
        var os = e.GetProperty("orientations");
        if (os.TryGetProperty(f, out var o)) return o;
        return os.EnumerateObject().First().Value;
    }

    static Dictionary<string, string> Sw(JsonElement? el)
    {
        var d = new Dictionary<string, string>();
        if (el is JsonElement e && e.ValueKind == JsonValueKind.Object)
            foreach (var p in e.EnumerateObject()) d[p.Name] = p.Value.GetString()!;
        return d;
    }

    Img LayerImg(JsonElement L, Dictionary<string, string> sw)
    {
        var img = lib.Png(L.GetProperty("file").GetString()!);
        if (sw.Count > 0 && L.TryGetProperty("mask", out var m)) img = lib.Recolor(img, lib.Png(m.GetString()!), sw);
        return img;
    }

    static (int, int) Anchor(JsonElement o) { var a = o.GetProperty("anchor"); return (a[0].GetInt32(), a[1].GetInt32()); }

    public void FloorRect(string fid, int U0, int V0, int W, int D, Dictionary<string, string> sw)
    {
        var e = lib.Get(fid);
        var per = e.GetProperty("period_tiles");
        int pu = per[0].GetInt32(), pv = per[1].GetInt32();
        for (int U = U0; U < U0 + W; U++)
            for (int V = V0; V < V0 + D; V++)
            {
                var vr = e.GetProperty("variants").GetProperty($"u{U % pu}v{V % pv}");
                var L = vr.GetProperty("layers").GetProperty("base");
                var img = lib.Png(L.GetProperty("file").GetString()!);
                if (sw.Count > 0) img = lib.Recolor(img, lib.Png(L.GetProperty("mask").GetString()!), sw);
                var (x, y) = Tile(U, V); var (ax, ay) = Anchor(vr);
                floor.Add((img, (int)x - ax, (int)y - ay));
            }
    }

    public void FloorOverlay(string aid, string key, int U, int V, Dictionary<string, string> sw)
    {
        var o = lib.Get(aid).GetProperty("orientations").GetProperty(key);
        var L = o.GetProperty("layers").GetProperty("base");
        var img = lib.Png(L.GetProperty("file").GetString()!);
        if (sw.Count > 0 && L.TryGetProperty("mask", out var m)) img = lib.Recolor(img, lib.Png(m.GetString()!), sw);
        var (x, y) = Tile(U, V); var (ax, ay) = Anchor(o);
        edges.Add((img, (int)x - ax, (int)y - ay));
    }

    public void SlabEdges(int W, int D)
    {
        var none = new Dictionary<string, string>();
        for (int V = 0; V < D - 1; V++) FloorOverlay("floor_edge", "se", W, V, none);
        for (int U = 0; U < W - 1; U++) FloorOverlay("floor_edge", "sw", U, D, none);
        FloorOverlay("floor_edge_corner", "all", W, D, none);
    }

    public void Wall(string kind, string orient, int U, int V, string? pattern, Dictionary<string, string> sw)
    {
        var e = lib.Get(kind.StartsWith("wall_") ? kind : "wall_" + kind);
        var o = e.GetProperty("orientations").GetProperty(orient);
        var L = o.GetProperty("layers").GetProperty("base");
        var rgba = lib.Png(L.GetProperty("file").GetString()!);
        if (L.TryGetProperty("mask", out var mm))
        {
            var mask = lib.Png(mm.GetString()!);
            if (pattern != null && (orient == "se" || orient == "sw"))
            {
                var po = lib.Get("pattern_" + pattern).GetProperty("orientations").GetProperty(orient);
                var ov = lib.Png(po.GetProperty("layers").GetProperty("overlay").GetProperty("file").GetString()!);
                var (a0x, a0y) = Anchor(o); var (pax, pay) = Anchor(po);
                int dx = a0x - pax, dy = a0y - pay;
                int stx = orient == "se" ? -32 : 32, sty = 16;
                int span = o.TryGetProperty("span_tiles", out var sp) ? sp.GetInt32() : 1;
                var offs = Enumerable.Range(0, span).Select(k => (dx + k * stx, dy + k * sty)).ToList();
                rgba = lib.ApplyWallPattern(rgba, mask, ov, sw, offs);
            }
            else if (sw.Count > 0) rgba = lib.Recolor(rgba, mask, sw);
        }
        var (x, y) = Tile(U, V); var (ax, ay) = Anchor(o);
        walls.Add((rgba, (int)x - ax, (int)y - ay));
    }

    public void WallItem(string aid, string wall, int index, Dictionary<string, string> sw)
    {
        var o = Orient(lib.Get(aid), wall == "left" ? "se" : "sw");
        var img = LayerImg(o.GetProperty("layers").GetProperty("base"), sw);
        var (x, y) = wall == "left" ? Tile(0, index) : Tile(index, 0);
        var (ax, ay) = Anchor(o);
        wallItems.Add((img, (int)x - ax, (int)y - ay));
    }

    public void Decor(string aid, int U, int V, string facing, Dictionary<string, string> sw)
    {
        var e = lib.Get(aid);
        var o = e.GetProperty("orientations").TryGetProperty(facing, out var oo) ? oo : e.GetProperty("orientations").GetProperty("se");
        var (x, y) = Tile(U, V); var (ax, ay) = Anchor(o);
        edges.Add((LayerImg(o.GetProperty("layers").GetProperty("base"), sw), (int)x - ax, (int)y - ay));
    }

    public void Place(string aid, int U, int V, string facing, JsonElement? children, Dictionary<string, string> sw, JsonElement? character)
    {
        var e = lib.Get(aid);
        var os = e.GetProperty("orientations");
        string f = os.TryGetProperty(facing, out _) ? facing : os.EnumerateObject().First().Name;
        var o = os.GetProperty(f);
        int x, y;
        bool centered = e.GetProperty("footprint_tiles").ValueKind == JsonValueKind.Null && e.TryGetProperty("size_su", out var ss) && ss.ValueKind != JsonValueKind.Null;
        if (centered) { var t = Tile(U + 0.5, V + 0.5); x = (int)Math.Round(t.x, MidpointRounding.ToEven); y = (int)Math.Round(t.y, MidpointRounding.ToEven); }
        else { var t = Tile(U, V); x = (int)t.x; y = (int)t.y; }
        var node = new Node { Aid = aid, Facing = f, Sx = x, Sy = y, Swatches = sw };
        node.IsChair = character is JsonElement ce0 && ce0.ValueKind == JsonValueKind.Object && o.TryGetProperty("occupant", out _);
        node.Character = character;
        double su = 1, sv = 1;
        if (o.TryGetProperty("footprint_world_tiles", out var fw)) { su = fw[0].GetDouble(); sv = fw[1].GetDouble(); }
        if (children is JsonElement ch && ch.ValueKind == JsonValueKind.Object)
            foreach (var p in ch.EnumerateObject())
            {
                if (p.Name == "slots")
                {
                    if (!o.TryGetProperty("slots", out var sl)) continue;
                    foreach (var it in p.Value.EnumerateArray().OrderBy(q => q.GetProperty("index").GetInt32()))
                    {
                        int i = it.GetProperty("index").GetInt32();
                        var pos = sl.GetProperty("positions")[i].GetProperty("offset_px");
                        node.After.Add(new Node { Aid = it.GetProperty("asset").GetString()!, Facing = f, Sx = x + pos[0].GetInt32(), Sy = y + pos[1].GetInt32(), Depth = 10000 + i });
                    }
                    continue;
                }
                Attach(node, o, p.Name, p.Value);
            }
        groups.Add(((U, U + su, V, V + sv), node));
    }

    void Attach(Node node, JsonElement o, string sock, JsonElement spec)
    {
        string asset = spec.ValueKind == JsonValueKind.String ? spec.GetString()! : spec.GetProperty("asset").GetString()!;
        var s = o.GetProperty("sockets").GetProperty(sock);
        string cf = s.GetProperty("child_facing").GetString()!;
        var off = s.GetProperty("offset_px"); var ws = s.GetProperty("world_su");
        var child = new Node { Aid = asset, Facing = cf, Sx = node.Sx + off[0].GetInt32(), Sy = node.Sy + off[1].GetInt32(),
                               Depth = ws[0].GetDouble() + ws[1].GetDouble() };
        if (spec.ValueKind == JsonValueKind.Object && spec.TryGetProperty("swatches", out var sw)) child.Swatches = Sw(sw);
        var co = Orient(lib.Get(asset), cf);
        if (spec.ValueKind == JsonValueKind.Object && spec.TryGetProperty("children", out var sub) && sub.ValueKind == JsonValueKind.Object)
            foreach (var p in sub.EnumerateObject()) Attach(child, co, p.Name, p.Value);
        string draw = s.TryGetProperty("draw", out var dr) ? dr.GetString()! : "after_parent";
        if (draw == "before_parent") node.Before.Add(child);
        else if (draw == "workstation")
        {
            if (spec.ValueKind == JsonValueKind.Object && spec.TryGetProperty("character", out var chr) && chr.ValueKind == JsonValueKind.Object) child.Character = chr;
            child.SeatDepth = ws[0].GetDouble() + ws[1].GetDouble();
            node.Chairs.Add(child);
        }
        else node.After.Add(child);
    }

    // --- personnages : hair_back -> body -> head -> hair_front, ancre d'assise de la base ---
    List<(Img img, int sx, int sy)> CharLayers(JsonElement spec, string orient)
    {
        var be = lib.Get(spec.GetProperty("base").GetString()!);
        var bo = be.GetProperty("orientations").GetProperty(orient);
        var sw = new Dictionary<string, string> { ["skin"] = spec.TryGetProperty("skin", out var sk) ? sk.GetString()! : be.GetProperty("channels").GetProperty("skin").GetString()! };
        if (spec.TryGetProperty("swatches", out var sws)) foreach (var kv in Sw(sws)) sw[kv.Key] = kv.Value;
        var hairSw = new Dictionary<string, string> { ["hair"] = spec.TryGetProperty("hair_ramp", out var hr) ? hr.GetString()! : "hair_brown" };
        var layers = new Dictionary<string, Img>();
        if (spec.TryGetProperty("hair", out var hid) && hid.ValueKind == JsonValueKind.String)
            foreach (var L in lib.Get(hid.GetString()!).GetProperty("orientations").GetProperty(orient).GetProperty("layers").EnumerateObject())
                layers[L.Name] = lib.Recolor(lib.Png(L.Value.GetProperty("file").GetString()!), lib.Png(L.Value.GetProperty("mask").GetString()!), hairSw);
        var all = new Dictionary<string, string>(sw); foreach (var kv in hairSw) all[kv.Key] = kv.Value;
        foreach (var ln in new[] { "body", "head" })
        {
            var L = bo.GetProperty("layers").GetProperty(ln);
            layers[ln] = lib.Recolor(lib.Png(L.GetProperty("file").GetString()!), lib.Png(L.GetProperty("mask").GetString()!), all);
        }
        var seat = be.GetProperty("seat_anchor");
        var outl = new List<(Img, int, int)>();
        foreach (var ln in new[] { "hair_back", "body", "head", "hair_front" })
            if (layers.TryGetValue(ln, out var im)) outl.Add((im, seat[0].GetInt32(), seat[1].GetInt32()));
        return outl;
    }

    void DrawNode(Img img, byte[]? shadow, int bw, int bh, Node node)
    {
        var o = Orient(lib.Get(node.Aid), node.Facing);
        var (ax, ay) = Anchor(o);
        var L = o.GetProperty("layers");
        foreach (var c in node.Before) DrawNode(img, shadow, bw, bh, c);
        if (shadow != null && L.TryGetProperty("shadow", out var sh)) Blend.StampMax(shadow, bw, bh, lib.Png(sh.GetProperty("file").GetString()!), node.Sx - ax, node.Sy - ay);
        Blend.Over(img, LayerImg(L.GetProperty("base"), node.Swatches), node.Sx - ax, node.Sy - ay);
        if (L.TryGetProperty("glass", out var gl)) Blend.Over(img, LayerImg(gl, node.Swatches), node.Sx - ax, node.Sy - ay);
        if (L.TryGetProperty("front", out var fr)) Blend.Over(img, LayerImg(fr, node.Swatches), node.Sx - ax, node.Sy - ay);
        foreach (var c in node.After.OrderBy(c => c.Depth)) DrawNode(img, null, bw, bh, c);
    }

    void DrawChair(Img img, byte[] shadow, int bw, int bh, Node chair)
    {
        var o = Orient(lib.Get(chair.Aid), chair.Facing);
        var (ax, ay) = Anchor(o);
        var L = o.GetProperty("layers");
        if (L.TryGetProperty("shadow", out var sh)) Blend.StampMax(shadow, bw, bh, lib.Png(sh.GetProperty("file").GetString()!), chair.Sx - ax, chair.Sy - ay);
        Blend.Over(img, LayerImg(L.GetProperty("base"), chair.Swatches), chair.Sx - ax, chair.Sy - ay);
        if (L.TryGetProperty("glass", out var gl)) Blend.Over(img, LayerImg(gl, chair.Swatches), chair.Sx - ax, chair.Sy - ay);
        if (chair.Character is JsonElement ch && o.TryGetProperty("occupant", out var occ))
        {
            var so = occ.GetProperty("seat_offset_px");
            int cx = chair.Sx + so[0].GetInt32(), cy = chair.Sy + so[1].GetInt32();
            foreach (var (li, sx, sy) in CharLayers(ch, occ.GetProperty("character_orientation").GetString()!)) Blend.Over(img, li, cx - sx, cy - sy);
        }
        if (L.TryGetProperty("front", out var fr)) Blend.Over(img, LayerImg(fr, chair.Swatches), chair.Sx - ax, chair.Sy - ay);
    }

    void DrawWorkstation(Img img, byte[] shadow, int bw, int bh, Node desk)
    {
        if (desk.Chairs.Count == 0) { DrawNode(img, shadow, bw, bh, desk); return; }
        var o = Orient(lib.Get(desk.Aid), desk.Facing);
        double center = 32;
        if (o.TryGetProperty("footprint_world_tiles", out var fw)) center = (fw[0].GetDouble() + fw[1].GetDouble()) * 16.0;
        foreach (var c in desk.Chairs.Where(c => c.SeatDepth < center).OrderBy(c => c.SeatDepth)) DrawChair(img, shadow, bw, bh, c);
        DrawNode(img, shadow, bw, bh, desk);
        foreach (var c in desk.Chairs.Where(c => c.SeatDepth >= center).OrderBy(c => c.SeatDepth)) DrawChair(img, shadow, bw, bh, c);
    }

    /// <summary>Tri topologique des emprises (A derriere B si A.u1 <= B.u0 ou A.v1 <= B.v0).</summary>
    public static List<int> TopoSort(List<(double u0, double u1, double v0, double v1)> fps)
    {
        int n = fps.Count;
        var before = Enumerable.Range(0, n).Select(_ => new HashSet<int>()).ToList();
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                var a = fps[i]; var b = fps[j];
                bool behind = a.u1 <= b.u0 || a.v1 <= b.v0, infront = b.u1 <= a.u0 || b.v1 <= a.v0;
                if (behind && !infront) before[j].Add(i);
            }
        var order = Enumerable.Range(0, n).OrderBy(k => fps[k].u1 + fps[k].v1).ThenBy(k => fps[k].u0 + fps[k].v0).ToList();
        var rank = new int[n]; for (int r = 0; r < n; r++) rank[order[r]] = r;
        var done = new HashSet<int>(); var outl = new List<int>();
        void Visit(int k, HashSet<int> stack)
        {
            if (done.Contains(k) || stack.Contains(k)) return;
            var st2 = new HashSet<int>(stack) { k };
            foreach (var d in before[k].OrderBy(q => rank[q])) Visit(d, st2);
            done.Add(k); outl.Add(k);
        }
        foreach (var k in order) Visit(k, new HashSet<int>());
        return outl;
    }

    public Img Render(int W, int H, (byte r, byte g, byte b) bg, (byte r, byte g, byte b) shadowColor)
    {
        var img = new Img(W, H);
        for (int i = 0; i < W * H; i++) { img.P[i * 4] = bg.r; img.P[i * 4 + 1] = bg.g; img.P[i * 4 + 2] = bg.b; img.P[i * 4 + 3] = 255; }
        foreach (var (s, x, y) in floor) Blend.Over(img, s, x, y);
        foreach (var (s, x, y) in edges) Blend.Over(img, s, x, y);
        var shadow = new byte[W * H];
        var objs = new Img(W, H);
        foreach (int k in TopoSort(groups.Select(g => g.fp).ToList()))
        {
            var node = groups[k].node;
            if (node.IsChair) DrawChair(objs, shadow, W, H, node); else DrawWorkstation(objs, shadow, W, H, node);
        }
        var sl = new Img(W, H);
        for (int i = 0; i < W * H; i++)
        {
            sl.P[i * 4] = shadowColor.r; sl.P[i * 4 + 1] = shadowColor.g; sl.P[i * 4 + 2] = shadowColor.b;
            sl.P[i * 4 + 3] = (byte)MathF.Round(shadow[i] / 255f * 0.30f * 255f);
        }
        Blend.Over(img, sl, 0, 0);
        foreach (var (s, x, y) in walls) Blend.Over(img, s, x, y);
        foreach (var (s, x, y) in wallItems) Blend.Over(img, s, x, y);
        Blend.Over(img, objs, 0, 0);
        return img;
    }
}

public static class Program
{
    static Dictionary<string, string> Sw(JsonElement p, string key)
    {
        var d = new Dictionary<string, string>();
        if (p.TryGetProperty(key, out var e) && e.ValueKind == JsonValueKind.Object)
            foreach (var q in e.EnumerateObject()) d[q.Name] = q.Value.GetString()!;
        return d;
    }

    public static int Main(string[] args)
    {
        if (args.Length < 3) { Console.WriteLine("usage : <racine> <room_id> <sortie.png> [reference.png]"); return 2; }
        var lib = new Library(args[0]);
        var plan = JsonDocument.Parse(File.ReadAllText(Path.Combine(args[0], "rooms", args[1] + ".json"))).RootElement;
        int W = plan.GetProperty("size_tiles")[0].GetInt32(), D = plan.GetProperty("size_tiles")[1].GetInt32();
        const int margin = 40;
        int cw = (W + D) * 32 + 2 * margin, chh = (W + D) * 16 + 112 + 2 * margin + 20;
        var sc = new Scene(lib, margin + D * 32, margin + 112 + 10);
        foreach (var f in plan.GetProperty("floor").EnumerateArray())
        {
            var r = f.GetProperty("rect");
            sc.FloorRect(f.GetProperty("material").GetString()!, r[0].GetInt32(), r[1].GetInt32(), r[2].GetInt32(), r[3].GetInt32(), Sw(f, "swatches"));
        }
        if (plan.TryGetProperty("floor_overlays", out var fo))
            foreach (var f in fo.EnumerateArray())
                sc.FloorOverlay(f.GetProperty("asset").GetString()!, f.GetProperty("key").GetString()!, f.GetProperty("tile")[0].GetInt32(), f.GetProperty("tile")[1].GetInt32(), Sw(f, "swatches"));
        sc.SlabEdges(W, D);
        var walls = plan.GetProperty("walls");
        var Lw = walls.GetProperty("left"); var Rw = walls.GetProperty("right");
        string? Pat(JsonElement w) => w.TryGetProperty("pattern", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
        sc.Wall("wall_corner_inner", "all", 0, 0, null, Sw(Lw, "swatches"));
        int vi = 0; foreach (var k in Lw.GetProperty("segments").EnumerateArray()) { if (k.ValueKind == JsonValueKind.String) sc.Wall(k.GetString()!, "se", 0, vi, Pat(Lw), Sw(Lw, "swatches")); vi++; }
        int ui = 0; foreach (var k in Rw.GetProperty("segments").EnumerateArray()) { if (k.ValueKind == JsonValueKind.String) sc.Wall(k.GetString()!, "sw", ui, 0, Pat(Rw), Sw(Rw, "swatches")); ui++; }
        sc.Wall("wall_end", "se", 0, D, null, Sw(Lw, "swatches"));
        sc.Wall("wall_end", "sw", W, 0, null, Sw(Rw, "swatches"));
        if (plan.TryGetProperty("wall_items", out var wis))
            foreach (var wi in wis.EnumerateArray())
                if (lib.Assets.ContainsKey(wi.GetProperty("asset").GetString()!))
                    sc.WallItem(wi.GetProperty("asset").GetString()!, wi.GetProperty("wall").GetString()!, wi.GetProperty("index").GetInt32(), Sw(wi, "swatches"));
        var decor = new List<JsonElement>();
        foreach (var ob in plan.GetProperty("objects").EnumerateArray())
        {
            string aid = ob.GetProperty("asset").GetString()!;
            var e = lib.Get(aid);
            if (e.TryGetProperty("render_layer", out var rl) && rl.GetString() == "floor_decor") { decor.Add(ob); continue; }
            string facing = ob.TryGetProperty("facing", out var fc) ? fc.GetString()! : "se";
            JsonElement? kids = ob.TryGetProperty("children", out var kd) ? kd : null;
            JsonElement? chr = ob.TryGetProperty("character", out var cr) ? cr : null;
            sc.Place(aid, ob.GetProperty("tile")[0].GetInt32(), ob.GetProperty("tile")[1].GetInt32(), facing, kids, Sw(ob, "swatches"), chr);
        }
        foreach (var ob in decor)
            sc.Decor(ob.GetProperty("asset").GetString()!, ob.GetProperty("tile")[0].GetInt32(), ob.GetProperty("tile")[1].GetInt32(),
                     ob.TryGetProperty("facing", out var fc) ? fc.GetString()! : "se", Sw(ob, "swatches"));
        var img = sc.Render(cw, chh, (46, 40, 50), lib.ShadowColor);
        Png.Save(img, args[2]);
        Console.WriteLine($"{args[1]} : {cw}x{chh} -> {args[2]}");
        if (args.Length > 3)
        {
            var refi = Png.Load(args[3]);
            if (refi.W != img.W || refi.H != img.H) { Console.WriteLine($"TAILLE DIFFERENTE {refi.W}x{refi.H}"); return 1; }
            int diff = 0, maxd = 0;
            for (int i = 0; i < img.P.Length; i += 4)
            {
                int d = 0;
                for (int k = 0; k < 4; k++) d = Math.Max(d, Math.Abs(img.P[i + k] - refi.P[i + k]));
                if (d > 0) diff++;
                maxd = Math.Max(maxd, d);
            }
            Console.WriteLine($"comparaison avec la reference Python : {diff} pixels differents sur {img.W * img.H}, ecart max {maxd}");
            return diff == 0 ? 0 : 1;
        }
        return 0;
    }
}
