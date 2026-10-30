using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Core.products;
using UI.Iso;
using UI.Pixel;

namespace UI.Hud;

/// <summary>
/// Le graphe des dépendances des composants d'un projet : chaque composant qui en demande un autre (« Requires » du
/// catalogue) y est relié par un lien du pack (link_*), de son port de sortie (port_out_right) au port d'entrée de l'autre
/// (port_in_left). Couleurs des liens selon le pack : acquis (succès), en file (sarcelle), à venir (pointillés, papier),
/// cassé si un composant dépendant est là sans ce qu'il demande (danger). Les angles et tés du pack sont des blocs 4 × 4.
/// </summary>
public class DependencyGraph : Canvas
{
    private const double Scale = 2, NodeW = 190, NodeH = 40, ColumnGap = 70, RowGap = 12;

    public enum LinkState { Satisfied, Required, Optional, Broken }

    public record Node(ComponentDef Def, int Level, bool Queued, int Column, int Row);

    public record Link(Node From, Node To, LinkState State);

    /// <summary>Les nœuds et liens d'un projet : seulement les composants reliés à au moins un autre, pour son type.</summary>
    public static (List<Node> Nodes, List<Link> Links) Build(Product product)
    {
        var defs = Core.products.Design.ForType(product.ArchetypeId).ToList();
        var linked = defs.Where(d => d.Requires != null && defs.Any(p => p.Id == d.Requires)).ToList();
        var roots = linked.Select(d => d.Requires!).Distinct().Select(id => defs.First(d => d.Id == id)).ToList();
        var nodes = new List<Node>();
        var links = new List<Link>();
        int row = 0;
        foreach (var root in roots)
        {
            var children = linked.Where(d => d.Requires == root.Id).ToList();
            var parent = Make(product, root, 0, row + (children.Count - 1) / 2);
            nodes.Add(parent);
            foreach (var child in children)
            {
                var node = Make(product, child, 1, row++);
                nodes.Add(node);
                var state = parent.Level > 0 ? (node.Level > 0 || node.Queued ? LinkState.Satisfied : LinkState.Optional)
                    : node.Level > 0 ? LinkState.Broken
                    : node.Queued || parent.Queued ? LinkState.Required : LinkState.Optional;
                links.Add(new Link(parent, node, state));
            }
        }
        return (nodes, links);
    }

    private static Node Make(Product product, ComponentDef def, int column, int row) =>
        new(def, product.Level(def.Id), product.Queue.Any(q => q.DefId == def.Id), column, row);

    public static string StateColor(LinkState state) => state switch
    {
        LinkState.Satisfied => "#62a04b",
        LinkState.Required => "#357c82",
        LinkState.Broken => "#c8453d",
        _ => "#d9c8a8",
    };

    private readonly List<Link> _links;

    public DependencyGraph(Product product)
    {
        var (nodes, links) = Build(product);
        _links = links;
        int rows = nodes.Count == 0 ? 0 : nodes.Max(n => n.Row) + 1;
        Width = 2 * NodeW + ColumnGap;
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        Height = rows * (NodeH + RowGap);
        if (nodes.Count > 0)
            Children.Add(new LinkLayer(links) { Width = Width, Height = Height });
        foreach (var node in nodes)
        {
            var (x, y) = Position(node);
            var card = NodeCard(node);
            SetLeft(card, x);
            SetTop(card, y);
            Children.Add(card);
            foreach (var (port, px, state) in Ports(node))
            {
                var image = new Image { Source = Sprite(port, state), Width = 8 * Scale, Height = 8 * Scale };
                RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.None);
                SetLeft(image, px - 4 * Scale);
                SetTop(image, y + NodeH / 2 - 4 * Scale);
                Children.Add(image);
            }
        }
    }

    private static (double X, double Y) Position(Node node) => (node.Column * (NodeW + ColumnGap), node.Row * (NodeH + RowGap));

    /// <summary>Ports d'un nœud : sortie à droite d'un composant dont d'autres dépendent, entrée à gauche d'un dépendant.</summary>
    private IEnumerable<(string Port, double X, string State)> Ports(Node node)
    {
        var (x, _) = Position(node);
        var outgoing = _links.Where(l => l.From.Def.Id == node.Def.Id).ToList();
        if (outgoing.Count > 0)
            yield return ("port_out_right", x + NodeW, outgoing.Any(l => l.State == LinkState.Broken) ? "invalid" : node.Level > 0 ? "connected" : "idle");
        if (_links.FirstOrDefault(l => l.To.Def.Id == node.Def.Id) is { } incoming)
            yield return ("port_in_left", x, incoming.State == LinkState.Broken ? "invalid" : incoming.State == LinkState.Satisfied ? "connected" : "idle");
    }

    private static Control NodeCard(Node node)
    {
        string icon = ComponentArt.Icon(node.Def.Id, node.Def.Icon);
        string level = node.Level > 0 ? $"niveau {node.Level}" : node.Queued ? "en file" : "pas encore";
        var content = Ui.Row(6, Ui.Icon(icon, 20),
            Ui.Column(0, Ui.Text(node.Def.Name, 13, Palette.Snow, wrap: false), Ui.Text(level, 11, node.Level > 0 ? Palette.Leaf : Palette.Fog, wrap: false)));
        var card = Ui.Card(content, node.Level > 0 ? Palette.Leaf : null);
        card.Width = NodeW;
        card.Height = NodeH;
        return card;
    }

    private static readonly Dictionary<string, Bitmap> Tinted = new();

    /// <summary>Un sprite du pack, teinté par multiplication (liens) ou dans son état (ports).</summary>
    private static Bitmap Sprite(string id, string? state = null, string? hex = null)
    {
        if (hex == null)
            return PackSkins.Sprite(id, state);
        string key = id + hex;
        if (Tinted.TryGetValue(key, out var bitmap))
            return bitmap;
        var img = PackLibrary.Png(PackLibrary.StateFile(PackLibrary.Asset(id), state)).Clone();
        var c = Color.Parse(hex);
        for (int i = 0; i < img.P.Length; i += 4)
        {
            img.P[i] = (byte)(img.P[i] * c.R / 255);
            img.P[i + 1] = (byte)(img.P[i + 1] * c.G / 255);
            img.P[i + 2] = (byte)(img.P[i + 2] * c.B / 255);
        }
        return Tinted[key] = img.ToBitmap();
    }

    /// <summary>Les liens, dessinés sous les cartes : horizontal, angle, vertical, angle, horizontal, puis la flèche au port d'entrée.</summary>
    private sealed class LinkLayer(List<Link> links) : Control
    {
    private readonly List<Link> _links = links;

    public override void Render(DrawingContext context)
    {
        using var _ = context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None });
        const double t = 4 * Scale;
        foreach (var group in _links.GroupBy(l => l.From.Def.Id))
            foreach (var link in group)
            {
                string hex = StateColor(link.State);
                bool dashed = link.State == LinkState.Optional;
                var (fx, fy) = Position(link.From);
                var (tx, ty) = Position(link.To);
                double x0 = fx + NodeW + 4 * Scale, y0 = fy + NodeH / 2 - t / 2;
                double x1 = tx - 4 * Scale - 7 * Scale, y1 = ty + NodeH / 2 - t / 2;
                double mid = x0 + (x1 - x0) / 2;
                Horizontal(context, x0, mid, y0, hex, dashed);
                if (System.Math.Abs(y1 - y0) > 0.5)
                {
                    // Angle de départ (un té quand le même port alimente plusieurs liens), descente, angle d'arrivée
                    string start = group.Count() > 1 && link != group.First() ? (y1 > y0 ? "link_tee_down" : "link_tee_up")
                        : y1 > y0 ? "link_corner_ne" : "link_corner_se";
                    Draw(context, Sprite(start, null, hex), mid, y0);
                    Vertical(context, System.Math.Min(y0, y1) + t, System.Math.Max(y0, y1), mid, hex, dashed);
                    Draw(context, Sprite(y1 > y0 ? "link_corner_sw" : "link_corner_nw", null, hex), mid, y1);
                }
                Horizontal(context, mid + t, x1, y1, hex, dashed);
                var arrow = Sprite("link_arrow_right", null, hex);
                context.DrawImage(arrow, new Rect(0, 0, 7, 7), new Rect(x1, y1 + t / 2 - 3.5 * Scale, 7 * Scale, 7 * Scale));
            }
    }

    private static void Draw(DrawingContext context, Bitmap tile, double x, double y) =>
        context.DrawImage(tile, new Rect(0, 0, 4, 4), new Rect(x, y, 4 * Scale, 4 * Scale));

    private static void Horizontal(DrawingContext context, double from, double to, double y, string hex, bool dashed)
    {
        var tile = Sprite(dashed ? "link_dashed_h" : "link_h", null, hex);
        for (double x = from; x < to - 0.5; x += 4 * Scale)
            context.DrawImage(tile, new Rect(0, 0, 4, 4), new Rect(x, y, System.Math.Min(4 * Scale, to - x), 4 * Scale));
    }

    private static void Vertical(DrawingContext context, double from, double to, double x, string hex, bool dashed)
    {
        var tile = Sprite(dashed ? "link_dashed_v" : "link_v", null, hex);
        for (double y = from; y < to - 0.5; y += 4 * Scale)
            context.DrawImage(tile, new Rect(0, 0, 4, 4), new Rect(x, y, 4 * Scale, System.Math.Min(4 * Scale, to - y)));
    }
    }
}
