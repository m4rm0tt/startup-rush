namespace Core.space;

public enum FinishKind { Floor, Wall }

/// <summary>
/// Une finition du local : un sol du pack (prix par case, posé sur toute la surface) ou un habillage des deux murs du fond
/// (prix par segment de mur). La plupart ne font que l'identité du lieu ; quelques-unes ont un effet plafonné.
/// </summary>
public record FinishDef(string Id, FinishKind Kind, string Name, decimal Price, string Effect, string? UnlockNode = null);

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

    private const string Decorative = "Décoratif : l'identité du local, sans autre effet.";

    public static readonly IReadOnlyList<FinishDef> Catalog =
    [
        new("floor_concrete", FinishKind.Floor, "Béton ciré", 20m, Decorative),
        new("floor_vinyl", FinishKind.Floor, "Vinyle moucheté", 25m, Decorative),
        new("floor_carpet", FinishKind.Floor, "Moquette", 30m, Decorative),
        new("floor_carpet_tiles", FinishKind.Floor, "Dalles de moquette", 35m, Decorative),
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
        new("pattern_acoustic", FinishKind.Wall, "Panneaux acoustiques", 160m, Decorative),
        new("pattern_wood_panels", FinishKind.Wall, "Lambris", 140m, "Un lieu chaleureux : +1 d'ambiance (dans le plafond de 6)."),
    ];

    public static bool Has(string id) => Catalog.Any(f => f.Id == id);

    public static FinishDef Get(string id) => Catalog.First(f => f.Id == id);

    /// <summary>Les zones de sol du local : tout le local dans le sol refait, sinon celles du plan.</summary>
    public static IReadOnlyList<PlanFloor> FloorZones(Premises premises) =>
        premises.FloorMaterial is { } material
            ? [new PlanFloor(material, 0, 0, premises.Width, premises.Height, new Dictionary<string, string>())]
            : Pack.FindPlan(premises.PlanId)?.Floors ?? [];

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

    /// <summary>Le motif d'un mur du fond (« left » ou « right ») : l'habillage refait, sinon celui du plan.</summary>
    public static string? Pattern(Premises premises, string wall) =>
        premises.WallPattern ?? (Pack.FindPlan(premises.PlanId) is { } plan ? wall == "left" ? plan.LeftPattern : plan.RightPattern : null);

    /// <summary>Segments des deux murs du fond (une grande fenêtre compte pour un).</summary>
    public static int WallSegments(Premises premises) =>
        Pack.FindPlan(premises.PlanId) is { } plan
            ? plan.LeftWall.Count(s => s != null) + plan.RightWall.Count(s => s != null)
            : premises.Width + premises.Height;

    /// <summary>Le prix des travaux dans ce local : par case pour un sol, par segment de mur pour un habillage.</summary>
    public static decimal Cost(Premises premises, FinishDef finish) =>
        finish.Price * (finish.Kind == FinishKind.Floor ? premises.Width * premises.Height : WallSegments(premises));

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

    /// <summary>Ambiance apportée par les murs (lambris), avant le plafond commun d'ambiance.</summary>
    public static double Ambiance(Premises premises) =>
        new[] { "left", "right" }.Count(w => Pattern(premises, w) == "pattern_wood_panels") * PanelAmbiance;
}
