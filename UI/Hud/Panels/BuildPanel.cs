using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Core.campaign;
using Core.space;
using UI.Game;
using UI.Iso;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>Aménagement : état des postes, confort du local, catalogue par rubrique, cartons, déménagement.</summary>
public class BuildPanel : GamePanel
{
    /// <summary>Rubrique du catalogue ouverte (gardée le temps de la session).</summary>
    private static string _group = "Bureaux et chaises";

    private static string GroupOf(FurnitureDef def) => def.Kind switch
    {
        FurnitureKind.Desk or FurnitureKind.Chair => "Bureaux et chaises",
        FurnitureKind.Laptop or FurnitureKind.Tower or FurnitureKind.Monitor or FurnitureKind.Keyboard or FurnitureKind.Peripheral => "Informatique",
        FurnitureKind.Coffee or FurnitureKind.Lounge or FurnitureKind.Kitchen or FurnitureKind.Refreshment => "Pauses",
        FurnitureKind.Whiteboard or FurnitureKind.Bookshelf or FurnitureKind.Meeting or FurnitureKind.Reception => "Organisation",
        FurnitureKind.Storage or FurnitureKind.Sanitary => "Confort de base",
        FurnitureKind.Partition => "Cloisons",
        FurnitureKind.Personal => "Objets personnels",
        FurnitureKind.Rack or FurnitureKind.RackModule or FurnitureKind.Server => "Serveurs et baies",
        FurnitureKind.Lab => "Laboratoire",
        FurnitureKind.Studio => "Studio audiovisuel",
        FurnitureKind.Xr => "Zone XR",
        FurnitureKind.Quantum => "Quantique",
        FurnitureKind.Station or FurnitureKind.Conveyor or FurnitureKind.Warehouse or FurnitureKind.Facility => "Production",
        FurnitureKind.Utility or FurnitureKind.Backup or FurnitureKind.Cabling or FurnitureKind.Supervision => "Énergie et réseau",
        _ => "Ambiance"
    };
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
                Ui.Muted(def.Pack.RackModule ? "Clique sur une baie : le module prend les premiers emplacements libres (en U)."
                    : def.Attached ? "Clique sur un bureau ou un meuble qui peut le recevoir : il prend le premier emplacement libre."
                    : def.OnWall ? "Clique une case le long d'un mur du fond (dans le coin, R choisit le mur)."
                    : def.Kind == FurnitureKind.Desk
                        ? "Clique sur une case libre : la case en pointillés, devant, reste libre pour s'asseoir."
                        : "Clique sur une case libre. Vert : possible, rouge : impossible."),
                Ui.Muted(def.Attached ? "Clic droit ou Échap pour arrêter." : "R : tourner. Clic droit ou Échap pour arrêter."),
                Ui.Button("Arrêter", () => Session.SetTool(null))), Palette.Gold));
        }

        // Postes de travail
        var posts = premises.Workstations.ToList();
        int complete = posts.Count(p => Layout.Check(premises, p) == WorkstationIssue.None);
        var stations = Ui.Column(4);
        foreach (var post in posts)
        {
            var desk = post.Desk;
            var issue = Layout.Check(premises, post);
            var owner = premises.ChairOf(post)?.AssignedPersonId is int id ? Campaign.FindPerson(id)?.Name : null;
            var line = Ui.Row(Ui.Icon(issue == WorkstationIssue.None ? "check" : "warning", 18),
                Ui.Text($"{(owner ?? "Libre")} — {Layout.Describe(issue)}", 14, issue == WorkstationIssue.None ? Palette.Cloud : Palette.Ember));
            var button = new Button { Content = line, Padding = new Thickness(4, 2) };
            button.Classes.Add("plain");
            button.Click += (_, _) => Session.Select(SelectionKind.Item, desk.Id);
            stations.Children.Add(button);
        }
        if (posts.Count == 0)
            stations.Children.Add(Ui.Muted("Aucun bureau."));
        int people = Campaign.People.Count;
        column.Children.Add(Ui.Section($"Postes : {complete} complet(s) pour {people} personne(s)", stations,
            Ui.Muted("Un poste = un bureau, une chaise, un ordinateur (un portable, ou une tour avec écran et clavier) et une case libre devant. Clique un bureau pour l'attribuer.")));

        var infra = Campaign.SiteInfra(premises);
        if (infra.Any || RealEstate.Get(premises.ListingId).Technical)
            column.Children.Add(InfraSection(infra));
        if (!RealEstate.Get(premises.ListingId).Technical)
            column.Children.Add(ComfortSection());
        column.Children.Add(FinishesSection());
        column.Children.Add(ModulesSection());

        // Catalogue, une rubrique à la fois
        var groups = FurnitureCatalog.Buyable.GroupBy(GroupOf).ToList();
        var wrap = new WrapPanel();
        foreach (var def in groups.First(g => g.Key == _group))
            wrap.Children.Add(CatalogButton(def));
        column.Children.Add(Ui.Section("Catalogue",
            Ui.Choice(groups.Select(g => (g.Key, g.Key)), _group, g =>
            {
                _group = g;
                Refresh();
            }),
            wrap));

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
                decimal value = Campaign.ResaleValue(item.PaidPrice);
                var sell = Ui.Button(value > 0 ? $"Vendre {value:N0}" : "Jeter", () => Run(Campaign.SellStored(item.Id)), "red");
                Grid.SetColumn(sell, 2);
                sell.Margin = new Thickness(4, 0, 0, 0);
                grid.Children.Add(sell);
                stored.Children.Add(grid);
            }
            column.Children.Add(Ui.Section($"Cartons ({Campaign.Storage.Count})", stored));
        }

        column.Children.Add(Ui.Section("Sites",
            Ui.Muted($"{premises.Name} : {premises.Width} × {premises.Height} cases, {premises.Workstations.Count()} poste(s) installé(s), loyer {(premises.Rent > 0 ? Ui.Chf(premises.Rent) : "compris dans ta rémunération")}." +
                     (Campaign.Sites.Count > 1 ? $" {Campaign.Sites.Count} sites en tout." : "")),
            Ui.Row(6, Ui.Button(Campaign.Sites.Count > 1 ? "Tous les sites, locations" : "Locaux à louer", () => Open(new SitesPanel())),
                Campaign.IsUnlocked("hardware") ? Ui.Button("Production et stocks", () => Open(new IndustryPanel())) : null)));
        return column;
    }

    /// <summary>L'infrastructure du site affiché : ce qu'elle fournit, ce qui la limite (électricité, froid, réseau), ses risques.</summary>
    private Control InfraSection(InfraSummary infra)
    {
        var column = Ui.Column(3,
            Ui.Pair("Hébergement", $"{infra.Hosting:0.#} unité(s)"),
            Ui.Pair("Calcul · GPU", $"{infra.Compute:0.#} · {infra.Gpu:0.#}"),
            Ui.Pair("Stockage", $"{infra.Storage:0.#} To"),
            infra.QuantumUnits > 0 ? Ui.Pair("Quantique", $"{Campaign.QuantumOnline(Campaign.Premises)} en service sur {infra.QuantumUnits} installation(s)") : null,
            Ui.Pair("Électricité", $"{infra.PowerDraw:0.#} kW utilisés sur {infra.PowerSupply:0.#}", infra.PowerDraw > infra.PowerSupply ? Palette.Ember : null),
            Ui.Pair("Froid", $"{infra.Heat:0.#} kW de chaleur, {infra.Cooling:0.#} évacués", infra.Heat > infra.Cooling ? Palette.Ember : null),
            Ui.Pair("Réseau", $"{infra.NetworkNeeded:0} demandés sur {infra.Network:0}", infra.NetworkNeeded > infra.Network ? Palette.Ember : null),
            Ui.Pair("Câblage", $"{infra.Organization:P0} organisé"),
            Ui.Pair("Onduleurs", $"{infra.BackupCoverage:P0} de la consommation couverte"),
            Ui.Pair("Risque d'incident", $"{infra.IncidentChance:P1} par jour"),
            Ui.Pair("Électricité et réseau", $"≈ {(decimal)infra.KwhPerDay * 30 * Infrastructure.KwhPrice + infra.MonthlyCost:N0} CHF / mois"));
        column.Margin = new Thickness(0, 0, 14, 0);
        foreach (string issue in infra.Issues)
            column.Children.Add(Ui.Text(issue, 13, Palette.Ember));
        column.Children.Add(Ui.Muted("Une baie a besoin d'un switch ; les modules se posent en cliquant la baie. Le froid et l'armoire électrique limitent ce qu'un site fournit.", 12));
        return Ui.Section("Infrastructure du site", column);
    }

    /// <summary>
    /// Ce que l'aménagement apporte, indicateur par indicateur, avec son plafond : la synthèse du local, et sur demande
    /// les objets qui y contribuent. Un manque prive seulement d'un bonus.
    /// </summary>
    private Control ComfortSection()
    {
        var c = Campaign.SiteComfort();
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), RowSpacing = 4, ColumnSpacing = 8 };
        int row = 0;
        void Line(string icon, string label, double fraction, string value, string tip)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var name = Ui.Row(4, Ui.Icon(icon, 16), Ui.Text(label, 14, Palette.Cloud, wrap: false));
            ToolTip.SetTip(name, tip);
            var bar = Ui.Bar(fraction, fraction >= 0.999 ? Palette.Leaf : Palette.Gold, 6);
            bar.VerticalAlignment = VerticalAlignment.Center;
            var number = Ui.Text(value, 14, Palette.Snow, wrap: false);
            Grid.SetRow(name, row);
            Grid.SetRow(bar, row);
            Grid.SetColumn(bar, 1);
            Grid.SetRow(number, row);
            Grid.SetColumn(number, 2);
            grid.Children.Add(name);
            grid.Children.Add(bar);
            grid.Children.Add(number);
            row++;
        }
        Line("heart", "Ambiance", c.Ambiance / Layout.MaxAmbiance, $"+{c.Ambiance:0.#} moral", "Plantes, lampes, tapis, affiches, trophées. 6 de moral au plus.");
        Line("people", "Détente", c.Lounge / Layout.MaxLounge, $"+{c.Lounge:0.#} moral", "Canapés et fauteuils. 4 de moral au plus.");
        Line("check", "Confort de base", c.Basics / Layout.MaxBasics, $"+{c.Basics:0.#} moral",
            $"Rangement pour {c.StorageCoverage:P0} de l'équipe, sanitaires {c.HygieneCoverage:P0}. 3 de moral au plus.");
        Line("bolt", "Pause", (c.Break - Layout.BaseBreak) / (Layout.MaxBreak - Layout.BaseBreak), $"{c.Break:0.#} énergie",
            "Une boisson ou une collation, et une place assise, pour chacun tant qu'il reste des places. 3 sans rien, 15 au plus.");
        Line("screen", "Concentration", c.Focus / Layout.MaxFocus, $"+{c.Focus:P0}", "Production en plus grâce aux cloisons, selon les personnes couvertes. 4 % au plus.");
        Line("crown", "Coordination", c.Coordination / Layout.MaxCoordination, $"+{System.Math.Floor(c.Coordination):0} pers.",
            "Personnes de plus par équipe sans perte : tableaux, planning, tables de réunion avec leurs chaises. 3 au plus.");
        Line("calendar", "Formation", c.Training / Layout.MaxTraining, $"+{c.Training:P0}", "Bibliothèques, livres, schémas, écran de présentation. 40 % au plus.");
        if (c.Recruits > 0)
            Line("user", "Accueil", 1, $"+{c.Recruits} candidat", "Une banque d'accueil : un candidat de plus chaque semaine.");

        // Qui contribue à quoi, sur demande
        var contributors = Ui.Column(2);
        foreach (var group in Campaign.Premises.Items.Select(i => i.Def)
                     .Where(d => d.Kind is not (FurnitureKind.Decor or FurnitureKind.Desk or FurnitureKind.Laptop or FurnitureKind.Tower
                         or FurnitureKind.Monitor or FurnitureKind.Keyboard or FurnitureKind.Chair or FurnitureKind.Kitchen))
                     .GroupBy(GroupOf).OrderBy(g => g.Key))
            contributors.Children.Add(Ui.Muted($"{group.Key} : " + string.Join(", ", group.GroupBy(d => d.Name).Select(d => d.Count() > 1 ? $"{d.Key} ×{d.Count()}" : d.Key)), 13));
        if (contributors.Children.Count == 0)
            contributors.Children.Add(Ui.Muted("Rien pour l'instant.", 13));
        string title = $"Confort du local ({c.People} personne{(c.People > 1 ? "s" : "")})";
        return Ui.Section(title, grid, Ui.Details("Ce qui y contribue", contributors));
    }

    /// <summary>
    /// Finitions du site affiché : le sol et l'habillage des murs du fond, en pastilles du pack (hud_pattern_cell). Ce qui est
    /// en place est sélectionné ; les travaux se confirment (ils se paient par case ou par segment de mur).
    /// </summary>
    private Control FinishesSection()
    {
        var premises = Campaign.Premises;
        var wall = new Dictionary<string, string>(Pack.FindPlan(premises.PlanId)?.LeftSwatches ?? new Dictionary<string, string>());
        WrapPanel Cells(FinishKind kind)
        {
            var wrap = new WrapPanel();
            foreach (var finish in Finishes.Catalog.Where(f => f.Kind == kind))
                wrap.Children.Add(FinishCell(finish, wall));
            // Revenir à ce que prévoit le plan (zones de service, motifs d'origine), sans frais
            bool redone = kind == FinishKind.Floor ? premises.FloorMaterial != null : premises.WallPattern != null;
            var back = Ui.Button(kind == FinishKind.Floor ? "Sols du plan" : "Murs du plan", () => Screen.Confirm(
                kind == FinishKind.Floor ? "Remettre les sols du plan ?" : "Remettre les murs du plan ?",
                "Le local retrouve ce que prévoit son plan. Les travaux déjà payés ne sont pas remboursés.", "Remettre",
                () => Run(Campaign.ResetFinish(kind)), danger: false), "tab", redone);
            back.VerticalAlignment = VerticalAlignment.Center;
            wrap.Children.Add(back);
            return wrap;
        }
        string Name(string? id) => id != null && Finishes.Has(id) ? Finishes.Get(id).Name : "celui du plan";
        string floor = premises.FloorMaterial is { } m ? Name(m) : "ceux du plan";
        string left = Name(Finishes.Pattern(premises, "left")), right = Name(Finishes.Pattern(premises, "right"));
        return Ui.Section("Finitions",
            Ui.Text($"Sol : {floor}", 14, Palette.Cloud), Cells(FinishKind.Floor),
            Ui.Text(left == right ? $"Murs : {left}" : $"Murs : {left} à gauche, {right} à droite", 14, Palette.Cloud), Cells(FinishKind.Wall),
            Ui.Muted("Le faux plancher distribue mieux le froid existant ; le lambris ajoute de l'ambiance ; la moquette et les panneaux acoustiques " +
                     "aident la concentration (plafond commun avec les cloisons). Le reste fait l'identité du local.", 12));
    }

    /// <summary>
    /// Les modules de pièce du pack : un coin prêt à poser (kitchenette, détente, réunion, îlot, accueil, sanitaires), acheté
    /// objet par objet au prix du catalogue avec son sol ; ses meubles donnent leurs effets habituels.
    /// </summary>
    private Control ModulesSection()
    {
        var wrap = new WrapPanel();
        foreach (var module in Pack.Modules)
        {
            decimal cost = Core.campaign.Campaign.ModuleCost(module);
            string? locked = Campaign.ModuleLock(module);
            var kinds = module.Objects.Select(o => FurnitureCatalog.Get(o.Asset)).GroupBy(d => d.Name).Select(g => g.Count() > 1 ? $"{g.Count()} × {g.Key}" : g.Key);
            string content = string.Join(", ", kinds);
            var card = Ui.Card(Ui.Column(4,
                Ui.Text(module.Name, 14, Palette.Snow, bold: true),
                Ui.Muted($"{module.Width} × {module.Depth} cases · {cost:N0} CHF", 12),
                Ui.Muted(content, 12),
                Ui.Button("Installer", () => Screen.Confirm($"Installer : {module.Name} ?",
                        $"{content}. {(module.Floors.Count > 0 ? "Avec son sol. " : "")}Coût : {cost:N0} CHF. Il se pose à la première place libre du site affiché" +
                        $"{(module.AgainstWall != null ? $", contre le mur {(module.AgainstWall == "left" ? "gauche" : "droit")} si possible" : "")}.",
                        "Installer", () => Run(Campaign.InstallModule(module.Id)), danger: false),
                    locked == null && Campaign.Ledger.CanSpend(cost) ? "gold" : "", locked == null, locked)));
            card.Width = 250;
            card.Margin = new Thickness(0, 0, 8, 8);
            wrap.Children.Add(card);
        }
        return Ui.Section("Modules prêts à poser", wrap,
            Ui.Muted("Chaque objet est acheté au prix du catalogue et donne ses effets habituels (pause, coordination, postes, confort).", 12));
    }

    private Control FinishCell(FinishDef finish, Dictionary<string, string> wall)
    {
        var premises = Campaign.Premises;
        bool current = Finishes.IsCurrent(premises, finish);
        bool locked = finish.UnlockNode is { } node && !Campaign.IsUnlocked(node);
        decimal cost = Finishes.Cost(premises, finish);
        bool floor = finish.Kind == FinishKind.Floor;
        var frame = Ui.Image(PackSkins.Sprite("hud_pattern_cell", current ? "selected" : "normal"), 52, 52);
        var tile = Ui.Image(PackLibrary.FinishTile(finish.Id, floor ? new Dictionary<string, string>() : RoomRenderer.PatternColors(finish.Id, wall)), 36, 36);
        tile.Margin = new Thickness(8);
        tile.HorizontalAlignment = HorizontalAlignment.Left;
        tile.VerticalAlignment = VerticalAlignment.Top;
        var content = new Panel { Width = 52, Height = 52, Children = { frame, tile }, Opacity = locked ? 0.5 : 1 };
        var button = new Button { Content = content, Padding = new Thickness(0), Margin = new Thickness(0, 0, 4, 4), IsEnabled = !locked };
        button.Classes.Add("plain");
        if (!current)
        {
            button.PointerEntered += (_, _) => frame.Source = PackSkins.Sprite("hud_pattern_cell", "hover");
            button.PointerExited += (_, _) => frame.Source = PackSkins.Sprite("hud_pattern_cell", "normal");
        }
        string unit = floor ? $"{finish.Price:N0} CHF la case, {premises.Width * premises.Height} cases" : $"{finish.Price:N0} CHF le segment, {Finishes.WallSegments(premises)} segments";
        ToolTip.SetTip(button, locked ? $"{finish.Name} — à débloquer : {Core.campaign.Campaign.UnlockName(finish.UnlockNode!)}."
            : current ? $"{finish.Name} — en place. {finish.Effect}"
            : $"{finish.Name} — {finish.Effect} {cost:N0} CHF ({unit}).");
        button.Click += (_, _) =>
        {
            if (current)
                return;
            Screen.Confirm(floor ? $"Refaire le sol : {finish.Name} ?" : $"Habiller les murs : {finish.Name} ?",
                $"{finish.Effect} Coût : {cost:N0} CHF ({unit}). {(floor ? "Tout le sol est refait, zones de service comprises." : "Les deux murs du fond sont refaits.")}",
                "Faire les travaux", () => Run(Campaign.SetFinish(finish.Id)), danger: false);
        };
        return button;
    }

    private Control CatalogButton(FurnitureDef def)
    {
        var image = PackLibrary.Thumbnail(def.Id);
        double scale = System.Math.Min(48.0 / image.PixelSize.Width, 40.0 / image.PixelSize.Height);
        var content = Ui.Column(2,
            Ui.Image(image, image.PixelSize.Width * System.Math.Min(1, scale), image.PixelSize.Height * System.Math.Min(1, scale)),
            Ui.Text(def.Name, 13, Palette.Snow, wrap: true),
            Ui.Number($"{def.Price:N0} CHF", 16, Campaign.Ledger.CanSpend(def.Price) ? Palette.Lemon : Palette.Blood));
        content.Width = 96;
        foreach (var child in content.Children)
            child.HorizontalAlignment = HorizontalAlignment.Center;
        bool selected = Session.Tool is { MovingItemId: null, StoredId: null } t && t.DefId == def.Id;
        bool locked = def.UnlockNode is { } node && !Campaign.IsUnlocked(node);
        var button = new Button { Content = content, Margin = new Thickness(0, 0, 4, 4), Padding = new Thickness(4, 4, 4, 8), IsEnabled = !locked };
        if (locked)
            content.Opacity = 0.5;
        button.Classes.Add("tab");
        if (selected)
            button.Classes.Add("selected");
        var cells = Premises.Footprint(def, 0, 0, def.Pack.DefaultFacing).Count();
        ToolTip.SetTip(button, def.Attached ? $"{def.Name} — {def.Effect} Se pose sur un meuble."
            : def.OnWall ? $"{def.Name} — {def.Effect} Se fixe sur un mur du fond."
            : def.Pack.Ceiling ? $"{def.Name} — {def.Effect} Au plafond."
            : $"{def.Name} — {def.Effect} Emprise : {cells} case(s).");
        if (locked)
            ToolTip.SetTip(button, $"{def.Name} — à débloquer : {Core.campaign.Campaign.UnlockName(def.UnlockNode!)}.");
        button.Click += (_, _) =>
        {
            Session.SetTool(selected ? null : new BuildTool(def.Id));
            Refresh();
        };
        return button;
    }
}
