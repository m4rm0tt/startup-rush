using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Core.campaign;
using Core.products;
using Core.team;
using Core.work;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>
/// Un produit : l'éditeur de ses pages (brouillon), la version en fabrication, ses résultats mesurés et son exploitation
/// (prix, serveurs, marketing, support). Le brouillon est la prochaine version ; la version en ligne ne change qu'à la publication.
/// </summary>
public class ProductPanel(int productId) : GamePanel
{
    private enum Tab { Pages, Version, Results, Operations }

    private Tab _tab = Tab.Pages;
    private int? _pageId;
    private int? _moduleId;
    private Forecast? _forecast;
    private string _forecastKey = "";

    private Product? Product => Campaign.FindProduct(productId);

    public override string Title => Product?.Name ?? "Produit";
    public override string Context => Product == null ? "" : $"{Product.Archetype.Name} · {Product.Segment.Name} · {ProductCatalog.ModelName(Product.Model)}";
    public override bool Large => true;

    // L'éditeur ne se reconstruit pas tout seul (pour ne pas perdre une saisie) ; les onglets de suivi oui
    public override bool AutoRefresh => _tab != Tab.Pages;

    public override Control Build()
    {
        var product = Product;
        if (product == null)
            return Ui.Muted("Ce produit n'existe plus.");

        var tabs = Ui.Choice([(Tab.Pages, "Pages (brouillon)"), (Tab.Version, VersionTabName(product)), (Tab.Results, "Résultats"), (Tab.Operations, "Exploitation")], _tab, t =>
        {
            _tab = t;
            Refresh();
        });
        Control body = _tab switch
        {
            Tab.Pages => Pages(product),
            Tab.Version => Version(product),
            Tab.Results => Results(product),
            _ => Operations(product),
        };
        return Ui.Column(10, tabs, body);
    }

    private string VersionTabName(Product product) =>
        product.Build == null ? "Version" : Campaign.BuildReady(product) ? $"v{product.Build.Version} prête !" : $"v{product.Build.Version} en cours";

    // ------------------------------------------------------------------ Pages

    private Control Pages(Product product)
    {
        var spec = product.Draft;
        bool editable = Campaign.CanEditDraft(product);
        var page = spec.Page(_pageId ?? spec.HomePageId) ?? spec.Pages.FirstOrDefault();
        if (page == null)
            return Ui.Muted("Aucune page.");
        _pageId = page.Id;
        var module = page.Modules.FirstOrDefault(m => m.Id == _moduleId);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("230,12,*,12,360") };
        grid.Children.Add(PageList(product, spec, page, editable));

        var preview = PagePreview.Build(product, spec, page, _moduleId,
            id =>
            {
                _moduleId = id;
                Refresh();
            },
            id =>
            {
                _pageId = id;
                _moduleId = null;
                Refresh();
            },
            m => Campaign.ModuleStage(product, m));
        var center = Ui.Column(8, preview, DesignSummary(product, spec));
        Grid.SetColumn(center, 2);
        grid.Children.Add(center);

        var properties = Ui.Column(8);
        if (!editable)
            properties.Children.Add(Ui.Card(Ui.Text($"La version {product.Build!.Version} est en fabrication : le brouillon est figé. " +
                                                    "Publie-la, ou annule-la dans l'onglet Version pour modifier à nouveau.", 14, Palette.Lemon), Palette.Gold));
        properties.Children.Add(module != null ? ModuleProperties(product, page, module, editable) : PageProperties(product, spec, page, editable));
        if (editable)
            properties.Children.Add(ModulePalette(product, page));
        Grid.SetColumn(properties, 4);
        grid.Children.Add(properties);
        return grid;
    }

    private Control PageList(Product product, ProductSpec spec, ProductPage current, bool editable)
    {
        var column = Ui.Column(4, Ui.Text("PAGES", 13, Palette.Gold, bold: true));
        var reachable = spec.ReachablePages();
        for (int i = 0; i < spec.Pages.Count; i++)
        {
            var page = spec.Pages[i];
            int index = i;
            string mark = page.Id == spec.HomePageId ? "⌂ " : "";
            var button = Ui.Button($"{mark}{page.Name}", () =>
            {
                _pageId = page.Id;
                _moduleId = null;
                Refresh();
            }, page.Id == current.Id ? "tab selected" : "tab");
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.HorizontalContentAlignment = HorizontalAlignment.Left;
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto") };
            row.Children.Add(button);
            if (editable)
            {
                var up = Ui.Button("↑", () => Move(spec, index, -1), "tab", enabled: index > 0, tip: "Monter");
                var down = Ui.Button("↓", () => Move(spec, index, 1), "tab", enabled: index < spec.Pages.Count - 1, tip: "Descendre");
                Grid.SetColumn(up, 1);
                Grid.SetColumn(down, 2);
                row.Children.Add(up);
                row.Children.Add(down);
            }
            column.Children.Add(row);
            if (!page.Visible)
                column.Children.Add(Ui.Muted("  masquée", 12));
            else if (!reachable.Contains(page.Id))
                column.Children.Add(Ui.Text("  aucun lien n'y mène", 12, Palette.Ember));
        }
        if (editable)
        {
            column.Children.Add(Ui.Button("+ Ajouter une page", () =>
            {
                var page = ProductDesign.NewPage(product, $"Page {spec.Pages.Count + 1}");
                spec.Pages.Add(page);
                // La nouvelle page est liée depuis l'accueil, pour qu'on la trouve
                spec.Page(spec.HomePageId)?.Links.Add(page.Id);
                _pageId = page.Id;
                _moduleId = null;
                Refresh();
            }));
        }
        return column;
    }

    private void Move(ProductSpec spec, int index, int delta)
    {
        var page = spec.Pages[index];
        spec.Pages.RemoveAt(index);
        spec.Pages.Insert(index + delta, page);
        Refresh();
    }

    private Control PageProperties(Product product, ProductSpec spec, ProductPage page, bool editable)
    {
        var column = Ui.Column(6, Ui.Text($"PAGE « {page.Name} »", 13, Palette.Gold, bold: true));
        if (!editable)
        {
            column.Children.Add(Ui.Muted($"Disposition {page.Layout}, thème {page.Theme}. {page.Modules.Count} module(s)."));
            return column;
        }

        TextBox Field(string value, Action<string> set)
        {
            var box = Ui.Input(value, set);
            box.LostFocus += (_, _) => Refresh();
            return box;
        }

        var swatches = new WrapPanel();
        foreach (int index in new[] { 17, 16, 12, 13, 10, 9, 8, 28, 27, 18, 23, 0 })
        {
            var face = new Border
            {
                Width = 26,
                Height = 26,
                Background = new SolidColorBrush(Palette.At(index)),
                BorderBrush = index == page.Accent ? Palette.Snow : Palette.Ink,
                BorderThickness = new Thickness(index == page.Accent ? 3 : 1),
                Margin = new Thickness(0, 0, 4, 4),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            };
            int chosen = index;
            face.PointerPressed += (_, e) =>
            {
                page.Accent = chosen;
                Refresh();
                e.Handled = true;
            };
            swatches.Children.Add(face);
        }

        var links = Ui.Column(2);
        foreach (var other in spec.Pages.Where(p => p.Id != page.Id))
        {
            var check = new CheckBox { Content = Ui.Text(other.Name, 14, Palette.Cloud, wrap: false), IsChecked = page.Links.Contains(other.Id) };
            check.IsCheckedChanged += (_, _) =>
            {
                if (check.IsChecked == true && !page.Links.Contains(other.Id))
                    page.Links.Add(other.Id);
                else if (check.IsChecked != true)
                    page.Links.Remove(other.Id);
                Refresh();
            };
            links.Children.Add(check);
        }
        if (links.Children.Count == 0)
            links.Children.Add(Ui.Muted("Ajoute d'autres pages pour créer un menu."));

        column.Children.Add(Ui.Text("Nom de la page", 13, Palette.Fog));
        column.Children.Add(Field(page.Name, v => page.Name = v));
        column.Children.Add(Ui.Text("Titre affiché", 13, Palette.Fog));
        column.Children.Add(Field(page.Title, v => page.Title = v));
        column.Children.Add(Ui.Text("Texte court", 13, Palette.Fog));
        column.Children.Add(Field(page.Subtitle, v => page.Subtitle = v));
        column.Children.Add(Ui.Text("Disposition", 13, Palette.Fog));
        column.Children.Add(Ui.Choice([("colonne", "Une colonne"), ("deux", "Deux colonnes"), ("grille", "Grille")], page.Layout, v => { page.Layout = v; Refresh(); }));
        column.Children.Add(Ui.Text("Thème", 13, Palette.Fog));
        column.Children.Add(Ui.Choice([("clair", "Clair"), ("sombre", "Sombre")], page.Theme, v => { page.Theme = v; Refresh(); }));
        column.Children.Add(Ui.Text("Couleur d'accent", 13, Palette.Fog));
        column.Children.Add(swatches);
        column.Children.Add(Ui.Muted("Disposition, thème et couleurs personnalisent l'apparence ; ils n'influencent pas la simulation.", 12));
        column.Children.Add(Ui.Text("Menu : liens vers", 13, Palette.Fog));
        column.Children.Add(links);
        column.Children.Add(Ui.Row(
            Ui.Button(page.Visible ? "Masquer la page" : "Afficher la page", () => { page.Visible = !page.Visible; Refresh(); }),
            page.Id != spec.HomePageId ? Ui.Button("Page d'accueil", () => { spec.HomePageId = page.Id; Refresh(); }) : null));
        if (spec.Pages.Count > 1)
            column.Children.Add(Ui.Button("Supprimer la page", () => Screen.Confirm($"Supprimer « {page.Name} » ?",
                $"Ses {page.Modules.Count} module(s) seront retirés du brouillon.", "Supprimer", () =>
                {
                    spec.Pages.Remove(page);
                    foreach (var p in spec.Pages)
                        p.Links.Remove(page.Id);
                    if (spec.HomePageId == page.Id)
                        spec.HomePageId = spec.Pages[0].Id;
                    _pageId = spec.HomePageId;
                    Refresh();
                }), "red"));
        return column;
    }

    private Control ModuleProperties(Product product, ProductPage page, ModuleInstance module, bool editable)
    {
        var def = module.Def;
        string stage = Campaign.ModuleStage(product, module);
        double design = ProductDesign.Effort(module, Discipline.Design), code = ProductDesign.Effort(module, Discipline.Code), test = ProductDesign.Effort(module, Discipline.Test);
        var column = Ui.Column(6,
            Ui.Row(Ui.Button("‹ Page", () => { _moduleId = null; Refresh(); }, "tab"), Ui.Text(def.Name.ToUpperInvariant(), 13, Palette.Gold, bold: true), Ui.Tag(stage, PagePreview.StageColor(stage))),
            Ui.Muted(def.Description),
            Ui.Muted($"Utile pour : {string.Join(", ", def.Needs.Select(n => $"{ProductCatalog.NeedName(n.Key)} {n.Value * module.Factor(c => c.ValueFactor):0.00}"))}" +
                     (ProductDesign.IsRelevant(product, def) ? "" : " — sans intérêt pour ce public"), 13),
            Ui.Card(Ui.Column(2,
                Ui.Pair("Travail (design / dev / tests)", $"{design:0} / {code:0} / {test:0} pts"),
                Ui.Pair("Maintenance", $"{def.Maintenance * module.Factor(c => c.EffortFactor):0.0} pts par semaine"),
                Ui.Pair("Charge serveur", $"{def.Load * module.Factor(c => c.LoadFactor):0.0} par 1 000 utilisateurs"),
                Ui.Pair("Tickets de support", $"{def.Tickets * module.Factor(c => c.TicketFactor):0.00} par 1 000 utilisateurs et par jour"),
                Ui.Pair("Complexité (risque de bugs)", $"{def.Complexity:0.0}"),
                def.Requires != null ? Ui.Muted($"Demande le module {ProductCatalog.Module(def.Requires).Name}.", 12) : null)));

        if (!editable)
            return column;

        var label = Ui.Input(module.Label, v => module.Label = v);
        label.LostFocus += (_, _) => Refresh();
        column.Children.Add(Ui.Text("Libellé", 13, Palette.Fog));
        column.Children.Add(label);
        foreach (var option in def.Options)
        {
            var choice = module.Choice(option);
            column.Children.Add(Ui.Text(option.Label + (option.VisualOnly ? " (apparence seulement)" : ""), 13, Palette.Fog));
            column.Children.Add(Ui.Choice(option.Choices.Select(c => (c.Value, c.Label)), choice.Value, v => { module.Options[option.Key] = v; Refresh(); }));
            if (!string.IsNullOrEmpty(choice.Note))
                column.Children.Add(Ui.Muted(choice.Note, 12));
        }
        int index = page.Modules.IndexOf(module);
        column.Children.Add(Ui.Row(
            Ui.Button("↑", () => { page.Modules.RemoveAt(index); page.Modules.Insert(index - 1, module); Refresh(); }, enabled: index > 0, tip: "Monter"),
            Ui.Button("↓", () => { page.Modules.RemoveAt(index); page.Modules.Insert(index + 1, module); Refresh(); }, enabled: index < page.Modules.Count - 1, tip: "Descendre"),
            Ui.Button(module.Visible ? "Masquer" : "Afficher", () => { module.Visible = !module.Visible; Refresh(); }, tip: "Un module masqué reste à maintenir mais n'apporte rien"),
            Ui.Button("Retirer", () => { page.Modules.Remove(module); _moduleId = null; Refresh(); }, "red")));
        return column;
    }

    private Control ModulePalette(Product product, ProductPage page)
    {
        var wrap = new WrapPanel();
        foreach (var def in ProductCatalog.Modules.OrderByDescending(d => ProductDesign.IsRelevant(product, d)))
        {
            bool relevant = ProductDesign.IsRelevant(product, def);
            var button = Ui.Button(def.Name, () =>
            {
                var module = ProductDesign.NewModule(product, def.Id);
                page.Modules.Add(module);
                _moduleId = module.Id;
                Refresh();
            }, relevant ? "" : "tab", tip: $"{def.Description} Travail : {def.Design + def.Code + def.Test:0} pts.{(relevant ? "" : " Peu utile pour ce public.")}");
            button.Margin = new Thickness(0, 0, 4, 4);
            wrap.Children.Add(button);
        }
        return Ui.Section($"Ajouter un module sur « {page.Name} »", wrap);
    }

    private Control DesignSummary(Product product, ProductSpec spec)
    {
        double quality = Campaign.AssumedQuality(product);
        double draft = ProductDesign.Appeal(product, spec, m => product.ModuleQuality.TryGetValue(m.Id, out double q) ? q : quality);
        double live = product.Live == null ? 0 : ProductDesign.Appeal(product, product.Live, m => Core.products.Market.Quality(product, m));
        var coverage = ProductDesign.Coverage(product, spec, m => product.ModuleQuality.TryGetValue(m.Id, out double q) ? q : quality);
        var needs = Ui.Column(2);
        foreach (var (need, weight) in product.Archetype.Needs.OrderByDescending(n => n.Value))
            needs.Children.Add(Ui.Row(Ui.Text($"{ProductCatalog.NeedName(need)} ({weight:P0})", 13, Palette.Fog), Ui.Bar(coverage[need], Palette.Sky, 6, 140)));
        var warnings = Ui.Column(2);
        foreach (var w in ProductDesign.Warnings(product, spec))
            warnings.Children.Add(Ui.Text("• " + w.Text, 13, w.Blocking ? Palette.Blood : Palette.Ember));
        return Ui.Card(Ui.Column(6,
            Ui.Row(Ui.Text($"Attrait estimé du brouillon : {draft:P0}", 15, Palette.Snow, bold: true), product.Live != null ? Ui.Muted($"(en ligne : {live:P0})") : null),
            Ui.Muted("Ce que ce public attend, et à quel point le brouillon y répond :", 13),
            needs,
            warnings.Children.Count > 0 ? warnings : Ui.Muted("Aucun problème de conception détecté.", 13)));
    }

    // ------------------------------------------------------------------ Version

    private Control Version(Product product)
    {
        if (product.Build is { } build)
            return BuildStatus(product, build);

        var plan = Campaign.PlanDraft(product);
        var column = Ui.Column(8);
        if (plan.Count == 0)
        {
            column.Children.Add(Ui.Muted(product.IsLive ? "Le brouillon est identique à la version en ligne. Modifie les pages pour préparer la suivante." : "Le brouillon est vide."));
            return column;
        }

        var team = Campaign.People.Where(p => p.Assignment.Is(AssignmentKind.Product, product.Id)).ToList();
        double speed = team.Sum(p => Campaign.WorkRate(p, p.BestDiscipline) * 60 * 7);
        double total = plan.Sum(w => w.Effort);
        double days = speed > 0 ? total / speed : double.PositiveInfinity;
        decimal teamCost = team.Where(p => !p.IsFounder).Sum(p => p.Salary) / 21m * (decimal)(double.IsInfinity(days) ? 0 : days);

        string key = System.Text.Json.JsonSerializer.Serialize(product.Draft) + product.MarketingBudget + product.HostingId + team.Count;
        if (key != _forecastKey)
        {
            _forecast = Campaign.ForecastDraft(product);
            _forecastKey = key;
        }

        var byDiscipline = plan.GroupBy(w => w.Discipline).Select(g => $"{Person.DisciplineName(g.Key)} {g.Sum(w => w.Effort):0}");
        var list = Ui.Column(2);
        foreach (var w in plan.Take(30))
            list.Children.Add(Ui.Muted($"• {w.Title} — {Person.DisciplineName(w.Discipline).ToLowerInvariant()} {w.Effort:0.#} pts", 13));
        if (plan.Count > 30)
            list.Children.Add(Ui.Muted($"… et {plan.Count - 30} autres tâches", 13));
        var blocking = ProductDesign.Warnings(product, product.Draft).FirstOrDefault(w => w.Blocking);

        column.Children.Add(Ui.Section($"Version {product.LiveVersion + 1} : ce qu'il faut faire", list,
            Ui.Pair("Travail total", $"{total:0} points ({string.Join(", ", byDiscipline)})"),
            Ui.Pair("Équipe affectée au développement", team.Count == 0 ? "personne" : string.Join(", ", team.Select(p => p.Name)), team.Count == 0 ? Palette.Ember : null),
            Ui.Pair("Durée estimée", double.IsInfinity(days) ? "—" : $"~{days:0.0} jours ouvrés (si tout le monde peut travailler sur tout)"),
            Ui.Pair("Coût en salaires", Ui.Chf(teamCost))));
        if (_forecast is { } f)
            column.Children.Add(Ui.Section("Prévision après publication (90 jours)",
                Ui.Pair("Attrait de la version", $"{f.Appeal:P0}"),
                Ui.Pair("Utilisateurs actifs à 90 jours", $"{f.UsersLow:N0} à {f.UsersHigh:N0}"),
                Ui.Pair("Revenus du 3e mois", $"{f.RevenueLow:N0} à {f.RevenueHigh:N0} CHF"),
                Ui.Pair("Bugs livrés attendus", $"{f.ExpectedBugsLow:0} à {f.ExpectedBugsHigh:0}"),
                Ui.Muted("Même modèle de marché que le jeu, avec la qualité probable de l'équipe ±15 %, ton marketing et ton hébergement actuels, sans réaction des concurrents. " +
                         "Ce n'est pas une promesse.", 12)));
        column.Children.Add(Ui.Row(
            Ui.Button("Lancer le développement", () => Run(Campaign.StartBuild(product.Id)), "gold", blocking == null, blocking?.Text),
            team.Count == 0 ? Ui.Button("M'y affecter", () => Run(Campaign.Assign(Campaign.Founder.Id, AssignmentKind.Product, product.Id))) : null));
        return column;
    }

    private Control BuildStatus(Product product, ProductBuild build)
    {
        var items = build.WorkItemIds.Select(Campaign.FindWork).Where(w => w != null).Select(w => w!).ToList();
        double effort = items.Sum(w => w.Effort), progress = items.Sum(w => w.Progress);
        bool ready = Campaign.BuildReady(product);
        var team = Campaign.People.Where(p => p.Assignment.Is(AssignmentKind.Product, product.Id)).ToList();

        var modules = Ui.Column(4);
        foreach (var group in items.GroupBy(w => w.Title.Split(" (")[0]).OrderBy(g => g.Min(w => w.Id)))
        {
            double done = group.Sum(w => w.Progress), total = group.Sum(w => w.Effort);
            var current = group.FirstOrDefault(w => !w.Done && Campaign.IsReady(w));
            modules.Children.Add(Ui.Row(Ui.Text(group.Key, 14, Palette.Cloud), Ui.Bar(done / Math.Max(0.1, total), done >= total ? Palette.Leaf : Palette.Sky, 8, 180),
                Ui.Muted(done >= total ? "fait" : current != null ? Person.DisciplineName(current.Discipline).ToLowerInvariant() : "en attente", 12)));
        }

        var column = Ui.Column(8,
            Ui.Text($"Version {build.Version} : {progress:0}/{effort:0} points", 18, Palette.Snow, bold: true),
            Ui.Bar(progress / Math.Max(1, effort), ready ? Palette.Leaf : Palette.Gold, 12),
            Ui.Pair("Équipe", team.Count == 0 ? "personne — rien n'avance" : string.Join(", ", team.Select(p => p.Name)), team.Count == 0 ? Palette.Blood : null),
            ready ? null : Ui.Muted($"Goulot : {Campaign.Bottleneck(WorkOwner.Product, product.Id)}."),
            modules);
        if (build.Forecast is { } f)
            column.Children.Add(Ui.Muted($"Prévision au lancement : {f.UsersLow:N0}–{f.UsersHigh:N0} utilisateurs à 90 jours, {f.ExpectedBugsLow:0}–{f.ExpectedBugsHigh:0} bugs.", 13));
        column.Children.Add(Ui.Row(
            Ui.Button(product.IsLive ? $"Publier la v{build.Version}" : "Mettre en ligne", () => Run(Campaign.Publish(product.Id)), "gold", ready),
            team.Count == 0 ? Ui.Button("M'y affecter", () => Run(Campaign.Assign(Campaign.Founder.Id, AssignmentKind.Product, product.Id))) : null,
            Ui.Button("Annuler la version", () => Screen.Confirm("Annuler cette version ?", "Le travail déjà fait est perdu. Le brouillon redevient modifiable.", "Annuler la version",
                () => Run(Campaign.CancelBuild(product.Id))), "red")));
        return column;
    }

    // ------------------------------------------------------------------ Résultats

    private Control Results(Product product)
    {
        if (!product.IsLive)
            return Ui.Muted("Pas encore publié : aucun résultat mesuré.");
        var last = product.History.LastOrDefault();
        decimal revenue30 = product.History.Skip(Math.Max(0, product.History.Count - 30)).Sum(d => d.Revenue);
        decimal costs30 = product.History.Skip(Math.Max(0, product.History.Count - 30)).Sum(d => d.Costs);
        double load = Core.products.Market.Load(product, product.Live!) / product.Hosting.Capacity;

        var kpis = new WrapPanel();
        void Kpi(string label, string value, IBrush? color = null) =>
            kpis.Children.Add(new PixelPanel { Skin = "inset", Padding = new Thickness(10, 6), Margin = new Thickness(0, 0, 6, 6), Width = 170, Child = Ui.Column(0, Ui.Muted(label, 12), Ui.Number(value, 22, color)) });
        Kpi("Utilisateurs actifs", $"{product.Users:N0}");
        Kpi(product.Model == RevenueModel.Commission ? "Ventes par jour" : "Clients payants", $"{product.Paying:N0}");
        if (product.ArchetypeId == "marketplace")
            Kpi("Vendeurs", $"{product.Sellers:N0}");
        Kpi("Revenus 30 jours", Ui.Chf(revenue30), Palette.Leaf);
        Kpi("Coûts 30 jours", Ui.Chf(costs30), Palette.Ember);
        Kpi("Notoriété", $"{product.Aware:N0} / {product.Segment.Size:N0}");
        Kpi("Attrait (après bugs, perf.)", $"{last?.Appeal ?? 0:P0}");
        Kpi("Part face aux concurrents", $"{last?.Share ?? 0:P0}");
        Kpi("Bugs connus", $"{product.Bugs:0}", product.Bugs > 10 ? Palette.Ember : null);
        Kpi("Tickets en attente", $"{product.SupportBacklog:0}", product.SupportBacklog > (last?.Tickets ?? 0) * 2 ? Palette.Ember : null);
        Kpi("Charge des serveurs", $"{load:P0}", load > 1 ? Palette.Blood : load > 0.8 ? Palette.Lemon : null);
        Kpi("Abandons par jour", $"{last?.Churned ?? 0:0.0}");

        var explain = Ui.Column(3);
        foreach (string reason in Core.products.Market.Explain(product))
            explain.Children.Add(Ui.Text("• " + reason, 14));

        var history = product.History.Skip(Math.Max(0, product.History.Count - 120)).ToList();
        var charts = new Grid { ColumnDefinitions = new ColumnDefinitions("*,8,*") };
        charts.Children.Add(new LineChart(history.Select(d => d.Users).ToList(), Palette.Sky, "Utilisateurs (120 j)"));
        var revenue = new LineChart(history.Select(d => (double)d.Revenue).ToList(), Palette.Leaf, "Revenus par jour");
        Grid.SetColumn(revenue, 2);
        charts.Children.Add(revenue);

        var column = Ui.Column(8, kpis, Ui.Section("Pourquoi ça bouge", explain), charts);
        if (product.LastForecast is { } f && product.PublishedAt is long published)
        {
            int days = (int)((Campaign.Minute - published) / GameTime.MinutesPerDay);
            column.Children.Add(Ui.Section($"Prévision de la v{product.LiveVersion} et mesure ({days} jours après publication)",
                Ui.Pair("Utilisateurs prévus à 90 jours", $"{f.UsersLow:N0} à {f.UsersHigh:N0}"),
                Ui.Pair(days >= 90 ? "Mesuré" : "Aujourd'hui", $"{product.Users:N0}",
                    days >= 90 ? (product.Users < f.UsersLow ? Palette.Ember : product.Users > f.UsersHigh ? Palette.Leaf : null) : null)));
        }
        return column;
    }

    // ------------------------------------------------------------------ Exploitation

    private Control Operations(Product product)
    {
        var column = Ui.Column(8);
        var segment = product.Segment;

        // Prix
        string priceUnit = product.Model == RevenueModel.Commission ? "% de commission" : "CHF par mois";
        if (product.Model != RevenueModel.Ads)
        {
            decimal[] options = product.Model == RevenueModel.Commission ? [5, 8, 10, 12, 15, 20] : [segment.ReferencePrice * 0.5m, segment.ReferencePrice * 0.75m, segment.ReferencePrice, segment.ReferencePrice * 1.25m, segment.ReferencePrice * 1.5m, segment.ReferencePrice * 2m];
            column.Children.Add(Ui.Section("Prix",
                Ui.Choice(options.Select(o => (Math.Round(o), $"{Math.Round(o)}")), Math.Round(product.Price), p => Run(Campaign.SetPrice(product.Id, p))),
                Ui.Muted($"Actuel : {product.Price:0} {priceUnit}. Référence de ce public : {(product.Model == RevenueModel.Commission ? 10 : segment.ReferencePrice)}. " +
                         $"Sensibilité au prix : {segment.PriceSensitivity:0.0}. Une hausse fait partir une partie des utilisateurs actuels.", 13)));
        }

        // Hébergement
        double load = product.IsLive ? Core.products.Market.Load(product, product.Live!) : 0;
        column.Children.Add(Ui.Section("Hébergement",
            Ui.Choice(ProductCatalog.Hosting.Select(h => (h.Id, $"{h.Name} ({h.MonthlyCost:N0} CHF)")), product.HostingId, id => Run(Campaign.SetHosting(product.Id, id))),
            Ui.Muted($"Charge actuelle {load:0.0} pour une capacité de {product.Hosting.Capacity:0} ({load / product.Hosting.Capacity:P0}). " +
                     "Au-delà de 80 %, les pages ralentissent ; au-delà de 100 %, des pannes font fuir les utilisateurs.", 13),
            Ui.Row(Ui.Button($"Optimiser les performances ({product.Optimizations}/3)", () => Run(Campaign.PlanOptimization(product.Id), "Optimisation ajoutée aux tâches du produit."),
                enabled: product.IsLive && product.Optimizations < 3, tip: "Tâche de développement : −15 % de charge serveur."))));

        // Marketing
        decimal[] budgets = [0, 300, 1_000, 3_000, 8_000, 20_000];
        column.Children.Add(Ui.Section("Marketing",
            Ui.Choice(budgets.Select(b => (b, b == 0 ? "Aucun" : $"{b:N0}/mois")), product.MarketingBudget, b => Run(Campaign.SetMarketing(product.Id, b, product.MarketingChannelId, product.SellerMarketingShare))),
            Ui.Choice(ProductCatalog.Channels.Select(c => (c.Id, c.Name)), product.MarketingChannelId, id => Run(Campaign.SetMarketing(product.Id, product.MarketingBudget, id, product.SellerMarketingShare))),
            Ui.Muted(ProductCatalog.Channel(product.MarketingChannelId).Description + " Chaque franc rapporte moins que le précédent : doubler le budget ne double pas les visiteurs.", 13),
            product.ArchetypeId == "marketplace"
                ? Ui.Column(4, Ui.Text("Part du budget pour attirer des vendeurs", 13, Palette.Fog),
                    Ui.Choice(new[] { 0.1, 0.3, 0.5, 0.7 }.Select(v => (v, $"{v:P0}")), product.SellerMarketingShare, v => Run(Campaign.SetMarketing(product.Id, product.MarketingBudget, product.MarketingChannelId, v))))
                : null));

        // Qualité et support
        var supporters = Campaign.People.Where(p => p.Assignment.Is(AssignmentKind.Support, product.Id)).Select(p => p.Name).ToList();
        var maintainers = Campaign.People.Where(p => p.Assignment.Is(AssignmentKind.Maintenance, product.Id)).Select(p => p.Name).ToList();
        column.Children.Add(Ui.Section("Qualité, maintenance et support",
            Ui.Pair("Bugs connus", $"{product.Bugs:0}"),
            Ui.Pair("Dette de maintenance", $"{product.MaintenanceDebt:0} pts"),
            Ui.Pair("Support", supporters.Count == 0 ? "personne" : string.Join(", ", supporters)),
            Ui.Pair("Maintenance continue", maintainers.Count == 0 ? "personne" : string.Join(", ", maintainers)),
            Ui.Row(Ui.Button("Planifier des correctifs", () => Run(Campaign.PlanBugFixes(product.Id), "Correctifs ajoutés aux tâches du produit."), enabled: product.Bugs >= 1),
                Ui.Button("Me mettre au support", () => Run(Campaign.Assign(Campaign.Founder.Id, AssignmentKind.Support, product.Id)), enabled: product.IsLive)),
            Ui.Text("Délégation : correctifs automatiques au-delà de", 13, Palette.Fog),
            Ui.Choice([(0, "Jamais"), (5, "5 bugs"), (10, "10 bugs"), (20, "20 bugs")], product.AutoFixThreshold, v => Run(Campaign.SetAutoFix(product.Id, v))),
            Ui.Muted("La règle crée une tâche de correctifs ; il faut toujours quelqu'un affecté au produit pour la faire.", 12)));

        column.Children.Add(Ui.Button("Arrêter ce produit", () => Screen.Confirm($"Arrêter {product.Name} ?",
            "Les serveurs et le marketing s'arrêtent, les utilisateurs partent, le travail en cours est perdu. Irréversible.", "Arrêter", () =>
            {
                Run(Campaign.Retire(product.Id));
                Screen.PopPanel();
            }), "red"));
        return column;
    }
}
