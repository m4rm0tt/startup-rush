namespace Core.space;

public readonly record struct Cell(int X, int Y)
{
    public Cell Offset(int dx, int dy) => new(X + dx, Y + dy);
}

/// <summary>Un meuble posé dans le local. Un ordinateur est posé sur un bureau (HostId) et n'occupe pas le sol.</summary>
public class PlacedItem
{
    public int Id { get; set; }
    public string DefId { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int? HostId { get; set; }

    /// <summary>Pour un bureau : la personne à qui le poste est attribué.</summary>
    public int? AssignedPersonId { get; set; }

    public FurnitureDef Def => FurnitureCatalog.Get(DefId);
}

/// <summary>
/// Le local occupé : une grille de cases vue de dessus (la rangée 0 longe le mur du fond).
/// La porte est dans le mur du bas ; la case juste à l'intérieur doit rester libre.
/// </summary>
public class Premises
{
    public string ListingId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public int DoorX { get; set; }

    /// <summary>Colonnes 0 à LivingWidth-1 : partie privée du logement, où l'on ne place pas de matériel d'entreprise.</summary>
    public int LivingWidth { get; set; }

    public decimal Rent { get; set; }
    public decimal Deposit { get; set; }
    public List<PlacedItem> Items { get; set; } = new();

    public Cell Entrance => new(DoorX, Height - 1);

    /// <summary>Juste dehors, derrière la porte : là où l'on arrive et d'où l'on part.</summary>
    public Cell Outside => new(DoorX, Height);

    public bool InBounds(Cell cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;

    public bool InLivingZone(Cell cell) => cell.X < LivingWidth;

    public PlacedItem? Find(int id) => Items.FirstOrDefault(i => i.Id == id);

    public IEnumerable<PlacedItem> Desks => Items.Where(i => i.Def.Kind == FurnitureKind.Desk);

    public IEnumerable<Cell> Footprint(PlacedItem item) => Footprint(item.Def, item.X, item.Y);

    public static IEnumerable<Cell> Footprint(FurnitureDef def, int x, int y)
    {
        if (def.OnDesk)
            yield break;
        for (int dy = 0; dy < def.Height; dy++)
        for (int dx = 0; dx < def.Width; dx++)
            yield return new Cell(x + dx, y + dy);
    }

    /// <summary>Case où l'on s'assoit pour travailler : juste derrière le bureau, côté mur du fond.</summary>
    public static Cell SeatCell(PlacedItem desk) => new(desk.X, desk.Y - 1);

    /// <summary>Case devant un objet (au sud), où l'on se tient pour l'utiliser.</summary>
    public static Cell FrontCell(FurnitureDef def, int x, int y) => new(x, y + def.Height);

    public PlacedItem? ChairAt(Cell cell) =>
        Items.FirstOrDefault(i => i.Def.Kind == FurnitureKind.Chair && i.X == cell.X && i.Y == cell.Y);

    public PlacedItem? ComputerOn(PlacedItem desk) =>
        Items.FirstOrDefault(i => i.HostId == desk.Id && i.Def.Kind == FurnitureKind.Computer);

    public PlacedItem? DeskOf(int personId) => Desks.FirstOrDefault(d => d.AssignedPersonId == personId);

    /// <summary>Grille des cases occupées par un meuble au sol (sans l'objet ignoré, par exemple celui qu'on déplace).</summary>
    public bool[,] Occupancy(int? ignoreId = null)
    {
        var grid = new bool[Width, Height];
        foreach (var item in Items)
        {
            if (item.Id == ignoreId)
                continue;
            foreach (var cell in Footprint(item))
                if (InBounds(cell))
                    grid[cell.X, cell.Y] = true;
        }
        return grid;
    }

    /// <summary>Nombre de bureaux que la surface de travail permet raisonnablement (affiché dans les annonces).</summary>
    public int Capacity => (Width - LivingWidth) * Height / 9;
}
