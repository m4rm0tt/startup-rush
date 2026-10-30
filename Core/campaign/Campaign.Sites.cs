using Core.finance;
using Core.progress;
using Core.space;
using Core.team;

namespace Core.campaign;

/// <summary>
/// Les sites de l'entreprise : une seule entreprise, plusieurs lieux. Louer un local de plus ajoute un site ; déménager
/// remplace le site affiché ; fermer rend la garantie et met en cartons ce qui t'appartient, sans perte silencieuse. Tout
/// se gère depuis la vue Sites : regarder un site ne change rien à la simulation.
/// </summary>
public partial class Campaign
{
    /// <summary>Affiche un autre site (un choix de vue : la simulation ne dépend pas du site regardé).</summary>
    public CommandResult ViewSite(int siteId)
    {
        if (FindSite(siteId) == null)
            return CommandResult.Fail("Site inconnu.");
        SiteId = siteId;
        return CommandResult.Success();
    }

    /// <summary>Une offre libre de ce type de local (à cette adresse, ou la première libre).</summary>
    public Offer? FreeOffer(string listingId, string? place = null) =>
        RealEstate.Offers(Sites).FirstOrDefault(o => o.Listing.Id == listingId && (place == null || o.Place == place));

    /// <summary>
    /// Peut-on louer ce local, en plus (additional) ou à la place du site affiché ? Les salles techniques et le siège se
    /// débloquent dans l'arbre ; un site de plus demande « Plusieurs sites » ; une salle technique n'accueille pas de bureaux.
    /// </summary>
    public (bool Ok, string Reason) CanRent(Listing listing, bool additional)
    {
        if (listing.UnlockNode is { } node && !IsUnlocked(node))
            return (false, $"À débloquer dans Développement : {DevTree.Node(node).Name}.");
        if (additional && !IsUnlocked("multisite"))
            return (false, "Pour garder ce local et en louer un autre : Développement, « Plusieurs sites ».");
        if (additional && listing.Id == RealEstate.Apartment)
            return (false, "Ta chambre ne se loue pas en plus.");
        if (!additional && listing.Technical && Premises.Workstations.Any(p => Premises.ChairOf(p)?.AssignedPersonId != null))
            return (false, "Une salle technique n'accueille pas de bureaux : loue-la comme site de plus.");
        if (!RealEstate.Offers(Sites).Any(o => o.Listing.Id == listing.Id))
            return (false, "Plus aucune adresse libre pour ce type de local.");
        return (true, "");
    }

    /// <summary>Coût total d'un déménagement du site affiché : garantie, forfait et transport des meubles, moins sa garantie rendue.</summary>
    public (decimal Upfront, decimal Refund) MoveCost(Listing listing)
    {
        int items = Premises.Items.Count(i => !i.Included) + Storage.Count;
        decimal upfront = listing.Deposit + listing.MovingCost + items * RealEstate.MovingCostPerItem;
        return (upfront, Math.Min(Premises.Deposit, Ledger.DepositHeld));
    }

    /// <summary>Ouvrir un site de plus : la garantie et la moitié du forfait (installation, raccordements).</summary>
    public static decimal OpeningCost(Listing listing) => listing.Deposit + Math.Round(listing.MovingCost * RealEstate.OpeningShare);

    /// <summary>Déménage le site affiché : tout ce qui t'appartient part dans les cartons ; ce qui était livré reste au local.</summary>
    public CommandResult MoveTo(string listingId, string? place = null)
    {
        var listing = RealEstate.Get(listingId);
        if (listing.Id == Premises.ListingId && (place == null || place == Premises.Place))
            return CommandResult.Fail("Tu es déjà ici.");
        var (ok, reason) = CanRent(listing, additional: false);
        if (!ok)
            return CommandResult.Fail(reason);
        var offer = FreeOffer(listingId, place)!;
        var (upfront, refund) = MoveCost(listing);
        if (!Ledger.CanSpend(upfront - refund))
            return CommandResult.Fail($"Il faut {upfront - refund:N0} CHF (garantie, déménageurs) pour emménager.");

        var old = Premises;
        if (refund > 0)
            Post(refund, Category.DepositRefund, $"Garantie rendue : {old.Name}");
        if (listing.Deposit > 0)
            Post(-listing.Deposit, Category.Deposit, $"Garantie de loyer : {offer.Name}");
        Post(-(upfront - listing.Deposit), Category.Moving, $"Déménagement vers {offer.Name}");
        Ledger.DepositHeld += listing.Deposit - refund;

        StoreAll(old);
        Premises = RealEstate.Create(listing, NewId, offer.Place);
        RelocateStock(old.Id, Premises);
        SendHome(Premises);
        Notify(Severity.Good, "local", $"Emménagement : {offer.Name}",
            $"{Storage.Count} objets sont dans les cartons. Ouvre Aménagement pour les installer. Nouveau loyer : {listing.Rent:N0} CHF par mois.", "build");
        return CommandResult.Success();
    }

    /// <summary>Loue un local de plus : un nouveau site, qui devient le site affiché.</summary>
    public CommandResult RentSite(string listingId, string? place = null)
    {
        var listing = RealEstate.Get(listingId);
        var (ok, reason) = CanRent(listing, additional: true);
        if (!ok)
            return CommandResult.Fail(reason);
        var offer = FreeOffer(listingId, place);
        if (offer == null)
            return CommandResult.Fail("Cette adresse n'est plus libre.");
        decimal cost = OpeningCost(listing);
        if (!Ledger.CanSpend(cost))
            return CommandResult.Fail($"Il faut {cost:N0} CHF (garantie, installation) pour ouvrir ce site.");
        var site = RealEstate.Create(listing, NewId, offer.Place);
        if (listing.Deposit > 0)
            Post(-listing.Deposit, Category.Deposit, $"Garantie de loyer : {offer.Name}", siteId: site.Id);
        if (cost > listing.Deposit)
            Post(-(cost - listing.Deposit), Category.Moving, $"Ouverture du site : {offer.Name}", siteId: site.Id);
        Ledger.DepositHeld += listing.Deposit;

        Sites.Add(site);
        SiteId = site.Id;
        InvalidateLayout();
        Notify(Severity.Good, "local", $"Nouveau site : {offer.Name}", $"Loyer : {listing.Rent:N0} CHF par mois, en plus des autres sites.", "sites");
        return CommandResult.Success();
    }

    /// <summary>Ce que touche la fermeture d'un site : personnes qui y ont leur poste, objets à mettre en cartons, garantie rendue.</summary>
    public (List<Person> People, int Items, decimal Refund) CloseImpact(Premises site) =>
        (People.Where(p => site.SeatOf(p.Id) != null).ToList(), site.Items.Count(i => !i.Included), Math.Min(site.Deposit, Ledger.DepositHeld));

    /// <summary>
    /// Ferme un site : sa garantie est rendue, ce qui t'appartient part dans les cartons, les personnes qui y travaillaient
    /// perdent leur poste (et sont nommées). Le dernier site ne se ferme pas : il faut déménager.
    /// </summary>
    public CommandResult CloseSite(int siteId)
    {
        var site = FindSite(siteId);
        if (site == null)
            return CommandResult.Fail("Site inconnu.");
        if (Sites.Count == 1)
            return CommandResult.Fail("C'est ton seul site : déménage plutôt.");
        var (people, items, refund) = CloseImpact(site);
        if (refund > 0)
            Post(refund, Category.DepositRefund, $"Garantie rendue : {site.Name}");
        Ledger.DepositHeld -= refund;
        StoreAll(site);
        RelocateStock(site.Id, site == Headquarters ? Sites.First(s => s != site) : Headquarters);
        Sites.Remove(site);
        if (SiteId == site.Id)
            SiteId = Sites[0].Id;
        InvalidateLayout();
        string who = people.Count == 0 ? "Personne n'y travaillait." : $"Sans poste à installer : {string.Join(", ", people.Select(p => p.Name))}.";
        Notify(people.Count == 0 ? Severity.Info : Severity.Warning, "local", $"Site fermé : {site.Name}",
            $"{items} objets dans les cartons. {who}", people.Count == 0 ? "sites" : "build");
        return CommandResult.Success();
    }

    /// <summary>Met dans les cartons tout ce qui t'appartient sur ce site (les postes se libèrent).</summary>
    private void StoreAll(Premises site)
    {
        foreach (var item in site.Items.Where(i => !i.Included))
            Storage.Add(new StoredItem { Id = item.Id, DefId = item.DefId, PaidPrice = item.PaidPrice });
        site.Items.RemoveAll(i => !i.Included);
        foreach (var chair in site.Items)
            chair.AssignedPersonId = null;
    }

    /// <summary>Après un déménagement : chacun repart de l'entrée du nouveau local.</summary>
    private void SendHome(Premises site)
    {
        foreach (var person in People.Where(p => SiteOf(p) == site || PostOf(p).Post == null))
        {
            person.Activity = person.TrainingDiscipline != null ? Activity.Training : Activity.Away;
            person.Path.Clear();
            person.Position = site.Outside;
            person.LastArrivalDay = -1;
        }
    }
}
