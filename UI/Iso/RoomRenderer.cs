using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Core.space;
using Core.team;

namespace UI.Iso;

/// <summary>Une personne assise : son apparence en couches (base, coiffure, teinte des cheveux et de la peau).</summary>
public record Seated(int PersonId, Appearance Look);

/// <summary>Une zone cliquable de la scène : un objet au sol (avec ce qui y est accroché) ou une personne assise.</summary>
public record HitRegion(bool Person, int Id, (double X, double Y)[] Polygon, int Order);

/// <summary>Le rendu d'une pièce : l'image, son origine (écran de la case 0, 0) et les zones cliquables.</summary>
public record RoomImage(Img Image, int OriginX, int OriginY, List<HitRegion> Hits, Dictionary<int, (int X, int Y)> Heads);

/// <summary>
/// Assemble une pièce comme le compositeur de référence du pack (INTEGRATION_GUIDE.md §4) : sols, bords de dalle, ombres
/// communes (maximum des alphas, puis 30 %), murs, objets muraux, puis objets au sol par tri topologique des emprises ;
/// chaque objet : enfants « avant », base, verre, devant, enfants « après » ; postes : chaises derrière, bureau, chaises devant,
/// chaque chaise avec son personnage (cheveux arrière → corps → tête → cheveux avant). Sols, murs et décor mural viennent
/// du plan ; les objets viennent du local réel de la campagne.
/// </summary>
public sealed class RoomRenderer
{
    private const int Margin = 40;
    private const int WallHeight = 112;

    private sealed class Node
    {
        public string Aid = "", Facing = "";
        public int Sx, Sy, ItemId;
        public double Depth, SeatDepth;
        public Dictionary<string, string> Swatches = new();
        public List<Node> Before = new(), After = new(), Chairs = new();
        public Seated? Character;
    }

    private readonly List<(Img img, int x, int y)> _floor = new(), _edges = new(), _walls = new(), _wallItems = new(), _ceiling = new();
    private readonly List<((double u0, double u1, double v0, double v1) fp, Node node)> _groups = new();
    private readonly int _ox, _oy, _w, _h;
    private readonly List<HitRegion> _hits = new();
    private readonly Dictionary<int, (int, int)> _heads = new();

    private RoomRenderer(int width, int depth)
    {
        _w = (width + depth) * 32 + 2 * Margin;
        _h = (width + depth) * 16 + WallHeight + 2 * Margin + 20;
        _ox = Margin + depth * 32;
        _oy = Margin + WallHeight + 10;
    }

    private (double X, double Y) Tile(double u, double v) => (_ox + (u - v) * 32, _oy + (u + v) * 16);

    private static Dictionary<string, string> Sw(JsonElement e, string key)
    {
        var d = new Dictionary<string, string>();
        if (e.TryGetProperty(key, out var s) && s.ValueKind == JsonValueKind.Object)
            foreach (var p in s.EnumerateObject())
                d[p.Name] = p.Value.GetString()!;
        return d;
    }

    /// <summary>Compose le local : « seated » donne, pour chaque chaise occupée en ce moment, qui y est assis.</summary>
    public static RoomImage Render(Premises premises, IReadOnlyDictionary<int, Seated> seated)
    {
        var r = new RoomRenderer(premises.Width, premises.Height);
        var plan = PackLibrary.Room(premises.PlanId);
        r.Structure(plan, premises.Width, premises.Height);
        foreach (var item in premises.Floor)
        {
            var asset = PackLibrary.Asset(item.DefId);
            if (asset.TryGetProperty("render_layer", out var rl) && rl.GetString() == "floor_decor")
                r.Decor(item);
            else if (item.Def.Pack.Ceiling)
                r.Hanging(item, r._ceiling, true);
            else
                r.Place(premises, item, seated);
        }
        foreach (var item in premises.WallItems)
            r.Hanging(item, r._wallItems, false);
        return new RoomImage(r.Compose(), r._ox, r._oy, r._hits, r._heads);
    }

    /// <summary>Sols, bords de dalle, murs avec motifs, objets muraux : la structure du plan.</summary>
    private void Structure(JsonElement plan, int width, int depth)
    {
        foreach (var f in plan.GetProperty("floor").EnumerateArray())
        {
            var rect = f.GetProperty("rect");
            FloorRect(f.GetProperty("material").GetString()!, rect[0].GetInt32(), rect[1].GetInt32(), rect[2].GetInt32(), rect[3].GetInt32(), Sw(f, "swatches"));
        }
        if (plan.TryGetProperty("floor_overlays", out var overlays))
            foreach (var f in overlays.EnumerateArray())
                FloorOverlay(f.GetProperty("asset").GetString()!, f.GetProperty("key").GetString()!, f.GetProperty("tile")[0].GetInt32(), f.GetProperty("tile")[1].GetInt32(), Sw(f, "swatches"));
        var none = new Dictionary<string, string>();
        for (int v = 0; v < depth - 1; v++)
            FloorOverlay("floor_edge", "se", width, v, none);
        for (int u = 0; u < width - 1; u++)
            FloorOverlay("floor_edge", "sw", u, depth, none);
        FloorOverlay("floor_edge_corner", "all", width, depth, none);

        var walls = plan.GetProperty("walls");
        var left = walls.GetProperty("left");
        var right = walls.GetProperty("right");
        string? Pattern(JsonElement w) => w.TryGetProperty("pattern", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
        Wall("wall_corner_inner", "all", 0, 0, null, Sw(left, "swatches"));
        int vi = 0;
        foreach (var k in left.GetProperty("segments").EnumerateArray())
        {
            if (k.ValueKind == JsonValueKind.String)
                Wall(k.GetString()!, "se", 0, vi, Pattern(left), Sw(left, "swatches"));
            vi++;
        }
        int ui = 0;
        foreach (var k in right.GetProperty("segments").EnumerateArray())
        {
            if (k.ValueKind == JsonValueKind.String)
                Wall(k.GetString()!, "sw", ui, 0, Pattern(right), Sw(right, "swatches"));
            ui++;
        }
        Wall("wall_end", "se", 0, depth, null, Sw(left, "swatches"));
        Wall("wall_end", "sw", width, 0, null, Sw(right, "swatches"));
    }

    /// <summary>
    /// Un objet du local hors du tri des emprises : au mur (après les murs, avant les objets au sol) ou au plafond
    /// (par-dessus tout). Les objets muraux viennent du local réel, plus du plan : on peut les poser, déplacer, revendre.
    /// </summary>
    private void Hanging(PlacedItem item, List<(Img img, int x, int y)> layer, bool centered)
    {
        if (!PackLibrary.Has(item.DefId))
            return;
        var o = PackLibrary.Orient(PackLibrary.Asset(item.DefId), item.Facing);
        var (x, y) = centered ? Tile(item.X + 0.5, item.Y + 0.5) : Tile(item.X, item.Y);
        var (ax, ay) = PackLibrary.Anchor(o);
        layer.Add((PackLibrary.Layer(o.GetProperty("layers").GetProperty("base"), item.Swatches), (int)Math.Round(x) - ax, (int)Math.Round(y) - ay));
        AddHit(false, item.Id, o, (int)Math.Round(x), (int)Math.Round(y), centered ? 90_000 : -2);
    }

    private void FloorRect(string fid, int u0, int v0, int w, int d, Dictionary<string, string> sw)
    {
        var e = PackLibrary.Asset(fid);
        var per = e.GetProperty("period_tiles");
        int pu = per[0].GetInt32(), pv = per[1].GetInt32();
        for (int u = u0; u < u0 + w; u++)
            for (int v = v0; v < v0 + d; v++)
            {
                var variant = e.GetProperty("variants").GetProperty($"u{u % pu}v{v % pv}");
                var img = PackLibrary.Layer(variant.GetProperty("layers").GetProperty("base"), sw);
                var (x, y) = Tile(u, v);
                var (ax, ay) = PackLibrary.Anchor(variant);
                _floor.Add((img, (int)x - ax, (int)y - ay));
            }
    }

    private void FloorOverlay(string aid, string key, int u, int v, Dictionary<string, string> sw)
    {
        var o = PackLibrary.Asset(aid).GetProperty("orientations").GetProperty(key);
        var img = PackLibrary.Layer(o.GetProperty("layers").GetProperty("base"), sw);
        var (x, y) = Tile(u, v);
        var (ax, ay) = PackLibrary.Anchor(o);
        _edges.Add((img, (int)x - ax, (int)y - ay));
    }

    private void Wall(string kind, string orient, int u, int v, string? pattern, Dictionary<string, string> sw)
    {
        var e = PackLibrary.Asset(kind.StartsWith("wall_") ? kind : "wall_" + kind);
        var o = e.GetProperty("orientations").GetProperty(orient);
        var layer = o.GetProperty("layers").GetProperty("base");
        Img rgba = PackLibrary.Png(layer.GetProperty("file").GetString()!);
        if (layer.TryGetProperty("mask", out var mm))
        {
            var mask = PackLibrary.Png(mm.GetString()!);
            if (pattern != null && orient is "se" or "sw")
            {
                var po = PackLibrary.Asset("pattern_" + pattern).GetProperty("orientations").GetProperty(orient);
                var ov = PackLibrary.Png(po.GetProperty("layers").GetProperty("overlay").GetProperty("file").GetString()!);
                var (a0x, a0y) = PackLibrary.Anchor(o);
                var (pax, pay) = PackLibrary.Anchor(po);
                int dx = a0x - pax, dy = a0y - pay;
                int stx = orient == "se" ? -32 : 32, sty = 16;
                int span = o.TryGetProperty("span_tiles", out var sp) ? sp.GetInt32() : 1;
                rgba = PackLibrary.WallPattern(rgba, mask, ov, sw, Enumerable.Range(0, span).Select(k => (dx + k * stx, dy + k * sty)).ToList());
            }
            else if (sw.Count > 0)
                rgba = PackLibrary.Recolor(rgba, mask, sw);
        }
        var (x, y) = Tile(u, v);
        var (ax, ay) = PackLibrary.Anchor(o);
        _walls.Add((rgba, (int)x - ax, (int)y - ay));
    }

    private void Decor(PlacedItem item)
    {
        var o = PackLibrary.Orient(PackLibrary.Asset(item.DefId), item.Facing);
        var (x, y) = Tile(item.X, item.Y);
        var (ax, ay) = PackLibrary.Anchor(o);
        _edges.Add((PackLibrary.Layer(o.GetProperty("layers").GetProperty("base"), item.Swatches), (int)x - ax, (int)y - ay));
        AddHit(false, item.Id, o, (int)x, (int)y, -1);
    }

    private void Place(Premises premises, PlacedItem item, IReadOnlyDictionary<int, Seated> seated)
    {
        var e = PackLibrary.Asset(item.DefId);
        var o = PackLibrary.Orient(e, item.Facing);
        string facing = e.GetProperty("orientations").TryGetProperty(item.Facing, out _) ? item.Facing : e.GetProperty("orientations").EnumerateObject().First().Name;
        bool centered = e.GetProperty("footprint_tiles").ValueKind == JsonValueKind.Null && e.TryGetProperty("size_su", out var ss) && ss.ValueKind != JsonValueKind.Null;
        int x, y;
        if (centered)
        {
            var t = Tile(item.X + 0.5, item.Y + 0.5);
            x = (int)Math.Round(t.X, MidpointRounding.ToEven);
            y = (int)Math.Round(t.Y, MidpointRounding.ToEven);
        }
        else
        {
            var t = Tile(item.X, item.Y);
            x = (int)t.X;
            y = (int)t.Y;
        }
        var node = new Node { Aid = item.DefId, Facing = facing, Sx = x, Sy = y, Swatches = item.Swatches, ItemId = item.Id };
        double su = 1, sv = 1;
        if (o.TryGetProperty("footprint_world_tiles", out var fw))
        {
            su = fw[0].GetDouble();
            sv = fw[1].GetDouble();
        }
        foreach (var child in premises.Children(item))
            Attach(premises, node, o, child, seated);
        _groups.Add(((item.X, item.X + su, item.Y, item.Y + sv), node));
        AddHit(false, item.Id, o, x, y, _groups.Count);
    }

    private void Attach(Premises premises, Node node, JsonElement o, PlacedItem child, IReadOnlyDictionary<int, Seated> seated)
    {
        // Module de baie : ancre à la position de son premier emplacement, même orientation que la baie, du bas vers le haut
        if (child.Socket is { } slot && slot.StartsWith("slot:"))
        {
            if (!o.TryGetProperty("slots", out var slots) || !PackLibrary.Has(child.DefId) || !int.TryParse(slot[5..], out int index))
                return;
            var position = slots.GetProperty("positions").EnumerateArray().FirstOrDefault(p => p.GetProperty("index").GetInt32() == index);
            if (position.ValueKind != JsonValueKind.Object || !PackLibrary.Asset(child.DefId).GetProperty("orientations").TryGetProperty(node.Facing, out _))
                return;
            var at = position.GetProperty("offset_px");
            node.After.Add(new Node
            {
                Aid = child.DefId, Facing = node.Facing, Sx = node.Sx + at[0].GetInt32(), Sy = node.Sy + at[1].GetInt32(),
                Depth = -index, Swatches = child.Swatches, ItemId = child.Id,
            });
            return;
        }
        if (!o.TryGetProperty("sockets", out var sockets) || !sockets.TryGetProperty(child.Socket!, out var s) || !PackLibrary.Has(child.DefId))
            return;
        string cf = s.GetProperty("child_facing").GetString()!;
        var off = s.GetProperty("offset_px");
        var ws = s.GetProperty("world_su");
        var c = new Node
        {
            Aid = child.DefId, Facing = cf, Sx = node.Sx + off[0].GetInt32(), Sy = node.Sy + off[1].GetInt32(),
            Depth = ws[0].GetDouble() + ws[1].GetDouble(), Swatches = child.Swatches, ItemId = child.Id,
        };
        var co = PackLibrary.Orient(PackLibrary.Asset(child.DefId), cf);
        foreach (var grandchild in premises.Children(child))
            Attach(premises, c, co, grandchild, seated);
        string draw = s.TryGetProperty("draw", out var dr) ? dr.GetString()! : "after_parent";
        if (draw == "before_parent")
            node.Before.Add(c);
        else if (draw == "workstation")
        {
            if (seated.TryGetValue(child.Id, out var who))
                c.Character = who;
            c.SeatDepth = ws[0].GetDouble() + ws[1].GetDouble();
            node.Chairs.Add(c);
        }
        else
            node.After.Add(c);
    }

    private void AddHit(bool person, int id, JsonElement o, int x, int y, int order)
    {
        if (!o.TryGetProperty("selection_poly", out var poly) || poly.ValueKind != JsonValueKind.Array)
            return;
        _hits.Add(new HitRegion(person, id, poly.EnumerateArray().Select(p => ((double)x + p[0].GetInt32(), (double)y + p[1].GetInt32())).ToArray(), order));
    }

    // ------------------------------------------------------------------ Composition

    private List<(Img img, int sx, int sy)> CharLayers(Seated who, string orient)
    {
        var be = PackLibrary.Asset(who.Look.Base);
        var bo = be.GetProperty("orientations").GetProperty(orient);
        var sw = new Dictionary<string, string> { ["skin"] = who.Look.Skin, ["hair"] = who.Look.HairRamp };
        var hairSw = new Dictionary<string, string> { ["hair"] = who.Look.HairRamp };
        var layers = new Dictionary<string, Img>();
        if (PackLibrary.Has(who.Look.Hair))
            foreach (var l in PackLibrary.Asset(who.Look.Hair).GetProperty("orientations").GetProperty(orient).GetProperty("layers").EnumerateObject())
                layers[l.Name] = PackLibrary.Layer(l.Value, hairSw);
        foreach (string name in new[] { "body", "head" })
            layers[name] = PackLibrary.Layer(bo.GetProperty("layers").GetProperty(name), sw);
        var seat = be.GetProperty("seat_anchor");
        var result = new List<(Img, int, int)>();
        foreach (string name in new[] { "hair_back", "body", "head", "hair_front" })
            if (layers.TryGetValue(name, out var img))
                result.Add((img, seat[0].GetInt32(), seat[1].GetInt32()));
        return result;
    }

    private static void DrawLayers(Img img, byte[]? shadow, int bw, int bh, Node node, JsonElement layers, (int X, int Y) anchor, bool front)
    {
        if (shadow != null && layers.TryGetProperty("shadow", out var sh))
            Blend.StampMax(shadow, bw, bh, PackLibrary.Png(sh.GetProperty("file").GetString()!), node.Sx - anchor.X, node.Sy - anchor.Y);
        Blend.Over(img, PackLibrary.Layer(layers.GetProperty("base"), node.Swatches), node.Sx - anchor.X, node.Sy - anchor.Y);
        if (layers.TryGetProperty("glass", out var gl))
            Blend.Over(img, PackLibrary.Layer(gl, node.Swatches), node.Sx - anchor.X, node.Sy - anchor.Y);
        if (front && layers.TryGetProperty("front", out var fr))
            Blend.Over(img, PackLibrary.Layer(fr, node.Swatches), node.Sx - anchor.X, node.Sy - anchor.Y);
    }

    private void DrawNode(Img img, byte[]? shadow, int bw, int bh, Node node)
    {
        var o = PackLibrary.Orient(PackLibrary.Asset(node.Aid), node.Facing);
        foreach (var c in node.Before)
            DrawNode(img, shadow, bw, bh, c);
        DrawLayers(img, shadow, bw, bh, node, o.GetProperty("layers"), PackLibrary.Anchor(o), true);
        foreach (var c in node.After.OrderBy(c => c.Depth))
            DrawNode(img, null, bw, bh, c);
    }

    private void DrawChair(Img img, byte[] shadow, int bw, int bh, Node chair)
    {
        var o = PackLibrary.Orient(PackLibrary.Asset(chair.Aid), chair.Facing);
        var layers = o.GetProperty("layers");
        var anchor = PackLibrary.Anchor(o);
        DrawLayers(img, shadow, bw, bh, chair, layers, anchor, false);
        if (chair.Character is { } who && o.TryGetProperty("occupant", out var occ))
        {
            var so = occ.GetProperty("seat_offset_px");
            int cx = chair.Sx + so[0].GetInt32(), cy = chair.Sy + so[1].GetInt32();
            foreach (var (li, sx, sy) in CharLayers(who, occ.GetProperty("character_orientation").GetString()!))
                Blend.Over(img, li, cx - sx, cy - sy);
            _heads[who.PersonId] = (cx, cy - 50);
            _hits.Add(new HitRegion(true, who.PersonId, [(cx - 14.0, cy - 50.0), (cx + 14.0, cy - 50.0), (cx + 14.0, cy + 6.0), (cx - 14.0, cy + 6.0)], 100_000));
        }
        if (layers.TryGetProperty("front", out var fr))
            Blend.Over(img, PackLibrary.Layer(fr, chair.Swatches), chair.Sx - anchor.X, chair.Sy - anchor.Y);
    }

    private void DrawWorkstation(Img img, byte[] shadow, int bw, int bh, Node desk)
    {
        if (desk.Chairs.Count == 0)
        {
            DrawNode(img, shadow, bw, bh, desk);
            return;
        }
        var o = PackLibrary.Orient(PackLibrary.Asset(desk.Aid), desk.Facing);
        double center = o.TryGetProperty("footprint_world_tiles", out var fw) ? (fw[0].GetDouble() + fw[1].GetDouble()) * 16.0 : 32;
        foreach (var c in desk.Chairs.Where(c => c.SeatDepth < center).OrderBy(c => c.SeatDepth))
            DrawChair(img, shadow, bw, bh, c);
        DrawNode(img, shadow, bw, bh, desk);
        foreach (var c in desk.Chairs.Where(c => c.SeatDepth >= center).OrderBy(c => c.SeatDepth))
            DrawChair(img, shadow, bw, bh, c);
    }

    /// <summary>Tri topologique des emprises (A derrière B si A.u1 ≤ B.u0 ou A.v1 ≤ B.v0), comme la référence.</summary>
    private static List<int> TopoSort(List<(double u0, double u1, double v0, double v1)> fps)
    {
        int n = fps.Count;
        var before = Enumerable.Range(0, n).Select(_ => new HashSet<int>()).ToList();
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                if (i == j)
                    continue;
                var a = fps[i];
                var b = fps[j];
                bool behind = a.u1 <= b.u0 || a.v1 <= b.v0, inFront = b.u1 <= a.u0 || b.v1 <= a.v0;
                if (behind && !inFront)
                    before[j].Add(i);
            }
        var order = Enumerable.Range(0, n).OrderBy(k => fps[k].u1 + fps[k].v1).ThenBy(k => fps[k].u0 + fps[k].v0).ToList();
        var rank = new int[n];
        for (int r = 0; r < n; r++)
            rank[order[r]] = r;
        var done = new HashSet<int>();
        var result = new List<int>();
        void Visit(int k, HashSet<int> stack)
        {
            if (done.Contains(k) || stack.Contains(k))
                return;
            var next = new HashSet<int>(stack) { k };
            foreach (int d in before[k].OrderBy(q => rank[q]))
                Visit(d, next);
            done.Add(k);
            result.Add(k);
        }
        foreach (int k in order)
            Visit(k, new HashSet<int>());
        return result;
    }

    private Img Compose()
    {
        var img = new Img(_w, _h);
        foreach (var (s, x, y) in _floor)
            Blend.Over(img, s, x, y);
        foreach (var (s, x, y) in _edges)
            Blend.Over(img, s, x, y);
        var shadow = new byte[_w * _h];
        var objects = new Img(_w, _h);
        var sorted = TopoSort(_groups.Select(g => g.fp).ToList());
        var rank = new Dictionary<int, int>();
        for (int i = 0; i < sorted.Count; i++)
        {
            var node = _groups[sorted[i]].node;
            rank[node.ItemId] = i;
            DrawWorkstation(objects, shadow, _w, _h, node);
        }
        // Les zones cliquables suivent l'ordre de dessin : la plus proche gagne
        for (int i = 0; i < _hits.Count; i++)
            if (!_hits[i].Person && rank.TryGetValue(_hits[i].Id, out int r))
                _hits[i] = _hits[i] with { Order = r };
        var (sr, sg, sb) = PackLibrary.ShadowColor;
        var layer = new Img(_w, _h);
        for (int i = 0; i < _w * _h; i++)
        {
            layer.P[i * 4] = sr;
            layer.P[i * 4 + 1] = sg;
            layer.P[i * 4 + 2] = sb;
            layer.P[i * 4 + 3] = (byte)MathF.Round(shadow[i] / 255f * 0.30f * 255f);
        }
        Blend.Over(img, layer, 0, 0);
        foreach (var (s, x, y) in _walls)
            Blend.Over(img, s, x, y);
        foreach (var (s, x, y) in _wallItems)
            Blend.Over(img, s, x, y);
        Blend.Over(img, objects, 0, 0);
        foreach (var (s, x, y) in _ceiling)
            Blend.Over(img, s, x, y);
        return img;
    }

    /// <summary>Une vignette de personne (portrait du pack, vue de face recadrée).</summary>
    public static Img Portrait(Appearance look)
    {
        var be = PackLibrary.Asset(look.Base);
        var bo = be.GetProperty("orientations").GetProperty("se");
        var canvas = new Img(40, 72);
        var sw = new Dictionary<string, string> { ["skin"] = look.Skin, ["hair"] = look.HairRamp };
        var hairSw = new Dictionary<string, string> { ["hair"] = look.HairRamp };
        var hair = PackLibrary.Has(look.Hair) ? PackLibrary.Asset(look.Hair).GetProperty("orientations").GetProperty("se").GetProperty("layers") : default;
        if (hair.ValueKind == JsonValueKind.Object && hair.TryGetProperty("hair_back", out var back))
            Blend.Over(canvas, PackLibrary.Layer(back, hairSw), 0, 0);
        Blend.Over(canvas, PackLibrary.Layer(bo.GetProperty("layers").GetProperty("body"), sw), 0, 0);
        Blend.Over(canvas, PackLibrary.Layer(bo.GetProperty("layers").GetProperty("head"), sw), 0, 0);
        if (hair.ValueKind == JsonValueKind.Object && hair.TryGetProperty("hair_front", out var frontHair))
            Blend.Over(canvas, PackLibrary.Layer(frontHair, hairSw), 0, 0);
        var crop = be.GetProperty("portrait_crop");
        int cx = crop[0].GetInt32(), cy = crop[1].GetInt32(), cw = crop[2].GetInt32(), ch = crop[3].GetInt32();
        var result = new Img(cw, ch);
        for (int y = 0; y < ch; y++)
            Buffer.BlockCopy(canvas.P, ((cy + y) * 40 + cx) * 4, result.P, y * cw * 4, cw * 4);
        return result;
    }
}
