using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Core.campaign;
using Core.products;
using Core.team;
using UI.Pixel;
using Design = Core.products.Design;

namespace UI.Hud.Panels;

/// <summary>
/// Un projet : l'aperçu (où il en est, quoi faire), l'atelier de composants, les avis et les réglages.
/// Chaque onglet commence par l'essentiel ; les détails sont repliés.
/// </summary>
public class ProjectPanel(int productId) : GamePanel
{
    public enum Tab { Overview, Components, Reviews, Settings }

    public Tab Current { get; set; } = Tab.Overview;
    private string? _highlight;

    private Product? Product => Campaign.FindProduct(productId);

    public override string Title => Product?.Name ?? "Projet";
    public override string Context => Product == null ? "" : $"{Product.Archetype.Name} · {Product.Segment.Name}";
    public override bool Large => true;

    /// <summary>Ouvre un onglet et met en avant un composant (depuis un avis, par exemple).</summary>
    public ProjectPanel Show(Tab tab, string? highlight = null)
    {
        Current = tab;
        _highlight = highlight;
        return this;
    }

    public override Control Build()
    {
        var product = Product;
        if (product == null)
            return Ui.Muted("Ce projet n'existe plus.");
        int problems = Reviews.Trends(product, Campaign.Minute, Campaign.IsUnlocked).Count(t => !t.Positive);
        var tabs = Ui.Choice([(Tab.Overview, "Aperçu"), (Tab.Components, "Composants"), (Tab.Reviews, problems > 0 ? $"Avis ({problems})" : "Avis"), (Tab.Settings, "Réglages")], Current, t =>
        {
            Current = t;
            _highlight = null;
            Refresh();
        });
        Control body = Current switch
        {
            Tab.Components => Components(product),
            Tab.Reviews => ReviewsTab(product),
            Tab.Settings => SettingsTab(product),
            _ => Overview(product),
        };
        return Ui.Column(10, tabs, body);
    }

    // ------------------------------------------------------------------ Aperçu

    /// <summary>
    /// Le matériel : ce qu'une unité coûte en matières et rapporte, le stock de tous les sites, les ventes perdues, le taux de
    /// pannes, et les canaux de vente (visibilité, commission, capacité). Un bouton ouvre la production.
    /// </summary>
    private Control HardwareSection(Product product)
    {
        decimal cost = Core.industry.IndustryCatalog.UnitCost(product);
        var sales = Campaign.SalesOf(product);
        decimal net = product.Price * (decimal)(1 - sales.Commission) - cost;
        var week = product.History.TakeLast(7).ToList();
        var column = Ui.Column(4,
            Ui.Pair("Coût des matières par unité", $"{cost:N0} CHF ({string.Join(", ", Core.industry.IndustryCatalog.Bom(product).Select(kv => $"{kv.Value} × {Core.industry.IndustryCatalog.Material(kv.Key).Name}"))})"),
            Ui.Pair("Marge par unité", $"{net:N0} CHF (prix {product.Price:N0}, commission {sales.Commission:P0})", net > 0 ? Palette.Leaf : Palette.Ember),
            Ui.Pair("Stock vendable", $"{sales.Stock} unité(s)", sales.Stock == 0 ? Palette.Ember : null),
            Ui.Pair("Ventes 7 jours", $"{week.Sum(d => d.Sold):0} vendues, {week.Sum(d => d.Lost):0} perdues", week.Sum(d => d.Lost) > 0 ? Palette.Ember : null),
            Ui.Pair("Pannes", product.Produced == 0 ? "rien fabriqué encore" : $"{product.DefectRate:P1} des unités reviennent", product.DefectRate > 0.05 ? Palette.Ember : null),
            product.FirstSeriesLeft > 0 ? Ui.Muted($"Première série réglée : encore {product.FirstSeriesLeft} unités à {product.FirstSeriesScore:P0} du meilleur réglage.", 12) : null);
        var channels = new WrapPanel();
        foreach (var channel in Core.industry.IndustryCatalog.Channels)
        {
            bool on = product.SalesChannels.Contains(channel.Id);
            bool unlocked = channel.UnlockNode == null || Campaign.IsUnlocked(channel.UnlockNode);
            var button = Ui.Button($"{channel.Name}{(on ? " ✓" : "")}", () => Run(Campaign.SetChannel(product.Id, channel.Id, !on)), on ? "tab selected" : "tab", unlocked,
                unlocked ? $"{channel.Pitch} Jusqu'à {channel.PerDay:0} ventes par jour." : $"À débloquer dans Développement : {Core.progress.DevTree.Node(channel.UnlockNode!).Name}.");
            button.Margin = new Thickness(0, 0, 6, 6);
            channels.Children.Add(button);
        }
        column.Children.Add(Ui.Text("Canaux de vente", 13, Palette.Fog));
        column.Children.Add(channels);
        column.Children.Add(Ui.Button("Production et stocks", () => Open(new IndustryPanel()), "gold"));
        return Ui.Section("Fabrication et ventes", column);
    }

    /// <summary>L'IA d'un produit : le modèle branché, sa qualité vue, et la capacité d'inférence contre la demande.</summary>
    private Control AiSection(Product product)
    {
        var model = Campaign.FindAiModel(product.AiModelId);
        double load = Campaign.AiLoad(product);
        double capacity = Campaign.InferenceCapacity(product);
        var column = Ui.Column(5,
            Ui.Pair("Modèle", model == null ? "aucun (publie un modèle et branche-le)" : $"{model.Name}, version {model.LiveNumber}", model == null ? Palette.Ember : null),
            model?.Live != null ? Ui.Pair("Qualité vue par les utilisateurs", $"{Campaign.AiQualityFor(model):P0}") : null,
            load > 0 ? Ui.Pair("Inférence", $"{capacity:0.#} de puissance pour {load:0.#} demandés", capacity < load ? Palette.Ember : null) : null,
            load > 0 && product.InferenceRented > 0 ? Ui.Pair("Location", $"{(decimal)product.InferenceRented * Core.ai.AiCatalog.ExternalInferencePrice:N0} CHF par mois") : null);
        column.Children.Add(Ui.Button("Modèles et capacité", () => Open(new AiPanel()), "gold"));
        return Ui.Section("Intelligence artificielle", column);
    }

    private Control Overview(Product product)
    {
        var column = Ui.Column(10);
        var header = Ui.Row(12, Ui.IconTile(product.Archetype.Icon, Palette.At(product.Color), 56),
            Ui.Column(2, Ui.Text(ProjectsPanel.Stage(Campaign, product), 20, product.IsLive ? Palette.Leaf : Palette.Lemon, bold: true),
                Ui.Muted($"{ProductCatalog.ModelName(product.Model)} · {product.Components.Count(c => c.Level > 0)} composant(s)", 13)));
        column.Children.Add(header);

        // L'action principale du moment
        if (!product.IsLive && Campaign.CanLaunch(product) && product.Queue.Count == 0)
            column.Children.Add(Ui.Button("Lancer le projet", () => LaunchView.Show(Screen, product.Id), "gold big"));
        else if (product.Queue.Count == 0)
            column.Children.Add(Ui.Button(product.IsLive ? "Préparer une mise à jour" : "Ajouter des composants", () => { Current = Tab.Components; Refresh(); }, "gold"));

        // Les grosses actions : me les proposer, ne pas me déranger, ou les déléguer (arbre « Outils du boss »)
        if (!product.IsLive)
        {
            int mode = Campaign.DelegateLaunches && Campaign.IsUnlocked("delegation") ? 2 : Campaign.ProposeBigActions ? 0 : 1;
            var modes = new List<(int, string)> { (0, "Me les proposer"), (1, "Ne pas me déranger") };
            if (Campaign.IsUnlocked("delegation"))
                modes.Add((2, "Déléguer par défaut"));
            column.Children.Add(Ui.Row(8, Ui.Muted("Lancements :", 13), Ui.Choice(modes, mode, v =>
            {
                Campaign.ProposeBigActions = v == 0;
                Campaign.DelegateLaunches = v == 2;
                Refresh();
            })));
        }

        // Matériel : stock, coût, défauts, canaux de vente, fabrication
        if (product.IsHardware)
            column.Children.Add(HardwareSection(product));

        // IA : le modèle qui propulse le produit, et ce que l'inférence permet de servir
        if (product.ArchetypeId == "ai" || product.Level("ai_core") > 0 || product.Level("ai_feature") > 0 || product.AiModelId != 0)
            column.Children.Add(AiSection(product));
        if (product.ArchetypeId is "xr_app" or "xr_kit")
        {
            var zone = Campaign.BestXr();
            column.Children.Add(Ui.Section("Zone de test XR", Ui.Muted(zone.Capacity > 0
                ? $"Capacité {zone.Capacity:P0} : {zone.DefectCut:P0} de défauts en moins à chaque amélioration." + (zone.Missing.Count > 0 ? $" Il manque : {string.Join(", ", zone.Missing)}." : "")
                : "Sans casque, balises ni tapis, aucun test immersif : les défauts restent.", 13),
                Ui.Button("Studio, XR et quantique", () => Open(new LabPanel()), "tab")));
        }

        // Automatisation avancée : l'équipe prépare seule les petites mises à jour
        if (product.IsLive && Campaign.IsUnlocked("automation"))
            column.Children.Add(Ui.Row(8, Ui.Muted("Petites mises à jour :", 13),
                Ui.Choice([(true, "Préparées par l'équipe"), (false, "Je les choisis")], Campaign.AutoImprove, v => { Campaign.AutoImprove = v; Refresh(); })));

        // Le travail du fondateur : au terminal, mot par mot
        var (canWork, why) = Campaign.CanOpenTerminal(product.Id);
        column.Children.Add(Ui.Row(8,
            Ui.Button("Travailler au terminal", () => Screen.OpenTerminal(product.Id), canWork ? "gold" : "", enabled: canWork),
            Ui.Muted(canWork ? "Tu tapes des mots : chacun fait avancer la tâche en cours." : why, 13)));

        if (product.IsLive)
        {
            var stats = new WrapPanel();
            var last = product.History.LastOrDefault();
            stats.Children.Add(Ui.Stat(product.ArchetypeId == "game" ? "Joueurs actifs" : "Utilisateurs", $"{product.Users:N0}"));
            if (product.ArchetypeId == "game")
                stats.Children.Add(Ui.Stat("Ventes au total", $"{product.TotalBuyers:N0}"));
            else if (product.Model != RevenueModel.Ads)
                stats.Children.Add(Ui.Stat(product.Model == RevenueModel.Commission ? "Ventes / jour" : "Payants", $"{product.Paying:N0}"));
            decimal revenue = product.History.Skip(Math.Max(0, product.History.Count - 30)).Sum(d => d.Revenue);
            decimal result = Campaign.Result30(product);
            stats.Children.Add(Ui.Stat("Revenus 30 j", Ui.Chf(revenue), Palette.Leaf));
            stats.Children.Add(Ui.Stat("Résultat 30 j", Ui.SignedChf(result), Ui.Signed(result), tip: "Revenus moins hébergement et marketing (hors salaires)."));
            stats.Children.Add(Ui.Stat("Note", $"{Reviews.Rating(product):0.0} / 5", Palette.Lemon));
            column.Children.Add(stats);

            var top = Reviews.Trends(product, Campaign.Minute, Campaign.IsUnlocked).FirstOrDefault(t => !t.Positive);
            if (top != null)
                column.Children.Add(Ui.Card(Ui.Row(8, Ui.Icon("warning", 20), Ui.Text($"{top.Share:P0} des avis : {top.Text}", 14, Palette.Lemon),
                    Ui.Button("Voir", () => FollowLink(top.Link), "tab")), Palette.Ember));
        }
        else if (product.ArchetypeId == "game")
            column.Children.Add(Ui.Muted($"{product.Wishlist:N0} personnes attendent la sortie. La promotion (Réglages) en attire davantage.", 14));

        column.Children.Add(QueueView(product));

        // Qui travaille dessus
        var team = Campaign.TeamOf(product).ToList();
        column.Children.Add(Ui.Section("Équipe",
            team.Count == 0 ? Ui.Text("Aucun employé dessus : c'est toi qui travailles, au terminal.", 14, Palette.Ember) : Ui.Text(string.Join(", ", team.Select(p => p.Name)), 14),
            product.IsLive ? Ui.Muted($"{Campaign.UpkeepShare(product):P0} de leur temps va au support et à la maintenance (Réglages → Entretien).", 12) : null));

        // Détails facultatifs
        if (product.IsLive)
        {
            var explain = Ui.Column(2);
            foreach (string line in Market.Explain(product))
                explain.Children.Add(Ui.Text("• " + line, 13));
            var history = product.History.Skip(Math.Max(0, product.History.Count - 120)).ToList();
            var charts = new Grid { ColumnDefinitions = new ColumnDefinitions("*,8,*") };
            charts.Children.Add(new LineChart(history.Select(d => d.Users).ToList(), Palette.Sky, product.ArchetypeId == "game" ? "Joueurs actifs" : "Utilisateurs"));
            var revenueChart = new LineChart(history.Select(d => (double)d.Revenue).ToList(), Palette.Leaf, "Revenus par jour");
            Grid.SetColumn(revenueChart, 2);
            charts.Children.Add(revenueChart);
            var competitors = Ui.Column(2);
            foreach (var c in Campaign.Competitors.Where(c => c.SegmentId == product.SegmentId))
                competitors.Children.Add(Ui.Row(Ui.Text(c.Name, 13, Palette.Cloud), Ui.Bar(c.Appeal, Palette.Plum, 6, 120), c.UpgradeAt != null ? Ui.Tag("nouvelle version bientôt", Palette.Ember) : null));
            double appeal = product.History.LastOrDefault()?.Appeal ?? 0;
            competitors.Children.Add(Ui.Row(Ui.Text(product.Name, 13, Palette.Gold, bold: true), Ui.Bar(appeal, Palette.Gold, 6, 120)));
            column.Children.Add(Ui.Details("Détails : tendances, courbes, concurrents", Ui.Column(8, explain, charts, Ui.Text("Attrait face aux concurrents", 13, Palette.Fog), competitors)));
        }
        else
        {
            var forecast = Campaign.ForecastProject(product);
            column.Children.Add(Ui.Details("Détails : prévision après la sortie",
                Ui.Muted($"Environ {forecast.UsersLow:N0} à {forecast.UsersHigh:N0} {(product.ArchetypeId == "game" ? "joueurs" : "utilisateurs")} après 90 jours, " +
                         $"{forecast.RevenueLow:N0} à {forecast.RevenueHigh:N0} CHF le troisième mois. Une estimation, pas une promesse.", 13)));
        }
        return column;
    }

    private Control QueueView(Product product)
    {
        if (product.Queue.Count == 0)
            return Ui.Section("En cours", Ui.Muted("Rien en file.", 13));
        var list = Ui.Column(6);
        foreach (var queued in product.Queue)
        {
            var def = ProductCatalog.Component(queued.DefId);
            double fraction = Campaign.QueueProgress(queued);
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,200,*,Auto") };
            row.Children.Add(Ui.Icon(def.Icon, 22));
            var name = Ui.Text(queued.ToLevel > 1 ? $"{def.Name} niv. {queued.ToLevel}" : def.Name, 14, Palette.Snow, wrap: false);
            name.Margin = new Thickness(8, 0);
            Grid.SetColumn(name, 1);
            row.Children.Add(name);
            var bar = Ui.Bar(fraction, Palette.Gold, 8);
            bar.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(bar, 2);
            row.Children.Add(bar);
            var cancel = Ui.Button("✕", () => Screen.Confirm($"Retirer {def.Name} de la file ?", fraction > 0 ? "Le travail déjà fait sera perdu." : "Rien n'a encore été fait.", "Retirer",
                () => Run(Campaign.CancelQueued(product.Id, queued.Id))), "tab", tip: "Retirer de la file");
            cancel.Margin = new Thickness(8, 0, 0, 0);
            Grid.SetColumn(cancel, 3);
            row.Children.Add(cancel);
            list.Children.Add(row);
        }
        var (_, days) = Campaign.QueueEstimate(product);
        list.Children.Add(Ui.Muted(double.IsInfinity(days) ? "Personne pour avancer." : $"Fini dans ~{Math.Ceiling(days)} jours ouvrés au rythme actuel.", 12));
        return Ui.Section("En cours", list);
    }

    private void FollowLink(string link)
    {
        if (link.StartsWith("component:"))
            Show(Tab.Components, link["component:".Length..]);
        else if (link == "components")
            Show(Tab.Components);
        else if (link == "ai")
            Open(new AiPanel());
        else
            Show(Tab.Settings, link);
        Refresh();
    }

    // ------------------------------------------------------------------ Atelier de composants

    private Control Components(Product product)
    {
        var requested = Reviews.Trends(product, Campaign.Minute, Campaign.IsUnlocked).Where(t => !t.Positive && t.Link.StartsWith("component:"))
            .Select(t => t.Link["component:".Length..]).ToHashSet();
        double speed = Campaign.TeamOf(product).Sum(p => Campaign.WorkRate(p, p.BestDiscipline) * 60 * 7);
        var wrap = new WrapPanel();
        foreach (var def in Design.ForType(product.ArchetypeId))
            wrap.Children.Add(ComponentCard(product, def, speed, requested.Contains(def.Id) || _highlight == def.Id));
        return Ui.Column(8,
            Hint("components", "Chaque composant répond à un besoin. Ajouter ou améliorer met le travail en file : l'équipe s'en charge."),
            NeedsBar(product),
            wrap);
    }

    private Control NeedsBar(Product product)
    {
        var coverage = Design.Coverage(product);
        var row = new WrapPanel();
        foreach (var (need, weight) in product.Archetype.Needs.OrderByDescending(n => n.Value))
            row.Children.Add(new Border
            {
                Margin = new Thickness(0, 0, 14, 4),
                Child = Ui.Column(2, Ui.Text(ProductCatalog.NeedName(need), 12, Palette.Fog, wrap: false), Ui.Bar(coverage[need], coverage[need] < 0.35 ? Palette.Ember : Palette.Sky, 8, 130)),
            });
        return Ui.Card(Ui.Column(4, Ui.Text("Ce que le public attend", 13, Palette.Gold, bold: true), row));
    }

    private Control ComponentCard(Product product, ComponentDef def, double speed, bool requested)
    {
        int level = product.Level(def.Id);
        int planned = product.PlannedLevel(def.Id);
        var (ok, reason, next) = Design.CanQueue(product, def.Id, Campaign.IsUnlocked);
        double points = next <= ProductCatalog.MaxLevel ? Design.TotalEffort(def, next) : 0;
        string effect = def.IsPerformance ? "Serveurs moins chargés" : string.Join(", ", def.Needs.OrderByDescending(n => n.Value).Select(n => "+" + ProductCatalog.NeedName(n.Key)));

        string action = planned > level ? "En file" : level == 0 ? "Ajouter" : "Améliorer";
        var button = Ui.Button(ok ? $"{action} · ~{(speed > 0 ? Math.Ceiling(points / speed) : 0)} j" : action == "En file" ? "En file" : "Verrouillé",
            () => Run(Campaign.QueueComponent(product.Id, def.Id)), ok ? (requested ? "gold" : "green") : "tab", ok, ok ? null : reason);

        // Carte de composant du pack : la couleur d'en-tête dit l'état, le libellé le redit en toutes lettres
        bool locked = !ok && level == 0 && planned == level;
        var (state, label) = planned > level ? ("in_dev", "En développement")
            : requested && level > 0 ? ("bug", "Demandé dans les avis")
            : level > 0 ? ("done", "En service")
            : locked ? ("locked", "Verrouillé")
            : ("planned", requested ? "Demandé dans les avis" : "Pas encore");
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Height = 28 };
        var title = Ui.Text(def.Name, 15, Palette.Ink, bold: true, wrap: false);
        title.VerticalAlignment = VerticalAlignment.Center;
        title.TextTrimming = TextTrimming.CharacterEllipsis;
        header.Children.Add(title);
        var pips = Ui.Pips(planned, ProductCatalog.MaxLevel, Palette.Ink);
        pips.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(pips, 1);
        header.Children.Add(pips);

        Control picture = ComponentArt.Preview(def.Id) is { } preview && !locked
            ? Ui.Image(preview, 96, 64)
            : Ui.Icon(locked ? "lock" : ComponentArt.Icon(def.Id, def.Icon), 32);
        double quality = product.Component(def.Id)?.Quality ?? 0;
        var status = Ui.Column(4,
            Ui.Row(6, Ui.Icon(ComponentArt.Icon(def.Id, def.Icon), 16), Ui.Text(label, 13, Palette.Ink, bold: true, wrap: false)),
            level > 0 ? Ui.Column(1, Ui.Text($"Qualité {quality:P0}", 12, Palette.Fog, wrap: false), Ui.Bar(quality, quality < 0.4 ? Palette.Ember : Palette.Leaf, 8, 110)) : null);
        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        picture.Margin = new Thickness(0, 0, 10, 0);
        picture.VerticalAlignment = VerticalAlignment.Top;
        body.Children.Add(picture);
        Grid.SetColumn(status, 1);
        body.Children.Add(status);

        var content = Ui.Column(6,
            header,
            body,
            Ui.Text(def.Role, 13, Palette.Cloud),
            Ui.Text(effect, 12, Palette.Sky),
            !ok && planned == level ? Ui.Muted(reason, 12) : null,
            button);
        var card = new PixelPanel { Skin = $"pack:comp_card:{state}", Padding = new Thickness(12, 4, 12, 12), Margin = new Thickness(0, 0, 8, 8), Width = 260, Child = content };
        ToolTip.SetTip(card, $"Travail : {points:0} points. Serveurs : {def.Load:0.0} par 1 000 utilisateurs. Support : {def.Tickets:0.0}. " +
                             $"Entretien : {def.Maintenance:0.0} pt/semaine.{(def.Requires != null ? $" Demande : {ProductCatalog.Component(def.Requires).Name}." : "")}");
        return card;
    }

    // ------------------------------------------------------------------ Avis

    private Control ReviewsTab(Product product)
    {
        if (!product.IsLive)
            return Ui.Muted("Les avis arrivent après la sortie.", 15);
        var trends = Reviews.Trends(product, Campaign.Minute, Campaign.IsUnlocked);
        var list = Ui.Column(6);
        foreach (var trend in trends)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
            row.Children.Add(Ui.Icon(trend.Positive ? "heart" : "warning", 22));
            var text = Ui.Column(0, Ui.Text(trend.Text, 15, trend.Positive ? Palette.Leaf : Palette.Snow),
                Ui.Muted(trend.Positive ? "Ce qui plaît" : $"Environ {trend.Share:P0} des avis en parlent", 12));
            text.Margin = new Thickness(8, 0);
            Grid.SetColumn(text, 1);
            row.Children.Add(text);
            if (!trend.Positive)
            {
                var go = Ui.Button(trend.Link.StartsWith("component") ? "Améliorer" : "Régler", () => FollowLink(trend.Link), "tab");
                Grid.SetColumn(go, 2);
                row.Children.Add(go);
            }
            list.Children.Add(Ui.Card(row, trend.Positive ? null : Palette.Ember));
        }
        if (trends.Count == 0)
            list.Children.Add(Ui.Muted("Pas encore assez d'avis.", 14));

        var recent = Ui.Column(4);
        foreach (var review in product.Reviews.AsEnumerable().Reverse().Take(8))
            recent.Children.Add(Ui.Row(8, Ui.Text(new string('★', review.Stars) + new string('☆', 5 - review.Stars), 13, Palette.Lemon, wrap: false), Ui.Text(review.Text, 13, Palette.Cloud)));

        return Ui.Column(10,
            Ui.Row(12, Ui.Number($"{Reviews.Rating(product):0.0}", 40, Palette.Lemon), Ui.Column(0, Ui.Text("sur 5", 14, Palette.Fog), Ui.Muted($"{product.Users:N0} {(product.ArchetypeId == "game" ? "joueurs" : "utilisateurs")}", 13))),
            Ui.Section("Tendances", list),
            Ui.Details("Derniers avis", recent));
    }

    // ------------------------------------------------------------------ Réglages

    private Control SettingsTab(Product product)
    {
        var column = Ui.Column(10);
        var segment = product.Segment;

        if (product.Model != RevenueModel.Ads)
        {
            decimal reference = product.Model == RevenueModel.Commission ? 10 : segment.ReferencePrice;
            string unit = product.Model == RevenueModel.Commission ? "%" : product.Model == RevenueModel.Purchase ? "CHF" : "CHF/mois";
            var options = new[] { 0.5m, 0.75m, 1m, 1.25m, 1.5m, 2m }.Select(f => Math.Max(1, Math.Round(reference * f))).Distinct();
            column.Children.Add(Ui.Section("Prix", Ui.Choice(options.Select(o => (o, $"{o:0} {unit}")), product.Price, p => Run(Campaign.SetPrice(product.Id, p))),
                Ui.Muted($"Prix habituel de ce public : {reference:0} {unit}.", 12)));
        }

        decimal[] budgets = [0, 200, 500, 1_000, 3_000];
        var channels = ProductCatalog.Channels.Where(c => c.UnlockNode == null || Campaign.IsUnlocked(c.UnlockNode)).ToList();
        column.Children.Add(Ui.Section(product.IsLive ? "Marketing" : "Promotion avant la sortie",
            Ui.Choice(budgets.Select(b => (b, b == 0 ? "Aucun" : $"{b:N0} CHF/mois")), product.MarketingBudget, b => Run(Campaign.SetMarketing(product.Id, b))),
            channels.Count > 1 ? Ui.Choice(channels.Select(c => (c.Id, c.Name)), product.MarketingChannelId, id => Run(Campaign.SetMarketing(product.Id, product.MarketingBudget, id))) : null,
            Ui.Muted($"{Market.Reach(product):0} visiteurs par jour. Doubler le budget ne double pas les visiteurs.", 12)));

        var upkeep = highlightBox("upkeep", Ui.Section("Entretien (support et corrections)",
            Ui.Choice([(Upkeep.Low, "Peu (10 %)"), (Upkeep.Normal, "Normal (25 %)"), (Upkeep.High, "Prioritaire (45 %)")], product.Upkeep, u => Run(Campaign.SetUpkeep(product.Id, u))),
            Ui.Muted($"Part du temps de l'équipe du projet. Bugs connus : {product.Bugs:0}. Questions en attente : {product.SupportBacklog:0}.", 12)));
        column.Children.Add(upkeep);

        double load = Market.Load(product, Campaign.IsUnlocked("optimization"));
        decimal[] caps = [0, 15, 100, 400, 1_500, 5_000];
        if (product.IsHardware)
            load = 0;
        bool ownServers = product.HostingInternal || Campaign.Sites.Any(s => Campaign.SiteInfra(s).Hosting > 0);
        string where = product.HostingInternal
            ? $"Tes serveurs : {product.InternalCapacity:0.#} unité(s)" + (product.OverflowActive ? $" + {product.Hosting.Name} ({product.Hosting.MonthlyCost:N0} CHF/mois)" : "")
            : $"{product.Hosting.Name} · {product.Hosting.MonthlyCost:N0} CHF/mois";
        column.Children.Add(highlightBox("hosting", Ui.Section("Serveurs",
            Ui.Row(8, Ui.Text(where, 15, Palette.Snow, bold: true), Ui.Bar(load / System.Math.Max(0.01, product.Capacity), load > product.Capacity * 0.8 ? Palette.Ember : Palette.Leaf, 8, 120)),
            ownServers ? Ui.Choice([(false, "Chez un hébergeur"), (true, "Sur tes serveurs")], product.HostingInternal, v => Run(Campaign.SetInternalHosting(product.Id, v))) : null,
            product.HostingInternal ? Ui.Muted("Ce qui ne tient pas sur tes serveurs déborde chez l'hébergeur, sans dépasser le plafond (0 : jamais).", 12) : null,
            Ui.Choice([(true, "Automatique"), (false, "Manuel")], product.HostingAuto, a => Run(Campaign.SetHosting(product.Id, a, product.HostingCap))),
            product.HostingAuto
                ? Ui.Column(4, Ui.Text("Plafond mensuel", 13, Palette.Fog), Ui.Choice(caps.Where(c => c > 0 || product.HostingInternal).Select(c => (c, c == 0 ? "Jamais" : $"{c:N0} CHF")), product.HostingCap, c => Run(Campaign.SetHosting(product.Id, true, c))))
                : Ui.Choice(ProductCatalog.Hosting.Where(Campaign.PlanAvailable).Select(h => (h.Id, $"{h.Name} ({h.MonthlyCost:N0})")), product.HostingId, id => Run(Campaign.SetHosting(product.Id, false, product.HostingCap, id))))));

        var swatches = new WrapPanel();
        foreach (int color in Campaign.ProjectColors)
        {
            var face = new Border
            {
                Width = 26, Height = 26, Margin = new Thickness(0, 0, 4, 4), Background = new SolidColorBrush(Palette.At(color)),
                BorderBrush = color == product.Color ? Palette.Snow : Palette.Ink, BorderThickness = new Thickness(color == product.Color ? 3 : 1),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            };
            face.PointerPressed += (_, e) =>
            {
                product.Color = color;
                Refresh();
                e.Handled = true;
            };
            swatches.Children.Add(face);
        }
        string name = product.Name;
        var rename = Ui.Input(product.Name, v => name = v, width: 240);
        column.Children.Add(Ui.Details("Nom, couleur, fermeture", Ui.Column(8,
            Ui.Row(rename, Ui.Button("Renommer", () => Run(Campaign.Rename(product.Id, name)))),
            swatches,
            Ui.Button("Fermer ce projet", () => Screen.Confirm($"Fermer {product.Name} ?", "Plus de revenus ni de coûts. Ses composants et ses utilisateurs disparaissent.", "Fermer",
                () =>
                {
                    Run(Campaign.Retire(product.Id));
                    Screen.PopPanel();
                }), "red"))));
        return column;

        Control highlightBox(string key, Control content) =>
            _highlight == key ? Ui.Card(content, Palette.Lemon) : content;
    }
}
