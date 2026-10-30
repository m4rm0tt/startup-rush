using Core.finance;
using Core.space;
using Core.team;

namespace Core.campaign;

/// <summary>
/// Aménagement : acheter, poser au sol ou accrocher à un socket, tourner, déplacer, ranger, revendre, attribuer les postes,
/// déménager. Un objet accroché suit son hôte ; ranger ou revendre un hôte range ce qui est dessus.
/// </summary>
public partial class Campaign
{
    /// <summary>Achète un objet et le pose au sol en (x, y) dans cette orientation.</summary>
    public CommandResult BuyAndPlace(string defId, int x, int y, string? facing = null)
    {
        var def = FurnitureCatalog.Get(defId);
        if (!def.Buyable)
            return CommandResult.Fail("Cet objet ne s'achète pas.");
        if (def.UnlockNode is { } node && !IsUnlocked(node))
            return CommandResult.Fail($"À débloquer : {UnlockName(node)}.");
        string f = facing ?? def.Pack.DefaultFacing;
        var check = Layout.CanPlace(Premises, def, x, y, f);
        if (!check.Ok)
            return CommandResult.Fail(check.Reason);
        var paid = Spend(def.Price, Category.Equipment, $"Achat : {def.Name}", Premises.Id);
        if (!paid.Ok)
            return paid;
        Premises.Items.Add(Placed(NewId(), def, x, y, f, def.Price));
        InvalidateLayout();
        return CommandResult.Success($"{def.Name} installé·e.");
    }

    /// <summary>Un objet posé en (x, y) : au sol, ou au mur que vise la case (son orientation suit alors le mur).</summary>
    private static PlacedItem Placed(int id, FurnitureDef def, int x, int y, string facing, decimal paid)
    {
        var item = new PlacedItem { Id = id, DefId = def.Id, X = x, Y = y, Facing = facing, PaidPrice = paid };
        if (def.OnWall && Layout.WallAt(x, y, facing) is { } wall)
        {
            item.Wall = wall;
            item.Facing = Layout.WallFacing(wall);
        }
        return item;
    }

    /// <summary>Achète un objet et l'accroche à un objet posé (écran sur un bureau, machine à café sur un plan de travail).</summary>
    public CommandResult BuyAndAttach(string defId, int hostId, string? socket = null)
    {
        var def = FurnitureCatalog.Get(defId);
        if (!def.Buyable)
            return CommandResult.Fail("Cet objet ne s'achète pas.");
        if (def.UnlockNode is { } node && !IsUnlocked(node))
            return CommandResult.Fail($"À débloquer : {UnlockName(node)}.");
        var (check, host, s) = AttachCheck(def, hostId, socket, null);
        if (!check.Ok)
            return CommandResult.Fail(check.Reason);
        var paid = Spend(def.Price, Category.Equipment, $"Achat : {def.Name}", Premises.Id);
        if (!paid.Ok)
            return paid;
        Premises.Items.Add(new PlacedItem { Id = NewId(), DefId = def.Id, HostId = host!.Id, Socket = s, Facing = host.Facing, PaidPrice = def.Price });
        InvalidateLayout();
        return CommandResult.Success($"{def.Name} installé·e.");
    }

    private (PlacementCheck Check, PlacedItem? Host, string Socket) AttachCheck(FurnitureDef def, int hostId, string? socket, int? ignoreId)
    {
        var host = Premises.Find(hostId);
        if (host == null)
            return (PlacementCheck.Invalid("Il faut cliquer un meuble posé."), null, "");
        string? s = socket ?? Layout.FreeSocket(Premises, def, host, ignoreId);
        if (s == null)
            return (PlacementCheck.Invalid($"Pas d'emplacement libre pour {def.Name} sur {host.Def.Name}."), host, "");
        return (Layout.CanAttach(Premises, def, host, s, ignoreId), host, s);
    }

    /// <summary>Pose au sol un objet rangé dans les cartons (déjà payé).</summary>
    public CommandResult PlaceFromStorage(int storedId, int x, int y, string? facing = null)
    {
        var stored = Storage.FirstOrDefault(s => s.Id == storedId);
        if (stored == null)
            return CommandResult.Fail("Objet introuvable dans les cartons.");
        var def = FurnitureCatalog.Get(stored.DefId);
        string f = facing ?? def.Pack.DefaultFacing;
        var check = Layout.CanPlace(Premises, def, x, y, f);
        if (!check.Ok)
            return CommandResult.Fail(check.Reason);
        Storage.Remove(stored);
        Premises.Items.Add(Placed(stored.Id, def, x, y, f, stored.PaidPrice));
        InvalidateLayout();
        return CommandResult.Success($"{def.Name} installé·e.");
    }

    /// <summary>Accroche à un objet posé un objet rangé dans les cartons.</summary>
    public CommandResult AttachFromStorage(int storedId, int hostId, string? socket = null)
    {
        var stored = Storage.FirstOrDefault(s => s.Id == storedId);
        if (stored == null)
            return CommandResult.Fail("Objet introuvable dans les cartons.");
        var def = FurnitureCatalog.Get(stored.DefId);
        var (check, host, s) = AttachCheck(def, hostId, socket, null);
        if (!check.Ok)
            return CommandResult.Fail(check.Reason);
        Storage.Remove(stored);
        Premises.Items.Add(new PlacedItem { Id = stored.Id, DefId = def.Id, HostId = host!.Id, Socket = s, Facing = host.Facing, PaidPrice = stored.PaidPrice });
        InvalidateLayout();
        return CommandResult.Success($"{def.Name} installé·e.");
    }

    /// <summary>Déplace un objet au sol (ce qui est accroché dessus suit), éventuellement dans une autre orientation.</summary>
    public CommandResult Move(int itemId, int x, int y, string? facing = null)
    {
        var item = Premises.Find(itemId);
        if (item == null || item.HostId != null)
            return CommandResult.Fail("Cet objet ne se déplace pas ainsi.");
        string f = facing ?? item.Facing;
        var check = Layout.CanPlace(Premises, item.Def, x, y, f, item.Id);
        if (!check.Ok)
            return CommandResult.Fail(check.Reason);
        var moved = Placed(item.Id, item.Def, x, y, f, item.PaidPrice);
        item.X = x;
        item.Y = y;
        item.Facing = moved.Facing;
        item.Wall = moved.Wall;
        foreach (var child in AllChildren(item))
            child.Facing = f;
        InvalidateLayout();
        return CommandResult.Success();
    }

    /// <summary>Tourne un objet au sol vers la prochaine orientation possible à la même place.</summary>
    public CommandResult Rotate(int itemId)
    {
        var item = Premises.Find(itemId);
        if (item == null || item.HostId != null)
            return CommandResult.Fail("Cet objet ne tourne pas.");
        if (item.Wall != null && !(item.X == 0 && item.Y == 0))
            return CommandResult.Fail("Un objet mural suit son mur : déplace-le dans le coin pour changer de mur.");
        var facings = item.Def.Pack.Facings.Keys.ToList();
        int start = facings.IndexOf(item.Facing);
        for (int k = 1; k < facings.Count; k++)
        {
            string f = facings[(start + k) % facings.Count];
            if (Layout.CanPlace(Premises, item.Def, item.X, item.Y, f, item.Id).Ok)
                return Move(itemId, item.X, item.Y, f);
        }
        return CommandResult.Fail(facings.Count > 1 ? "Pas la place de le tourner ici." : "Cet objet n'a qu'une orientation.");
    }

    private List<PlacedItem> AllChildren(PlacedItem host)
    {
        var result = new List<PlacedItem>();
        foreach (var child in Premises.Children(host).ToList())
        {
            result.Add(child);
            result.AddRange(AllChildren(child));
        }
        return result;
    }

    /// <summary>Prix de revente : la moitié de ce qui a été payé (rien pour ce qui était déjà là).</summary>
    public static decimal ResaleValue(decimal paid) => Math.Round(paid * (decimal)Tuning.Finance.ResaleShare / 5m) * 5m;

    /// <summary>Retire un objet du local vers les cartons, avec ce qui est accroché dessus. Les chaises libèrent leur poste.</summary>
    private void Unplace(PlacedItem item)
    {
        foreach (var child in AllChildren(item).Prepend(item))
        {
            Premises.Items.Remove(child);
            if (!child.Included)
                Storage.Add(new StoredItem { Id = child.Id, DefId = child.DefId, PaidPrice = child.PaidPrice });
        }
        InvalidateLayout();
    }

    public CommandResult Sell(int itemId)
    {
        var item = Premises.Find(itemId);
        if (item == null || item.Included)
            return CommandResult.Fail("Cet objet fait partie du local : il ne se vend pas.");
        Unplace(item);
        var stored = Storage.First(s => s.Id == item.Id);
        Storage.Remove(stored);
        decimal value = ResaleValue(item.PaidPrice);
        if (value > 0)
            Post(value, Category.Resale, $"Revente : {item.Def.Name}", siteId: Premises.Id);
        return CommandResult.Success(value > 0 ? $"{item.Def.Name} revendu·e {value:N0} CHF." : $"{item.Def.Name} retiré·e (sans valeur de revente).");
    }

    public CommandResult SellStored(int storedId)
    {
        var stored = Storage.FirstOrDefault(s => s.Id == storedId);
        if (stored == null)
            return CommandResult.Fail("Objet introuvable.");
        Storage.Remove(stored);
        decimal value = ResaleValue(stored.PaidPrice);
        if (value > 0)
            Post(value, Category.Resale, $"Revente : {FurnitureCatalog.Get(stored.DefId).Name}");
        return CommandResult.Success();
    }

    /// <summary>Range un objet dans les cartons (pour le reposer ailleurs plus tard), avec ce qui est accroché dessus.</summary>
    public CommandResult Store(int itemId)
    {
        var item = Premises.Find(itemId);
        if (item == null || item.Included)
            return CommandResult.Fail("Cet objet fait partie du local : il reste en place.");
        Unplace(item);
        return CommandResult.Success();
    }

    /// <summary>
    /// Attribue un poste du bureau à une personne : celui où elle est déjà, sinon le premier poste libre qui a une chaise.
    /// Chacun a au plus un poste ; personId null libère les postes de ce bureau.
    /// </summary>
    public CommandResult AssignDesk(int deskId, int? personId)
    {
        var desk = Premises.Find(deskId);
        if (desk is not { Def.Kind: FurnitureKind.Desk })
            return CommandResult.Fail("Ce n'est pas un bureau.");
        var chairs = Premises.Workstations.Where(p => p.Desk.Id == deskId).Select(Premises.ChairOf).Where(c => c != null).Select(c => c!).ToList();
        if (personId is not int id)
        {
            foreach (var c in chairs)
                c.AssignedPersonId = null;
            return CommandResult.Success("Poste libéré.");
        }
        if (FindPerson(id) is not { } person)
            return CommandResult.Fail("Personne inconnue.");
        var chair = chairs.FirstOrDefault(c => c.AssignedPersonId == id) ?? chairs.FirstOrDefault(c => c.AssignedPersonId == null);
        if (chair == null)
            return CommandResult.Fail(chairs.Count == 0 ? "Il faut d'abord une chaise à ce bureau." : "Tous les postes de ce bureau sont pris.");
        foreach (var other in Sites.SelectMany(s => s.Items).Where(i => i.AssignedPersonId == id))
            other.AssignedPersonId = null;
        chair.AssignedPersonId = id;
        if (person.Activity is Activity.Working or Activity.Idle)
            TakePlace(person);
        var issue = Layout.Check(Premises, Premises.SeatOf(id)!);
        return CommandResult.Success(issue == WorkstationIssue.None ? "Poste attribué." : $"Poste attribué, mais incomplet : {Layout.Describe(issue)}.");
    }

    /// <summary>
    /// Les objets muraux du plan sont devenus de vrais objets du local (ils comptent dans le confort). Une sauvegarde plus
    /// ancienne ne les a pas : on les ajoute une fois au chargement, comme livrés avec le local.
    /// </summary>
    public bool WallItemsImported { get; set; }

    public void ImportWallItems()
    {
        if (WallItemsImported)
            return;
        foreach (var site in Sites.Where(s => !s.WallItems.Any() && s.PlanId != ""))
            site.Items.AddRange(RealEstate.WallItems(Pack.Plan(site.PlanId), NewId));
        WallItemsImported = true;
        InvalidateLayout();
    }
}
