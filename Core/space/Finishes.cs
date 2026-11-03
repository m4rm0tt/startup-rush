namespace Core.space;

public enum FinishKind { Floor, Wall }

/// <summary>Une zone de sol posée avec un module de pièce : matériau du pack sur un rectangle de cases, avec ses couleurs.</summary>
public class FloorPatch
{
    public string Material { get; set; } = "";
    public int U { get; set; }
    public int V { get; set; }
    public int Width { get; set; }
    public int Depth { get; set; }
    public Dictionary<string, string> Swatches { get; set; } = new();
}

/// <summary>
/// Une finition du local : un sol du pack (prix par case, posé sur toute la surface) ou un habillage des deux murs du fond
/// (prix par segment de mur). La plupart ne font que l'identité du lieu ; quelques-unes ont un effet plafonné.
/// </summary>
public record FinishDef(string Id, FinishKind Kind, string Name, decimal Price, string Effect, string? UnlockNode = null);

/// <summary>
/// Où poser une finition. Mur : « left », « right » ou null (les deux). Sol : un rectangle de cases (une zone, ou une seule
/// case), ou tout le local si la largeur vaut 0.
/// </summary>
public record FinishTarget(string? Wall = null, int U = 0, int V = 0, int Width = 0, int Depth = 0)
{
    public static readonly FinishTarget Everywhere = new();

    public bool WholeFloor => Width <= 0 || Depth <= 0;

    public static FinishTarget Rect(int u, int v, int width, int depth) => new(null, u, v, width, depth);
}

/// <summary>
/// Les finitions : ce que le local porte vraiment (celles du plan, ou celles que tu as refaites) et leurs effets. Le faux
/// plancher distribue mieux le froid existant sans en créer ; le lambris ajoute un peu d'ambiance, dans son plafond.
/// </summary>
public static class Finishes
{
    /// <summary>Froid en plus avec un faux plancher sur toute la surface ⚙️ (à proportion de la surface couverte).</summary>
    public const double RaisedCooling = 0.05;

    /// <summary>Froid en plus avec des dalles perforées dans le faux plancher ⚙️ (au total, à proportion de la surface).</summary>
    public const double VentCooling = 0.10;

    /// <summary>Ambiance apportée par un mur en lambris ⚙️ (deux murs : 1), comptée dans le plafond d'ambiance.</summary>
    public const double PanelAmbiance = 0.5;

    /// <summary>Concentration apportée par chaque mur en panneaux acoustiques ⚙️, dans le plafond commun de concentration.</summary>
    public const double AcousticWallFocus = 0.005;

    /// <summary>Concentration apportée par un sol textile (moquette) sur toute la surface ⚙️, à proportion de la surface.</summary>
    public const double TextileFloorFocus = 0.01;

    private const string Decorative = "Décoratif : l'identité du local, sans autre effet.";

    public static readonly IReadOnlyList<FinishDef> Catalog =
    [
        new("floor_concrete", FinishKind.Floor, "Béton ciré", 20m, Decorative),
        new("floor_vinyl", FinishKind.Floor, "Vinyle moucheté", 25m, Decorative),
        new("floor_carpet", FinishKind.Floor, "Moquette", 30m, "Étouffe le bruit : concentration +1 % (plafond commun de 4 %)."),
        new("floor_carpet_tiles", FinishKind.Floor, "Dalles de moquette", 35m, "Étouffe le bruit : concentration +1 % (plafond commun de 4 %)."),
        new("floor_tile_ceramic", FinishKind.Floor, "Carrelage", 45m, Decorative),
        new("floor_wood_planks", FinishKind.Floor, "Plancher en lames", 50m, Decorative),
        new("floor_parquet", FinishKind.Floor, "Parquet à bâtons", 60m, Decorative),
        new("floor_raised", FinishKind.Floor, "Faux plancher technique", 120m,
            "Le froid du site est mieux distribué : +5 % du froid existant (sans en créer).", "server_room"),
        new("floor_raised_vent", FinishKind.Floor, "Faux plancher perforé", 160m,
            "Dalles de soufflage : +10 % du froid existant (sans en créer).", "server_room"),

        new("pattern_paint", FinishKind.Wall, "Peinture unie", 40m, Decorative),
        new("pattern_wallpaper", FinishKind.Wall, "Papier peint", 60m, Decorative),
        new("pattern_two_tone", FinishKind.Wall, "Deux tons et cimaise", 70m, Decorative),
        new("pattern_stripes", FinishKind.Wall, "Bandes verticales", 70m, Decorative),
        new("pattern_concrete", FinishKind.Wall, "Béton banché", 80m, Decorative),
        new("pattern_brick", FinishKind.Wall, "Brique", 110m, Decorative),
        new("pattern_acoustic", FinishKind.Wall, "Panneaux acoustiques", 160m,
            "Concentration +1 % (plafond commun de 4 %) ; tient lieu de séparation acoustique pour le studio."),
        new("pattern_wood_panels", FinishKind.Wall, "Lambris", 140m, "Un lieu chaleureux : +1 d'ambiance (dans le plafond de 6)."),
    ];

    public static bool Has(string id) => Catalog.Any(f => f.Id == id);

    public static FinishDef Get(string id) => Catalog.First(f => f.Id == id);

    /// <summary>Les zones de sol du local : tout le local dans le sol refait, sinon celles du plan.</summary>
    public static IReadOnlyList<PlanFloor> FloorZones(Premises premises) =>
        (premises.FloorMaterial is { } material
            ? [new PlanFloor(material, 0, 0, premises.Width, premises.Height, new Dictionary<string, string>())]
            : Pack.FindPlan(premises.PlanId)?.Floors ?? [])
        .Concat(premises.FloorPatches.Select(p => new PlanFloor(p.Material, p.U, p.V, p.Width, p.Depth, p.Swatches))).ToList();

    /// <summary>Prix d'un sol posé avec un module, par case (le prix de la finition, 0 si elle n'est pas au catalogue).</summary>
    public static decimal FloorPrice(string material) => Has(material) ? Get(material).Price : 0;

    /// <summary>Le matériau de chaque case (null : aucune zone ne la couvre).</summary>
    public static string?[,] FloorGrid(Premises premises)
    {
        var grid = new string?[premises.Width, premises.Height];
        foreach (var zone in FloorZones(premises))
            for (int u = Math.Max(0, zone.U); u < Math.Min(premises.Width, zone.U + zone.Width); u++)
                for (int v = Math.Max(0, zone.V); v < Math.Min(premises.Height, zone.V + zone.Depth); v++)
                    grid[u, v] = zone.Material;
        return grid;
    }

    /// <summary>Le motif d'un mur du fond (« left » ou « right ») : celui de ce mur, sinon l'habillage des deux, sinon le plan.</summary>
    public static string? Pattern(Premises premises, string wall) =>
        (wall == "left" ? premises.LeftPattern : premises.RightPattern)
        ?? premises.WallPattern ?? (Pack.FindPlan(premises.PlanId) is { } plan ? wall == "left" ? plan.LeftPattern : plan.RightPattern : null);

    /// <summary>La couleur choisie pour un mur (teinte du canal « wall »), null : celle du plan.</summary>
    public static string? WallColor(Premises premises, string wall) => wall == "left" ? premises.LeftWallColor : premises.RightWallColor;

    /// <summary>Segments des deux murs du fond (une grande fenêtre compte pour un).</summary>
    public static int WallSegments(Premises premises) => WallSegments(premises, "left") + WallSegments(premises, "right");

    /// <summary>Segments d'un mur du fond.</summary>
    public static int WallSegments(Premises premises, string wall) =>
        Pack.FindPlan(premises.PlanId) is { } plan
            ? (wall == "left" ? plan.LeftWall : plan.RightWall).Count(s => s != null)
            : wall == "left" ? premises.Height : premises.Width;

    /// <summary>Le prix des travaux dans ce local : par case pour un sol, par segment de mur pour un habillage.</summary>
    public static decimal Cost(Premises premises, FinishDef finish) => Cost(premises, finish, FinishTarget.Everywhere);

    /// <summary>Le prix des travaux sur une cible : les cases du rectangle, ou les segments du ou des murs choisis.</summary>
    public static decimal Cost(Premises premises, FinishDef finish, FinishTarget target)
    {
        if (finish.Kind == FinishKind.Wall)
            return finish.Price * (target.Wall is { } wall ? WallSegments(premises, wall) : WallSegments(premises));
        if (target.WholeFloor)
            return finish.Price * premises.Width * premises.Height;
        int u0 = Math.Max(0, target.U), v0 = Math.Max(0, target.V);
        int u1 = Math.Min(premises.Width, target.U + target.Width), v1 = Math.Min(premises.Height, target.V + target.Depth);
        return finish.Price * Math.Max(0, u1 - u0) * Math.Max(0, v1 - v0);
    }

    /// <summary>Teintes proposées pour un sol (canal « floor ») ou un mur (canal « wall »).</summary>
    public static IReadOnlyList<string> Colors(FinishKind kind) => Pack.SwatchOptions(kind == FinishKind.Floor ? "floor" : "wall");

    /// <summary>
    /// La zone de sol qui couvre cette case : la dernière zone posée par-dessus (module, case refaite), sinon celle du plan,
    /// sinon tout le local. C'est ce que « Une zone » refait quand on clique une case.
    /// </summary>
    public static FinishTarget ZoneAt(Premises premises, Cell cell)
    {
        foreach (var zone in FloorZones(premises).Reverse())
            if (cell.X >= zone.U && cell.X < zone.U + zone.Width && cell.Y >= zone.V && cell.Y < zone.V + zone.Depth)
                return FinishTarget.Rect(Math.Max(0, zone.U), Math.Max(0, zone.V),
                    Math.Min(premises.Width, zone.U + zone.Width) - Math.Max(0, zone.U), Math.Min(premises.Height, zone.V + zone.Depth) - Math.Max(0, zone.V));
        return FinishTarget.Everywhere;
    }

    /// <summary>
    /// Pose une finition (sans la payer : <c>Campaign.SetFinish</c> s'en charge, et l'aperçu l'applique à une copie du local).
    /// Sol : tout le local (les zones posées disparaissent), ou une zone de sol par-dessus. Mur : un mur ou les deux, avec sa
    /// couleur si elle est donnée.
    /// </summary>
    public static void Apply(Premises premises, FinishDef finish, FinishTarget target, string? color = null)
    {
        if (finish.Kind == FinishKind.Floor)
        {
            var swatches = color != null ? new Dictionary<string, string> { ["floor"] = color } : new Dictionary<string, string>();
            if (target.WholeFloor && color == null)
            {
                premises.FloorMaterial = finish.Id;
                premises.FloorPatches.Clear();
                return;
            }
            if (target.WholeFloor)
            {
                premises.FloorMaterial = finish.Id;
                premises.FloorPatches.Clear();
                premises.FloorPatches.Add(new FloorPatch { Material = finish.Id, U = 0, V = 0, Width = premises.Width, Depth = premises.Height, Swatches = swatches });
                return;
            }
            // Une zone refaite remplace celles qu'elle recouvre entièrement (pas d'empilement inutile de cases)
            premises.FloorPatches.RemoveAll(p => p.U >= target.U && p.V >= target.V && p.U + p.Width <= target.U + target.Width && p.V + p.Depth <= target.V + target.Depth);
            premises.FloorPatches.Add(new FloorPatch { Material = finish.Id, U = target.U, V = target.V, Width = target.Width, Depth = target.Depth, Swatches = swatches });
            return;
        }
        foreach (string wall in target.Wall is { } w ? [w] : new[] { "left", "right" })
        {
            if (wall == "left")
            {
                premises.LeftPattern = finish.Id;
                if (color != null)
                    premises.LeftWallColor = color;
            }
            else
            {
                premises.RightPattern = finish.Id;
                if (color != null)
                    premises.RightWallColor = color;
            }
        }
        if (target.Wall == null)
            premises.WallPattern = finish.Id;
    }

    /// <summary>La cible porte-t-elle déjà exactement cette finition (et cette couleur) ?</summary>
    public static bool IsCurrent(Premises premises, FinishDef finish, FinishTarget target, string? color)
    {
        if (finish.Kind == FinishKind.Wall)
            return (target.Wall is { } w ? [w] : new[] { "left", "right" })
                .All(wall => Pattern(premises, wall) == finish.Id && (color == null || WallColor(premises, wall) == color));
        if (color != null)
            return false;
        var grid = FloorGrid(premises);
        for (int u = 0; u < premises.Width; u++)
        for (int v = 0; v < premises.Height; v++)
            if ((target.WholeFloor || u >= target.U && u < target.U + target.Width && v >= target.V && v < target.V + target.Depth) && grid[u, v] != finish.Id)
                return false;
        return true;
    }

    /// <summary>Le local porte-t-il déjà cette finition partout ?</summary>
    public static bool IsCurrent(Premises premises, FinishDef finish) =>
        finish.Kind == FinishKind.Floor
            ? FloorGrid(premises).Cast<string?>().All(m => m == finish.Id)
            : Pattern(premises, "left") == finish.Id && Pattern(premises, "right") == finish.Id;

    /// <summary>
    /// Part de froid en plus grâce au faux plancher (0 à 10 %) : à proportion des cases en faux plancher, perforé ou non ;
    /// avec des dalles perforées dans le local, le froid est soufflé là où il sert.
    /// </summary>
    public static double CoolingBonus(Premises premises)
    {
        var cells = FloorGrid(premises).Cast<string?>().ToList();
        if (cells.Count == 0)
            return 0;
        double raised = cells.Count(m => m is "floor_raised" or "floor_raised_vent") / (double)cells.Count;
        return raised * (cells.Contains("floor_raised_vent") ? VentCooling : RaisedCooling);
    }

    /// <summary>
    /// Traitement acoustique des finitions : panneaux acoustiques aux murs du fond et sol textile, à proportion de la surface.
    /// Compté avec les cloisons dans le même plafond de concentration (Layout.MaxFocus).
    /// </summary>
    public static double AcousticFocus(Premises premises)
    {
        var cells = FloorGrid(premises).Cast<string?>().ToList();
        double textile = cells.Count == 0 ? 0 : cells.Count(m => m is "floor_carpet" or "floor_carpet_tiles") / (double)cells.Count;
        return AcousticWalls(premises) * AcousticWallFocus + textile * TextileFloorFocus;
    }

    /// <summary>Murs du fond habillés de panneaux acoustiques (0 à 2).</summary>
    public static int AcousticWalls(Premises premises) => new[] { "left", "right" }.Count(w => Pattern(premises, w) == "pattern_acoustic");

    /// <summary>Ambiance apportée par les murs (lambris), avant le plafond commun d'ambiance.</summary>
    public static double Ambiance(Premises premises) =>
        new[] { "left", "right" }.Count(w => Pattern(premises, w) == "pattern_wood_panels") * PanelAmbiance;
}
