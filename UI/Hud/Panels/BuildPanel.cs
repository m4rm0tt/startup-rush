using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Core.campaign;
using Core.space;
using UI.Game;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>Aménagement : catalogue, cartons, état des postes, déménagement.</summary>
public class BuildPanel : GamePanel
{
    public override string Title => "Aménagement";
    public override string Context => Campaign.Premises.Name;

    public override Control Build()
    {
        var premises = Campaign.Premises;
        var column = Ui.Column(8);

        if (Session.Tool is { } tool)
        {
            var def = FurnitureCatalog.Get(tool.DefId);
            string what = tool.MovingItemId != null ? $"Déplacer : {def.Name}" : tool.StoredId != null ? $"Poser depuis les cartons : {def.Name}" : $"Poser : {def.Name} ({def.Price:N0} CHF)";
            column.Children.Add(Ui.Card(Ui.Column(4,
                Ui.Text(what, 16, Palette.Gold, bold: true),
                Ui.Muted(def.OnDesk ? "Clique sur un bureau." : def.Kind == FurnitureKind.Desk
                    ? "Clique sur une case libre : la case au-dessus (pointillés) reste pour la chaise."
                    : "Clique sur une case libre. Vert : possible, rouge : impossible."),
                Ui.Muted("Clic droit ou Échap pour arrêter."),
                Ui.Button("Arrêter", () => Session.SetTool(null))), Palette.Gold));
        }

        // Postes de travail
        var desks = premises.Desks.ToList();
        int complete = desks.Count(d => Layout.Check(premises, d) == WorkstationIssue.None);
        var stations = Ui.Column(4);
        foreach (var desk in desks)
        {
            var issue = Layout.Check(premises, desk);
            var owner = desk.AssignedPersonId is int id ? Campaign.FindPerson(id)?.Name : null;
            var line = Ui.Row(Ui.Icon(issue == WorkstationIssue.None ? "check" : "warning", 18),
                Ui.Text($"{(owner ?? "Libre")} — {Layout.Describe(issue)}", 14, issue == WorkstationIssue.None ? Palette.Cloud : Palette.Ember));
            var button = new Button { Content = line, Padding = new Thickness(4, 2) };
            button.Classes.Add("plain");
            button.Click += (_, _) => Session.Select(SelectionKind.Item, desk.Id);
            stations.Children.Add(button);
        }
        if (desks.Count == 0)
            stations.Children.Add(Ui.Muted("Aucun bureau."));
        int people = Campaign.People.Count;
        column.Children.Add(Ui.Section($"Postes : {complete} complet(s) pour {people} personne(s)", stations,
            Ui.Muted("Un poste = un bureau, une chaise juste derrière, un ordinateur dessus, et un accès libre. Clique un bureau pour l'attribuer.")));

        // Catalogue
        foreach (var group in FurnitureCatalog.Buyable.Concat([FurnitureCatalog.Get("laptop")]).Where(d => d.Price > 0).GroupBy(d => d.Kind switch
                 {
                     FurnitureKind.Desk or FurnitureKind.Chair or FurnitureKind.Computer => "Postes de travail",
                     FurnitureKind.Coffee or FurnitureKind.Sofa => "Pauses",
                     FurnitureKind.Whiteboard or FurnitureKind.Bookshelf => "Organisation",
                     _ => "Ambiance"
                 }))
        {
            var wrap = new WrapPanel();
            foreach (var def in group)
                wrap.Children.Add(CatalogButton(def));
            column.Children.Add(Ui.Section(group.Key, wrap));
        }

        // Cartons
        if (Campaign.Storage.Count > 0)
        {
            var stored = Ui.Column(4);
            foreach (var item in Campaign.Storage)
            {
                var def = FurnitureCatalog.Get(item.DefId);
                var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto") };
                grid.Children.Add(Ui.Text(def.Name, 14));
                var place = Ui.Button("Poser", () => Session.SetTool(new BuildTool(def.Id, StoredId: item.Id)), "green");
                Grid.SetColumn(place, 1);
                grid.Children.Add(place);
                decimal value = Campaign.ResaleValue(def);
                var sell = Ui.Button(value > 0 ? $"Vendre {value:N0}" : "Jeter", () => Run(Campaign.SellStored(item.Id)), "red");
                Grid.SetColumn(sell, 2);
                sell.Margin = new Thickness(4, 0, 0, 0);
                grid.Children.Add(sell);
                stored.Children.Add(grid);
            }
            column.Children.Add(Ui.Section($"Cartons ({Campaign.Storage.Count})", stored));
        }

        column.Children.Add(Ui.Section("Locaux",
            Ui.Muted($"{premises.Name} : {premises.Width} × {premises.Height} cases, loyer {(premises.Rent > 0 ? Ui.Chf(premises.Rent) : "compris dans ta rémunération")}."),
            Ui.Button("Voir les locaux à louer", () => Open(new PremisesPanel()))));
        return column;
    }

    private Control CatalogButton(FurnitureDef def)
    {
        var image = Atlas.Cropped(def.Id);
        var content = Ui.Column(2,
            image != null ? Ui.Image(image, 48, 40) : null,
            Ui.Text(def.Name, 13, Palette.Snow, wrap: true),
            Ui.Number($"{def.Price:N0} CHF", 16, Campaign.Ledger.CanSpend(def.Price) ? Palette.Lemon : Palette.Blood));
        content.Width = 96;
        foreach (var child in content.Children)
            child.HorizontalAlignment = HorizontalAlignment.Center;
        bool selected = Session.Tool is { MovingItemId: null, StoredId: null } t && t.DefId == def.Id;
        var button = new Button { Content = content, Margin = new Thickness(0, 0, 4, 4), Padding = new Thickness(4, 4, 4, 8) };
        button.Classes.Add("tab");
        if (selected)
            button.Classes.Add("selected");
        ToolTip.SetTip(button, $"{def.Name} — {def.Effect} Emprise : {def.Width} × {def.Height} case(s).");
        button.Click += (_, _) =>
        {
            Session.SetTool(selected ? null : new BuildTool(def.Id));
            Refresh();
        };
        return button;
    }
}

/// <summary>Les locaux à louer, et ce que coûte un déménagement.</summary>
public class PremisesPanel : GamePanel
{
    public override string Title => "Locaux à louer";

    public override Control Build()
    {
        var column = Ui.Column(10, Ui.Muted("En déménageant, tous tes meubles partent dans les cartons : rien n'est perdu ni à racheter. " +
                                            "La garantie du local actuel est rendue, celle du nouveau est bloquée."));
        foreach (var listing in RealEstate.Listings)
        {
            bool current = listing.Id == Campaign.Premises.ListingId;
            var (upfront, refund) = Campaign.MoveCost(listing);
            var card = Ui.Column(4,
                Ui.Row(Ui.Text(listing.Name, 17, Palette.Snow, bold: true), current ? Ui.Tag("ICI", Palette.Leaf) : null),
                Ui.Muted(listing.Description),
                Ui.Pair("Surface", $"{listing.Width} × {listing.Height} cases"),
                Ui.Pair("Loyer mensuel", listing.Rent > 0 ? Ui.Chf(listing.Rent) : "compris"),
                Ui.Pair("Garantie", Ui.Chf(listing.Deposit)),
                current ? null : Ui.Pair("À payer pour emménager", Ui.Chf(upfront - refund), Campaign.Ledger.CanSpend(upfront - refund) ? Palette.Lemon : Palette.Blood),
                current ? null : Ui.Button("Emménager", () => Screen.Confirm($"Emménager : {listing.Name} ?",
                    $"Coût immédiat : {upfront - refund:N0} CHF (garantie {listing.Deposit:N0}, déménageurs, transport des meubles, moins la garantie rendue). " +
                    $"Loyer : {listing.Rent:N0} CHF par mois. Les postes devront être réinstallés depuis les cartons.",
                    "Emménager", () =>
                    {
                        Run(Campaign.MoveTo(listing.Id));
                        Screen.OpenRoot(new BuildPanel());
                    }, danger: false), "gold"));
            column.Children.Add(Ui.Card(card, current ? Palette.Leaf : null));
        }
        return column;
    }
}
