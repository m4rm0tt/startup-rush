using System;
using System.Linq;
using Avalonia.Controls;
using Core.campaign;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>
/// Les offres cloud : vendre une part de ta capacité (hébergement, stockage, calcul, GPU) à d'autres entreprises. Pour
/// chaque offre : prix, capacité réservée, clients, recettes, et pourquoi ça monte ou baisse, avec le bon levier.
/// </summary>
public class CloudPanel : GamePanel
{
    public override string Title => "Services cloud";
    public override string Context => $"{Campaign.CloudRevenue30:N0} CHF sur 30 jours";
    public override bool Large => true;

    public override Control Build()
    {
        var column = Ui.Column(10, Hint("cloud", "Tes projets passent d'abord ; les offres vendent ce qui reste. Les clients viennent selon le prix, la fiabilité et la sécurité."));
        var totals = Campaign.Infra();
        var wrap = new WrapPanel();
        foreach (var kind in Enum.GetValues<CloudKind>())
            // Le quantique n'apparaît qu'avec ses déblocages : l'écran de départ ne montre pas ses jauges
            if (kind != CloudKind.Quantum || Campaign.IsUnlocked("advanced_lab") || Campaign.CloudOffers.Any(o => o.Kind == kind))
                wrap.Children.Add(OfferCard(kind, totals));
        column.Children.Add(wrap);
        return column;
    }

    private Control OfferCard(CloudKind kind, InfraTotals totals)
    {
        var info = Campaign.CloudInfo(kind);
        var offer = Campaign.CloudOffers.FirstOrDefault(o => o.Kind == kind);
        var body = Ui.Column(6, Ui.Text(info.Name, 17, Palette.Snow, bold: true),
            Ui.Muted($"Prix de référence : {info.Reference:N0} CHF par {info.Unit} et par mois. Capacité libre : {Campaign.Units(kind, Campaign.CloudCapacity(kind, totals))}.", 13));
        if (offer == null)
        {
            bool unlocked = Campaign.IsUnlocked(info.Node);
            body.Children.Add(Ui.Button("Ouvrir cette offre", () => Run(Campaign.OpenCloudOffer(kind)), "gold", unlocked,
                unlocked ? null : $"À débloquer dans Développement : {Core.progress.DevTree.Node(info.Node).Name}."));
        }
        else
        {
            double available = Campaign.CloudAvailable(offer);
            body.Children.Add(Ui.Row(14,
                Ui.Column(0, Ui.Muted("Clients", 11), Ui.Number($"{offer.Served:0} / {offer.Demand:0}", 20)),
                Ui.Column(0, Ui.Muted("Recettes 30 j", 11), Ui.Number(Ui.Chf(offer.Revenue30), 20, Palette.Leaf))));
            body.Children.Add(Ui.Row(6, Ui.Muted("Prix :", 13),
                Ui.Button("−", () => Run(Campaign.SetCloudOffer(offer.Id, offer.Price * 0.9m, offer.Reserved, offer.Paused)), "tab"),
                Ui.Text($"{offer.Price:N0} CHF", 15, Palette.Snow, wrap: false),
                Ui.Button("+", () => Run(Campaign.SetCloudOffer(offer.Id, offer.Price * 1.1m, offer.Reserved, offer.Paused)), "tab")));
            double step = Math.Max(1, Math.Round(available / 10));
            body.Children.Add(Ui.Row(6, Ui.Muted("Réservé :", 13),
                Ui.Button("−", () => Run(Campaign.SetCloudOffer(offer.Id, offer.Price, Math.Max(0, offer.Reserved - step), offer.Paused)), "tab"),
                Ui.Text($"{offer.Reserved:0} / {Campaign.Units(kind, available)}", 15, Palette.Snow, wrap: false),
                Ui.Button("+", () => Run(Campaign.SetCloudOffer(offer.Id, offer.Price, offer.Reserved + step, offer.Paused)), "tab")));
            body.Children.Add(Ui.Choice([(false, "En vente"), (true, "En pause")], offer.Paused, v => Run(Campaign.SetCloudOffer(offer.Id, offer.Price, offer.Reserved, v))));
            foreach (var trend in Campaign.CloudTrends(offer))
                body.Children.Add(Ui.Row(6, Ui.Icon(trend.Positive ? "check" : "warning", 16),
                    Ui.Text(trend.Text, 13, trend.Positive ? Palette.Leaf : Palette.Ember),
                    trend.Lever == "sites" ? Ui.Button("Sites", () => Open(new SitesPanel()), "tab")
                    : trend.Lever == "capacity" && available > offer.Reserved + 0.5 ? Ui.Button("Tout réserver", () => Run(Campaign.SetCloudOffer(offer.Id, offer.Price, available, offer.Paused)), "tab")
                    : null));
        }
        body.Width = 340;
        return new PixelPanel { Skin = "inset", Padding = new Avalonia.Thickness(12, 10), Margin = new Avalonia.Thickness(0, 0, 10, 10), Child = body };
    }
}
