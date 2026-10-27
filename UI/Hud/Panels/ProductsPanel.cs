using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Core.campaign;
using Core.products;
using Core.team;
using Core.work;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>Les produits de l'entreprise, et la création d'un nouveau produit.</summary>
public class ProductsPanel : GamePanel
{
    public override string Title => "Produits";
    public override bool Large => true;

    private string _name = "";
    private string _segment = "freelances";
    private RevenueModel _model = RevenueModel.Subscription;
    private int _company;

    public override bool AutoRefresh => true;

    public override Control Build()
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,16,420") };

        var list = Ui.Column(8);
        foreach (var product in Campaign.Products)
            list.Children.Add(ProductCard(product));
        if (Campaign.Products.Count == 0)
            list.Children.Add(Ui.Muted("Aucun produit. Un produit demande du temps de développement sans rapporter tout de suite : " +
                                       "c'est le pari face aux prestations, qui paient vite mais ne durent pas."));
        grid.Children.Add(Ui.Section("Tes produits", list));

        var form = NewProductForm();
        Grid.SetColumn(form, 2);
        grid.Children.Add(form);
        return grid;
    }

    private Control ProductCard(Product product)
    {
        var last = product.History.LastOrDefault();
        string state = product.Build != null
            ? $"v{product.Build.Version} en développement ({BuildProgress(product):P0})"
            : product.IsLive ? $"v{product.LiveVersion} en ligne" : "Brouillon, jamais publié";
        decimal revenue30 = product.History.Skip(System.Math.Max(0, product.History.Count - 30)).Sum(d => d.Revenue);
        var card = Ui.Column(4,
            Ui.Row(Ui.Text(product.Name, 18, Palette.Snow, bold: true), Ui.Tag(product.Archetype.Name, Palette.Sky), Ui.Tag(product.Segment.Name, Palette.Steel, Palette.Snow)),
            Ui.Muted($"{state} · {ProductCatalog.ModelName(product.Model)} · {(product.Model == RevenueModel.Commission ? $"{product.Price:0} % de commission" : product.Model == RevenueModel.Ads ? "gratuit, financé par la publicité" : $"{product.Price:0} CHF/mois")}"),
            product.IsLive ? Ui.Row(16,
                Ui.Column(0, Ui.Muted("Utilisateurs", 12), Ui.Number($"{product.Users:N0}", 20)),
                Ui.Column(0, Ui.Muted("Payants", 12), Ui.Number($"{product.Paying:N0}", 20)),
                Ui.Column(0, Ui.Muted("Revenus 30 j", 12), Ui.Number(Ui.Chf(revenue30), 20, Palette.Leaf)),
                Ui.Column(0, Ui.Muted("Bugs", 12), Ui.Number($"{product.Bugs:0}", 20, product.Bugs > 10 ? Palette.Ember : null))) : null,
            Ui.Button("Ouvrir", () => Open(new ProductPanel(product.Id)), "gold"));
        return Ui.Card(card);
    }

    private double BuildProgress(Product product)
    {
        var items = product.Build!.WorkItemIds.Select(Campaign.FindWork).Where(w => w != null).ToList();
        return items.Sum(w => w!.Progress) / System.Math.Max(1, items.Sum(w => w!.Effort));
    }

    private Control NewProductForm()
    {
        var segment = ProductCatalog.Segment(_segment);
        var archetype = ProductCatalog.Archetype(segment.ArchetypeId);
        if (!archetype.Models.Contains(_model))
            _model = archetype.Models[0];
        if (Campaign.FindCompany(_company) == null)
            _company = Campaign.MainCompany.Id;

        var segments = Ui.Column(4);
        foreach (var group in ProductCatalog.Segments.GroupBy(s => s.ArchetypeId))
        {
            var a = ProductCatalog.Archetype(group.Key);
            segments.Children.Add(Ui.Text(a.Name, 14, Palette.Gold, bold: true));
            segments.Children.Add(Ui.Choice(group.Select(s => (s.Id, s.Name)), _segment, id =>
            {
                _segment = id;
                Refresh();
            }));
        }

        decimal price = _model == RevenueModel.Commission ? 10 : _model == RevenueModel.Ads ? 0 : segment.ReferencePrice;
        var column = Ui.Column(8,
            Ui.Text("NOUVEAU PRODUIT", 13, Palette.Gold, bold: true),
            Ui.Text("Nom", 14, Palette.Fog),
            Ui.Input(_name, v => _name = v, "par ex. Factura"),
            Ui.Text("Type et public visé", 14, Palette.Fog),
            segments,
            Ui.Card(Ui.Column(4, Ui.Text(archetype.Name, 15, Palette.Snow, bold: true), Ui.Muted(archetype.Description, 13),
                Ui.Muted($"Public « {segment.Name} » : {segment.Size:N0} personnes. {segment.Description} Prix de référence : {segment.ReferencePrice} CHF.", 13),
                Ui.Muted(archetype.RequirementText, 13))),
            Ui.Text("Modèle de revenus", 14, Palette.Fog),
            Ui.Choice(archetype.Models.Select(m => (m, ProductCatalog.ModelName(m))), _model, m =>
            {
                _model = m;
                Refresh();
            }),
            Campaign.Companies.Count > 1 ? Ui.Choice(Campaign.Companies.Select(c => (c.Id, c.Name)), _company, id =>
            {
                _company = id;
                Refresh();
            }) : null,
            Ui.Muted("Le produit commence par une page d'accueil. Tu construiras ensuite ses pages et ses modules dans l'éditeur, " +
                     "puis l'équipe le développera avant la publication."),
            Ui.Button("Créer le produit", () =>
            {
                var (result, product) = Campaign.CreateProduct(_company, _name, _segment, _model, price);
                Run(result, result.Ok ? $"{_name} créé. Construis ses pages." : null);
                if (product != null)
                {
                    _name = "";
                    Open(new ProductPanel(product.Id));
                }
            }, "gold"));
        return new PixelPanel { Skin = "inset", Padding = new Thickness(12), Child = column };
    }
}
