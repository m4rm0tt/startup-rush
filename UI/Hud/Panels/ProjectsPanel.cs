using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Core.campaign;
using Core.products;
using UI.Pixel;
using Design = Core.products.Design;

namespace UI.Hud.Panels;

/// <summary>Le portefeuille : une carte par projet, lisible en quelques secondes, et la création d'un nouveau projet.</summary>
public class ProjectsPanel : GamePanel
{
    public override string Title => "Projets";
    public override string Context => $"{Campaign.Products.Count} projet(s)";
    public override bool Large => true;

    public static string Stage(Campaign campaign, Product product)
    {
        if (product.IsLive)
            return product.Queue.Count > 0 ? $"En ligne · v{product.Version} · mise à jour en cours" : $"En ligne · v{product.Version}";
        if (product.Queue.Count == 0)
            return campaign.CanLaunch(product) ? "Prêt à sortir" : "En conception";
        return "En développement";
    }

    public override Control Build()
    {
        var column = Ui.Column(10, Hint("projects", "Chaque projet avance tout seul : choisis ses composants, l'équipe les développe, puis lance-le."));
        var wrap = new WrapPanel();
        foreach (var product in Campaign.Products.OrderBy(p => p.Priority))
            wrap.Children.Add(Card(product));
        wrap.Children.Add(NewCard());
        column.Children.Add(wrap);
        if (Campaign.Products.Count > 1)
            column.Children.Add(Ui.Muted("Les personnes en automatique travaillent sur le premier projet qui a du travail en file. Change l'ordre avec ◀ ▶.", 13));
        return column;
    }

    private Control Card(Product product)
    {
        var type = product.Archetype;
        var (_, days) = Campaign.QueueEstimate(product);
        decimal result = Campaign.Result30(product);
        bool problem = Campaign.Notices.Any(n => n.IsProblem && !n.Resolved && n.Target == $"product:{product.Id}");

        Control progress;
        if (product.Queue.Count > 0)
        {
            double fraction = Campaign.QueueProgress(product.Queue[0]);
            string eta = double.IsInfinity(days) ? "personne dessus" : $"~{Math.Ceiling(days)} j";
            progress = Ui.Column(2, Ui.Bar(fraction, Palette.Gold, 8), Ui.Muted($"{product.Queue.Count} en file · {eta}", 12));
        }
        else if (!product.IsLive && Campaign.CanLaunch(product))
            progress = Ui.Button("Lancer !", () => LaunchView.Show(Screen, product.Id), "gold");
        else
            progress = Ui.Muted(product.IsLive ? "Rien en file" : "Ajoute des composants", 12);

        Control numbers = product.IsLive
            ? Ui.Row(14,
                Ui.Column(0, Ui.Muted(product.ArchetypeId == "game" ? "Joueurs" : "Utilisateurs", 11), Ui.Number($"{product.Users:N0}", 20)),
                Ui.Column(0, Ui.Muted("Résultat 30 j", 11), Ui.Number(Ui.SignedChf(result), 20, Ui.Signed(result))),
                Ui.Column(0, Ui.Muted("Note", 11), Ui.Number($"{Reviews.Rating(product):0.0}★", 20, Palette.Lemon)))
            : Ui.Muted(product.ArchetypeId == "game" && product.Wishlist > 1 ? $"{product.Wishlist:N0} intéressés avant la sortie" : $"{product.Components.Count(c => c.Level > 0)} composant(s) prêt(s)", 13);

        int index = Campaign.Products.OrderBy(p => p.Priority).ToList().IndexOf(product);
        var header = Ui.Row(10, Ui.IconTile(type.Icon, Palette.At(product.Color)),
            Ui.Column(1, Ui.Text(product.Name, 18, Palette.Snow, bold: true, wrap: false),
                Ui.Row(4, Ui.Tag(type.Name, Palette.Sky), Ui.Tag(product.Segment.Name, Palette.Steel, Palette.Snow))),
            problem ? Ui.Icon("warning", 22) : null);
        var body = Ui.Column(8, header,
            Ui.Text(Stage(Campaign, product), 14, product.IsLive ? Palette.Leaf : Palette.Lemon),
            numbers, progress,
            Ui.Row(4,
                Ui.Button("Ouvrir", () => Open(new ProjectPanel(product.Id)), "gold"),
                Campaign.Products.Count > 1 ? Ui.Button("◀", () => Run(Campaign.MoveProject(product.Id, -1)), "tab", index > 0, "Plus prioritaire") : null,
                Campaign.Products.Count > 1 ? Ui.Button("▶", () => Run(Campaign.MoveProject(product.Id, 1)), "tab", index < Campaign.Products.Count - 1, "Moins prioritaire") : null));
        return new PixelPanel { Skin = "inset", Padding = new Thickness(12, 10), Margin = new Thickness(0, 0, 10, 10), Width = 300, Child = body, BorderBrush = problem ? Palette.Ember : null, BorderThickness = new Thickness(problem ? 2 : 0) };
    }

    private Control NewCard()
    {
        var content = Ui.Column(8, Ui.Icon("plus", 40), Ui.Title(Campaign.Products.Count == 0 ? "Créer ton premier projet" : "Nouveau projet", 17), Ui.Muted("Une application, un jeu…", 13));
        foreach (var child in content.Children)
            child.HorizontalAlignment = HorizontalAlignment.Center;
        var button = new Button { Content = content, Width = 300, Height = 200, Margin = new Thickness(0, 0, 10, 10) };
        button.Classes.Add(Campaign.Products.Count == 0 ? "gold" : "tab");
        button.Click += (_, _) => Open(new NewProjectPanel());
        return button;
    }
}

/// <summary>Créer un projet : type, public, et c'est tout. Les valeurs par défaut sont viables ; le reste se règle plus tard.</summary>
public class NewProjectPanel : GamePanel
{
    public override string Title => "Nouveau projet";
    public override bool Large => true;

    private string _type = "saas";
    private string _segment = "freelances";
    private string _name = "";

    public override Control Build()
    {
        var segment = ProductCatalog.Segment(_segment);
        if (segment.TypeId != _type)
            _segment = ProductCatalog.Segments.First(s => s.TypeId == _type).Id;
        segment = ProductCatalog.Segment(_segment);
        var type = ProductCatalog.Type(_type);

        // 1. Le type
        var types = new WrapPanel();
        foreach (var t in ProductCatalog.Types)
        {
            bool available = Campaign.TypeAvailable(t.Id);
            bool chosen = t.Id == _type;
            var content = Ui.Column(4,
                Ui.Row(8, Ui.Icon(available ? t.Icon : "lock", 28), Ui.Text(t.Name, 16, chosen ? Palette.Ink : available ? Palette.Snow : Palette.Fog, bold: true, wrap: false)),
                Ui.Text(available ? t.Pitch : $"À débloquer : {Core.progress.DevTree.Node(t.UnlockNode!).Name}", 12, chosen ? Palette.Ink : Palette.Fog));
            var button = new Button { Content = content, Width = 260, Margin = new Thickness(0, 0, 8, 8), IsEnabled = available, HorizontalContentAlignment = HorizontalAlignment.Left };
            button.Classes.Add("tab");
            if (t.Id == _type)
                button.Classes.Add("selected");
            button.Click += (_, _) =>
            {
                _type = t.Id;
                Refresh();
            };
            types.Children.Add(button);
        }

        // 2. Le public
        var publics = Ui.Choice(ProductCatalog.Segments.Where(s => s.TypeId == _type).Select(s => (s.Id, s.Name)), _segment, id =>
        {
            _segment = id;
            Refresh();
        });

        // 3. Ce qui sera fait, et combien de temps
        var starters = type.Starters.Select(ProductCatalog.Component).ToList();
        double points = starters.Sum(c => Design.TotalEffort(c, 1));
        // Les employés en automatique travaillent seuls ; le fondateur, au terminal (mots à vitesse normale)
        double speed = Campaign.Staff.Where(p => p.Assignment.Kind == Core.team.AssignmentKind.None).Sum(p => Campaign.WorkRate(p, p.BestDiscipline) * 60 * 7);
        double perWord = Tuning.Terminal.PointsPerWord * (0.4 + Campaign.Founder.Skill(Campaign.Founder.BestDiscipline) * 0.012) * Campaign.StationOutput(Campaign.Founder);
        string effort = speed > 0 ? $"environ {Math.Ceiling(points / speed)} jours de travail de l'équipe"
            : perWord > 0 ? $"environ {Math.Ceiling(points / perWord / 10) * 10:0} mots au terminal" : "il te faut un poste complet";
        var model = type.Models[0];
        decimal price = ProductCatalog.DefaultPrice(segment, model);
        var startersRow = new WrapPanel();
        foreach (var c in starters)
            startersRow.Children.Add(new Border { Margin = new Thickness(0, 0, 6, 6), Child = Ui.Row(4, Ui.Icon(c.Icon, 20), Ui.Text(c.Name, 14, Palette.Cloud, wrap: false)) });

        var column = Ui.Column(10,
            Ui.Section("1. Quoi ?", types),
            Ui.Section("2. Pour qui ?", publics, Ui.Muted($"{segment.Pitch} {segment.Size:N0} personnes.", 13)),
            Ui.Section("3. Pour commencer", startersRow,
                Ui.Muted($"{ProductCatalog.ModelName(model)}{(price > 0 ? $" à {price:0} {(model == RevenueModel.Commission ? "%" : model == RevenueModel.Purchase ? "CHF" : "CHF/mois")}" : "")} · {effort} · aucun coût en plus", 13)),
            Ui.Section("Nom", Ui.Input(_name, v => _name = v, "Laisse vide pour un nom proposé", 320)),
            Ui.Button("Créer et commencer", () =>
            {
                var (result, product) = Campaign.CreateProject(_name, _segment, model);
                Run(result, result.Ok ? null : "");
                if (product != null)
                {
                    Screen.PopPanel();
                    Open(new ProjectPanel(product.Id));
                }
            }, "gold big"));
        return column;
    }
}
