using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Core.products;
using UI.Pixel;

namespace UI.Hud;

/// <summary>
/// L'aperçu d'une page du produit, simulé dans le jeu : chaque module est dessiné selon ses réglages
/// (colonnes, filtres, offres...), la page suit son thème, sa couleur et sa disposition. On clique un module
/// pour le sélectionner, un lien du menu pour changer de page.
/// </summary>
public static class PagePreview
{
    public static Control Build(Product product, ProductSpec spec, ProductPage page, int? selectedModule,
        Action<int> selectModule, Action<int> openPage, Func<ModuleInstance, string> stage)
    {
        bool dark = page.Theme == "sombre";
        var bg = dark ? Palette.Brush("#262b44") : Palette.Brush("#ffffff");
        var ink = dark ? Palette.Snow : Palette.Brush("#262b44");
        var block = dark ? Palette.Brush("#3a4466") : Palette.Brush("#e3e8f0");
        var accent = new SolidColorBrush(Palette.At(page.Accent));
        var accentInk = Palette.At(page.Accent).R * 0.3 + Palette.At(page.Accent).G * 0.59 + Palette.At(page.Accent).B * 0.11 > 150 ? Palette.Ink : Palette.Snow;

        // Barre du navigateur et menu de la page
        var nav = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
        foreach (int link in page.Links)
        {
            var target = spec.Page(link);
            if (target == null)
                continue;
            var tab = new Button { Content = target.Name + (target.Visible ? "" : " (masquée)"), FontSize = 12, Padding = new Thickness(8, 2, 8, 4), Foreground = accentInk };
            tab.Classes.Add("plain");
            tab.Background = accent;
            tab.Margin = new Thickness(0, 0, 4, 0);
            tab.Click += (_, _) => openPage(target.Id);
            nav.Children.Add(new Border { Background = accent, Child = tab, Margin = new Thickness(0, 0, 4, 2) });
        }
        if (page.Links.Count == 0)
            nav.Children.Add(Ui.Text("(aucun lien de menu sur cette page)", 12, Palette.Fog));

        var header = new Border
        {
            Background = accent,
            Padding = new Thickness(12, 8),
            Child = Ui.Column(2, Ui.Text(page.Title, 20, accentInk, bold: true), string.IsNullOrWhiteSpace(page.Subtitle) ? null : Ui.Text(page.Subtitle, 13, accentInk)),
        };

        Panel modules = page.Layout switch
        {
            "deux" => new UniformGrid { Columns = 2 },
            "grille" => new WrapPanel(),
            _ => new StackPanel { Spacing = 6 },
        };
        foreach (var module in page.Modules)
        {
            var content = Module(product, module, ink, block, accent, accentInk);
            var frame = new Border
            {
                Background = block,
                Padding = new Thickness(8, 6),
                Margin = new Thickness(3),
                BorderBrush = module.Id == selectedModule ? Palette.Gold : Brushes.Transparent,
                BorderThickness = new Thickness(3),
                Opacity = module.Visible ? 1 : 0.4,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Child = Ui.Column(4,
                    Ui.Row(Ui.Text(module.Label, 13, ink, bold: true, wrap: false), Ui.Tag(stage(module), StageColor(stage(module))),
                        module.Visible ? null : Ui.Tag("masqué", Palette.Fog)),
                    content),
            };
            if (page.Layout == "grille")
                frame.Width = 176;
            int id = module.Id;
            frame.PointerPressed += (_, e) =>
            {
                selectModule(id);
                e.Handled = true;
            };
            modules.Children.Add(frame);
        }
        if (page.Modules.Count == 0)
            modules.Children.Add(Ui.Text("Page vide : ajoute des modules à droite.", 14, Palette.Fog));

        var browser = Ui.Column(0,
            new Border
            {
                Background = Palette.Slate,
                Padding = new Thickness(8, 4),
                Child = Ui.Row(Ui.Text("● ● ●", 10, Palette.Fog), Ui.Text($"{Slug(product.Name)}.ch/{Slug(page.Name)}", 12, Palette.Cloud, wrap: false)),
            },
            new Border { Background = bg, Padding = new Thickness(10), Child = Ui.Column(8, header, nav, modules) });
        return new Border { BorderBrush = Palette.Steel, BorderThickness = new Thickness(2), Child = browser };
    }

    public static IBrush StageColor(string stage) => stage switch
    {
        "Publié" => Palette.Leaf,
        "Prêt" => Palette.Ice,
        "En test" => Palette.Sky,
        "En développement" => Palette.Gold,
        _ => Palette.Fog
    };

    private static string Slug(string text) => new string(text.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static Control Line(IBrush color, double width, double height = 6) =>
        new Border { Background = color, Width = width, Height = height, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 1) };

    private static Control Pill(string text, IBrush background, IBrush ink) =>
        new Border { Background = background, Padding = new Thickness(8, 2, 8, 3), Margin = new Thickness(0, 0, 4, 2), Child = Ui.Text(text, 12, ink, wrap: false) };

    private static Control Module(Product product, ModuleInstance module, IBrush ink, IBrush block, IBrush accent, IBrush accentInk)
    {
        string Opt(string key) => module.Options.TryGetValue(key, out var v) ? v : "";
        var faint = Palette.Fog;
        switch (module.DefId)
        {
            case "landing":
            {
                var column = Ui.Column(4, Line(ink, 160, 8), Line(faint, 220), Line(faint, 180),
                    Pill(Opt("cta") == "demo" ? "▶ Voir la démo" : "Créer un compte", accent, accentInk));
                if (Opt("style") != "aucune")
                    column.Children.Insert(0, new Border { Background = Opt("style") == "photo" ? Palette.Steel : Palette.Plum, Height = 34, Width = 120, HorizontalAlignment = HorizontalAlignment.Left });
                return column;
            }
            case "login":
                return Ui.Column(3, Field("E-mail", ink), Field("Mot de passe", ink), Ui.Row(4, Pill("Se connecter", accent, accentInk),
                    Opt("methods") == "social" ? Pill("Continuer avec un réseau", Palette.Steel, Palette.Snow) : null));
            case "profile":
                return Ui.Row(new Border { Background = accent, Width = 28, Height = 28 },
                    Ui.Column(2, Line(ink, 90), Line(faint, 120), Opt("depth") == "riche" ? Ui.Row(4, Pill("site", Palette.Steel, Palette.Snow), Pill("bio", Palette.Steel, Palette.Snow)) : null));
            case "catalog":
            {
                int columns = int.TryParse(Opt("columns"), out int c) ? c : 3;
                int count = Math.Min(columns * 2, int.TryParse(Opt("pageSize"), out int n) ? n : 24);
                var grid = new UniformGrid { Columns = columns };
                for (int i = 0; i < count; i++)
                    grid.Children.Add(new Border { Background = i % 3 == 0 ? accent : Palette.Steel, Height = 22, Margin = new Thickness(2) });
                return Ui.Column(2, grid, Ui.Text($"{Opt("pageSize")} éléments par page", 11, faint));
            }
            case "search":
            {
                int filters = Opt("filters") switch { "aucun" => 0, "avances" => 5, _ => 2 };
                var row = new WrapPanel();
                string[] names = ["Prix", "Lieu", "Date", "Note", "Catégorie"];
                for (int i = 0; i < filters; i++)
                    row.Children.Add(Pill(names[i], Palette.Steel, Palette.Snow));
                return Ui.Column(3, Field("Rechercher…", ink), row);
            }
            case "feed":
                return Ui.Column(3, Post(ink, faint, accent), Post(ink, faint, accent), Post(ink, faint, accent),
                    Ui.Text(Opt("paging") == "infini" ? "↓ la suite se charge en continu" : "‹ 1 2 3 ›", 11, faint));
            case "subscribe":
            {
                int plans = int.TryParse(Opt("plans"), out int p) ? p : 2;
                var row = Ui.Row(4);
                for (int i = 0; i < plans; i++)
                    row.Children.Add(new Border
                    {
                        Background = i == plans - 1 ? accent : Palette.Steel,
                        Padding = new Thickness(6, 4),
                        Child = Ui.Column(1, Ui.Text(new[] { "Solo", "Pro", "Équipe" }[i], 12, i == plans - 1 ? accentInk : Palette.Snow, bold: true),
                            Ui.Text($"{product.Price * (1 + i):0} CHF/mois", 11, i == plans - 1 ? accentInk : Palette.Snow, wrap: false)),
                    });
                return Ui.Column(3, row, Opt("trial") == "oui" ? Pill("14 jours d'essai gratuit", Palette.Leaf, Palette.Ink) : null);
            }
            case "checkout":
                return Ui.Column(3, Ui.Row(Line(ink, 100), Ui.Text("55 CHF", 12, ink)), Ui.Row(4, Pill("Payer par carte", accent, accentInk),
                    Opt("methods") == "mobile" ? Pill("Paiement mobile", Palette.Steel, Palette.Snow) : null));
            case "reviews":
                return Ui.Column(2, Ui.Text("★★★★☆  « Pratique »", 12, ink), Ui.Text("★★★★★  « Je recommande »", 12, ink),
                    Opt("moderation") == "manuelle" ? Ui.Text("Avis vérifiés par l'équipe", 11, faint) : null);
            case "messaging":
                return Ui.Column(3, Bubble("Salut ! C'est toujours dispo ?", Palette.Steel, Palette.Snow, false), Bubble("Oui, je te l'envoie.", accent, accentInk, true),
                    Opt("realtime") == "oui" ? Ui.Text("● en ligne", 11, Palette.Leaf) : Ui.Text("réponse par e-mail", 11, faint));
            case "support":
                return Ui.Column(2, Ui.Text("? Comment changer d'offre", 12, ink), Ui.Text("? Exporter mes données", 12, ink),
                    Opt("mode") == "chat" ? Pill("Discuter avec nous", accent, accentInk) : null);
            case "dashboard":
            {
                int widgets = int.TryParse(Opt("widgets"), out int w) ? w : 6;
                var grid = new UniformGrid { Columns = 3 };
                for (int i = 0; i < widgets; i++)
                    grid.Children.Add(new Border
                    {
                        Background = Palette.Steel,
                        Margin = new Thickness(2),
                        Padding = new Thickness(3),
                        Child = Ui.Column(1, Line(i % 2 == 0 ? accent : Palette.Leaf, 20 + i * 5 % 30, 4), Line(Palette.Cloud, 30, 3)),
                    });
                return grid;
            }
            case "export":
                return Ui.Row(4, Pill("Exporter CSV", Palette.Steel, Palette.Snow), Opt("formats") == "api" ? Pill("Clé d'API", accent, accentInk) : null);
            case "notifications":
                return Ui.Column(2, Ui.Text("3 nouveautés", 12, ink), Ui.Text(Opt("frequency") == "quotidien" ? "Résumé chaque jour" : "Résumé chaque semaine", 11, faint));
            case "listings":
                return Ui.Column(3, Field("Titre de l'annonce", ink), Field("Prix", ink), Ui.Row(4, Pill("Publier", accent, accentInk),
                    Ui.Text(Opt("validation") == "manuelle" ? "validée par l'équipe" : "en ligne tout de suite", 11, faint)));
            case "stats":
            {
                var bars = Ui.Row(3);
                bars.VerticalAlignment = VerticalAlignment.Bottom;
                foreach (int h in new[] { 8, 14, 11, 20, 26, 18, 30 })
                    bars.Children.Add(new Border { Background = accent, Width = 10, Height = h, VerticalAlignment = VerticalAlignment.Bottom });
                return bars;
            }
            default:
                return Line(faint, 120);
        }
    }

    private static Control Field(string placeholder, IBrush ink) => new Border
    {
        BorderBrush = Palette.Fog,
        BorderThickness = new Thickness(1),
        Padding = new Thickness(6, 2),
        Width = 200,
        HorizontalAlignment = HorizontalAlignment.Left,
        Child = Ui.Text(placeholder, 12, Palette.Fog, wrap: false),
    };

    private static Control Post(IBrush ink, IBrush faint, IBrush accent) =>
        Ui.Row(new Border { Background = accent, Width = 14, Height = 14 }, Ui.Column(1, Line(ink, 110, 5), Line(faint, 160, 4)));

    private static Control Bubble(string text, IBrush background, IBrush ink, bool right) => new Border
    {
        Background = background,
        Padding = new Thickness(6, 2, 6, 3),
        HorizontalAlignment = right ? HorizontalAlignment.Right : HorizontalAlignment.Left,
        Child = Ui.Text(text, 12, ink, wrap: false),
    };
}
