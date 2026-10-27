using Core.finance;
using Core.space;
using Core.team;

namespace Core.campaign;

/// <summary>Aménagement : acheter, poser, déplacer, revendre, attribuer les postes, déménager.</summary>
public partial class Campaign
{
    /// <summary>Achète un meuble et le pose en (x, y). Pour un ordinateur, (x, y) est un bureau.</summary>
    public CommandResult BuyAndPlace(string defId, int x, int y)
    {
        var def = FurnitureCatalog.Get(defId);
        var check = Layout.CanPlace(Premises, def, x, y);
        if (!check.Ok)
            return CommandResult.Fail(check.Reason);
        var paid = Spend(def.Price, Category.Equipment, $"Achat : {def.Name}");
        if (!paid.Ok)
            return paid;
        Place(def, x, y, NewId());
        return CommandResult.Success($"{def.Name} installé·e.");
    }

    /// <summary>Pose un meuble rangé dans les cartons (déjà payé).</summary>
    public CommandResult PlaceFromStorage(int storedId, int x, int y)
    {
        var stored = Storage.FirstOrDefault(s => s.Id == storedId);
        if (stored == null)
            return CommandResult.Fail("Objet introuvable dans les cartons.");
        var def = FurnitureCatalog.Get(stored.DefId);
        var check = Layout.CanPlace(Premises, def, x, y);
        if (!check.Ok)
            return CommandResult.Fail(check.Reason);
        Storage.Remove(stored);
        Place(def, x, y, stored.Id);
        return CommandResult.Success($"{def.Name} installé·e.");
    }

    private void Place(FurnitureDef def, int x, int y, int id)
    {
        var item = new PlacedItem { Id = id, DefId = def.Id, X = x, Y = y };
        if (def.OnDesk)
        {
            var desk = Premises.Desks.First(d => d.X <= x && x < d.X + d.Def.Width && d.Y == y);
            item.X = desk.X;
            item.Y = desk.Y;
            item.HostId = desk.Id;
        }
        Premises.Items.Add(item);
        InvalidateLayout();
    }

    public CommandResult Move(int itemId, int x, int y)
    {
        var item = Premises.Find(itemId);
        if (item == null || !item.Def.Movable)
            return CommandResult.Fail("Cet objet ne se déplace pas.");
        var check = Layout.CanPlace(Premises, item.Def, x, y, item.Id);
        if (!check.Ok)
            return CommandResult.Fail(check.Reason);

        int dx = x - item.X, dy = y - item.Y;
        if (item.Def.OnDesk)
        {
            var desk = Premises.Desks.First(d => d.X <= x && x < d.X + d.Def.Width && d.Y == y);
            item.X = desk.X;
            item.Y = desk.Y;
            item.HostId = desk.Id;
        }
        else
        {
            item.X = x;
            item.Y = y;
            // L'ordinateur suit son bureau
            foreach (var hosted in Premises.Items.Where(i => i.HostId == item.Id))
            {
                hosted.X += dx;
                hosted.Y += dy;
            }
        }
        InvalidateLayout();
        return CommandResult.Success();
    }

    /// <summary>Prix de revente : la moitié du prix d'achat (rien pour le vieux portable ni les équipements fixes).</summary>
    public static decimal ResaleValue(FurnitureDef def) => Math.Round(def.Price * (decimal)Tuning.Finance.ResaleShare / 5m) * 5m;

    public CommandResult Sell(int itemId)
    {
        var item = Premises.Find(itemId);
        if (item == null || !item.Def.Movable)
            return CommandResult.Fail("Cet objet ne se vend pas.");
        // Ce qui est posé dessus part dans les cartons
        foreach (var hosted in Premises.Items.Where(i => i.HostId == item.Id).ToList())
        {
            Premises.Items.Remove(hosted);
            Storage.Add(new StoredItem { Id = hosted.Id, DefId = hosted.DefId });
        }
        Premises.Items.Remove(item);
        decimal value = ResaleValue(item.Def);
        if (value > 0)
            Post(value, Category.Resale, $"Revente : {item.Def.Name}");
        InvalidateLayout();
        return CommandResult.Success(value > 0 ? $"{item.Def.Name} revendu·e {value:N0} CHF." : $"{item.Def.Name} retiré·e.");
    }

    public CommandResult SellStored(int storedId)
    {
        var stored = Storage.FirstOrDefault(s => s.Id == storedId);
        if (stored == null)
            return CommandResult.Fail("Objet introuvable.");
        Storage.Remove(stored);
        decimal value = ResaleValue(FurnitureCatalog.Get(stored.DefId));
        if (value > 0)
            Post(value, Category.Resale, $"Revente : {FurnitureCatalog.Get(stored.DefId).Name}");
        return CommandResult.Success();
    }

    /// <summary>Range un objet dans les cartons (pour le reposer ailleurs plus tard).</summary>
    public CommandResult Store(int itemId)
    {
        var item = Premises.Find(itemId);
        if (item == null || !item.Def.Movable)
            return CommandResult.Fail("Cet objet ne se range pas.");
        foreach (var hosted in Premises.Items.Where(i => i.HostId == item.Id).ToList())
        {
            Premises.Items.Remove(hosted);
            Storage.Add(new StoredItem { Id = hosted.Id, DefId = hosted.DefId });
        }
        Premises.Items.Remove(item);
        Storage.Add(new StoredItem { Id = item.Id, DefId = item.DefId });
        InvalidateLayout();
        return CommandResult.Success();
    }

    /// <summary>Attribue un bureau à une personne (chacun a au plus un bureau, chaque bureau au plus une personne).</summary>
    public CommandResult AssignDesk(int deskId, int? personId)
    {
        var desk = Premises.Find(deskId);
        if (desk is not { Def.Kind: FurnitureKind.Desk })
            return CommandResult.Fail("Ce n'est pas un bureau.");
        if (personId is int id)
        {
            if (FindPerson(id) == null)
                return CommandResult.Fail("Personne inconnue.");
            foreach (var other in Premises.Desks.Where(d => d.AssignedPersonId == id))
                other.AssignedPersonId = null;
        }
        desk.AssignedPersonId = personId;
        var person = personId is int pid ? FindPerson(pid) : null;
        if (person != null && person.Activity is Activity.Working or Activity.Idle)
            GoToPost(person);
        var issue = Layout.Check(Premises, desk);
        return CommandResult.Success(issue == WorkstationIssue.None ? "Poste attribué." : $"Poste attribué, mais incomplet : {Layout.Describe(issue)}.");
    }

    /// <summary>Coût total d'un déménagement : garantie, forfait et transport des meubles, moins la garantie actuelle rendue.</summary>
    public (decimal Upfront, decimal Refund) MoveCost(Listing listing)
    {
        int items = Premises.Items.Count(i => i.Def.Movable) + Storage.Count;
        decimal upfront = listing.Deposit + listing.MovingCost + items * RealEstate.MovingCostPerItem;
        return (upfront, Ledger.DepositHeld);
    }

    public CommandResult MoveTo(string listingId)
    {
        var listing = RealEstate.Get(listingId);
        if (listing.Id == Premises.ListingId)
            return CommandResult.Fail("Tu es déjà ici.");
        var (upfront, refund) = MoveCost(listing);
        if (!Ledger.CanSpend(upfront - refund))
            return CommandResult.Fail($"Il faut {upfront - refund:N0} CHF (garantie, déménageurs) pour emménager.");

        if (refund > 0)
            Post(refund, Category.DepositRefund, $"Garantie rendue : {Premises.Name}");
        if (listing.Deposit > 0)
            Post(-listing.Deposit, Category.Deposit, $"Garantie de loyer : {listing.Name}");
        Post(-(upfront - listing.Deposit), Category.Moving, $"Déménagement vers {listing.Name}");
        Ledger.DepositHeld = listing.Deposit;

        // Tout ce qui se déplace part dans les cartons, rien n'est perdu ni à racheter
        foreach (var item in Premises.Items.Where(i => i.Def.Movable))
            Storage.Add(new StoredItem { Id = item.Id, DefId = item.DefId });
        Premises = RealEstate.Create(listing, NewId);
        InvalidateLayout();
        foreach (var person in People)
        {
            person.Activity = person.TrainingDiscipline != null ? Activity.Training : Activity.Away;
            person.Path.Clear();
            person.Position = Premises.Outside;
            person.LastArrivalDay = -1;
        }
        Notify(Severity.Good, "local", $"Emménagement : {listing.Name}",
            $"{Storage.Count} objets sont dans les cartons. Ouvre Aménagement pour les installer. Nouveau loyer : {listing.Rent:N0} CHF par mois.", "build");
        return CommandResult.Success();
    }
}
