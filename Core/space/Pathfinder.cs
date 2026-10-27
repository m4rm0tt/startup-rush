namespace Core.space;

/// <summary>
/// Recherche de chemin en largeur sur la grille du local (4 directions). À cette échelle (quelques centaines de cases)
/// c'est instantané, toujours le plus court, et l'on ne traverse jamais un meuble ni un mur.
/// </summary>
public static class Pathfinder
{
    private static readonly (int Dx, int Dy)[] Directions = [(0, -1), (1, 0), (0, 1), (-1, 0)];

    /// <summary>
    /// Chemin de « from » à « to », extrémités comprises. La case d'arrivée peut être occupée (une chaise, une machine) :
    /// on a le droit d'y aller, pas de la traverser. La case de départ peut être dehors, devant la porte.
    /// </summary>
    public static List<Cell>? FindPath(Premises premises, bool[,] blocked, Cell from, Cell to)
    {
        if (from == to)
            return [from];

        var start = from;
        var prefix = new List<Cell>();
        if (!premises.InBounds(from))
        {
            // Dehors : on entre forcément par la porte
            if (from != premises.Outside)
                return null;
            prefix.Add(from);
            start = premises.Entrance;
            if (blocked[start.X, start.Y] && start != to)
                return null;
            if (start == to)
                return [from, to];
        }

        if (!premises.InBounds(to))
        {
            if (to != premises.Outside)
                return null;
            var toDoor = FindPath(premises, blocked, start, premises.Entrance);
            if (toDoor == null)
                return null;
            toDoor.Add(to);
            prefix.AddRange(toDoor);
            return prefix;
        }

        int width = premises.Width, height = premises.Height;
        var previous = new int[width * height];
        Array.Fill(previous, -2);
        var queue = new Queue<Cell>();
        queue.Enqueue(start);
        previous[start.Y * width + start.X] = -1;

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            if (cell == to)
                break;
            foreach (var (dx, dy) in Directions)
            {
                var next = cell.Offset(dx, dy);
                if (!premises.InBounds(next) || previous[next.Y * width + next.X] != -2)
                    continue;
                if (blocked[next.X, next.Y] && next != to)
                    continue;
                previous[next.Y * width + next.X] = cell.Y * width + cell.X;
                queue.Enqueue(next);
            }
        }

        if (previous[to.Y * width + to.X] == -2)
            return null;

        var path = new List<Cell>();
        int index = to.Y * width + to.X;
        while (index != -1)
        {
            path.Add(new Cell(index % width, index / width));
            index = previous[index];
        }
        path.Reverse();
        prefix.AddRange(path);
        return prefix;
    }

    /// <summary>Toutes les cases libres atteignables depuis l'entrée.</summary>
    public static bool[,] Reachable(Premises premises, bool[,] blocked)
    {
        var reach = new bool[premises.Width, premises.Height];
        var entrance = premises.Entrance;
        if (blocked[entrance.X, entrance.Y])
            return reach;
        var queue = new Queue<Cell>();
        queue.Enqueue(entrance);
        reach[entrance.X, entrance.Y] = true;
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            foreach (var (dx, dy) in Directions)
            {
                var next = cell.Offset(dx, dy);
                if (!premises.InBounds(next) || reach[next.X, next.Y] || blocked[next.X, next.Y])
                    continue;
                reach[next.X, next.Y] = true;
                queue.Enqueue(next);
            }
        }
        return reach;
    }

    /// <summary>Une case (libre ou occupée) est accessible si elle est atteignable ou touche une case atteignable.</summary>
    public static bool IsAccessible(Premises premises, bool[,] reach, Cell cell)
    {
        if (!premises.InBounds(cell))
            return false;
        if (reach[cell.X, cell.Y])
            return true;
        foreach (var (dx, dy) in Directions)
        {
            var next = cell.Offset(dx, dy);
            if (premises.InBounds(next) && reach[next.X, next.Y])
                return true;
        }
        return false;
    }
}
