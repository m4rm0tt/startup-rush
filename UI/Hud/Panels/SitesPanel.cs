using System.Linq;
using Avalonia.Controls;
using Core.campaign;
using Core.space;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>
/// La vue Sites : tous les lieux de l'entreprise (activité, effectif, capacité, loyer, alertes), les capacités réunies, et
/// les locaux à louer. Ouvrir un site l'affiche dans la scène ; tout le reste se gère d'ici sans y aller.
/// </summary>
public class SitesPanel : GamePanel
{
    public override string Title => "Sites";
    public override string Context => $"{Campaign.Sites.Count} site(s)";
    public override bool Large => true;

    public override Control Build()
    {
        var column = Ui.Column(10);
        var sites = new WrapPanel();
        foreach (var site in Campaign.Sites)
            sites.Children.Add(SiteCard(site));
        column.Children.Add(Ui.Section("Tes sites", sites));
        column.Children.Add(Totals());
        column.Children.Add(Offers());
        return column;
    }

    private Control SiteCard(Premises site)
    {
        bool shown = site == Campaign.Premises;
        var people = Campaign.People.Where(p => Campaign.SiteOf(p) == site && Campaign.PostOf(p).Post != null).ToList();
        var infra = Campaign.SiteInfra(site);
        var comfort = Campaign.SiteComfort(site);
        var card = Ui.Column(4,
            Ui.Row(Ui.Text(site.Name, 17, Palette.Snow, bold: true), site == Campaign.Headquarters ? Ui.Tag("SIÈGE", Palette.Gold) : null,
                shown ? Ui.Tag("AFFICHÉ", Palette.Leaf) : null),
            Ui.Pair("Effectif", RealEstate.Get(site.ListingId).Technical ? "salle technique" : $"{people.Count} à leur poste, {site.Workstations.Count()} poste(s)"),
            Ui.Pair("Loyer", site.Rent > 0 ? Ui.Chf(site.Rent) + " / mois" : "compris"),
            RealEstate.Get(site.ListingId).Technical ? null : Ui.Pair("Confort", $"+{comfort.Morale:0.#} moral, pauses {comfort.Break:0.#}"));
        if (infra.Any)
        {
            card.Children.Add(Ui.Pair("Capacités", Capacities(infra.Hosting, infra.Compute, infra.Gpu, infra.Storage)));
            card.Children.Add(Ui.Pair("Électricité", $"{infra.PowerDraw:0.#} / {infra.PowerSupply:0.#} kW · froid {infra.Heat:0.#} / {infra.Cooling:0.#} kW"));
        }
        if (Campaign.IncidentsToday.Contains(site.Id))
            card.Children.Add(Ui.Text("Incident aujourd'hui : moitié de la capacité.", 13, Palette.Ember));
        foreach (string issue in infra.Issues)
            card.Children.Add(Ui.Text(issue, 13, Palette.Ember));
        var buttons = Ui.Row(6,
            shown ? null : Ui.Button("Afficher", () =>
            {
                Run(Campaign.ViewSite(site.Id));
                Screen.OpenRoot(new BuildPanel());
            }, "gold"),
            Campaign.Sites.Count > 1 ? Ui.Button("Fermer", () => ConfirmClose(site), "red") : null);
        card.Children.Add(buttons);
        card.Width = 360;
        var frame = Ui.Card(card, shown ? Palette.Leaf : null);
        frame.Margin = new Avalonia.Thickness(0, 0, 8, 8);
        return frame;
    }

    private static string Capacities(double hosting, double compute, double gpu, double storage) =>
        string.Join(" · ", new[]
        {
            hosting > 0 ? $"hébergement {hosting:0}" : null, compute > 0 ? $"calcul {compute:0}" : null,
            gpu > 0 ? $"GPU {gpu:0}" : null, storage > 0 ? $"{storage:0} To" : null,
        }.Where(s => s != null));

    private void ConfirmClose(Premises site)
    {
        var (people, items, refund) = Campaign.CloseImpact(site);
        string who = people.Count == 0 ? "Personne n'y a son poste." : $"Perdront leur poste : {string.Join(", ", people.Select(p => p.Name))}.";
        Screen.Confirm($"Fermer {site.Name} ?",
            $"{items} objet(s) partent dans les cartons (rien n'est perdu). Garantie rendue : {refund:N0} CHF. Le loyer s'arrête. {who} " +
            "Les projets hébergés sur ses serveurs passent sur ceux des autres sites, sinon à l'hébergeur externe (sous leur plafond).",
            "Fermer le site", () => Run(Campaign.CloseSite(site.Id)));
    }

    private Control Totals()
    {
        var totals = Campaign.Infra();
        var column = Ui.Column(4);
        column.Margin = new Avalonia.Thickness(0, 0, 14, 0);
        if (totals.Hosting + totals.Compute + totals.Gpu + totals.Storage <= 0)
            column.Children.Add(Ui.Muted("Aucun serveur pour l'instant : tes projets sont chez un hébergeur. Un serveur tour suffit pour commencer ; " +
                                         "la salle serveur (Développement) accueille des baies."));
        else
        {
            column.Children.Add(Ui.Pair("Tous sites réunis", Capacities(totals.Hosting, totals.Compute, totals.Gpu, totals.Storage)));
            column.Children.Add(Ui.Pair("Hébergement encore libre", $"{Campaign.HostingLeft:0} unité(s) (après tes projets)"));
            column.Children.Add(Ui.Pair("Électricité et réseau", $"≈ {totals.MonthlyEnergy:N0} CHF / mois"));
            column.Children.Add(Ui.Row(8, Ui.Muted("Réserve gardée libre :", 13),
                Ui.Choice([(0.0, "Aucune"), (0.1, "10 %"), (0.25, "25 %")], Campaign.HostingReserve, v =>
                {
                    Campaign.HostingReserve = v;
                    Refresh();
                })));
            column.Children.Add(Ui.Muted("Ordre d'usage : tes projets d'abord (dans l'ordre du portefeuille), puis les offres cloud. " +
                                         "Une même unité n'est jamais promise deux fois.", 13));
        }
        return Ui.Section("Capacités de l'entreprise", column);
    }

    private Control Offers()
    {
        var column = Ui.Column(8, Ui.Muted("« Déménager » remplace le site affiché (ton matériel part dans les cartons). « Ouvrir en plus » ajoute un site " +
                                           "et garde les autres. Ce qui est livré avec un local y reste ; les postes, baies et serveurs des plans ne sont qu'un aperçu.", 13));
        var free = RealEstate.Offers(Campaign.Sites).ToList();
        foreach (var listing in RealEstate.Listings)
        {
            var places = free.Where(o => o.Listing == listing).ToList();
            var (moveOk, moveWhy) = Campaign.CanRent(listing, additional: false);
            var (addOk, addWhy) = Campaign.CanRent(listing, additional: true);
            var (upfront, refund) = Campaign.MoveCost(listing);
            decimal opening = Campaign.OpeningCost(listing);
            var card = Ui.Column(4,
                Ui.Text(listing.Kind, 16, Palette.Snow, bold: true),
                Ui.Muted(listing.Description, 13),
                Ui.Pair("Surface, loyer", $"{listing.Plan.Width} × {listing.Plan.Depth} cases · {(listing.Rent > 0 ? Ui.Chf(listing.Rent) + " / mois" : "compris")}"),
                Ui.Pair("Adresses libres", places.Count == 0 ? "aucune" : string.Join(", ", places.Select(p => p.Place))));
            foreach (var offer in places)
            {
                string place = offer.Place;
                card.Children.Add(Ui.Row(6, Ui.Text(place, 14, Palette.Cloud, wrap: false),
                    Ui.Button(upfront >= refund ? $"Déménager ({upfront - refund:N0})" : $"Déménager (rend {refund - upfront:N0})", () => Screen.Confirm($"Déménager vers {offer.Name} ?",
                        $"Coût immédiat : {upfront - refund:N0} CHF (garantie {listing.Deposit:N0}, déménageurs, transport, moins la garantie de {Campaign.Premises.Name}). " +
                        $"Loyer : {listing.Rent:N0} CHF par mois, à la place de {Campaign.Premises.Rent:N0}. Tout ton matériel part dans les cartons.", "Déménager", () =>
                        {
                            Run(Campaign.MoveTo(listing.Id, place));
                            Screen.OpenRoot(new BuildPanel());
                        }, danger: false), "", enabled: moveOk, tip: moveOk ? null : moveWhy),
                    Ui.Button($"Ouvrir en plus ({opening:N0})", () => Screen.Confirm($"Ouvrir un site : {offer.Name} ?",
                        $"Coût immédiat : {opening:N0} CHF (garantie {listing.Deposit:N0} et installation). Loyer : {listing.Rent:N0} CHF par mois, " +
                        "en plus de tes autres sites.", "Ouvrir", () =>
                        {
                            Run(Campaign.RentSite(listing.Id, place));
                            Screen.OpenRoot(new BuildPanel());
                        }, danger: false), "gold", enabled: addOk, tip: addOk ? null : addWhy)));
            }
            if (places.Count > 0 && !moveOk && !addOk)
                card.Children.Add(Ui.Muted(addWhy, 13));
            var frame = Ui.Card(card);
            column.Children.Add(frame);
        }
        return Ui.Section("Locaux à louer", column);
    }
}
