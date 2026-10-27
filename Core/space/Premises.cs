namespace Core.space;

public readonly record struct Cell(int X, int Y)
{
    public Cell Offset(int dx, int dy) => new(X + dx, Y + dy);
}

/// <summary>
/// Un objet du local. Posé au sol : sa case d'origine (X = U, Y = V, coin nord de l'emprise) et son orientation.
/// Accroché : l'objet hôte (HostId) et le socket occupé ; il n'a pas de case à lui. Au mur : le mur du fond (Wall) et la
/// case qui le longe (X = 0 pour le mur « left », Y = 0 pour le mur « right ») ; il ne prend aucune case au sol.
/// </summary>
public class PlacedItem
{
    public int Id { get; set; }
    public string DefId { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }

    /// <summary>Orientation du pack : se, sw, nw ou ne.</summary>
    public string Facing { get; set; } = "se";

    public int? HostId { get; set; }

    /// <summary>Mur du fond qui porte l'objet (« left » ou « right »), null pour un objet au sol ou accroché.</summary>
    public string? Wall { get; set; }

    /// <summary>Rang le long de son mur : V pour le mur « left », U pour le mur « right ».</summary>
    public int WallIndex => Wall == "left" ? Y : X;

    /// <summary>Socket de l'hôte occupé par cet objet (« chair », « monitor_main », « laptop_1 »...).</summary>
    public string? Socket { get; set; }

    /// <summary>Pour une chaise de poste : la personne qui y travaille.</summary>
    public int? AssignedPersonId { get; set; }

    /// <summary>Livré avec le local (ton lit, la cuisine d'un bureau loué) : il reste sur place et ne se revend pas.</summary>
    public bool Included { get; set; }

    /// <summary>Prix réellement payé (0 pour ce qui était déjà là) : la revente en rend la moitié.</summary>
    public decimal PaidPrice { get; set; }

    /// <summary>Couleurs choisies par canal de masque (« wood » → « oak »), purement visuelles.</summary>
    public Dictionary<string, string> Swatches { get; set; } = new();

    public FurnitureDef Def => FurnitureCatalog.Get(DefId);
}

/// <summary>Un poste de travail : un bureau et l'un de ses sockets de chaise (un bench en a trois).</summary>
public record Workstation(PlacedItem Desk, string ChairSocket)
{
    /// <summary>« » pour un bureau à un poste, « _1 » pour le deuxième poste d'un bench.</summary>
    public string Suffix => ChairSocket.Length > 5 ? ChairSocket[5..] : "";

    public int Index => Suffix.Length > 1 && int.TryParse(Suffix[1..], out int i) ? i : 0;
}

/// <summary>
/// Le local occupé : un plan de pièce du pack, grille de cases U × V (vue isométrique : U vers le bas-droite, V vers le
/// bas-gauche). Les murs du fond longent U = 0 et V = 0 ; la porte est dans l'un d'eux, sa case d'accès doit rester libre.
/// </summary>
public class Premises
{
    /// <summary>Identifiant du site dans la campagne.</summary>
    public int Id { get; set; }

    public string ListingId { get; set; } = "";

    /// <summary>Adresse de l'offre louée (« Flon ») : deux sites du même type ont deux adresses.</summary>
    public string Place { get; set; } = "";

    /// <summary>Plan du pack (rooms/*.json) : sols, murs et objets livrés se lisent là.</summary>
    public string PlanId { get; set; } = "";

    public string Name { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>Case juste à l'intérieur de la porte.</summary>
    public int EntranceX { get; set; }

    public int EntranceY { get; set; }

    /// <summary>Mur de la porte : « left » (U = 0) ou « right » (V = 0).</summary>
    public string EntranceWall { get; set; } = "left";

    /// <summary>Nombre maximal de postes (0 = seulement limité par la place). La chambre reste une chambre.</summary>
    public int MaxDesks { get; set; }

    public decimal Rent { get; set; }
    public decimal Deposit { get; set; }
    public List<PlacedItem> Items { get; set; } = new();

    public Cell Entrance => new(EntranceX, EntranceY);

    /// <summary>Juste dehors, derrière la porte : là où l'on se trouve hors des heures de travail.</summary>
    public Cell Outside => EntranceWall == "left" ? new(EntranceX - 1, EntranceY) : new(EntranceX, EntranceY - 1);

    public bool InBounds(Cell cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;

    public PlacedItem? Find(int id) => Items.FirstOrDefault(i => i.Id == id);

    public IEnumerable<PlacedItem> Floor => Items.Where(i => i.HostId == null && i.Wall == null);

    /// <summary>Les objets fixés aux murs du fond (affiches, horloge, stores, planning).</summary>
    public IEnumerable<PlacedItem> WallItems => Items.Where(i => i.Wall != null);

    public IEnumerable<PlacedItem> Desks => Floor.Where(i => i.Def.Kind == FurnitureKind.Desk);

    public IEnumerable<PlacedItem> Children(PlacedItem host) => Items.Where(i => i.HostId == host.Id);

    public PlacedItem? ChildAt(PlacedItem host, string socket) => Items.FirstOrDefault(i => i.HostId == host.Id && i.Socket == socket);

    /// <summary>L'objet au sol qui porte cet objet (lui-même s'il est au sol).</summary>
    public PlacedItem Root(PlacedItem item)
    {
        while (item.HostId is int host && Find(host) is { } parent)
            item = parent;
        return item;
    }

    /// <summary>
    /// Cases qui bloquent le passage : l'emprise de l'orientation. Comme dans le pack, un petit objet centré (corbeille,
    /// lampadaire, table de chevet), un tapis ou un objet accroché ne bloque pas : on passe à côté.
    /// </summary>
    public static IEnumerable<Cell> Footprint(FurnitureDef def, int x, int y, string facing)
    {
        var pack = def.Pack;
        if (def.Attached || pack.FloorDecor || pack.WallMount || pack.Ceiling || pack.Centered)
            yield break;
        foreach (var c in pack.Facing(facing).Collision)
            yield return new Cell(x + c.X, y + c.Y);
    }

    public IEnumerable<Cell> Footprint(PlacedItem item) =>
        item.HostId != null ? [] : Footprint(item.Def, item.X, item.Y, item.Facing);

    /// <summary>Cases où l'objet se trouve, même s'il ne bloque pas le passage (un objet centré est sur sa case).</summary>
    public static IEnumerable<Cell> Cells(FurnitureDef def, int x, int y, string facing) =>
        def.OnWall ? [] : def.Pack.Centered && !def.Attached ? [new Cell(x, y)] : Footprint(def, x, y, facing);

    public IEnumerable<Cell> Cells(PlacedItem item) =>
        item.HostId != null || item.Wall != null ? [] : Cells(item.Def, item.X, item.Y, item.Facing);

    /// <summary>Cases d'usage à laisser libres (devant un bureau : là où l'on s'assoit).</summary>
    public static IEnumerable<Cell> AccessCells(FurnitureDef def, int x, int y, string facing) =>
        def.Attached ? [] : def.Pack.Facing(facing).Access.Select(c => new Cell(x + c.X, y + c.Y));

    public IEnumerable<Cell> AccessCells(PlacedItem item) =>
        item.HostId != null || item.Wall != null ? [] : AccessCells(item.Def, item.X, item.Y, item.Facing);

    /// <summary>
    /// Les postes de travail : chaque socket de chaise de chaque bureau, et des établis et bancs de laboratoire (ils
    /// servent eux-mêmes d'outil de travail : pas d'ordinateur à ajouter).
    /// </summary>
    public IEnumerable<Workstation> Workstations =>
        Desks.Concat(Floor.Where(i => i.Def.Kind == FurnitureKind.Lab)).SelectMany(d => d.Def.Pack.Facing(d.Facing).Sockets.Keys.Where(s => s == "chair" || s.StartsWith("chair_")).Select(s => new Workstation(d, s)));

    public PlacedItem? ChairOf(Workstation post) => ChildAt(post.Desk, post.ChairSocket);

    public Workstation? SeatOf(int personId)
    {
        var chair = Items.FirstOrDefault(i => i.AssignedPersonId == personId && i.HostId != null);
        return chair == null || Find(chair.HostId!.Value) is not { } desk ? null : new Workstation(desk, chair.Socket!);
    }

    public PlacedItem? DeskOf(int personId) => SeatOf(personId)?.Desk;

    /// <summary>La case où la personne du poste s'assoit (case d'accès du bureau correspondant au poste).</summary>
    public Cell SeatCell(Workstation post)
    {
        var access = AccessCells(post.Desk).ToList();
        if (access.Count == 0)
            return new Cell(post.Desk.X, post.Desk.Y);
        // Un bench liste ses accès du dernier poste au premier
        return access[Math.Clamp(access.Count - 1 - post.Index, 0, access.Count - 1)];
    }

    /// <summary>L'ordinateur du poste : un portable dans son socket, ou une tour (qui demande alors un écran et un clavier).</summary>
    public PlacedItem? ComputerOf(Workstation post) =>
        post.Desk.Def.Kind == FurnitureKind.Lab ? post.Desk
        : ChildAt(post.Desk, "laptop" + post.Suffix) is { Def.Kind: FurnitureKind.Laptop } laptop ? laptop
        : post.Suffix == "" && ChildAt(post.Desk, "tower_floor") is { Def.Kind: FurnitureKind.Tower } tower ? tower
        : null;

    /// <summary>Les écrans du poste (le principal, le second, ceux de l'arrière du bureau).</summary>
    public IEnumerable<PlacedItem> MonitorsOf(Workstation post) =>
        Children(post.Desk).Where(c => c.Def.Kind == FurnitureKind.Monitor
                                       && (post.Suffix == "" ? !char.IsDigit(c.Socket![^1]) : c.Socket == "monitor" + post.Suffix));

    /// <summary>Le clavier du poste.</summary>
    public PlacedItem? KeyboardOf(Workstation post) => ChildAt(post.Desk, "keyboard" + post.Suffix) is { Def.Kind: FurnitureKind.Keyboard } k ? k : null;

    /// <summary>Grille des cases occupées par un objet au sol (sans l'objet ignoré, par exemple celui qu'on déplace).</summary>
    public bool[,] Occupancy(int? ignoreId = null)
    {
        var grid = new bool[Width, Height];
        foreach (var item in Floor)
        {
            if (item.Id == ignoreId)
                continue;
            foreach (var cell in Footprint(item))
                if (InBounds(cell))
                    grid[cell.X, cell.Y] = true;
        }
        return grid;
    }

    /// <summary>Nombre de postes que la surface permet raisonnablement (affiché dans les annonces).</summary>
    public int Capacity => MaxDesks > 0 ? MaxDesks : Width * Height / 6;
}
