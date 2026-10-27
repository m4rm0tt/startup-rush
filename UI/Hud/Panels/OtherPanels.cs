using Avalonia.Media;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Core.campaign;
using Core.products;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>Entreprises du groupe, jalons et locaux.</summary>
public class CompanyPanel : GamePanel
{
    public override string Title => "Entreprise";

    private string _newName = "";
    private string _rename = "";

    public override Control Build()
    {
        var column = Ui.Column(8);
        foreach (var company in Campaign.Companies)
        {
            int products = Campaign.Products.Count(p => p.CompanyId == company.Id);
            int contracts = Campaign.Contracts.Count(c => c.CompanyId == company.Id && c.Status == Core.work.ContractStatus.Completed);
            string reuse = company.ReuseLevels.Count == 0 ? "aucun" : string.Join(", ", company.ReuseLevels.Select(r => $"{Core.work.ContractKinds.Get(r.Key).Name} {r.Value}"));
            column.Children.Add(Ui.Card(Ui.Column(4,
                Ui.Text(company.Name, 18, Palette.Snow, bold: true),
                Ui.Muted($"{company.Sector} · fondée le {GameTime.FormatDate(company.FoundedAt)}"),
                Ui.Row(Ui.Text("Réputation", 14, Palette.Fog), Ui.Bar(company.Reputation / 100, Palette.Gold, 8, 160), Ui.Number($"{company.Reputation:0}", 18)),
                Ui.Muted($"{products} produit(s), {contracts} prestation(s) livrée(s). Code réutilisable : {reuse}.", 13),
                Ui.Row(Ui.Input(_rename, v => _rename = v, "Nouveau nom", 200), Ui.Button("Renommer", () => { Run(Campaign.RenameCompany(company.Id, _rename)); _rename = ""; })))));
        }
        column.Children.Add(Ui.Section("Créer une autre entreprise",
            Ui.Muted($"Pour séparer une activité (un produit, un autre métier). Frais d'inscription : {Campaign.CompanyRegistrationCost:N0} CHF. La trésorerie reste commune au groupe ; " +
                     "les produits et contrats sont rattachés à leur entreprise.", 13),
            Ui.Row(Ui.Input(_newName, v => _newName = v, "Nom", 200), Ui.Button("Créer", () => { Run(Campaign.CreateCompany(_newName, "Produits numériques")); _newName = ""; }, "gold"))));

        var milestones = Ui.Column(3);
        foreach (var (id, name, description) in Campaign.MilestoneDefs)
        {
            bool done = Campaign.Milestones.TryGetValue(id, out long when);
            milestones.Children.Add(Ui.Row(Ui.Icon(done ? "trophy" : "lock", 18),
                Ui.Column(0, Ui.Text(name, 14, done ? Palette.Gold : Palette.Fog, bold: done), Ui.Muted(done ? $"{description} — le {GameTime.FormatDate(when)}" : description, 12))));
        }
        column.Children.Add(Ui.Section("Jalons", milestones, Ui.Muted("Des objectifs, pas une fin : la campagne continue tant que l'entreprise tient.", 12)));
        column.Children.Add(Ui.Section("Valeur", Ui.Pair("Valeur estimée", Ui.Chf(Campaign.Valuation())), Ui.Pair("Ta part du capital", $"{Campaign.FounderEquity:P0}")));
        return column;
    }
}

/// <summary>Marché : publics visés, concurrents (et leurs annonces), marketing des produits.</summary>
public class MarketPanel : GamePanel
{
    public override string Title => "Marché et marketing";

    public override Control Build()
    {
        var column = Ui.Column(8);
        var ours = Campaign.Products.Select(p => p.SegmentId).ToHashSet();
        foreach (var segment in ProductCatalog.Segments.OrderByDescending(s => ours.Contains(s.Id)))
        {
            var competitors = Campaign.Competitors.Where(c => c.SegmentId == segment.Id).ToList();
            var lines = Ui.Column(2);
            foreach (var c in competitors)
                lines.Children.Add(Ui.Row(Ui.Text(c.Name, 14, Palette.Cloud), Ui.Bar(c.Appeal, Palette.Plum, 6, 120), Ui.Muted($"attrait {c.Appeal:P0}", 12),
                    c.UpgradeAt is long at ? Ui.Tag($"nouvelle version ~{GameTime.FormatDate(at)}", Palette.Ember) : null));
            foreach (var p in Campaign.Products.Where(p => p.SegmentId == segment.Id && p.IsLive))
            {
                double appeal = p.History.LastOrDefault()?.Appeal ?? 0;
                lines.Children.Add(Ui.Row(Ui.Text(p.Name, 14, Palette.Gold, bold: true), Ui.Bar(appeal, Palette.Gold, 6, 120), Ui.Muted($"attrait {appeal:P0} · {p.Users:N0} utilisateurs", 12)));
            }
            column.Children.Add(Ui.Card(Ui.Column(4,
                Ui.Row(Ui.Text(segment.Name, 16, Palette.Snow, bold: true), Ui.Tag(ProductCatalog.Archetype(segment.ArchetypeId).Name, Palette.Sky)),
                Ui.Muted($"{segment.Size:N0} personnes · prix de référence {segment.ReferencePrice} CHF · sensibilité au prix {segment.PriceSensitivity:0.0}. {segment.Description}", 13),
                lines), ours.Contains(segment.Id) ? Palette.Gold : null));
        }

        var budgets = Ui.Column(4);
        foreach (var product in Campaign.Products)
        {
            var last = product.History.LastOrDefault();
            budgets.Children.Add(Ui.Row(Ui.Text(product.Name, 14, Palette.Snow, bold: true),
                Ui.Muted($"{product.MarketingBudget:N0} CHF/mois sur {ProductCatalog.Channel(product.MarketingChannelId).Name} · {last?.MarketingReach ?? 0:0} visiteurs/jour", 13),
                Ui.Button("Régler", () => { Open(new ProductPanel(product.Id)); })));
        }
        if (Campaign.Products.Count > 0)
            column.Insert(0, Ui.Section("Budgets marketing", budgets));
        column.Children.Insert(0, Ui.Muted("Les concurrents progressent lentement et annoncent leurs nouvelles versions quelques semaines à l'avance. " +
                                           "Ils ne copient pas tes chiffres.", 13));
        return column;
    }
}

/// <summary>Le journal des notifications, regroupées par sujet ; chacune ouvre le menu concerné.</summary>
public class NoticesPanel : GamePanel
{
    public override string Title => "Journal";

    private string _filter = "";

    public override Control Build()
    {
        var categories = Campaign.Notices.Select(n => n.Category).Distinct().OrderBy(c => c).ToList();
        var column = Ui.Column(6,
            Ui.Choice(categories.Select(c => (c, c)).Prepend(("", "Tout")), _filter, f => { _filter = f; Refresh(); }),
            Ui.Button("Tout marquer comme lu", () => { foreach (var n in Campaign.Notices) n.Read = true; Refresh(); }));
        foreach (var notice in Campaign.Notices.Where(n => _filter == "" || n.Category == _filter).Reverse().Take(60))
        {
            IBrush color = notice.Severity switch
            {
                Severity.Critical => Palette.Blood,
                Severity.Warning => Palette.Ember,
                Severity.Good => Palette.Leaf,
                _ => Palette.Sky
            };
            var content = Ui.Column(2,
                Ui.Row(Ui.Tag(notice.Category, color), Ui.Muted(GameTime.Format(notice.Minute), 12), notice.Read ? null : Ui.Tag("nouveau", Palette.Lemon)),
                Ui.Text(notice.Title, 14, Palette.Snow, bold: true),
                string.IsNullOrEmpty(notice.Detail) ? null : Ui.Muted(notice.Detail, 13));
            var button = new Button { Content = content, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left, Padding = new Thickness(8, 4, 8, 8) };
            button.Classes.Add("tab");
            var n = notice;
            button.Click += (_, _) => Screen.Navigate(n);
            column.Children.Add(button);
        }
        return column;
    }
}

internal static class StackPanelExtensions
{
    public static void Insert(this StackPanel panel, int index, Control child) => panel.Children.Insert(index, child);
}
