using System.Text.Json.Serialization;
using Core.finance;
using Core.industry;
using Core.products;
using Core.space;
using Core.team;

namespace Core.campaign;

public enum OrderMode { Quantity, TargetStock, Continuous }

/// <summary>Un ordre de fabrication sur un site : une recette, et combien en faire (quantité, stock cible ou en continu).</summary>
public class ProductionOrder
{
    public int Id { get; set; }
    public int SiteId { get; set; }

    /// <summary>« board », « chip », « gpu » (sous-ensembles) ou « product:&lt;id&gt; » (produit fini).</summary>
    public string RouteId { get; set; } = "";

    public OrderMode Mode { get; set; }

    /// <summary>Quantité à faire, ou stock cible.</summary>
    public int Quantity { get; set; }

    public int Started { get; set; }
    public int Finished { get; set; }
    public bool Paused { get; set; }
}

/// <summary>Une unité en cours de fabrication : où elle est (station ou convoyeur), d'où elle vient, quelles étapes sont faites.</summary>
public class FlowUnit
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int Stage { get; set; }
    public int NodeId { get; set; }
    public int FromId { get; set; }

    /// <summary>Minutes de travail restantes à la station.</summary>
    public double Busy { get; set; }

    /// <summary>L'étape de la station est faite : l'unité attend de pouvoir sortir.</summary>
    public bool Done { get; set; }
}

/// <summary>Un achat engagé : l'argent est déjà parti, la marchandise arrive après le délai du fournisseur.</summary>
public class Purchase
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public string Item { get; set; } = "";
    public int Qty { get; set; }
    public long ArrivesAt { get; set; }
    public decimal Cost { get; set; }
    public bool Auto { get; set; }
}

/// <summary>Une réserve à tenir : l'approvisionnement automatique recommande jusqu'à ce niveau (stock + en transit).</summary>
public class ReserveTarget
{
    public int SiteId { get; set; }
    public string Item { get; set; } = "";
    public int Target { get; set; }
}

/// <summary>Où en est une station ou un site : le diagnostic lisible, pour l'interface et les tests.</summary>
public record FlowStatus(bool Working, string Text);

/// <summary>
/// La production : stocks par site (bornés par les rangements), achats avec délai et approvisionnement automatique sous un
/// plafond mensuel, ordres de fabrication, et lignes réelles (Core/industry/Industry.cs) : chaque unité suit les ports
/// et les convoyeurs, une station à la fois, sans téléportation ; une saturation bloque proprement l'étape d'avant.
/// </summary>
public partial class Campaign
{
    public List<ProductionOrder> Orders { get; set; } = new();
    public List<FlowUnit> FlowUnits { get; set; } = new();

    /// <summary>Stock par site : article (matière, ou « p:&lt;id&gt; » pour un produit fini emballé) → quantité.</summary>
    public Dictionary<int, Dictionary<string, int>> Stock { get; set; } = new();

    public List<Purchase> Purchases { get; set; } = new();
    public List<ReserveTarget> Reserves { get; set; } = new();

    /// <summary>Plafond des achats automatiques par mois civil, en CHF.</summary>
    public decimal ProcurementCap { get; set; } = 5_000m;

    /// <summary>Achats automatiques déjà engagés ce mois-ci (remis à zéro le 1er).</summary>
    public decimal ProcurementSpent { get; set; }

    public string ProcurementMonth { get; set; } = "";

    /// <summary>Unités rodées aujourd'hui, par site (bancs de rodage).</summary>
    public Dictionary<int, int> StressTested { get; set; } = new();

    /// <summary>Diagnostic de chaque station et convoyeur (calculé, non sauvegardé).</summary>
    [JsonIgnore]
    public Dictionary<int, FlowStatus> Flow { get; } = new();

    private readonly Dictionary<int, (Premises Site, int Items, FlowGraph Graph)> _graphs = new();

    public FlowGraph SiteFlow(Premises site)
    {
        if (_graphs.TryGetValue(site.Id, out var g) && g.Site == site && g.Items == site.Items.Count)
            return g.Graph;
        var graph = FlowGraph.Build(site);
        _graphs[site.Id] = (site, site.Items.Count, graph);
        return graph;
    }

    // ------------------------------------------------------------------ Stock

    public int StockAt(int siteId, string item) => Stock.TryGetValue(siteId, out var s) ? s.GetValueOrDefault(item) : 0;

    public int TotalStock(string item) => Stock.Values.Sum(s => s.GetValueOrDefault(item));

    public int StorageCapacity(Premises site) => IndustryCatalog.BaseStorage + site.Items.Sum(i => IndustryCatalog.StorageCapacity(i.DefId));

    public int StockUsed(int siteId) => Stock.TryGetValue(siteId, out var s) ? s.Values.Sum() : 0;

    public int StockFree(Premises site) => Math.Max(0, StorageCapacity(site) - StockUsed(site.Id));

    private void AddStock(int siteId, string item, int qty)
    {
        if (!Stock.TryGetValue(siteId, out var s))
            Stock[siteId] = s = new();
        s[item] = s.GetValueOrDefault(item) + qty;
    }

    private bool TakeStock(int siteId, string item, int qty)
    {
        if (StockAt(siteId, item) < qty)
            return false;
        Stock[siteId][item] -= qty;
        if (Stock[siteId][item] == 0)
            Stock[siteId].Remove(item);
        return true;
    }

    /// <summary>Ce qui est en route vers un site (achats engagés pas encore livrés).</summary>
    public int InTransit(int siteId, string item) => Purchases.Where(p => p.SiteId == siteId && p.Item == item).Sum(p => p.Qty);

    /// <summary>Ce que les ordres en cours vont prendre au stock de ce site (pour l'afficher comme réservé).</summary>
    public int Reserved(int siteId, string item) =>
        Orders.Where(o => o.SiteId == siteId && !o.Paused).Sum(o => RouteOf(o) is { } r && r.Inputs.TryGetValue(item, out int q) ? q * Math.Min(Remaining(o), 50) : 0);

    /// <summary>Unités en cours de fabrication qui deviendront cet article.</summary>
    public int InProduction(string item) =>
        FlowUnits.Sum(u => Orders.FirstOrDefault(o => o.Id == u.OrderId) is { } o && RouteOf(o) is { } r && r.Output == item ? r.OutputQty : 0);

    // ------------------------------------------------------------------ Achats

    /// <summary>Achète une matière pour un site : payée tout de suite, livrée après le délai du fournisseur, si la place suffit.</summary>
    public CommandResult BuyMaterial(int siteId, string item, int qty)
    {
        var site = FindSite(siteId);
        if (site == null || !IndustryCatalog.IsMaterial(item) || qty <= 0)
            return CommandResult.Fail("Achat impossible.");
        int room = StockFree(site) - Purchases.Where(p => p.SiteId == siteId).Sum(p => p.Qty);
        if (qty > room)
            return CommandResult.Fail($"Pas assez de place sur le site : {Math.Max(0, room)} unité(s) au plus (ajoute des racks ou des palettes).");
        var material = IndustryCatalog.Material(item);
        decimal cost = material.Price * qty;
        var paid = Spend(cost, Category.Materials, $"Achat : {qty} × {material.Name}");
        if (!paid.Ok)
            return paid;
        Purchases.Add(new Purchase { Id = NewId(), SiteId = siteId, Item = item, Qty = qty, Cost = cost, ArrivesAt = GameTime.StartOfDay(Day + material.LeadDays) });
        return CommandResult.Success($"{qty} × {material.Name} : livraison dans {material.LeadDays} jour(s).");
    }

    /// <summary>Règle la réserve à tenir d'une matière sur un site (0 : plus d'achat automatique).</summary>
    public CommandResult SetReserve(int siteId, string item, int target)
    {
        if (FindSite(siteId) == null || !IndustryCatalog.IsMaterial(item))
            return CommandResult.Fail("Réglage impossible.");
        Reserves.RemoveAll(r => r.SiteId == siteId && r.Item == item);
        if (target > 0)
            Reserves.Add(new ReserveTarget { SiteId = siteId, Item = item, Target = target });
        return CommandResult.Success();
    }

    public CommandResult SetProcurementCap(decimal cap)
    {
        ProcurementCap = Math.Max(0, cap);
        return CommandResult.Success();
    }

    /// <summary>Ce que l'approvisionnement automatique dépenserait aujourd'hui (récapitulatif avant de valider un réglage).</summary>
    public decimal ProcurementPreview() =>
        Reserves.Sum(r => IndustryCatalog.Material(r.Item).Price * Math.Max(0, r.Target - StockAt(r.SiteId, r.Item) - InTransit(r.SiteId, r.Item)));

    /// <summary>
    /// Chaque matin : livraisons arrivées (si la place le permet), puis réapprovisionnement des réserves, sans dépasser le
    /// plafond du mois. L'argent est engagé une seule fois ; ce qui est en route compte (pas de rachat à l'infini).
    /// </summary>
    private void RunSupply()
    {
        if (ProcurementMonth != Month)
        {
            ProcurementMonth = Month;
            ProcurementSpent = 0;
        }
        foreach (var purchase in Purchases.Where(p => p.ArrivesAt <= Minute).ToList())
        {
            var site = FindSite(purchase.SiteId);
            if (site == null)
            {
                purchase.SiteId = Headquarters.Id;
                continue;
            }
            int qty = Math.Min(purchase.Qty, StockFree(site));
            if (qty > 0)
            {
                AddStock(site.Id, purchase.Item, qty);
                purchase.Qty -= qty;
            }
            if (purchase.Qty == 0)
                Purchases.Remove(purchase);
            else
                Notify(Severity.Warning, "production", $"Livraison en attente : {site.Name}", "Plus de place dans le stock : ajoute des racks ou des palettes.", "industry", key: $"stockfull:{site.Id}");
        }

        foreach (var reserve in Reserves.ToList())
        {
            var site = FindSite(reserve.SiteId);
            if (site == null)
                continue;
            int missing = reserve.Target - StockAt(site.Id, reserve.Item) - InTransit(site.Id, reserve.Item);
            int room = StockFree(site) - Purchases.Where(p => p.SiteId == site.Id).Sum(p => p.Qty);
            var material = IndustryCatalog.Material(reserve.Item);
            int byCap = (int)Math.Floor((ProcurementCap - ProcurementSpent) / material.Price);
            int qty = Math.Min(missing, Math.Min(room, byCap));
            if (missing > 0 && qty < missing && byCap < missing)
                Notify(Severity.Info, "production", "Plafond d'achats atteint", $"Les réserves ne sont pas toutes refaites ce mois-ci ({ProcurementCap:N0} CHF).", "industry", key: "procurecap");
            if (qty <= 0)
                continue;
            decimal cost = material.Price * qty;
            if (!Ledger.CanSpend(cost))
                continue;
            Post(-cost, Category.Materials, $"Approvisionnement : {qty} × {material.Name}");
            ProcurementSpent += cost;
            Purchases.Add(new Purchase { Id = NewId(), SiteId = site.Id, Item = reserve.Item, Qty = qty, Cost = cost, Auto = true, ArrivesAt = GameTime.StartOfDay(Day + material.LeadDays) });
        }
        StressTested.Clear();
    }

    // ------------------------------------------------------------------ Ordres

    public Route? RouteOf(ProductionOrder order)
    {
        if (order.RouteId.StartsWith("product:") && int.TryParse(order.RouteId[8..], out int id))
            return FindProduct(id) is { } product && product.IsHardware ? IndustryCatalog.ProductRoute(product) : null;
        return IndustryCatalog.FindRoute(order.RouteId);
    }

    /// <summary>Combien d'unités il reste à lancer pour cet ordre.</summary>
    public int Remaining(ProductionOrder order)
    {
        var route = RouteOf(order);
        if (route == null)
            return 0;
        return order.Mode switch
        {
            OrderMode.Quantity => Math.Max(0, order.Quantity - order.Started),
            OrderMode.TargetStock => Math.Max(0, (int)Math.Ceiling((order.Quantity - TotalStock(route.Output) - InProduction(route.Output)) / (double)route.OutputQty)),
            _ => 1,
        };
    }

    public CommandResult CreateOrder(int siteId, string routeId, OrderMode mode, int quantity)
    {
        var site = FindSite(siteId);
        if (site == null)
            return CommandResult.Fail("Site inconnu.");
        var order = new ProductionOrder { Id = NewId(), SiteId = siteId, RouteId = routeId, Mode = mode, Quantity = Math.Max(1, quantity) };
        var route = RouteOf(order);
        if (route == null)
            return CommandResult.Fail("Recette inconnue.");
        if (route.Research is { } research && !IsUnlocked(research))
            return CommandResult.Fail("Cette recette se débloque par la recherche.");
        Orders.Add(order);
        var (ok, why) = LineFor(site, route);
        return CommandResult.Success(ok ? $"Ordre lancé : {route.Name}." : $"Ordre enregistré, mais {why}");
    }

    public CommandResult SetOrder(int orderId, bool paused, int? quantity = null)
    {
        var order = Orders.FirstOrDefault(o => o.Id == orderId);
        if (order == null)
            return CommandResult.Fail("Ordre inconnu.");
        order.Paused = paused;
        if (quantity is int q)
            order.Quantity = Math.Max(1, q);
        return CommandResult.Success();
    }

    /// <summary>Supprime un ordre : les unités en cours sont terminées d'abord (l'ordre reste en pause tant qu'il en reste).</summary>
    public CommandResult CancelOrder(int orderId)
    {
        var order = Orders.FirstOrDefault(o => o.Id == orderId);
        if (order == null)
            return CommandResult.Fail("Ordre inconnu.");
        if (FlowUnits.Any(u => u.OrderId == orderId))
        {
            order.Mode = OrderMode.Quantity;
            order.Quantity = order.Started;
            return CommandResult.Success("Plus aucune unité ne sera lancée ; celles en cours se terminent.");
        }
        Orders.Remove(order);
        return CommandResult.Success();
    }

    /// <summary>
    /// Le site a-t-il une ligne pour cette recette ? Une station de la première étape sans amont, puis chaque étape suivante
    /// atteignable en suivant les ports, jusqu'à une fin de ligne. Sinon, la raison.
    /// </summary>
    public (bool Ok, string Why) LineFor(Premises site, Route route)
    {
        var graph = SiteFlow(site);
        var heads = graph.Nodes.Values.Where(n => n.Station?.Step == route.Steps[0]).ToList();
        if (heads.Count == 0)
            return (false, $"il manque une station de {IndustryCatalog.StepName(route.Steps[0])}.");
        if (heads.Any(h => h.Station!.Loader && route.Steps.Length == 1))
            return (true, "");
        foreach (var head in heads)
        {
            // Parcours des états (nœud, étape faite) en suivant les sorties
            var seen = new HashSet<(int, int)>();
            var queue = new Queue<(int Node, int Stage)>([(head.Item.Id, 1)]);
            while (queue.Count > 0)
            {
                var (id, stage) = queue.Dequeue();
                if (!seen.Add((id, stage)))
                    continue;
                foreach (int next in graph.Nodes[id].Next)
                {
                    var node = graph.Nodes[next];
                    if (node.End && stage == route.Steps.Length)
                        return (true, "");
                    if (node.Station != null)
                    {
                        var pass = Passage(route, stage, node.Station);
                        if (pass != null)
                            queue.Enqueue((next, pass.Value ? stage + 1 : stage));
                    }
                    else if (!node.End)
                        queue.Enqueue((next, stage));
                }
            }
        }
        return (false, $"la ligne ne relie pas {string.Join(" → ", route.Steps.Select(IndustryCatalog.StepName))} jusqu'à une fin de ligne (ports, sens des convoyeurs).");
    }

    // ------------------------------------------------------------------ Simulation des lignes

    /// <summary>
    /// Ce que fait une station d'une unité arrivée à l'étape « stage » : true, elle la travaille (c'est l'étape suivante) ;
    /// false, elle la laisse passer (son étape n'est plus au programme) ; null, mauvaise entrée (une étape d'avant manque).
    /// </summary>
    private static bool? Passage(Route route, int stage, StationDef station) =>
        stage < route.Steps.Length && station.Step == route.Steps[stage] ? true
        : route.Steps.Skip(stage).Contains(station.Step) ? null
        : false;

    /// <summary>Installe une disposition conseillée sur le site affiché, à la première rangée libre ; tout ou rien.</summary>
    public CommandResult InstallLayout(string layoutId)
    {
        var layout = IndustryCatalog.Layouts.First(l => l.Id == layoutId);
        if (!IsUnlocked(layout.Node))
            return CommandResult.Fail($"À débloquer dans Développement : {progress.DevTree.Node(layout.Node).Name}.");
        decimal cost = layout.Parts.Sum(p => FurnitureCatalog.Get(p.Asset).Price);
        if (!Ledger.CanSpend(cost))
            return CommandResult.Fail($"Il faut {cost:N0} CHF pour cette ligne.");
        var site = Premises;
        for (int v = 0; v < site.Height; v++)
            for (int u = 0; u < site.Width; u++)
            {
                var placed = new List<PlacedItem>();
                bool ok = true;
                foreach (var (asset, du, facing) in layout.Parts)
                {
                    var def = FurnitureCatalog.Get(asset);
                    if (!Layout.CanPlace(site, def, u + du, v, facing).Ok)
                    {
                        ok = false;
                        break;
                    }
                    var temp = new PlacedItem { Id = -1000 - placed.Count, DefId = asset, X = u + du, Y = v, Facing = facing };
                    site.Items.Add(temp);
                    placed.Add(temp);
                }
                foreach (var temp in placed)
                    site.Items.Remove(temp);
                if (!ok)
                    continue;
                foreach (var (asset, du, facing) in layout.Parts)
                    BuyAndPlace(asset, u + du, v, facing);
                return CommandResult.Success($"{layout.Name} installée ({cost:N0} CHF).");
            }
        return CommandResult.Fail("Pas de rangée assez longue et libre (avec la place devant pour les opérateurs).");
    }

    /// <summary>Opérateurs présents en ce moment sur un site (affectés à sa production, à leur poste).</summary>
    public int OperatorsPresent(Premises site) =>
        People.Count(p => p.Assignment.Is(AssignmentKind.Production, site.Id) && p.Activity == Activity.Working);

    /// <summary>Une salle propre complète : sas, 4 cloisons, aspiration, refroidissement.</summary>
    public static bool Cleanroom(Premises site) =>
        site.Items.Any(i => i.DefId == "cleanroom_airlock") && site.Items.Count(i => i.DefId == "cleanroom_partition") >= 4
        && site.Items.Any(i => i.DefId == "dust_extraction") && site.Items.Any(i => i.DefId == "industrial_chiller");

    /// <summary>Toutes les 10 minutes de travail : avancer, déplacer, lancer les unités de chaque ligne.</summary>
    private void RunProduction()
    {
        foreach (var site in Sites)
        {
            var orders = Orders.Where(o => o.SiteId == site.Id).ToList();
            if (orders.Count == 0)
                continue;
            var graph = SiteFlow(site);
            if (graph.Nodes.Count == 0)
                continue;
            var units = FlowUnits.Where(u => graph.Nodes.ContainsKey(u.NodeId)).OrderBy(u => u.Id).ToList();
            foreach (int node in graph.Nodes.Keys)
                Flow[node] = new FlowStatus(false, "En attente");

            // Opérateurs : d'abord la supervision des automates (armoires de contrôle comprises), puis les postes manuels
            int present = OperatorsPresent(site);
            var stations = graph.Nodes.Values.Where(n => n.Station != null).ToList();
            int automates = stations.Count(n => !n.Station!.Manual);
            int cabinets = site.Items.Count(i => i.DefId == "control_cabinet");
            int supervisors = (int)Math.Ceiling(Math.Max(0, automates - cabinets * IndustryCatalog.AutomatesPerSupervisor) / (double)IndustryCatalog.AutomatesPerSupervisor);
            bool automatesOk = present >= supervisors;
            int manualSlots = Math.Max(0, present - supervisors);
            var infra = SiteInfra(site);
            double speed = Math.Max(0.3, infra.Factor);
            bool clean = Cleanroom(site);

            // 1. Travail aux stations
            int manualUsed = 0;
            foreach (var unit in units.Where(u => graph.Nodes[u.NodeId].Station != null && !u.Done))
            {
                var station = graph.Nodes[unit.NodeId].Station!;
                if (station.Cleanroom && !clean)
                {
                    Flow[unit.NodeId] = new FlowStatus(false, "Salle propre incomplète (sas, 4 cloisons, aspiration, refroidissement)");
                    continue;
                }
                if (station.Manual ? manualUsed >= manualSlots : !automatesOk)
                {
                    Flow[unit.NodeId] = new FlowStatus(false, "Opérateur absent");
                    continue;
                }
                if (station.Manual)
                    manualUsed++;
                // Méthodes de la recherche : gabarits (postes manuels), cobotique (cellules robotisées)
                double method = station.Manual && IsUnlocked("res_lean") ? 1.25 : station.Asset == "assembly_cell_robotic" && IsUnlocked("res_cobot") ? 1.3 : 1;
                unit.Busy -= 10 * speed * method;
                Flow[unit.NodeId] = new FlowStatus(true, speed < 1 ? "Ralentie : électricité ou refroidissement insuffisants" : "En marche");
                if (unit.Busy <= 0)
                {
                    unit.Stage++;
                    unit.Done = true;
                }
            }

            // 2. Déplacements : les unités les plus avancées d'abord, pour libérer la place devant
            foreach (var unit in units.OrderByDescending(u => u.Stage).ThenBy(u => u.Id).ToList())
            {
                var node = graph.Nodes[unit.NodeId];
                if (node.Station != null && !unit.Done)
                    continue;
                var order = Orders.FirstOrDefault(o => o.Id == unit.OrderId);
                var route = order == null ? null : RouteOf(order);
                if (order == null || route == null)
                {
                    FlowUnits.Remove(unit);
                    continue;
                }
                // Station chargée à la main : l'unité terminée est déchargée au stock du site
                if (node.Station is { Loader: true } && unit.Stage >= route.Steps.Length)
                {
                    if (Deliver(site, unit, order, route))
                        continue;
                    Flow[unit.NodeId] = new FlowStatus(false, "Stock plein : ajoute des racks ou des palettes");
                    continue;
                }
                if (!TryMove(site, graph, unit, order, route) && node.Station != null)
                    Flow[unit.NodeId] = new FlowStatus(false, node.Next.Count == 0 ? "Sortie non reliée : ajoute un convoyeur et une fin de ligne" : "Sortie bloquée");
            }

            // 3. Lancements : une station libre de la première étape, sans unité qui l'attend en amont, est chargée au stock
            foreach (var head in stations.OrderBy(n => n.Prev.Count).ThenBy(n => n.Item.Id))
            {
                if (FlowUnits.Any(u => u.NodeId == head.Item.Id) || head.Prev.Any(p => FlowUnits.Any(u => u.NodeId == p)))
                    continue;
                foreach (var order in orders.Where(o => !o.Paused).OrderBy(o => o.Id))
                {
                    var route = RouteOf(order);
                    if (route == null || route.Steps[0] != head.Station!.Step || Remaining(order) <= 0)
                        continue;
                    if (route.Inputs.FirstOrDefault(kv => StockAt(site.Id, kv.Key) < kv.Value) is { Key: not null } missing)
                    {
                        Flow[head.Item.Id] = new FlowStatus(false, $"Matière manquante : {IndustryCatalog.Material(missing.Key).Name}");
                        continue;
                    }
                    foreach (var (item, qty) in route.Inputs)
                        TakeStock(site.Id, item, qty);
                    order.Started++;
                    FlowUnits.Add(new FlowUnit { Id = NewId(), OrderId = order.Id, NodeId = head.Item.Id, Busy = head.Station.Minutes });
                    Flow[head.Item.Id] = new FlowStatus(true, "Chargée");
                    break;
                }
            }
        }
    }

    /// <summary>Fait avancer une unité d'un pas : vers une station de la bonne étape, un convoyeur libre, ou une fin de ligne.</summary>
    private bool TryMove(Premises site, FlowGraph graph, FlowUnit unit, ProductionOrder order, Route route)
    {
        var node = graph.Nodes[unit.NodeId];
        foreach (int next in node.Next.Where(n => n != unit.FromId))
        {
            var target = graph.Nodes[next];
            if (target.End)
            {
                if (unit.Stage < route.Steps.Length)
                {
                    Flow[next] = new FlowStatus(false, "Unité incomplète en fin de ligne : une étape manque");
                    continue;
                }
                if (!Deliver(site, unit, order, route))
                {
                    Flow[next] = new FlowStatus(false, "Stock plein : ajoute des racks ou des palettes");
                    continue;
                }
                Flow[next] = new FlowStatus(true, "Unités terminées");
                return true;
            }
            int there = FlowUnits.Count(u => u.NodeId == next);
            if (there >= target.Capacity)
                continue;
            if (target.Station != null)
            {
                var pass = Passage(route, unit.Stage, target.Station);
                if (pass == null)
                {
                    Flow[next] = new FlowStatus(false, $"Mauvaise entrée : cette unité attend d'abord {IndustryCatalog.StepName(route.Steps[unit.Stage])}");
                    continue;
                }
                // Une station dont l'étape n'est plus au programme laisse passer l'unité, sans la travailler
                unit.Busy = pass.Value ? target.Station.Minutes : 0;
                unit.Done = !pass.Value;
            }
            unit.FromId = unit.NodeId;
            unit.NodeId = next;
            return true;
        }
        return false;
    }

    /// <summary>Met une unité terminée au stock du site (si la place le permet) ; une plaquette sans contrôle de précision perd la moitié de ses puces.</summary>
    private bool Deliver(Premises site, FlowUnit unit, ProductionOrder order, Route route)
    {
        int qty = route.Id == "chip" && site.Items.All(i => i.DefId != "precision_station") ? route.OutputQty / 2 : route.OutputQty;
        if (StockFree(site) < qty)
            return false;
        AddStock(site.Id, route.Output, qty);
        order.Finished += qty;
        FlowUnits.Remove(unit);
        if (route.Output.StartsWith("p:"))
            FinishProductUnit(site, route);
        return true;
    }

    /// <summary>
    /// Une unité de produit fini : son risque de panne dépend de la qualité de conception, de la recherche en contrôle
    /// qualité, du rodage disponible sur le site et du réglage de la première série (mini-jeu de calibration).
    /// </summary>
    private void FinishProductUnit(Premises site, Route route)
    {
        if (FindProduct(int.Parse(route.Output[2..])) is not { } product)
            return;
        double quality = product.Components.Where(c => c.Level > 0).Select(c => c.Quality).DefaultIfEmpty(0.5).Average();
        double defect = Tuning.Hardware.BaseDefects * (1.2 - quality) * (IsUnlocked("res_quality") ? 0.7 : 1);
        int racks = site.Items.Count(i => i.DefId == "stress_test_rack");
        if (StressTested.GetValueOrDefault(site.Id) < racks * IndustryCatalog.StressTestPerDay)
        {
            StressTested[site.Id] = StressTested.GetValueOrDefault(site.Id) + 1;
            defect *= 0.6;
        }
        if (product.FirstSeriesLeft > 0)
        {
            product.FirstSeriesLeft--;
            defect *= 1 - 0.5 * product.FirstSeriesScore;
        }
        product.DefectRate = product.Produced == 0 ? defect : product.DefectRate * 0.97 + defect * 0.03;
        product.Produced++;
    }

    /// <summary>Ce que le matériel peut vendre aujourd'hui : stock de tous les sites, canaux actifs (visibilité, capacité, commission).</summary>
    public HardwareSales SalesOf(Product product)
    {
        var channels = product.SalesChannels.Where(c => IndustryCatalog.Channels.Any(x => x.Id == c))
            .Select(IndustryCatalog.Channel).Where(c => c.UnlockNode == null || IsUnlocked(c.UnlockNode)).ToList();
        if (channels.Count == 0)
            channels.Add(IndustryCatalog.Channel("direct"));
        double visibility = channels.Sum(c => c.Visibility);
        double commission = channels.Sum(c => c.Visibility * c.Commission) / Math.Max(0.01, visibility);
        return new HardwareSales(TotalStock($"p:{product.Id}"), visibility, channels.Sum(c => c.PerDay), commission);
    }

    public CommandResult SetChannel(int productId, string channelId, bool on)
    {
        var product = FindProduct(productId);
        if (product == null || !product.IsHardware)
            return CommandResult.Fail("Projet inconnu.");
        var channel = IndustryCatalog.Channel(channelId);
        if (channel.UnlockNode is { } node && !IsUnlocked(node))
            return CommandResult.Fail($"À débloquer dans Développement : {progress.DevTree.Node(node).Name}.");
        if (on)
            product.SalesChannels.Add(channelId);
        else if (product.SalesChannels.Count > 1)
            product.SalesChannels.Remove(channelId);
        return CommandResult.Success();
    }

    /// <summary>Après la journée de ventes : on sort les unités vendues du stock (le plus gros stock d'abord) et on paie les retours.</summary>
    private void SettleHardware(Product product, ProductDay day)
    {
        int sold = (int)day.Sold;
        string item = $"p:{product.Id}";
        foreach (var (siteId, stock) in Stock.OrderByDescending(s => s.Value.GetValueOrDefault(item)).ToList())
        {
            int take = Math.Min(sold, stock.GetValueOrDefault(item));
            if (take > 0)
                TakeStock(siteId, item, take);
            sold -= take;
        }
        // Garanties : les unités qui reviennent en panne coûtent leur réparation (au coût des matières)
        decimal warranty = Math.Round((decimal)(day.Sold * product.DefectRate) * IndustryCatalog.UnitCost(product), 2);
        if (warranty > 0)
            Post(-warranty, Category.Warranty, $"{product.Name} : retours et garanties");
        day.Costs += warranty;
        if (day.Lost >= 1)
            Notify(Severity.Warning, "production", $"{product.Name} en rupture", "Des clients repartent sans rien : fabrique plus, ou ouvre un canal plus grand.", $"product:{product.Id}", key: $"stockout:{product.Id}");
        else if (day.Sold > 0)
            Resolve($"stockout:{product.Id}");
    }

    /// <summary>Frais mensuels des canaux utilisés par au moins un produit en vente, comptés chaque jour.</summary>
    private void ChannelFees()
    {
        var used = Products.Where(p => p.IsLive && p.IsHardware).SelectMany(p => p.SalesChannels).Distinct();
        decimal fees = used.Select(IndustryCatalog.Channel).Sum(c => c.MonthlyFee) / 30;
        if (fees > 0)
            Ledger.Accrue(Category.ChannelFees, Math.Round(fees, 2));
    }

    /// <summary>
    /// Installe la disposition conseillée du plan (la ligne du pack) sur ce site : achète chaque station et convoyeur à sa
    /// place, si la place est libre et l'argent suffit. Renvoie le coût annoncé quand « confirm » est faux.
    /// </summary>
    public (decimal Cost, int Count) SuggestedLine(Premises site)
    {
        var plan = Pack.Plan(site.PlanId);
        var gear = plan.Objects.Where(o => FurnitureCatalog.Get(o.Asset).Kind is FurnitureKind.Station or FurnitureKind.Conveyor
                                           && (FurnitureCatalog.Get(o.Asset).UnlockNode is not { } n || IsUnlocked(n))).ToList();
        return (gear.Sum(o => FurnitureCatalog.Get(o.Asset).Price), gear.Count);
    }

    public CommandResult InstallSuggestedLine()
    {
        var site = Premises;
        var plan = Pack.Plan(site.PlanId);
        int placed = 0;
        foreach (var o in plan.Objects.Where(o => FurnitureCatalog.Get(o.Asset).Kind is FurnitureKind.Station or FurnitureKind.Conveyor))
        {
            var def = FurnitureCatalog.Get(o.Asset);
            if (def.UnlockNode is { } n && !IsUnlocked(n))
                continue;
            if (BuyAndPlace(o.Asset, o.U, o.V, o.Facing).Ok)
                placed++;
        }
        return placed == 0 ? CommandResult.Fail("Rien n'a pu être posé (place prise, argent ou déblocages manquants).")
            : CommandResult.Success($"{placed} élément(s) de la ligne conseillée installé(s).");
    }
}
