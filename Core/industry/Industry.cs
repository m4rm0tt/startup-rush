using Core.products;
using Core.space;

namespace Core.industry;

/// <summary>Les étapes de fabrication : chaque station du pack en fait une.</summary>
public enum Step { Place, Reflow, Inspect, Assemble, Flash, Pack, Fab, Precision }

/// <summary>Une matière du stock : achetée chez un fournisseur (prix, délai), et parfois fabricable.</summary>
public record Material(string Id, string Name, string Asset, decimal Price, int LeadDays);

/// <summary>
/// Une station de production : l'étape qu'elle fait, son temps par unité, si elle demande un opérateur. « Loader » : chargée
/// et déchargée à la main par ses ports de chargement (outil de traitement des plaquettes), sans convoyeur.
/// </summary>
public record StationDef(string Asset, Step Step, double Minutes, bool Manual, bool Cleanroom = false, bool Loader = false);

/// <summary>Une disposition conseillée : des stations et convoyeurs alignés le long de U, déjà reliés port à port.</summary>
public record LineLayout(string Id, string Name, string Description, (string Asset, int Du, string Facing)[] Parts, string Node);

/// <summary>
/// Une recette : une suite d'étapes, dans l'ordre, que les unités suivent le long d'une ligne reliée ; les matières sont
/// prises au stock du site à la première station. Ce sont des abstractions de jeu, pas des procédés industriels réels.
/// </summary>
public record Route(string Id, string Name, Step[] Steps, IReadOnlyDictionary<string, int> Inputs, string Output, int OutputQty, string? Research);

/// <summary>Un canal de vente du matériel : visibilité, commission, capacité par jour, frais mensuels.</summary>
public record SalesChannel(string Id, string Name, string Pitch, double Visibility, double Commission, double PerDay, decimal MonthlyFee, string? UnlockNode);

public static class IndustryCatalog
{
    public static readonly IReadOnlyList<Material> Materials =
    [
        new("kit", "Composants (lot)", "tote_bin", 3m, 2),
        new("pcb", "Circuit imprimé nu", "product_pcb", 4m, 3),
        new("carte", "Carte électronique", "product_pcb", 28m, 4),
        new("wafer", "Plaquette de silicium", "product_wafer", 1_100m, 10),
        new("puce", "Processeur", "product_cpu", 90m, 6),
        new("memoire", "Module mémoire", "product_ram", 25m, 4),
        new("gpu", "Carte graphique (sous-ensemble)", "product_gpu", 220m, 6),
        new("boitier", "Boîtier", "product_case", 30m, 3),
        new("carton", "Emballage", "product_box", 3m, 1),
    ];

    public static readonly IReadOnlyList<StationDef> Stations =
    [
        new("pick_and_place_machine", Step.Place, 6, false),
        new("reflow_oven", Step.Reflow, 6, false),
        new("aoi_inspection", Step.Inspect, 4, false),
        new("pcb_tester", Step.Inspect, 8, false),
        new("assembly_station_manual", Step.Assemble, 30, true),
        new("assembly_cell_robotic", Step.Assemble, 12, false),
        new("flash_station", Step.Flash, 10, true),
        new("packing_station", Step.Pack, 8, true),
        new("chip_fab_tool", Step.Fab, 240, false, Cleanroom: true, Loader: true),
    ];

    /// <summary>
    /// Dispositions conseillées, réutilisables : chaque station en « sw » (entrée à gauche, sortie à droite), chaque convoyeur en
    /// « se » ; la rangée de devant reste libre pour les opérateurs.
    /// </summary>
    public static readonly IReadOnlyList<LineLayout> Layouts =
    [
        new("assembly", "Ligne d'assemblage", "Assemblage, logiciel, emballage, fin de ligne : les produits finis (10 cases).",
            [("assembly_station_manual", 0, "sw"), ("conveyor_straight", 2, "se"), ("flash_station", 3, "sw"), ("conveyor_straight", 5, "se"),
             ("packing_station", 6, "sw"), ("conveyor_straight", 8, "se"), ("conveyor_end", 9, "se")], "hardware"),
        new("boards", "Ligne de cartes", "Placement, refusion, inspection, fin de ligne : les cartes électroniques (10 cases).",
            [("pick_and_place_machine", 0, "sw"), ("conveyor_straight", 2, "se"), ("reflow_oven", 3, "sw"), ("conveyor_straight", 6, "se"),
             ("aoi_inspection", 7, "sw"), ("conveyor_straight", 8, "se"), ("conveyor_end", 9, "se")], "industry"),
        new("gpu", "Ligne de GPU", "Assemblage puis test : les cartes graphiques (7 cases).",
            [("assembly_station_manual", 0, "sw"), ("conveyor_straight", 2, "se"), ("pcb_tester", 3, "sw"), ("conveyor_straight", 5, "se"),
             ("conveyor_end", 6, "se")], "industry"),
    ];

    /// <summary>Les sous-ensembles fabricables (le reste s'achète). Une recette se débloque par la recherche.</summary>
    public static readonly IReadOnlyList<Route> Routes =
    [
        new("board", "Cartes électroniques", [Step.Place, Step.Reflow, Step.Inspect], new Dictionary<string, int> { ["pcb"] = 1, ["kit"] = 2 }, "carte", 1, "res_smt"),
        new("chip", "Processeurs", [Step.Fab], new Dictionary<string, int> { ["wafer"] = 1 }, "puce", 20, "res_chips"),
        new("gpu", "Cartes graphiques", [Step.Assemble, Step.Inspect], new Dictionary<string, int> { ["puce"] = 1, ["memoire"] = 2, ["carte"] = 1 }, "gpu", 1, "res_gpu"),
    ];

    /// <summary>Étapes du produit fini : assemblage, logiciel, emballage (le carton est pris au stock).</summary>
    public static readonly Step[] ProductSteps = [Step.Assemble, Step.Flash, Step.Pack];

    public static readonly IReadOnlyList<SalesChannel> Channels =
    [
        new("direct", "Vente directe", "Commandes reçues sur ton site : peu de visibilité, aucune commission.", 0.35, 0, 4, 0, null),
        new("shop", "Boutique en ligne", "Une vraie boutique : bien plus visible, 4 % de frais et 150 CHF par mois.", 1.0, 0.04, 60, 150, "online_shop"),
        new("distributors", "Distributeurs", "Les revendeurs : très visibles et gros volumes, mais 25 % de commission.", 1.6, 0.25, 500, 0, "distributors"),
    ];

    /// <summary>Capacité de stockage d'un objet (unités) : racks, palettes, bacs, étagères.</summary>
    public static int StorageCapacity(string asset) => asset switch
    {
        "storage_rack_industrial" => 150,
        "pallet" => 40,
        "tote_bin" => 10,
        "shelf_metal" => 20,
        _ => 0,
    };

    /// <summary>Stock que tout site peut garder sans rangement dédié.</summary>
    public const int BaseStorage = 40;

    /// <summary>Unités qu'un convoyeur droit, courbe, en T ou une table de transfert peut tenir.</summary>
    public const int ConveyorCapacity = 2;

    /// <summary>Automates qu'un opérateur (ou une armoire de contrôle) supervise.</summary>
    public const int AutomatesPerSupervisor = 4;

    /// <summary>Unités par jour qu'un banc de rodage contrôle (−40 % de défauts sur celles-là).</summary>
    public const int StressTestPerDay = 30;

    public static Material Material(string id) => Materials.First(m => m.Id == id);
    public static bool IsMaterial(string id) => Materials.Any(m => m.Id == id);
    public static StationDef? Station(string asset) => Stations.FirstOrDefault(s => s.Asset == asset);
    public static Route? FindRoute(string id) => Routes.FirstOrDefault(r => r.Id == id);
    public static SalesChannel Channel(string id) => Channels.First(c => c.Id == id);

    public static bool IsConveyor(string asset) => asset.StartsWith("conveyor_") && asset != "conveyor_end";

    public static string StepName(Step step) => step switch
    {
        Step.Place => "placement",
        Step.Reflow => "refusion",
        Step.Inspect => "inspection",
        Step.Assemble => "assemblage",
        Step.Flash => "logiciel",
        Step.Pack => "emballage",
        Step.Fab => "traitement des plaquettes",
        _ => "contrôle de précision",
    };

    /// <summary>Le nom lisible d'un article du stock : une matière, ou un produit fini (« p:12 »).</summary>
    public static string ItemName(string item, Func<int, Product?> find) =>
        item.StartsWith("p:") && int.TryParse(item[2..], out int id) ? find(id)?.Name ?? "Produit" : IsMaterial(item) ? Material(item).Name : item;

    /// <summary>La nomenclature d'une unité de ce produit : celle de sa famille, plus ce qu'ajoutent ses composants (par niveau).</summary>
    public static Dictionary<string, int> Bom(Product product)
    {
        var bom = new Dictionary<string, int>(product.Archetype.Bom);
        foreach (var component in product.Components.Where(c => c.Level > 0))
            foreach (var (item, qty) in component.Def.Bom)
                bom[item] = bom.GetValueOrDefault(item) + qty * component.Level;
        bom["carton"] = 1;
        return bom;
    }

    /// <summary>Le coût des matières d'une unité, aux prix d'achat.</summary>
    public static decimal UnitCost(Product product) => Bom(product).Sum(kv => Material(kv.Key).Price * kv.Value);

    /// <summary>La recette du produit fini : ses matières, assemblées, chargées en logiciel, emballées.</summary>
    public static Route ProductRoute(Product product) => new($"product:{product.Id}", product.Name, ProductSteps, Bom(product), $"p:{product.Id}", 1, null);
}

/// <summary>Un nœud du flux : une station, un convoyeur ou une fin de ligne, avec ses voisins reliés port à port.</summary>
public class FlowNode
{
    public required PlacedItem Item { get; init; }
    public StationDef? Station { get; init; }
    public bool End { get; init; }
    public List<int> Next { get; } = new();
    public List<int> Prev { get; } = new();
    public int Capacity => Station != null ? 1 : End ? int.MaxValue : IndustryCatalog.ConveyorCapacity;
}

/// <summary>
/// Le graphe de flux d'un site : deux objets sont reliés quand le port de sortie de l'un donne sur la case d'un port
/// d'entrée de l'autre, orienté vers lui (manifeste du pack, par orientation). Rien n'est relié par simple voisinage.
/// </summary>
public class FlowGraph
{
    public Dictionary<int, FlowNode> Nodes { get; } = new();

    public static FlowGraph Build(Premises site)
    {
        var graph = new FlowGraph();
        var ports = new List<(int Id, PackPort Port, Cell At)>();
        foreach (var item in site.Floor)
        {
            var facing = item.Def.Pack.Facing(item.Facing);
            if (facing.Ports.Count == 0)
                continue;
            graph.Nodes[item.Id] = new FlowNode
            {
                Item = item, Station = IndustryCatalog.Station(item.DefId), End = item.DefId == "conveyor_end",
            };
            foreach (var port in facing.Ports)
                ports.Add((item.Id, port, new Cell(item.X + port.Tile.X, item.Y + port.Tile.Y)));
        }
        foreach (var (a, pa, at) in ports.Where(p => p.Port.Out))
        {
            var target = new Cell(at.X + pa.Dir.X, at.Y + pa.Dir.Y);
            foreach (var (b, pb, bt) in ports.Where(p => p.Id != a && p.Port.In && p.At == target && p.Port.Dir.X == -pa.Dir.X && p.Port.Dir.Y == -pa.Dir.Y))
                if (!graph.Nodes[a].Next.Contains(b))
                {
                    graph.Nodes[a].Next.Add(b);
                    graph.Nodes[b].Prev.Add(a);
                }
        }
        return graph;
    }
}
