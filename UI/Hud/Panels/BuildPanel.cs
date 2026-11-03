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

    /// <summary>Recherche dans le catalogue (2 lettres au moins), retenue tant que le jeu est ouvert.</summary>
    private static string _search = "";

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

    /// <summary>Ouvre l'aménagement directement sur les cartons (après un déménagement, depuis les cartons de la scène).</summary>
    public static BuildPanel AtCartons() => new() { FocusTag = CartonsTag };

    public const string CartonsTag = "section:cartons";

    /// <summary>
    /// L'objet en cours de pose reste en vue au-dessus du catalogue (hors du défilement) : choisir un meuble en bas de la liste ne
    /// fait plus sauter la liste, et on voit toujours quoi arrêter.
    /// </summary>
    public override Control? BuildHeader()
    {
        if (Session.Finish is { } finish)
            return FinishHeader(finish);
        if (Session.Tool is not { } tool)
            return null;
        var column = Ui.Column(8);
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
        return column;
    }

    public override Control Build()
    {
        var premises = Campaign.Premises;
        var column = Ui.Column(8);

        // Les cartons d'abord : après un déménagement, c'est ce qu'il reste à faire
        if (Campaign.Storage.Count > 0)
            column.Children.Add(CartonsSection());

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
            Ui.OnClick(button, () => Session.Select(SelectionKind.Item, desk.Id));
            stations.Children.Add(button);
        }
        if (posts.Count == 0)
            stations.Children.Add(Ui.Muted("Aucun bureau."));
        // Capacité réelle de l'espace de travail : postes complets sur ce que le local peut accueillir (règles de pose)
        var capacity = Campaign.Capacity(premises);
        var stats = new WrapPanel();
        foreach (var stat in new[]
                 {
                     Ui.Stat("Postes", $"{capacity.Complete} / {capacity.Max}", capacity.Complete < capacity.People ? Palette.Ember : null, 140,
                         $"{capacity.Complete} poste(s) complet(s), {capacity.Installed} installé(s), {capacity.Max} au plus dans ce local."),
                     Ui.Stat("Équipe ici", $"{capacity.People} pers.", null, 140, "Personnes dont le poste est dans ce local."),
                     Ui.Stat("Bureaux en plus", capacity.LimitedByLease ? $"{capacity.Spare}" : $"+{capacity.Spare}", capacity.Spare == 0 ? Palette.Ember : null, 140,
                         capacity.LimitedByLease ? $"Le bail limite ce local à {premises.MaxDesks} bureau(x)." : "Bureaux compacts qui tiennent encore, passages et accès compris."),
                 })
        {
            stat.Margin = new Thickness(0, 0, 6, 6);
            stats.Children.Add(stat);
        }
        column.Children.Add(Ui.Section($"Postes : {capacity.Complete} / {capacity.Max}", stats, stations,
            Ui.Muted("Un poste = bureau, chaise, ordinateur et une case libre devant. Clique un bureau pour l'attribuer.", 12)));
        if (premises.Litter.Count > 0 || Core.campaign.Campaign.BinCapacity(premises) < capacity.People)
            column.Children.Add(CleanSection(capacity.People));
        if (IncludedSection() is { } included)
            column.Children.Add(included);

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
        // Une recherche cherche dans tout le catalogue (sans reconstruire le panneau : la saisie garde le focus) ; sinon une rubrique
        void Fill()
        {
            wrap.Children.Clear();
            string query = _search.Trim();
            var shown = query.Length >= 2
                ? FurnitureCatalog.Buyable.Where(d => d.Name.Contains(query, System.StringComparison.CurrentCultureIgnoreCase)).ToList()
                : groups.First(g => g.Key == _group).ToList();
            foreach (var def in shown)
                wrap.Children.Add(CatalogButton(def));
            if (shown.Count == 0)
                wrap.Children.Add(Ui.Muted("Aucun objet ne correspond.", 13));
        }
        Fill();
        var catalog = Ui.Section("Catalogue",
            Ui.Row(8,
                HudKit.Dropdown(groups.Select(g => (g.Key, g.Key)), _group, g =>
                {
                    _group = g;
                    _search = "";
                    Refresh();
                }),
                Ui.Input(_search, v =>
                {
                    _search = v;
                    Fill();
                }, "Chercher un objet", 220, search: true)),
            wrap);
        catalog.Tag = "section:catalogue";
        column.Children.Add(catalog);

        column.Children.Add(Ui.Section("Sites",
            Ui.Muted($"{premises.Name} : {premises.Width} × {premises.Height} cases, {premises.Workstations.Count()} poste(s) installé(s), loyer {(premises.Rent > 0 ? Ui.Chf(premises.Rent) : "compris dans ta rémunération")}." +
                     (Campaign.Sites.Count > 1 ? $" {Campaign.Sites.Count} sites en tout." : "")),
            Ui.Row(6, Ui.Button(Campaign.Sites.Count > 1 ? "Tous les sites, locations" : "Locaux à louer", () => Open(new SitesPanel())),
                Campaign.IsUnlocked("hardware") ? Ui.Button("Production (Projets)", () => Screen.OpenRoot(ProjectsPanel.At("production"))) : null)));
        return column;
    }

    /// <summary>Propreté : corbeilles pour qui, déchets au sol, et les ramasser.</summary>
    private Control CleanSection(int people)
    {
        var premises = Campaign.Premises;
        int bins = Core.campaign.Campaign.BinCapacity(premises);
        double penalty = Core.campaign.Campaign.MessPenalty(premises);
        return Ui.Section("Propreté",
            Ui.Pair("Corbeilles", $"{System.Math.Min(bins, people)} personne(s) couverte(s) sur {people}", bins < people ? Palette.Ember : null),
            Ui.Pair("Déchets au sol", premises.Litter.Count == 0 ? "aucun" : $"{premises.Litter.Count} (−{penalty:0.#} de moral)", premises.Litter.Count > 0 ? Palette.Ember : null),
            Ui.Row(6, Ui.Button("Tout ramasser", () => Run(Campaign.CleanUp()), "gold", premises.Litter.Count > 0),
                Ui.Button("Acheter une corbeille", () => Session.SetTool(new BuildTool("trash_bin")), "", bins < people)),
            Ui.Muted("Sans corbeille, les déchets finissent par terre. Clique un déchet dans la scène pour le ramasser.", 12));
    }

    /// <summary>
    /// Les objets livrés avec le local (plan d'origine) : on peut les retirer (ils partent dans les cartons, et deviennent les
    /// tiens), sauf ce qui fait partie du bâtiment (sanitaires, cuisine équipée, électricité, réseau).
    /// </summary>
    private Control? IncludedSection()
    {
        var premises = Campaign.Premises;
        var removable = premises.Floor.Concat(premises.WallItems).Where(i => i.Included && Core.campaign.Campaign.CanRemove(i).Ok).ToList();
        int fixedCount = premises.Items.Count(i => i.Included && i.HostId == null && !Core.campaign.Campaign.CanRemove(i).Ok);
        if (removable.Count == 0)
            return null;
        var list = Ui.Column(3);
        foreach (var group in removable.GroupBy(i => i.Def.Name).OrderBy(g => g.Key))
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            var name = Ui.Text(group.Count() > 1 ? $"{group.Count()} × {group.Key}" : group.Key, 13);
            name.VerticalAlignment = VerticalAlignment.Center;
            grid.Children.Add(name);
            var first = group.First();
            var remove = Ui.Button("Retirer", () => Run(Campaign.Store(first.Id), $"{first.Def.Name} : dans les cartons."), "tab");
            Grid.SetColumn(remove, 1);
            grid.Children.Add(remove);
            list.Children.Add(grid);
        }
        var section = Ui.Section($"Livré avec le local ({removable.Count})",
            Ui.Muted("Ces objets étaient déjà là. Retirés, ils partent dans les cartons : tu peux les reposer ailleurs ou les jeter." +
                     (fixedCount > 0 ? $" {fixedCount} élément(s) font partie du bâtiment et restent en place." : ""), 12),
            Ui.Details("Voir la liste", list),
            Ui.Button("Retirer le décor", () => Screen.Confirm("Retirer le décor livré ?",
                "Le décor part dans les cartons. Les postes de travail, le lit, les sanitaires, la cuisine équipée et les installations techniques restent.",
                "Retirer", () =>
                {
                    // Le décor seulement : les postes (bureau, chaise, ordinateur) et le lit se retirent un par un, exprès
                    foreach (var item in removable.Where(i => i.HostId == null && i.Def.Kind is not (FurnitureKind.Desk or FurnitureKind.Chair or FurnitureKind.Bed) && !i.Def.IsComputer))
                        Campaign.Store(item.Id);
                    Run(CommandResult.Success("Le local est vidé : tout est dans les cartons."));
                }, danger: false), "tab"));
        section.Tag = "section:livre";
        return section;
    }

    /// <summary>Les objets des cartons, chacun avec « Poser » et « Vendre ».</summary>
    private Control CartonsSection()
    {
        {
            var stored = Ui.Column(4);
            foreach (var item in Campaign.Storage)
            {
                var def = FurnitureCatalog.Get(item.DefId);
                var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto") };
                // Le nom centré sur la hauteur des boutons (il était collé en haut de la rangée)
                var name = Ui.Text(def.Name, 14);
                name.VerticalAlignment = VerticalAlignment.Center;
                grid.Children.Add(name);
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
            var section = Ui.Section($"Cartons ({Campaign.Storage.Count})", stored,
                Ui.Muted("« Poser » prend l'objet en main : clique ensuite une case du local (ou un bureau pour un objet de bureau).", 13));
            section.Tag = CartonsTag;
            return section;
        }
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

    /// <summary>Finitions : sol ou murs (gardé le temps de la session).</summary>
    private static FinishKind _finishKind = FinishKind.Floor;

    /// <summary>Où poser : sol « all », « zone » ou « tile » ; murs « both » ou « pick ».</summary>
    private static string _floorMode = "all", _wallMode = "both";

    /// <summary>Couleur choisie (null : la couleur d'origine du matériau ou du mur).</summary>
    private static string? _floorColor, _wallColor;

    private static string Mode => _finishKind == FinishKind.Floor ? _floorMode : _wallMode;

    private static string? Color => _finishKind == FinishKind.Floor ? _floorColor : _wallColor;

    /// <summary>
    /// Finitions du site affiché : choisir sol ou murs, où (tout, une zone, une case ; les deux murs, ou un mur visé dans la
    /// scène), la couleur, puis la finition. Choisir une finition la montre dans la scène avant de payer (aperçu) : on pose en
    /// confirmant, ou en cliquant la case ou le mur visé.
    /// </summary>
    private Control FinishesSection()
    {
        var premises = Campaign.Premises;
        string Name(string? id) => id != null && Finishes.Has(id) ? Finishes.Get(id).Name : "celui du plan";
        string left = Name(Finishes.Pattern(premises, "left")), right = Name(Finishes.Pattern(premises, "right"));
        string floor = premises.FloorMaterial is { } m && premises.FloorPatches.Count == 0 ? Name(m) : premises.FloorMaterial == null && premises.FloorPatches.Count == 0 ? "ceux du plan" : "plusieurs zones";

        var kind = Ui.Choice([(FinishKind.Floor, "Sol"), (FinishKind.Wall, "Murs")], _finishKind, k =>
        {
            _finishKind = k;
            Session.SetFinish(null);
            Refresh();
        });
        var where = _finishKind == FinishKind.Floor
            ? Ui.Choice([("all", "Tout le local"), ("zone", "Une zone"), ("tile", "Une case")], _floorMode, mode => { _floorMode = mode; Retool(); })
            : Ui.Choice([("both", "Les deux murs"), ("pick", "Un mur (vise-le)")], _wallMode, mode => { _wallMode = mode; Retool(); });

        var colors = new WrapPanel();
        colors.Children.Add(Ui.Button("D'origine", () => { SetColor(null); }, Color == null ? "tab selected" : "tab"));
        foreach (string ramp in Finishes.Colors(_finishKind))
            colors.Children.Add(Ui.Swatch(PackLibrary.RampColor(ramp), Color == ramp, () => SetColor(ramp), $"Couleur : {UI.Screens.NewCampaignScreen.OutfitName(ramp)}"));

        var wall = new Dictionary<string, string>(Pack.FindPlan(premises.PlanId)?.LeftSwatches ?? new Dictionary<string, string>());
        if (_wallColor != null)
            wall["wall"] = _wallColor;
        var cells = new WrapPanel();
        foreach (var finish in Finishes.Catalog.Where(f => f.Kind == _finishKind))
            cells.Children.Add(FinishCell(finish, wall));
        bool redone = _finishKind == FinishKind.Floor ? premises.FloorMaterial != null || premises.FloorPatches.Count > 0
            : premises.WallPattern != null || premises.LeftPattern != null || premises.RightPattern != null || premises.LeftWallColor != null || premises.RightWallColor != null;
        var back = Ui.Button(_finishKind == FinishKind.Floor ? "Sols du plan" : "Murs du plan", () => Screen.Confirm(
            _finishKind == FinishKind.Floor ? "Remettre les sols du plan ?" : "Remettre les murs du plan ?",
            "Le local retrouve ce que prévoit son plan. Les travaux déjà payés ne sont pas remboursés.", "Remettre",
            () => Run(Campaign.ResetFinish(_finishKind)), danger: false), "tab", redone);
        back.VerticalAlignment = VerticalAlignment.Center;
        cells.Children.Add(back);

        var section = Ui.Section("Finitions",
            Ui.Muted(left == right ? $"Sol : {floor} · murs : {left}" : $"Sol : {floor} · murs : {left} à gauche, {right} à droite", 12),
            Ui.Row(8, kind, where), colors, cells,
            Ui.Muted("Choisis une finition : la scène la montre avant de payer. Le faux plancher distribue mieux le froid, le lambris ajoute " +
                     "de l'ambiance, la moquette et les panneaux acoustiques aident la concentration.", 12));
        section.Tag = "section:finitions";
        return section;
    }

    /// <summary>Change la couleur ; l'aperçu en cours la reprend aussitôt.</summary>
    private void SetColor(string? color)
    {
        if (_finishKind == FinishKind.Floor)
            _floorColor = color;
        else
            _wallColor = color;
        Retool();
    }

    /// <summary>L'aperçu en cours suit le nouveau réglage (mode, couleur).</summary>
    private void Retool()
    {
        if (Session.Finish is { } tool && tool.Def.Kind == _finishKind)
            StartFinish(tool.FinishId);
        else
            Refresh();
    }

    private void StartFinish(string finishId)
    {
        Session.SetFinish(new FinishTool(finishId, Mode, Color));
        Session.BuildMode = !Session.Finish!.Immediate;
        Refresh();
    }

    /// <summary>L'aperçu d'une finition : ce qu'elle coûte et comment la poser (bandeau fixe au-dessus de la liste).</summary>
    private Control FinishHeader(FinishTool tool)
    {
        var def = tool.Def;
        var premises = Campaign.Premises;
        string color = tool.Color != null ? $", {UI.Screens.NewCampaignScreen.OutfitName(tool.Color)}" : "";
        Control body;
        if (tool.Immediate)
        {
            var target = FinishTarget.Everywhere;
            decimal cost = Finishes.Cost(premises, def, target);
            bool current = Finishes.IsCurrent(premises, def, target, tool.Color);
            body = Ui.Column(4,
                Ui.Muted(def.Kind == FinishKind.Floor ? "Aperçu sur tout le sol, dans la scène." : "Aperçu sur les deux murs du fond, dans la scène."),
                Ui.Row(6,
                    Ui.Button(current ? "Déjà en place" : $"Faire les travaux · {cost:N0} CHF", () =>
                    {
                        var result = Campaign.SetFinish(def.Id, target, tool.Color);
                        if (result.Ok)
                            Session.SetFinish(null);
                        Run(result);
                    }, "gold", !current && Campaign.Ledger.CanSpend(cost)),
                    Ui.Button("Annuler", StopFinish)));
        }
        else
        {
            string unit = def.Kind == FinishKind.Wall ? $"{def.Price:N0} CHF le segment de mur"
                : $"{def.Price:N0} CHF la case";
            body = Ui.Column(4,
                Ui.Muted(tool.Mode == "pick" ? "Survole une case le long d'un mur du fond : l'aperçu montre ce mur. Clic : faire les travaux."
                    : tool.Mode == "zone" ? "Survole le local : l'aperçu montre la zone de sol sous la souris. Clic : faire les travaux."
                    : "Survole le local : l'aperçu montre la case sous la souris. Clic : la refaire (on peut enchaîner)."),
                Ui.Muted($"{unit}. Clic droit ou Échap : arrêter."),
                Ui.Button("Arrêter", StopFinish));
        }
        return Ui.Card(Ui.Column(4, Ui.Text($"Aperçu : {def.Name}{color}", 16, Palette.Gold, bold: true), body), Palette.Gold);
    }

    private void StopFinish()
    {
        Session.SetFinish(null);
        Session.BuildMode = false;
        Refresh();
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
            // Le titre est sur la bande sarcelle de la carte du pack : en blanc (4,8:1), l'encre n'y faisait que 3,2:1
            // Le titre occupe toute la bande sarcelle de la carte (16 px du pack, soit 32) : la description commence sous la
            // bande avec une vraie marge, au lieu d'en toucher le bord (BUG-011)
            var title = Ui.Text(module.Name, 14, locked == null ? Palette.White : Palette.Snow, wrap: false);
            title.TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis;
            title.Height = 22;
            title.Margin = new Thickness(0, 0, 0, 8);
            var card = HudKit.Card(Ui.Column(4,
                title,
                Ui.Muted($"{module.Width} × {module.Depth} cases · {cost:N0} CHF", 12),
                Ui.Muted(content, 12),
                Ui.Button("Installer", () => Screen.Confirm($"Installer : {module.Name} ?",
                        $"{content}. {(module.Floors.Count > 0 ? "Avec son sol. " : "")}Coût : {cost:N0} CHF. Il se pose à la première place libre du site affiché" +
                        $"{(module.AgainstWall != null ? $", contre le mur {(module.AgainstWall == "left" ? "gauche" : "droit")} si possible" : "")}.",
                        "Installer", () => Run(Campaign.InstallModule(module.Id)), danger: false),
                    locked == null && Campaign.Ledger.CanSpend(cost) ? "gold" : "", locked == null, locked)), locked == null);
            card.Width = 250;
            card.Margin = new Thickness(0, 0, 8, 8);
            wrap.Children.Add(card);
        }
        var section = Ui.Section("Modules prêts à poser", wrap,
            Ui.Muted("Chaque objet est acheté au prix du catalogue et donne ses effets habituels (pause, coordination, postes, confort).", 12));
        section.Tag = "section:modules";
        return section;
    }

    private Control FinishCell(FinishDef finish, Dictionary<string, string> wall)
    {
        var premises = Campaign.Premises;
        bool floor = finish.Kind == FinishKind.Floor;
        bool previewing = Session.Finish?.FinishId == finish.Id;
        bool current = Finishes.IsCurrent(premises, finish, FinishTarget.Everywhere, Color);
        bool locked = finish.UnlockNode is { } node && !Campaign.IsUnlocked(node);
        var frame = new HoverImage
        {
            Normal = PackSkins.Sprite("hud_pattern_cell", previewing || current ? "selected" : "normal"),
            Hover = previewing || locked ? null : PackSkins.Sprite("hud_pattern_cell", "hover"), Width = 52, Height = 52,
        };
        var swatches = floor ? (_floorColor != null ? new Dictionary<string, string> { ["floor"] = _floorColor } : new Dictionary<string, string>())
            : RoomRenderer.PatternColors(finish.Id, wall);
        var tile = Ui.Image(PackLibrary.FinishTile(finish.Id, swatches), 36, 36);
        tile.Margin = new Thickness(8);
        tile.HorizontalAlignment = HorizontalAlignment.Left;
        tile.VerticalAlignment = VerticalAlignment.Top;
        var content = new Panel { Width = 52, Height = 52, Children = { frame, tile }, Opacity = locked ? 0.5 : 1 };
        var button = new Button { Content = content, Padding = new Thickness(0), Margin = new Thickness(0, 0, 4, 4), IsEnabled = !locked };
        button.Classes.Add("plain");
        string unit = floor ? $"{finish.Price:N0} CHF la case" : $"{finish.Price:N0} CHF le segment de mur";
        ToolTip.SetTip(button, locked ? $"{finish.Name} — à débloquer : {Core.campaign.Campaign.UnlockName(finish.UnlockNode!)}."
            : $"{finish.Name} — {finish.Effect} {unit}. Clic : aperçu dans la scène.");
        Ui.OnClick(button, () =>
        {
            if (previewing)
                StopFinish();
            else
                StartFinish(finish.Id);
        });
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
        if (locked)
            content.Children.Insert(0, Ui.Image(Icons.Get("icon_unavailable", "#9c8878"), 16, 16));
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
        Ui.OnClick(button, () =>
        {
            Session.SetTool(selected ? null : new BuildTool(def.Id));
            Refresh();
        });
        return button;
    }
}
