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
    /// <summary>Moral d'ambiance au plus (plantes, lampes, tapis, affiches, trophées...).</summary>
    public const double MaxAmbiance = 6;

    /// <summary>Moral apporté au plus par les objets personnels d'un poste.</summary>
    public const double MaxPersonal = 1;

    public const double MaxScreenBonus = 0.06;

    /// <summary>Périphériques d'un poste (casque, station d'accueil ; tablette graphique) : plafonds par poste.</summary>
    public const double MaxPeripheralFocus = 0.03, MaxPeripheralQuality = 0.03;
    public const int MaxUsefulBookshelves = 2;

    /// <summary>Moral au plus des coins détente (canapés, fauteuils).</summary>
    public const double MaxLounge = 4;

    /// <summary>Moral au plus du confort de base : moitié rangement, moitié sanitaires.</summary>
    public const double MaxBasics = 3;

    public const double MaxFocus = 0.04;
    public const double MaxCoordination = 3;

    /// <summary>Les petits supports (notes, croquis, carnets) comptent pour une personne au plus.</summary>
    public const double MaxSmallCoordination = 1;

    public const double MaxTraining = 0.4;

    /// <summary>Énergie d'une pause sans rien (3), et au plus (15).</summary>
    public const double BaseBreak = 3;

    public const double MaxBreak = 15;

    /// <summary>
    /// Le mur du fond que vise la case (x, y) pour un objet mural : « left » le long de U = 0, « right » le long de V = 0 ;
    /// dans le coin, l'orientation choisie départage (« se » : mur de gauche, « sw » : mur de droite). Null loin des murs.
    /// </summary>
    public static string? WallAt(int x, int y, string facing) =>
        x == 0 && y == 0 ? (facing == "sw" ? "right" : "left") : x == 0 ? "left" : y == 0 ? "right" : null;

    /// <summary>L'orientation d'un objet sur ce mur (le pack dessine le mur de gauche en « se », celui de droite en « sw »).</summary>
    public static string WallFacing(string wall) => wall == "left" ? "se" : "sw";

    /// <summary>
    /// Un objet mural se fixe sur un mur plein (des stores : sur une fenêtre), jamais sur la porte, sans chevaucher un autre
    /// objet du même mur. Un planning prend deux segments.
    /// </summary>
    public static PlacementCheck CanHang(Premises premises, FurnitureDef def, int x, int y, string facing, int? ignoreId = null)
    {
        string? wall = WallAt(x, y, facing);
        if (wall == null || !premises.InBounds(new Cell(x, y)))
            return PlacementCheck.Invalid("Cet objet se fixe au mur : clique une case le long d'un mur du fond.");
        var plan = Pack.Plan(premises.PlanId);
        int index = wall == "left" ? y : x;
        int span = Math.Max(1, def.Pack.Span);
        bool blinds = def.Id == "window_blinds";
        for (int k = 0; k < span; k++)
        {
            string? segment = plan.Segment(wall, index + k);
            // La seconde case d'une grande fenêtre n'a pas de segment à elle
            bool window = segment is "window" or "window_wide" || segment == null && plan.Segment(wall, index + k - 1) == "window_wide";
            if (segment == null && !window)
                return PlacementCheck.Invalid("Le mur s'arrête là.");
            if (segment == "door")
                return PlacementCheck.Invalid("La porte doit rester libre.");
            if (blinds != window)
                return PlacementCheck.Invalid(blinds ? "Les stores se posent sur une fenêtre." : "Il faut un mur plein, pas une fenêtre.");
        }
        foreach (var other in premises.WallItems.Where(i => i.Id != ignoreId && i.Wall == wall))
        {
            int start = other.WallIndex, end = start + Math.Max(1, other.Def.Pack.Span);
            if (index < end && start < index + span)
                return PlacementCheck.Invalid($"Il y a déjà : {other.Def.Name}.");
        }
        return PlacementCheck.Valid;
    }

    /// <summary>Vérifie qu'un objet peut être posé au sol en (x, y) dans cette orientation. « ignoreId » : l'objet qu'on déplace.</summary>
    /// <param name="freeStanding">Une chaise posée seule au sol (chaises d'attente des modules du pack), sans meuble hôte.</param>
    public static PlacementCheck CanPlace(Premises premises, FurnitureDef def, int x, int y, string facing, int? ignoreId = null, bool freeStanding = false)
    {
        if (def.Attached && !(freeStanding && def.Kind == FurnitureKind.Chair))
            return PlacementCheck.Invalid("Cet objet se pose sur un meuble : clique un bureau ou un plan de travail.");
        if (def.OnWall)
            return CanHang(premises, def, x, y, facing, ignoreId);
        if (!def.Pack.Facings.ContainsKey(facing))
            return PlacementCheck.Invalid("Cette orientation n'existe pas pour cet objet.");
        if (def.Pack.Ceiling)
        {
            // Au plafond : rien à gêner au sol, seulement un autre luminaire sur la même case
            if (!premises.InBounds(new Cell(x, y)))
                return PlacementCheck.Invalid("Hors du local.");
            return premises.Floor.Any(i => i.Id != ignoreId && i.Def.Pack.Ceiling && i.X == x && i.Y == y)
                ? PlacementCheck.Invalid("Il y a déjà un luminaire ici.") : PlacementCheck.Valid;
        }
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
        // Les chemins de câbles passent au-dessus (ou au ras du sol) : ils partagent la case
        bool shares(FurnitureDef d) => d.Pack.FloorDecor || d.Pack.Ceiling || d.Pack.Walkable;
        if (!shares(def) && premises.Floor.Any(i => i.Id != ignoreId && !shares(i.Def) && premises.Cells(i).Any(cells.Contains)))
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

    /// <summary>
    /// Un module de baie occupe « units » emplacements consécutifs à partir de « slot:i », dans une baie qui l'accepte (fiche
    /// « fits » du pack), sans chevaucher un autre module.
    /// </summary>
    public static PlacementCheck CanSlot(Premises premises, FurnitureDef def, PlacedItem rack, int index, int? ignoreId = null)
    {
        if (!def.Pack.Fits.Contains(rack.DefId))
            return PlacementCheck.Invalid($"{def.Name} se monte dans une baie.");
        int count = rack.Def.Pack.Facing(rack.Facing).Slots;
        int units = Math.Max(1, def.Pack.Units);
        if (index < 0 || index + units > count)
            return PlacementCheck.Invalid($"Plus assez de place dans la baie ({units}U).");
        foreach (var (module, slot) in Infrastructure.Modules(premises, rack))
            if (module.Id != ignoreId && index < slot + Math.Max(1, module.Def.Pack.Units) && slot < index + units)
                return PlacementCheck.Invalid("Ces emplacements sont pris.");
        return PlacementCheck.Valid;
    }

    /// <summary>Peut-on accrocher cet objet à ce socket de l'hôte ? (Un portable remplace l'écran et le clavier, et inversement.)</summary>
    public static PlacementCheck CanAttach(Premises premises, FurnitureDef def, PlacedItem host, string socket, int? ignoreId = null)
    {
        if (!def.Attached)
            return PlacementCheck.Invalid("Cet objet se pose au sol.");
        if (def.Pack.RackModule)
            return socket.StartsWith("slot:") && int.TryParse(socket[5..], out int index)
                ? CanSlot(premises, def, host, index, ignoreId) : PlacementCheck.Invalid($"{def.Name} se monte dans une baie.");
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
        def.Pack.RackModule
            ? Enumerable.Range(0, host.Def.Pack.Facing(host.Facing).Slots).Select(i => $"slot:{i}").FirstOrDefault(s => CanAttach(premises, def, host, s, ignoreId).Ok)
            : host.Def.Pack.Facing(host.Facing).Sockets.Keys.FirstOrDefault(s => CanAttach(premises, def, host, s, ignoreId).Ok);

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

    /// <summary>Moral commun : l'ambiance, les coins détente et le confort de base, chacun plafonné (voir Comfort).</summary>
    public static double SharedMorale(Premises premises, int people = 1)
    {
        var c = Comfort(premises, people);
        return c.Ambiance + c.Lounge + c.Basics;
    }

    /// <summary>Moral propre à un poste : son bureau, et les objets personnels posés dessus (plafonnés).</summary>
    public static double PostMorale(Premises premises, Workstation post) =>
        post.Desk.Def.Morale + Math.Min(MaxPersonal, premises.Children(post.Desk).Where(c => c.Def.Kind == FurnitureKind.Personal).Sum(c => c.Def.Morale));

    /// <summary>Gain des écrans en plus (grand écran, second écran), plafonné par poste.</summary>
    public static double ScreenBonus(Premises premises, Workstation post) =>
        Math.Min(MaxScreenBonus, premises.MonitorsOf(post).Sum(m => m.Def.ScreenBonus));

    /// <summary>
    /// Ce que les périphériques posés sur le bureau d'un poste apportent : production (casque ; station d'accueil seulement avec
    /// un portable) et qualité (tablette graphique), chacun plafonné par poste.
    /// </summary>
    public static (double Focus, double Quality) Peripherals(Premises premises, Workstation post)
    {
        bool laptop = premises.ComputerOf(post)?.Def.Kind == FurnitureKind.Laptop;
        var items = premises.Children(post.Desk).Where(c => c.Def.Kind == FurnitureKind.Peripheral && (c.Def.Id != "docking_station" || laptop)).ToList();
        return (Math.Min(MaxPeripheralFocus, items.Sum(i => i.Def.Focus)), Math.Min(MaxPeripheralQuality, items.Sum(i => i.Def.Quality)));
    }

    /// <summary>Personnes de plus par équipe sans perte de coordination (tableaux, planning, réunions, petits supports).</summary>
    public static int CoordinationBonus(Premises premises) => (int)Math.Floor(Comfort(premises, 1).Coordination);

    public static double TrainingBonus(Premises premises) => Comfort(premises, 1).Training;

    /// <summary>Énergie de plus que le fondateur récupère la nuit grâce à son lit (le meilleur, pas la somme).</summary>
    public static double NightRecovery(Premises premises) =>
        premises.Items.Select(i => i.Def.NightRecovery).DefaultIfEmpty(0).Max();

    /// <summary>Énergie rendue par une pause (de 3 à 15) : voir Comfort.</summary>
    public static double BreakRecovery(Premises premises, int people) => Comfort(premises, people).Break;

    /// <summary>Production en plus grâce aux cloisons (plafonnée, selon les personnes couvertes).</summary>
    public static double Focus(Premises premises, int people) => Comfort(premises, people).Focus;

    /// <summary>
    /// La synthèse du confort d'un local pour « people » personnes : chaque groupe d'objets agit sur un seul indicateur, avec
    /// un plafond et, quand un objet ne sert qu'un nombre de personnes, une couverture (cent plantes ne doublent rien, une
    /// fontaine ne désaltère pas trente personnes). Rien n'est obligatoire : un manque prive seulement d'un bonus.
    /// </summary>
    public static ComfortSummary Comfort(Premises premises, int people)
    {
        people = Math.Max(1, people);
        var items = premises.Items;
        double Covered(IEnumerable<PlacedItem> list) => Math.Min(1, list.Sum(i => i.Def.Covers) / (double)people);

        // Les murs comptent aussi (lambris), dans le même plafond
        double ambiance = Math.Min(MaxAmbiance, items.Where(i => i.Def.Kind is FurnitureKind.Plant or FurnitureKind.Ambiance).Sum(i => i.Def.Morale)
                                                + Finishes.Ambiance(premises));
        var lounge = items.Where(i => i.Def.Kind == FurnitureKind.Lounge).ToList();
        // Une table basse complète un coin pause : seule, elle ne sert à rien
        bool seated = lounge.Any(i => i.Def.Id != "table_coffee");
        var seats = lounge.Where(i => seated || i.Def.Id != "table_coffee").ToList();
        double loungeMorale = Math.Min(MaxLounge, seats.Sum(i => i.Def.Morale));

        // Confort de base : moitié rangement, moitié sanitaires (ceux de ton appartement quand tu es dans ta chambre)
        double storage = Covered(items.Where(i => i.Def.Kind == FurnitureKind.Storage));
        var sanitary = items.Where(i => i.Def.Kind == FurnitureKind.Sanitary).ToList();
        var toilets = sanitary.Where(i => i.Def.Id == "toilet").ToList();
        double hygiene = premises.ListingId == RealEstate.Apartment ? 1
            : toilets.Count == 0 ? 0
            : Covered(toilets) * (0.7 + (sanitary.Any(i => i.Def.Id == "washbasin") ? 0.2 : 0) + (sanitary.Any(i => i.Def.Id == "toilet_stall") ? 0.1 : 0));
        double basics = MaxBasics / 2 * storage + MaxBasics / 2 * hygiene;

        // Pause : chacun prend la meilleure boisson ou collation encore libre, et la meilleure place assise encore libre
        double Slots(IEnumerable<PlacedItem> services)
        {
            double total = 0;
            int left = people;
            foreach (var s in services.OrderByDescending(s => s.Def.BreakRecovery))
            {
                int n = Math.Min(left, s.Def.Covers);
                total += n * s.Def.BreakRecovery;
                left -= n;
                if (left == 0)
                    break;
            }
            return total / people;
        }
        // L'évier et la table basse complètent ce qui existe déjà : un petit plus pour les personnes qu'ils couvrent
        bool kitchen = items.Any(i => i.Def.Id is "fridge" or "microwave" || i.Def.Kind == FurnitureKind.Coffee);
        var sinks = items.Where(i => i.Def.Id == "kitchen_sink").ToList();
        var tables = seats.Where(i => i.Def.Id == "table_coffee").ToList();
        double refresh = Slots(items.Where(i => i.Def.Kind is FurnitureKind.Coffee or FurnitureKind.Refreshment && i.Def.Id != "kitchen_sink"))
                         + (kitchen && sinks.Count > 0 ? sinks[0].Def.BreakRecovery * Covered(sinks) : 0);
        double rest = Slots(seats.Except(tables)) + (tables.Count > 0 ? tables[0].Def.BreakRecovery * Covered(tables) : 0);
        double pause = Math.Min(MaxBreak, BaseBreak + refresh + rest);

        var partitions = items.Where(i => i.Def.Kind == FurnitureKind.Partition).ToList();
        // Cloisons (pour les personnes qu'elles couvrent) et traitement acoustique des finitions, dans le même plafond
        double focus = Math.Min(MaxFocus, Math.Min(MaxFocus, partitions.Sum(i => i.Def.Focus)) * Covered(partitions) + Finishes.AcousticFocus(premises));

        // Coordination : une table de réunion compte avec ses chaises ; les petits supports pour un quart chacun
        double big = 0, small = 0, training = 0;
        int bookcases = 0;
        foreach (var item in items)
        {
            var def = item.Def;
            int chairs = premises.Children(item).Count(c => c.Def.Kind == FurnitureKind.Chair);
            if (def.Id == "table_meeting_small" && chairs < 2 || def.Id == "table_meeting_large" && chairs < 4)
                continue;
            if (def.Coordination >= 0.5)
                big += def.Coordination;
            else
                small += def.Coordination;
            if (def.Id == "bookcase" && ++bookcases > MaxUsefulBookshelves)
                continue;
            training += def.TrainingBonus;
        }
        double coordination = Math.Min(MaxCoordination, big + Math.Min(MaxSmallCoordination, small));

        return new ComfortSummary(people, ambiance, loungeMorale, basics, storage, hygiene, pause, focus, coordination,
            Math.Min(MaxTraining, training), Math.Min(1, items.Sum(i => i.Def.Recruits)));
    }
}

/// <summary>Le confort d'un local, indicateur par indicateur, déjà plafonné : ce que l'aménagement apporte vraiment.</summary>
public record ComfortSummary(
    int People,
    double Ambiance,
    double Lounge,
    double Basics,
    double StorageCoverage,
    double HygieneCoverage,
    double Break,
    double Focus,
    double Coordination,
    double Training,
    int Recruits)
{
    /// <summary>Moral commun apporté (ambiance + détente + confort de base).</summary>
    public double Morale => Ambiance + Lounge + Basics;
}
