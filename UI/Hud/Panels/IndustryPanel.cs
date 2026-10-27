using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Core.campaign;
using Core.industry;
using Core.space;
using Core.team;
using UI.Iso;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>
/// Production et stocks du site affiché : ses lignes et leurs diagnostics, les dispositions conseillées, les ordres de
/// fabrication, le stock (disponible, réservé, en production, en transit) et l'approvisionnement sous plafond.
/// </summary>
public class IndustryPanel : GamePanel
{
    private static int _quantity = 20;

    public override string Title => "Production et stocks";
    public override string Context => Campaign.Premises.Name;
    public override bool Large => true;

    public override Control Build()
    {
        var site = Campaign.Premises;
        var column = Ui.Column(10, Hint("industry", "Une ligne : des stations reliées par des convoyeurs, dans le sens des flèches, jusqu'à une fin de ligne. Il faut des opérateurs."));
        column.Children.Add(Lines(site));
        column.Children.Add(Orders(site));
        column.Children.Add(StockSection(site));
        return column;
    }

    private Control Lines(Premises site)
    {
        var graph = Campaign.SiteFlow(site);
        var stations = graph.Nodes.Values.Where(n => n.Station != null).ToList();
        var operators = Campaign.Staff.Where(p => p.Assignment.Is(AssignmentKind.Production, site.Id)).ToList();
        var column = Ui.Column(4,
            Ui.Muted($"{stations.Count} station(s), {graph.Nodes.Count - stations.Count} convoyeur(s) et fin(s) de ligne. " +
                     $"Opérateurs : {(operators.Count == 0 ? "aucun (fiche d'une personne : « Travaille sur », Production)" : string.Join(", ", operators.Select(p => p.Name)))}.", 13));
        foreach (var node in stations.OrderBy(n => n.Item.Y).ThenBy(n => n.Item.X))
        {
            var status = Campaign.Flow.TryGetValue(node.Item.Id, out var s) ? s : new FlowStatus(false, "En attente");
            column.Children.Add(Ui.Row(6, Ui.Icon(status.Working ? "check" : "warning", 16),
                Ui.Text($"{node.Item.Def.Name} ({IndustryCatalog.StepName(node.Station!.Step)}, {node.Station.Minutes:0} min)", 14, Palette.Cloud, wrap: false),
                Ui.Muted(status.Text, 13)));
        }
        if (Campaign.SiteInfra(site).Issues.Count > 0)
            foreach (string issue in Campaign.SiteInfra(site).Issues)
                column.Children.Add(Ui.Text(issue, 13, Palette.Ember));
        var layouts = new WrapPanel();
        foreach (var layout in IndustryCatalog.Layouts)
        {
            decimal cost = layout.Parts.Sum(p => FurnitureCatalog.Get(p.Asset).Price);
            bool unlocked = Campaign.IsUnlocked(layout.Node);
            var button = Ui.Button($"{layout.Name} ({cost:N0})", () => Screen.Confirm($"Installer : {layout.Name} ?",
                $"{layout.Description} Coût : {cost:N0} CHF. Elle se pose à la première rangée libre du site affiché.", "Installer",
                () => Run(Campaign.InstallLayout(layout.Id)), danger: false), "", unlocked,
                unlocked ? layout.Description : $"À débloquer dans Développement : {Core.progress.DevTree.Node(layout.Node).Name}.");
            button.Margin = new Avalonia.Thickness(0, 0, 6, 6);
            layouts.Children.Add(button);
        }
        var (suggested, count) = Campaign.SuggestedLine(site);
        if (count > 0)
            layouts.Children.Add(Ui.Button($"Ligne du plan ({count} éléments, {suggested:N0})", () => Screen.Confirm("Installer la ligne du plan ?",
                $"Les stations et convoyeurs du plan de ce local, à leur place : {suggested:N0} CHF.", "Installer", () => Run(Campaign.InstallSuggestedLine()), danger: false)));
        column.Children.Add(Ui.Text("Dispositions conseillées", 13, Palette.Fog));
        column.Children.Add(layouts);
        return Ui.Section("Lignes du site", column);
    }

    private Control Orders(Premises site)
    {
        var column = Ui.Column(6);
        foreach (var order in Campaign.Orders.Where(o => o.SiteId == site.Id).ToList())
        {
            var route = Campaign.RouteOf(order);
            if (route == null)
                continue;
            var (ok, why) = Campaign.LineFor(site, route);
            string mode = order.Mode switch
            {
                OrderMode.Quantity => $"{order.Finished}/{order.Quantity * route.OutputQty} faits",
                OrderMode.TargetStock => $"stock cible {order.Quantity}",
                _ => "en continu",
            };
            column.Children.Add(Ui.Card(Ui.Column(3,
                Ui.Row(Ui.Text(route.Name, 15, Palette.Snow, bold: true), Ui.Muted(mode, 13), order.Paused ? Ui.Tag("PAUSE", Palette.Lemon) : null),
                Ui.Muted($"Étapes : {string.Join(" → ", route.Steps.Select(IndustryCatalog.StepName))}. Par unité : {string.Join(", ", route.Inputs.Select(kv => $"{kv.Value} × {IndustryCatalog.Material(kv.Key).Name}"))}.", 12),
                ok ? null : Ui.Text("Ligne incomplète : " + why, 13, Palette.Ember),
                Ui.Row(6, Ui.Button(order.Paused ? "Reprendre" : "Pause", () => Run(Campaign.SetOrder(order.Id, !order.Paused))),
                    Ui.Button("Arrêter", () => Run(Campaign.CancelOrder(order.Id)), "red")))));
        }

        // Nouvel ordre : un produit matériel conçu, ou un sous-ensemble débloqué par la recherche
        var routes = Campaign.Products.Where(p => p.IsHardware).Select(p => ($"product:{p.Id}", p.Name))
            .Concat(IndustryCatalog.Routes.Where(r => r.Research == null || Campaign.IsUnlocked(r.Research)).Select(r => (r.Id, r.Name))).ToList();
        if (routes.Count == 0)
            column.Children.Add(Ui.Muted("Rien à fabriquer : crée un projet matériel (Projets), ou débloque une recette par la recherche.", 13));
        else
        {
            var row = new WrapPanel();
            foreach (var (id, name) in routes)
            {
                var b = Ui.Button(name, () => Run(Campaign.CreateOrder(site.Id, id, OrderMode.Quantity, _quantity)), "gold");
                b.Margin = new Avalonia.Thickness(0, 0, 6, 6);
                row.Children.Add(b);
                var target = Ui.Button("stock cible", () => Run(Campaign.CreateOrder(site.Id, id, OrderMode.TargetStock, _quantity)), "tab");
                target.Margin = new Avalonia.Thickness(0, 0, 10, 6);
                row.Children.Add(target);
            }
            column.Children.Add(Ui.Row(8, Ui.Muted("Quantité :", 13), Ui.Choice([(5, "5"), (20, "20"), (50, "50"), (200, "200")], _quantity, q =>
            {
                _quantity = q;
                Refresh();
            })));
            column.Children.Add(row);
        }
        return Ui.Section("Ordres de fabrication", column);
    }

    private Control StockSection(Premises site)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto,Auto,Auto,Auto"), RowSpacing = 4, ColumnSpacing = 10 };
        int row = 0;
        void Cell(Control c, int col)
        {
            Grid.SetRow(c, row);
            Grid.SetColumn(c, col);
            grid.Children.Add(c);
        }
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        string[] heads = ["", "Article", "Dispo.", "Réservé", "En prod.", "En route", "Réserve auto"];
        for (int i = 0; i < heads.Length; i++)
            Cell(Ui.Text(heads[i], 12, Palette.Fog, wrap: false), i);
        var items = IndustryCatalog.Materials.Select(m => m.Id)
            .Concat(Campaign.Products.Where(p => p.IsHardware).Select(p => $"p:{p.Id}")).ToList();
        foreach (string item in items)
        {
            row++;
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            bool material = IndustryCatalog.IsMaterial(item);
            string asset = material ? IndustryCatalog.Material(item).Asset : "product_box";
            var thumb = PackLibrary.Thumbnail(asset);
            Cell(Ui.Image(thumb, 20, 20), 0);
            Cell(Ui.Text(IndustryCatalog.ItemName(item, Campaign.FindProduct), 14, Palette.Cloud, wrap: false), 1);
            Cell(Ui.Text($"{Campaign.StockAt(site.Id, item)}", 14, Palette.Snow, wrap: false), 2);
            Cell(Ui.Muted(material ? $"{Campaign.Reserved(site.Id, item)}" : "—", 13), 3);
            Cell(Ui.Muted($"{Campaign.InProduction(item)}", 13), 4);
            Cell(Ui.Muted(material ? $"{Campaign.InTransit(site.Id, item)}" : "—", 13), 5);
            if (material)
            {
                int target = Campaign.Reserves.FirstOrDefault(r => r.SiteId == site.Id && r.Item == item)?.Target ?? 0;
                var mat = IndustryCatalog.Material(item);
                Cell(Ui.Row(4,
                    Ui.Choice([(0, "—"), (20, "20"), (100, "100"), (500, "500")], target, t => Run(Campaign.SetReserve(site.Id, item, t))),
                    Ui.Button($"+20 ({mat.Price * 20:N0})", () => Run(Campaign.BuyMaterial(site.Id, item, 20)), "tab", tip: $"{mat.Price:N0} CHF pièce, livré en {mat.LeadDays} j")), 6);
            }
        }
        int used = Campaign.StockUsed(site.Id), capacity = Campaign.StorageCapacity(site);
        var column = Ui.Column(6,
            Ui.Row(8, Ui.Text($"Stock du site : {used}/{capacity} unités", 14, used >= capacity ? Palette.Ember : Palette.Cloud), Ui.Bar(used / (double)System.Math.Max(1, capacity), Palette.Gold, 8, 160)),
            grid,
            Ui.Row(8, Ui.Muted("Plafond des achats automatiques par mois :", 13),
                Ui.Choice([(1_000m, "1 000"), (5_000m, "5 000"), (20_000m, "20 000"), (100_000m, "100 000")], Campaign.ProcurementCap, c => Run(Campaign.SetProcurementCap(c)))),
            Ui.Muted($"Engagé ce mois-ci : {Campaign.ProcurementSpent:N0} CHF. Pour refaire toutes les réserves aujourd'hui : {Campaign.ProcurementPreview():N0} CHF. " +
                     "L'argent part à la commande ; la marchandise arrive après le délai du fournisseur ; ce qui est en route compte.", 12));
        return Ui.Section("Stock et achats", column);
    }
}
