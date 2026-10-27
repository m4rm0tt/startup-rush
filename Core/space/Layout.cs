namespace Core.space;

public enum WorkstationIssue { None, NoChair, NoComputer, NoScreen, Unreachable }

public record PlacementCheck(bool Ok, string Reason)
{
    public static readonly PlacementCheck Valid = new(true, "");
    public static PlacementCheck Invalid(string reason) => new(false, reason);
}

/// <summary>
/// Les règles d'aménagement : où poser un objet au sol, où accrocher un objet dans un socket, ce qui fait un poste
/// complet, et les effets du mobilier avec leurs plafonds (rendement décroissant, pas de cumul sans fin).
/// </summary>
public static class Layout
{
    /// <summary>Moral d'ambiance au plus (plantes, lampes, tapis, rangement...).</summary>
    public const double MaxAmbiance = 6;

    /// <summary>Moral apporté au plus par les objets personnels d'un poste.</summary>
    public const double MaxPersonal = 1;

    public const double MaxScreenBonus = 0.06;
    public const int MaxUsefulWhiteboards = 2;
    public const int MaxUsefulBookshelves = 2;
    public const int PeoplePerCoffeeMachine = 8;

    /// <summary>Vérifie qu'un objet peut être posé au sol en (x, y) dans cette orientation. « ignoreId » : l'objet qu'on déplace.</summary>
    public static PlacementCheck CanPlace(Premises premises, FurnitureDef def, int x, int y, string facing, int? ignoreId = null)
    {
        if (def.Attached)
            return PlacementCheck.Invalid("Cet objet se pose sur un meuble : clique un bureau ou un plan de travail.");
        if (!def.Pack.Facings.ContainsKey(facing))
            return PlacementCheck.Invalid("Cette orientation n'existe pas pour cet objet.");
        if (def.Kind == FurnitureKind.Desk && premises.MaxDesks > 0 && premises.Desks.Count(d => d.Id != ignoreId) >= premises.MaxDesks)
            return PlacementCheck.Invalid($"{premises.Name} ne peut accueillir que {premises.MaxDesks} bureaux : il faut de vrais locaux.");

        var cells = Premises.Cells(def, x, y, facing).ToList();
        var blocking = Premises.Footprint(def, x, y, facing).ToList();
        var origin = new Cell(x, y);
        if (!premises.InBounds(origin) || cells.Any(c => !premises.InBounds(c)))
            return PlacementCheck.Invalid("Hors du local.");
        var occupied = premises.Occupancy(ignoreId);
        if (cells.Any(c => occupied[c.X, c.Y]))
            return PlacementCheck.Invalid("La place est déjà prise.");
        // Deux objets ne partagent pas une case, même s'ils ne bloquent pas le passage (tapis mis à part)
        if (!def.Pack.FloorDecor && premises.Floor.Any(i => i.Id != ignoreId && !i.Def.Pack.FloorDecor && premises.Cells(i).Any(cells.Contains)))
            return PlacementCheck.Invalid("La place est déjà prise.");
        if (cells.Contains(premises.Entrance))
            return PlacementCheck.Invalid("L'entrée doit rester libre.");
        foreach (var other in premises.Floor.Where(i => i.Id != ignoreId))
            if (premises.AccessCells(other).Any(cells.Contains))
                return PlacementCheck.Invalid($"Il faut garder la place devant : {other.Def.Name}.");

        var access = Premises.AccessCells(def, x, y, facing).ToList();
        if (access.Any(c => !premises.InBounds(c) || occupied[c.X, c.Y] || cells.Contains(c)))
            return PlacementCheck.Invalid(def.Kind == FurnitureKind.Desk ? "Il faut une case libre devant le bureau pour s'y asseoir." : "Il faut une case libre devant pour s'en servir.");

        // Ce qui était accessible doit le rester, et le nouvel objet doit être accessible
        var before = Requirements(premises, occupied, null, ignoreId);
        var after = (bool[,])occupied.Clone();
        foreach (var c in blocking)
            after[c.X, c.Y] = true;
        var placed = new PlacedItem { Id = -1, DefId = def.Id, X = x, Y = y, Facing = facing };
        foreach (var (key, satisfied) in Requirements(premises, after, placed, ignoreId))
        {
            if (satisfied)
                continue;
            if (key == "new")
                return PlacementCheck.Invalid(def.Kind == FurnitureKind.Desk ? "Personne ne pourrait atteindre ce bureau." : "Personne ne pourrait y accéder.");
            if (before.TryGetValue(key, out bool was) && was)
                return PlacementCheck.Invalid("Cela bloquerait le passage vers un poste ou un équipement.");
        }
        return PlacementCheck.Valid;
    }

    /// <summary>Les accès à vérifier : les cases d'usage de chaque objet doivent être atteignables depuis l'entrée.</summary>
    private static Dictionary<string, bool> Requirements(Premises premises, bool[,] blocked, PlacedItem? extra, int? ignoreId)
    {
        var reach = Pathfinder.Reachable(premises, blocked);
        var result = new Dictionary<string, bool>();
        var items = premises.Floor.Where(i => i.Id != ignoreId).ToList();
        if (extra != null)
            items.Add(extra);
        foreach (var item in items)
        {
            var access = premises.AccessCells(item).ToList();
            if (access.Count > 0)
                result[item == extra ? "new" : item.Id.ToString()] = access.All(c => premises.InBounds(c) && reach[c.X, c.Y]);
        }
        return result;
    }

    /// <summary>Peut-on accrocher cet objet à ce socket de l'hôte ? (Un portable remplace l'écran et le clavier, et inversement.)</summary>
    public static PlacementCheck CanAttach(Premises premises, FurnitureDef def, PlacedItem host, string socket, int? ignoreId = null)
    {
        if (!def.Attached)
            return PlacementCheck.Invalid("Cet objet se pose au sol.");
        if (!host.Def.Pack.Facing(host.Facing).Sockets.TryGetValue(socket, out var accepts))
            return PlacementCheck.Invalid("Pas d'emplacement ici.");
        if (!accepts.Intersect(def.Pack.Provides).Any())
            return PlacementCheck.Invalid($"{def.Name} ne va pas à cet emplacement.");
        if (premises.ChildAt(host, socket) is { } taken && taken.Id != ignoreId)
            return PlacementCheck.Invalid("Cet emplacement est déjà pris.");

        string suffix = socket.Contains('_') && char.IsDigit(socket[^1]) ? socket[socket.LastIndexOf('_')..] : "";
        bool Filled(string s) => premises.ChildAt(host, s) is { } c && c.Id != ignoreId;
        if (socket == "laptop" + suffix && (Filled((suffix == "" ? "monitor_main" : "monitor" + suffix)) || Filled("keyboard" + suffix)))
            return PlacementCheck.Invalid("Un portable remplace l'écran et le clavier : retire-les d'abord.");
        if ((socket == "keyboard" + suffix || socket == (suffix == "" ? "monitor_main" : "monitor" + suffix)) && Filled("laptop" + suffix))
            return PlacementCheck.Invalid("Il y a déjà un portable à ce poste.");
        return PlacementCheck.Valid;
    }

    /// <summary>Le premier socket de l'hôte où cet objet peut aller, dans l'ordre du pack, ou null.</summary>
    public static string? FreeSocket(Premises premises, FurnitureDef def, PlacedItem host, int? ignoreId = null) =>
        host.Def.Pack.Facing(host.Facing).Sockets.Keys.FirstOrDefault(s => CanAttach(premises, def, host, s, ignoreId).Ok);

    public static WorkstationIssue Check(Premises premises, Workstation post)
    {
        if (premises.ChairOf(post) == null)
            return WorkstationIssue.NoChair;
        var computer = premises.ComputerOf(post);
        if (computer == null)
            return WorkstationIssue.NoComputer;
        if (computer.Def.Kind == FurnitureKind.Tower && (!premises.MonitorsOf(post).Any() || premises.KeyboardOf(post) == null))
            return WorkstationIssue.NoScreen;
        var seat = premises.SeatCell(post);
        var reach = Pathfinder.Reachable(premises, premises.Occupancy());
        if (!premises.InBounds(seat) || !reach[seat.X, seat.Y])
            return WorkstationIssue.Unreachable;
        return WorkstationIssue.None;
    }

    public static string Describe(WorkstationIssue issue) => issue switch
    {
        WorkstationIssue.None => "Poste complet",
        WorkstationIssue.NoChair => "Il manque une chaise au bureau",
        WorkstationIssue.NoComputer => "Il manque un ordinateur (portable, ou tour sous le bureau)",
        WorkstationIssue.NoScreen => "La tour a besoin d'un écran et d'un clavier",
        _ => "On ne peut pas atteindre ce poste"
    };

    public static IEnumerable<Workstation> CompleteWorkstations(Premises premises) =>
        premises.Workstations.Where(p => Check(premises, p) == WorkstationIssue.None);

    /// <summary>Moral commun : l'ambiance (plantes, lampes, tapis, rangement), plafonnée, plus les coins détente.</summary>
    public static double SharedMorale(Premises premises)
    {
        double ambiance = premises.Items.Where(i => i.Def.Kind is FurnitureKind.Plant or FurnitureKind.Ambiance).Sum(i => i.Def.Morale);
        double lounges = premises.Items.Where(i => i.Def.Kind == FurnitureKind.Lounge).Sum(i => i.Def.Morale);
        return Math.Min(MaxAmbiance, ambiance) + Math.Min(4, lounges);
    }

    /// <summary>Moral propre à un poste : son bureau, et les objets personnels posés dessus (plafonnés).</summary>
    public static double PostMorale(Premises premises, Workstation post) =>
        post.Desk.Def.Morale + Math.Min(MaxPersonal, premises.Children(post.Desk).Where(c => c.Def.Kind == FurnitureKind.Personal).Sum(c => c.Def.Morale));

    /// <summary>Gain des écrans en plus (grand écran, second écran), plafonné par poste.</summary>
    public static double ScreenBonus(Premises premises, Workstation post) =>
        Math.Min(MaxScreenBonus, premises.MonitorsOf(post).Sum(m => m.Def.ScreenBonus));

    public static int CoordinationBonus(Premises premises) =>
        Math.Min(premises.Items.Where(i => i.Def.Kind == FurnitureKind.Whiteboard).Sum(i => i.Def.Coordination), MaxUsefulWhiteboards);

    public static double TrainingBonus(Premises premises) =>
        Math.Min(premises.Items.Count(i => i.Def.Kind == FurnitureKind.Bookshelf), MaxUsefulBookshelves) * 0.15;

    /// <summary>Énergie de plus que le fondateur récupère la nuit grâce à son lit (le meilleur, pas la somme).</summary>
    public static double NightRecovery(Premises premises) =>
        premises.Items.Select(i => i.Def.NightRecovery).DefaultIfEmpty(0).Max();

    /// <summary>Les endroits où faire une pause, avec l'énergie qu'ils rendent.</summary>
    public static IEnumerable<PlacedItem> BreakSpots(Premises premises) =>
        premises.Items.Where(i => i.Def.BreakRecovery > 0);

    /// <summary>Énergie rendue par une pause : le meilleur coin, réduit s'il y a trop de monde pour trop peu de machines.</summary>
    public static double BreakRecovery(Premises premises, int people)
    {
        var spots = BreakSpots(premises).ToList();
        if (spots.Count == 0)
            return 3;
        double best = spots.Max(s => s.Def.BreakRecovery);
        int machines = spots.Count(s => s.Def.Kind == FurnitureKind.Coffee);
        double crowding = machines == 0 ? 1 : Math.Min(1, machines * PeoplePerCoffeeMachine / (double)Math.Max(1, people));
        return 3 + (best - 3) * crowding;
    }
}
