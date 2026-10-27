using Core.space;
using Core.team;

namespace Core.campaign;

/// <summary>
/// Installer un poste complet avec les commandes ordinaires : d'abord ce qui est dans les cartons, puis, si on l'accepte,
/// ce qui manque à l'achat. Sert aux bots, à la migration des anciennes sauvegardes et au réaménagement après un déménagement.
/// </summary>
public partial class Campaign
{
    public const string DefaultDesk = "desk_simple";
    public const string DefaultChair = "chair_budget";
    public const string DefaultTower = "tower_office";
    public const string DefaultMonitor = "monitor_basic";
    public const string DefaultKeyboard = "keyboard_mouse";
    public const string DefaultLaptop = "laptop_standard";

    /// <summary>La première place où poser ce meuble, case par case, dans une orientation possible.</summary>
    public (int X, int Y, string Facing)? FindSpot(FurnitureDef def)
    {
        var facings = def.Pack.Facings.Keys.OrderBy(f => f == def.Pack.DefaultFacing ? 0 : 1).ToList();
        for (int v = 0; v < Premises.Height; v++)
            for (int u = 0; u < Premises.Width; u++)
                foreach (string f in facings)
                    if (Layout.CanPlace(Premises, def, u, v, f).Ok)
                        return (u, v, f);
        return null;
    }

    private StoredItem? Stored(FurnitureKind kind) => Storage.FirstOrDefault(s => FurnitureCatalog.Get(s.DefId).Kind == kind);

    /// <summary>Accroche un objet des cartons de ce genre, sinon (si on achète) un neuf ; renvoie l'objet accroché.</summary>
    private PlacedItem? Equip(PlacedItem host, string socket, FurnitureKind kind, string buyId, bool buy)
    {
        if (Premises.ChildAt(host, socket) is { } already)
            return already;
        var ok = Stored(kind) is { } stored && Layout.CanAttach(Premises, FurnitureCatalog.Get(stored.DefId), host, socket).Ok
            ? AttachFromStorage(stored.Id, host.Id, socket)
            : buy ? BuyAndAttach(buyId, host.Id, socket) : CommandResult.Fail("");
        return ok.Ok ? Premises.ChildAt(host, socket) : null;
    }

    /// <summary>
    /// Fait en sorte qu'il existe un poste complet sans personne : complète un poste existant, ou installe un bureau.
    /// Renvoie ce poste, ou null s'il n'y a pas la place, pas l'argent, ou rien dans les cartons (sans achat).
    /// </summary>
    public Workstation? InstallWorkstation(bool buy)
    {
        bool Free(Workstation p) => Premises.ChairOf(p)?.AssignedPersonId == null;
        if (Layout.CompleteWorkstations(Premises).FirstOrDefault(Free) is { } ready)
            return ready;

        var post = Premises.Workstations.FirstOrDefault(Free);
        if (post == null)
        {
            CommandResult placed;
            if (Stored(FurnitureKind.Desk) is { } desk && FindSpot(FurnitureCatalog.Get(desk.DefId)) is var (x, y, f))
                placed = PlaceFromStorage(desk.Id, x, y, f);
            else if (buy && FindSpot(FurnitureCatalog.Get(DefaultDesk)) is var (bx, by, bf))
                placed = BuyAndPlace(DefaultDesk, bx, by, bf);
            else
                return null;
            if (!placed.Ok)
                return null;
            post = Premises.Workstations.Last(Free);
        }

        if (Equip(post.Desk, post.ChairSocket, FurnitureKind.Chair, DefaultChair, buy) == null)
            return null;
        if (Premises.ComputerOf(post) == null)
        {
            bool towerSlot = post.Suffix == "" && post.Desk.Def.Pack.Facing(post.Desk.Facing).Sockets.ContainsKey("tower_floor");
            bool laptopFirst = Stored(FurnitureKind.Laptop) != null || !towerSlot;
            if (laptopFirst)
                Equip(post.Desk, "laptop" + post.Suffix, FurnitureKind.Laptop, DefaultLaptop, buy);
            else
            {
                Equip(post.Desk, post.Suffix == "" ? "monitor_main" : "monitor" + post.Suffix, FurnitureKind.Monitor, DefaultMonitor, buy);
                Equip(post.Desk, "keyboard" + post.Suffix, FurnitureKind.Keyboard, DefaultKeyboard, buy);
                Equip(post.Desk, "tower_floor", FurnitureKind.Tower, DefaultTower, buy);
            }
        }
        return Layout.Check(Premises, post) == WorkstationIssue.None ? post : null;
    }

    /// <summary>Assoit une personne à ce poste (elle quitte son ancien poste).</summary>
    public void Seat(Person person, Workstation post)
    {
        foreach (var other in Premises.Items.Where(i => i.AssignedPersonId == person.Id))
            other.AssignedPersonId = null;
        if (Premises.ChairOf(post) is { } chair)
            chair.AssignedPersonId = person.Id;
    }

    /// <summary>Donne un poste complet à chacun de ceux qui n'en ont pas, avec les cartons (et des achats si on l'accepte).</summary>
    public void SeatEveryone(bool buy)
    {
        foreach (var person in People.OrderByDescending(p => p.IsFounder).ToList())
        {
            if (Premises.SeatOf(person.Id) is { } current && Layout.Check(Premises, current) == WorkstationIssue.None)
                continue;
            if (InstallWorkstation(buy) is not { } post)
                return;
            Seat(person, post);
        }
    }
}
