using System.Reflection;
using System.Text.Json;

namespace Core.space;

/// <summary>Une orientation d'un asset : cases occupées, cases d'usage à laisser libres, sockets d'accroche.</summary>
public record PackFacing(
    string Name,
    IReadOnlyList<Cell> Collision,
    IReadOnlyList<Cell> Access,
    IReadOnlyDictionary<string, string[]> Sockets)
{
    /// <summary>Emplacements d'une baie (en U), 0 pour un autre objet.</summary>
    public int Slots { get; init; }

    /// <summary>Ports de flux (machines, convoyeurs) : case relative, sens (in, out, inout) et direction vers le voisin.</summary>
    public IReadOnlyList<PackPort> Ports { get; init; } = [];
}

/// <summary>Un port de flux : « Dir » pointe vers la case voisine qu'il relie (+U, −U, +V, −V).</summary>
public record PackPort(string Name, string Kind, Cell Tile, Cell Dir)
{
    public bool In => Kind is "in" or "inout";
    public bool Out => Kind is "out" or "inout";
}

/// <summary>
/// Ce que le moteur sait d'un asset du pack (assets/StartupRushOriginalAssets/manifest.json) : emprise, accès et
/// sockets par orientation, types de socket qu'il remplit. Le dessin lui-même est l'affaire de l'interface.
/// </summary>
public record PackAsset(
    string Id,
    string Category,
    string Subcategory,
    string NameFr,
    string Kind,
    string RenderLayer,
    bool Centered,
    string[] Provides,
    IReadOnlyDictionary<string, PackFacing> Facings,
    int Span = 1)
{
    /// <summary>Hauteur d'un module de baie, en U.</summary>
    public int Units { get; init; }

    /// <summary>Les baies qui acceptent ce module.</summary>
    public string[] Fits { get; init; } = [];

    /// <summary>On marche dessous ou dessus (chemin de câbles, passe-câble) : il ne prend la case de personne.</summary>
    public bool Walkable { get; init; }

    public bool RackModule => Kind == "rack_module";

    public PackFacing Facing(string facing) => Facings.TryGetValue(facing, out var f) ? f : Facings.Values.First();

    public string DefaultFacing => Facings.ContainsKey("se") ? "se" : Facings.Keys.First();

    public bool WallMount => Kind == "wall";

    /// <summary>Accroché au plafond (luminaire) : il ne prend aucune case au sol.</summary>
    public bool Ceiling => Kind == "ceiling";

    /// <summary>Tapis et marquages : posés sur le sol, on marche dessus.</summary>
    public bool FloorDecor => RenderLayer == "floor_decor";
}

/// <summary>Un objet d'un plan de pièce, avec ce qui est accroché à ses sockets.</summary>
public record PlanObject(string Asset, int U, int V, string Facing, IReadOnlyList<PlanChild> Children, IReadOnlyDictionary<string, string> Swatches);

public record PlanChild(string Socket, string Asset, bool HasCharacter);

/// <summary>Un objet mural d'un plan : mur du fond (« left » : U = 0, « right » : V = 0) et rang le long de ce mur.</summary>
public record PlanWallItem(string Asset, string Wall, int Index);

/// <summary>Une zone de sol d'un plan : matériau du pack (« floor_vinyl ») sur un rectangle de cases, avec ses couleurs.</summary>
public record PlanFloor(string Material, int U, int V, int Width, int Depth, IReadOnlyDictionary<string, string> Swatches);

/// <summary>
/// Un plan de pièce du pack (rooms/*.json) : taille, entrée, objets livrés, et les segments des deux murs du fond
/// (« plain », « window », « window_wide », « door », null pour la suite d'une grande fenêtre).
/// </summary>
public record RoomPlan(string Id, string NameFr, int Width, int Depth, Cell Entrance, string EntranceWall, IReadOnlyList<PlanObject> Objects)
{
    public IReadOnlyList<string?> LeftWall { get; init; } = [];
    public IReadOnlyList<string?> RightWall { get; init; } = [];
    public IReadOnlyList<PlanWallItem> WallItems { get; init; } = [];

    /// <summary>Les zones de sol, dans l'ordre de pose : une zone recouvre les précédentes.</summary>
    public IReadOnlyList<PlanFloor> Floors { get; init; } = [];

    /// <summary>Motif de chaque mur du fond (identifiant du pack, « pattern_paint »), null pour un mur nu.</summary>
    public string? LeftPattern { get; init; }

    public string? RightPattern { get; init; }

    /// <summary>Couleurs de chaque mur du fond (« wall » → « cream »).</summary>
    public IReadOnlyDictionary<string, string> LeftSwatches { get; init; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, string> RightSwatches { get; init; } = new Dictionary<string, string>();

    /// <summary>Le segment de mur à ce rang (null : hors du mur ou suite d'une grande fenêtre).</summary>
    public string? Segment(string wall, int index)
    {
        var segments = wall == "left" ? LeftWall : RightWall;
        return index >= 0 && index < segments.Count ? segments[index] : null;
    }
}

/// <summary>Le pack original, embarqué dans le moteur : manifeste et plans de pièces.</summary>
public static class Pack
{
    private static readonly Lazy<Dictionary<string, PackAsset>> AssetsById = new(LoadAssets);
    private static readonly Lazy<Dictionary<string, RoomPlan>> PlansById = new(LoadPlans);

    public static IReadOnlyDictionary<string, PackAsset> Assets => AssetsById.Value;

    public static bool Has(string id) => AssetsById.Value.ContainsKey(id);

    public static PackAsset Asset(string id) => AssetsById.Value[id];

    public static RoomPlan Plan(string id) => PlansById.Value[id];

    /// <summary>Le plan, ou null (local sans plan connu, comme dans certaines vieilles sauvegardes).</summary>
    public static RoomPlan? FindPlan(string id) => PlansById.Value.GetValueOrDefault(id);

    private static readonly Lazy<HashSet<string>> AcceptedTypes = new(() =>
        AssetsById.Value.Values.SelectMany(a => a.Facings.Values).SelectMany(f => f.Sockets.Values).SelectMany(t => t).ToHashSet());

    /// <summary>Un socket du pack accepte-t-il ce type d'objet ? (Un caisson « sous le bureau » n'a aucun hôte : il se pose au sol.)</summary>
    public static bool Accepted(string type) => AcceptedTypes.Value.Contains(type);

    private static Stream Resource(string name) =>
        Assembly.GetExecutingAssembly().GetManifestResourceStream(name) ?? throw new InvalidOperationException($"Ressource du pack absente : {name}");

    private static Cell CellOf(JsonElement e) => new(e[0].GetInt32(), e[1].GetInt32());

    private static Dictionary<string, string> Swatches(JsonElement e)
    {
        var d = new Dictionary<string, string>();
        if (e.TryGetProperty("swatches", out var s) && s.ValueKind == JsonValueKind.Object)
            foreach (var p in s.EnumerateObject())
                d[p.Name] = p.Value.GetString()!;
        return d;
    }

    private static Cell Direction(string dir) => dir switch
    {
        "+U" => new Cell(1, 0),
        "-U" => new Cell(-1, 0),
        "+V" => new Cell(0, 1),
        _ => new Cell(0, -1),
    };

    private static Dictionary<string, PackAsset> LoadAssets()
    {
        using var document = JsonDocument.Parse(Resource("pack/manifest.json"));
        var result = new Dictionary<string, PackAsset>();
        foreach (var a in document.RootElement.GetProperty("assets").EnumerateArray())
        {
            var facings = new Dictionary<string, PackFacing>();
            if (a.TryGetProperty("orientations", out var orientations) && orientations.ValueKind == JsonValueKind.Object)
                foreach (var o in orientations.EnumerateObject())
                {
                    var collision = o.Value.TryGetProperty("collision_tiles", out var c) && c.ValueKind == JsonValueKind.Array
                        ? c.EnumerateArray().Select(CellOf).ToList() : [];
                    var access = o.Value.TryGetProperty("access", out var ac) && ac.ValueKind == JsonValueKind.Array
                        ? ac.EnumerateArray().Where(x => x.TryGetProperty("tile", out _)).Select(x => CellOf(x.GetProperty("tile")))
                            // La place de la personne derrière un comptoir est dans l'emprise : rien à laisser libre
                            .Where(t => !collision.Contains(t)).ToList() : [];
                    var sockets = new Dictionary<string, string[]>();
                    if (o.Value.TryGetProperty("sockets", out var s) && s.ValueKind == JsonValueKind.Object)
                        foreach (var sock in s.EnumerateObject())
                            sockets[sock.Name] = sock.Value.GetProperty("accepts").EnumerateArray().Select(x => x.GetString()!).ToArray();
                    int slots = o.Value.TryGetProperty("slots", out var sl) && sl.ValueKind == JsonValueKind.Object && sl.TryGetProperty("count", out var n) ? n.GetInt32() : 0;
                    var ports = o.Value.TryGetProperty("ports", out var po) && po.ValueKind == JsonValueKind.Array
                        ? po.EnumerateArray().Select(x => new PackPort(x.GetProperty("name").GetString()!, x.GetProperty("kind").GetString()!,
                            CellOf(x.GetProperty("tile")), Direction(x.GetProperty("world_dir").GetString()!))).ToList()
                        : [];
                    facings[o.Name] = new PackFacing(o.Name, collision, access, sockets) { Slots = slots, Ports = ports };
                }
            bool centered = a.TryGetProperty("footprint_tiles", out var fp) && fp.ValueKind == JsonValueKind.Null
                            && a.TryGetProperty("size_su", out var su) && su.ValueKind != JsonValueKind.Null;
            string Str(string key) => a.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
            var provides = a.TryGetProperty("provides", out var p) && p.ValueKind == JsonValueKind.Array
                ? p.EnumerateArray().Select(x => x.GetString()!).ToArray() : [];
            int span = a.TryGetProperty("footprint_tiles", out var ft) && ft.ValueKind == JsonValueKind.Array
                ? ft.EnumerateArray().Max(x => x.GetInt32()) : 1;
            result[Str("id")] = new PackAsset(Str("id"), Str("category"), Str("subcategory"), Str("name_fr"), Str("kind"),
                Str("render_layer"), centered, provides, facings, span)
            {
                Units = a.TryGetProperty("units", out var u) && u.ValueKind == JsonValueKind.Number ? u.GetInt32() : 0,
                Fits = a.TryGetProperty("fits", out var fi) && fi.ValueKind == JsonValueKind.Array ? fi.EnumerateArray().Select(x => x.GetString()!).ToArray() : [],
                Walkable = a.TryGetProperty("walkable", out var w) && w.ValueKind == JsonValueKind.True,
            };
        }
        return result;
    }

    private static Dictionary<string, RoomPlan> LoadPlans()
    {
        var result = new Dictionary<string, RoomPlan>();
        foreach (string name in Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(n => n.StartsWith("pack/rooms/")))
        {
            using var document = JsonDocument.Parse(Resource(name));
            var root = document.RootElement;
            var size = root.GetProperty("size_tiles");
            var objects = new List<PlanObject>();
            foreach (var o in root.GetProperty("objects").EnumerateArray())
            {
                var children = new List<PlanChild>();
                if (o.TryGetProperty("children", out var ch) && ch.ValueKind == JsonValueKind.Object)
                    foreach (var c in ch.EnumerateObject())
                    {
                        if (c.Value.ValueKind == JsonValueKind.String)
                            children.Add(new PlanChild(c.Name, c.Value.GetString()!, false));
                        else if (c.Value.ValueKind == JsonValueKind.Object && c.Value.TryGetProperty("asset", out var ca))
                            children.Add(new PlanChild(c.Name, ca.GetString()!, c.Value.TryGetProperty("character", out _)));
                    }
                var swatches = new Dictionary<string, string>();
                if (o.TryGetProperty("swatches", out var sw) && sw.ValueKind == JsonValueKind.Object)
                    foreach (var s in sw.EnumerateObject())
                        swatches[s.Name] = s.Value.GetString()!;
                objects.Add(new PlanObject(o.GetProperty("asset").GetString()!, o.GetProperty("tile")[0].GetInt32(), o.GetProperty("tile")[1].GetInt32(),
                    o.TryGetProperty("facing", out var f) ? f.GetString()! : "se", children, swatches));
            }
            var entrance = root.GetProperty("entrances")[0];
            string id = root.GetProperty("id").GetString()!;
            List<string?> Segments(string wall) =>
                root.TryGetProperty("walls", out var w) && w.TryGetProperty(wall, out var side) && side.TryGetProperty("segments", out var seg)
                    ? seg.EnumerateArray().Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : null).ToList() : [];
            var wallItems = root.TryGetProperty("wall_items", out var wi) && wi.ValueKind == JsonValueKind.Array
                ? wi.EnumerateArray().Select(x => new PlanWallItem(x.GetProperty("asset").GetString()!, x.GetProperty("wall").GetString()!, x.GetProperty("index").GetInt32())).ToList()
                : [];
            var floors = root.TryGetProperty("floor", out var fl) && fl.ValueKind == JsonValueKind.Array
                ? fl.EnumerateArray().Select(x =>
                {
                    var r = x.GetProperty("rect");
                    return new PlanFloor(x.GetProperty("material").GetString()!, r[0].GetInt32(), r[1].GetInt32(), r[2].GetInt32(), r[3].GetInt32(), Swatches(x));
                }).ToList()
                : [];
            JsonElement? Wall(string wall) => root.TryGetProperty("walls", out var w) && w.TryGetProperty(wall, out var side) ? side : null;
            string? Pattern(string wall) => Wall(wall) is { } side && side.TryGetProperty("pattern", out var p) && p.ValueKind == JsonValueKind.String
                ? "pattern_" + p.GetString() : null;
            result[id] = new RoomPlan(id, root.GetProperty("name_fr").GetString()!, size[0].GetInt32(), size[1].GetInt32(),
                CellOf(entrance.GetProperty("access_tile")), entrance.GetProperty("wall").GetString()!, objects)
            {
                LeftWall = Segments("left"),
                RightWall = Segments("right"),
                WallItems = wallItems,
                Floors = floors,
                LeftPattern = Pattern("left"),
                RightPattern = Pattern("right"),
                LeftSwatches = Wall("left") is { } l ? Swatches(l) : new Dictionary<string, string>(),
                RightSwatches = Wall("right") is { } rw ? Swatches(rw) : new Dictionary<string, string>(),
            };
        }
        return result;
    }
}
