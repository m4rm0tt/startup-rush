using Core.space;

namespace Core.campaign;

/// <summary>
/// Les déchets du local. Chaque jour de travail, chacun produit des déchets (emballages, gobelets, papiers) : ceux qui ont une
/// corbeille à portée (corbeilles et bacs de tri du site, selon les personnes qu'ils couvrent) les y jettent, les autres les
/// laissent par terre. Les déchets au sol coûtent du moral à tout le site (plafonné) ; on les ramasse en cliquant dessus, ou
/// tout d'un coup depuis l'aménagement. Quand tout le monde a sa corbeille, l'équipe ramasse un déchet par jour toute seule.
/// Simulation volontairement légère : un compte par site, tiré d'un hasard propre (il ne touche pas au hasard de la partie).
/// </summary>
public partial class Campaign
{
    /// <summary>Moral perdu par déchet au sol, et au plus pour tout le site.</summary>
    public const double LitterMorale = 0.75;

    public const double MaxLitterMorale = 5;

    /// <summary>Chance qu'une personne sans corbeille laisse un déchet par terre, chaque jour de travail.</summary>
    public const double LitterChance = 0.5;

    /// <summary>Déchets au sol au plus par site (au-delà, le local ne devient pas « plus sale » à l'écran).</summary>
    public const int MaxLitter = 24;

    /// <summary>Personnes qu'une corbeille ou des bacs de tri couvrent sur ce site.</summary>
    public static int BinCapacity(Premises site) =>
        site.Items.Where(i => i.DefId is "trash_bin" or "recycling_station").Sum(i => i.Def.Covers);

    /// <summary>Moral perdu sur ce site à cause des déchets au sol.</summary>
    public static double MessPenalty(Premises site) => Math.Min(MaxLitterMorale, site.Litter.Count * LitterMorale);

    /// <summary>Les déchets de la journée qui s'achève (appelé chaque nuit, avant le moral).</summary>
    private void RunLitter(long day)
    {
        bool workday = GameTime.IsWorkday(GameTime.StartOfDay(day));
        foreach (var site in Sites)
        {
            int present = People.Count(p => !OnVacation(p) && p.TrainingDiscipline == null && Minute >= p.StartsAt && SiteOf(p) == site);
            if (present == 0)
                continue;
            int uncovered = Math.Max(0, present - BinCapacity(site));
            if (uncovered == 0)
            {
                // Tout le monde a sa corbeille : l'équipe ramasse un peu ce qui traîne encore
                if (site.Litter.Count > 0)
                    site.Litter.RemoveAt(0);
                continue;
            }
            if (!workday)
                continue;
            var random = new SimRandom(Seed ^ (ulong)(day * 7919 + site.Id) * 0x9E3779B97F4A7C15UL);
            for (int i = 0; i < uncovered && site.Litter.Count < MaxLitter; i++)
                if (random.Chance(LitterChance) && LitterSpot(site, random) is { } cell)
                    site.Litter.Add(new LitterSpot { X = cell.X, Y = cell.Y, Kind = random.Next(0, 3) });
            if (site.Litter.Count >= 4)
                Notify(Severity.Warning, "local", $"{site.Name} : des déchets traînent",
                    BinCapacity(site) == 0 ? "Personne n'a de corbeille : pose-en une (Aménagement), et ramasse ce qui traîne en cliquant dessus."
                        : $"Les corbeilles couvrent {BinCapacity(site)} personne(s) sur {present} : ajoutes-en, et ramasse ce qui traîne.",
                    "build", key: $"litter:{site.Id}");
        }
    }

    /// <summary>Une case libre où tombe un déchet : près d'un bureau de préférence, jamais sur l'entrée ni sur un meuble.</summary>
    private Cell? LitterSpot(Premises site, SimRandom random)
    {
        var blocked = Blocked(site);
        var taken = site.Litter.Select(l => new Cell(l.X, l.Y)).ToHashSet();
        var free = new List<Cell>();
        for (int v = 0; v < site.Height; v++)
        for (int u = 0; u < site.Width; u++)
        {
            var cell = new Cell(u, v);
            if (!blocked[u, v] && cell != site.Entrance && !taken.Contains(cell))
                free.Add(cell);
        }
        if (free.Count == 0)
            return null;
        // Les déchets tombent là où l'on travaille : les cases d'accès des postes d'abord
        var near = site.Workstations.Select(site.SeatCell).Where(c => free.Contains(c)).ToList();
        var pool = near.Count > 0 && random.Chance(0.6) ? near : free;
        return pool[random.Next(0, pool.Count)];
    }

    /// <summary>Ramasse le déchet de cette case du site affiché.</summary>
    public CommandResult PickUpLitter(int x, int y)
    {
        var spot = Premises.Litter.FirstOrDefault(l => l.X == x && l.Y == y);
        if (spot == null)
            return CommandResult.Fail("Rien à ramasser ici.");
        Premises.Litter.Remove(spot);
        if (Premises.Litter.Count == 0)
            Resolve($"litter:{Premises.Id}");
        return CommandResult.Success("Ramassé.");
    }

    /// <summary>Ramasse tous les déchets du site affiché.</summary>
    public CommandResult CleanUp()
    {
        int count = Premises.Litter.Count;
        if (count == 0)
            return CommandResult.Fail("Le local est propre.");
        Premises.Litter.Clear();
        Resolve($"litter:{Premises.Id}");
        return CommandResult.Success(count == 1 ? "Un déchet ramassé." : $"{count} déchets ramassés.");
    }
}
