namespace Core.space;

public enum WorkstationIssue { None, NoChair, NoComputer, Unreachable }

public record PlacementCheck(bool Ok, string Reason)
{
    public static readonly PlacementCheck Valid = new(true, "");
    public static PlacementCheck Invalid(string reason) => new(false, reason);
}

/// <summary>
/// Les règles d'aménagement : où l'on peut poser un meuble, ce qui fait un poste de travail complet,
/// et les effets du mobilier (avec leurs plafonds).
/// </summary>
public static class Layout
{
    public const int MaxUsefulPlants = 6;
    public const int MaxUsefulWhiteboards = 2;
    public const int MaxUsefulBookshelves = 2;
    public const int PeoplePerCoffeeMachine = 8;

    /// <summary>
    /// Vérifie qu'un meuble peut être posé en (x, y). Pour un ordinateur, (x, y) est la position d'un bureau.
    /// « ignoreId » est l'objet qu'on déplace : il ne se gêne pas lui-même.
    /// </summary>
    public static PlacementCheck CanPlace(Premises premises, FurnitureDef def, int x, int y, int? ignoreId = null)
    {
        if (!def.Movable)
            return PlacementCheck.Invalid("Équipement fixe.");

        if (def.OnDesk)
        {
            var desk = premises.Desks.FirstOrDefault(d => d.X <= x && x < d.X + d.Def.Width && d.Y == y);
            if (desk == null)
                return PlacementCheck.Invalid("Un ordinateur se pose sur un bureau.");
            var existing = premises.ComputerOn(desk);
            if (existing != null && existing.Id != ignoreId)
                return PlacementCheck.Invalid("Ce bureau a déjà un ordinateur.");
            return PlacementCheck.Valid;
        }

        if (def.Kind == FurnitureKind.Desk && premises.MaxDesks > 0 && premises.Desks.Count(d => d.Id != ignoreId) >= premises.MaxDesks)
            return PlacementCheck.Invalid($"{premises.Name} ne peut accueillir que {premises.MaxDesks} postes : il faut de vrais locaux.");

        var cells = Premises.Footprint(def, x, y).ToList();
        if (cells.Any(c => !premises.InBounds(c)))
            return PlacementCheck.Invalid("Hors du local.");
        if (def.Kind is not (FurnitureKind.Plant or FurnitureKind.Decor) && cells.Any(premises.InLivingZone))
            return PlacementCheck.Invalid("Partie privée du logement : pas de matériel de travail ici.");

        var occupied = premises.Occupancy(ignoreId);
        if (cells.Any(c => occupied[c.X, c.Y]))
            return PlacementCheck.Invalid("La place est déjà prise.");
        if (cells.Contains(premises.Entrance))
            return PlacementCheck.Invalid("L'entrée doit rester libre.");

        // Derrière un bureau, seule une chaise a sa place
        if (def.Kind != FurnitureKind.Chair)
        {
            foreach (var desk in premises.Desks.Where(d => d.Id != ignoreId))
                if (cells.Contains(Premises.SeatCell(desk)))
                    return PlacementCheck.Invalid("Il faut garder la place de la chaise derrière ce bureau.");
        }

        if (def.Kind == FurnitureKind.Desk)
        {
            var seat = new Cell(x, y - 1);
            if (!premises.InBounds(seat))
                return PlacementCheck.Invalid("Pas de place pour la chaise derrière le bureau (mur).");
            if (occupied[seat.X, seat.Y] && premises.ChairAt(seat) == null)
                return PlacementCheck.Invalid("La place de la chaise, derrière le bureau, est prise.");
            if (seat == premises.Entrance)
                return PlacementCheck.Invalid("La chaise bloquerait l'entrée.");
        }

        if (def.HasFrontAccess)
        {
            var front = Premises.FrontCell(def, x, y);
            if (!premises.InBounds(front) || occupied[front.X, front.Y])
                return PlacementCheck.Invalid("Il faut une case libre devant pour s'en servir.");
        }

        // Ce qui était accessible doit le rester, et le nouvel objet doit être accessible
        var before = Requirements(premises, occupied, extra: null);
        var after = (bool[,])occupied.Clone();
        foreach (var c in cells)
            after[c.X, c.Y] = true;
        var placed = new PlacedItem { Id = -1, DefId = def.Id, X = x, Y = y };
        var afterRequirements = Requirements(premises, after, placed, ignoreId);

        foreach (var (key, satisfied) in afterRequirements)
        {
            if (satisfied)
                continue;
            if (key.StartsWith("new:"))
                return PlacementCheck.Invalid(def.Kind == FurnitureKind.Desk
                    ? "Personne ne pourrait atteindre la chaise de ce bureau."
                    : "Personne ne pourrait y accéder.");
            if (before.TryGetValue(key, out bool was) && was)
                return PlacementCheck.Invalid("Cela bloquerait le passage vers un poste ou un équipement.");
        }

        return PlacementCheck.Valid;
    }

    /// <summary>Les accès à vérifier : chaque place de chaise, chaque devant d'équipement, et l'entrée.</summary>
    private static Dictionary<string, bool> Requirements(Premises premises, bool[,] blocked, PlacedItem? extra, int? ignoreId = null)
    {
        var reach = Pathfinder.Reachable(premises, blocked);
        var result = new Dictionary<string, bool>();
        var items = premises.Items.Where(i => i.Id != ignoreId).ToList();
        if (extra != null)
            items.Add(extra);

        foreach (var item in items)
        {
            string key = item == extra ? "new:" + item.Id : item.Id.ToString();
            if (item.Def.Kind == FurnitureKind.Desk)
                result[key] = Pathfinder.IsAccessible(premises, reach, Premises.SeatCell(item));
            else if (item.Def.HasFrontAccess)
            {
                var front = Premises.FrontCell(item.Def, item.X, item.Y);
                result[key] = premises.InBounds(front) && reach[front.X, front.Y];
            }
        }
        return result;
    }

    public static WorkstationIssue Check(Premises premises, PlacedItem desk)
    {
        var seat = Premises.SeatCell(desk);
        if (premises.ChairAt(seat) == null)
            return WorkstationIssue.NoChair;
        if (premises.ComputerOn(desk) == null)
            return WorkstationIssue.NoComputer;
        var reach = Pathfinder.Reachable(premises, premises.Occupancy());
        if (!Pathfinder.IsAccessible(premises, reach, seat))
            return WorkstationIssue.Unreachable;
        return WorkstationIssue.None;
    }

    public static string Describe(WorkstationIssue issue) => issue switch
    {
        WorkstationIssue.None => "Poste complet",
        WorkstationIssue.NoChair => "Il manque une chaise derrière le bureau",
        WorkstationIssue.NoComputer => "Il manque un ordinateur sur le bureau",
        _ => "La chaise est inaccessible"
    };

    public static IEnumerable<PlacedItem> CompleteWorkstations(Premises premises) =>
        premises.Desks.Where(d => Check(premises, d) == WorkstationIssue.None);

    /// <summary>Moral apporté par le mobilier commun, avec les plafonds (les plantes au-delà de 6 ne comptent plus).</summary>
    public static double SharedMorale(Premises premises)
    {
        double plants = premises.Items.Count(i => i.Def.Kind == FurnitureKind.Plant);
        double sofas = premises.Items.Where(i => i.Def.Kind == FurnitureKind.Sofa).Sum(i => i.Def.Morale);
        return Math.Min(plants, MaxUsefulPlants) * FurnitureCatalog.Get("plant").Morale + Math.Min(sofas, 4);
    }

    public static int CoordinationBonus(Premises premises) =>
        Math.Min(premises.Items.Count(i => i.Def.Kind == FurnitureKind.Whiteboard), MaxUsefulWhiteboards);

    public static double TrainingBonus(Premises premises) =>
        Math.Min(premises.Items.Count(i => i.Def.Kind == FurnitureKind.Bookshelf), MaxUsefulBookshelves) * 0.15;

    /// <summary>Les endroits où faire une pause, avec l'énergie qu'ils rendent.</summary>
    public static IEnumerable<PlacedItem> BreakSpots(Premises premises) =>
        premises.Items.Where(i => i.Def.BreakRecovery > 0);

    /// <summary>Énergie rendue par une pause : la meilleure machine, réduite si trop de monde pour trop peu de machines.</summary>
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
