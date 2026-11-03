using Core.space;

namespace Core.campaign;

/// <summary>Ce que l'espace de travail permet d'installer : postes complets, postes installés, et le plafond réel.</summary>
public record WorkspaceCapacity(int Complete, int Installed, int Max, int People, bool LimitedByLease)
{
    /// <summary>Places encore possibles (bureaux à ajouter) avant le plafond.</summary>
    public int Spare => Math.Max(0, Max - Installed);
}

/// <summary>
/// Personnalisation du local : couleurs des meubles (un seul système, celui des masques du pack : chaque objet dont le masque
/// a des canaux recolorables peut changer de teinte), capacité de l'espace de travail.
/// </summary>
public partial class Campaign
{
    /// <summary>Canaux qu'un joueur peut changer (les écrans, la peau, les cheveux et le verre gardent leurs couleurs).</summary>
    public static readonly string[] PaintableChannels = ["fabric", "wood", "primary", "secondary", "metal", "accent", "plant"];

    /// <summary>Les canaux de couleur de cet objet que le joueur peut changer, avec la teinte actuelle de chacun.</summary>
    public static IReadOnlyList<(string Channel, string Current, IReadOnlyList<string> Options)> ColorChannels(PlacedItem item)
    {
        var result = new List<(string, string, IReadOnlyList<string>)>();
        foreach (var (channel, original) in item.Def.Pack.Channels)
        {
            if (!PaintableChannels.Contains(channel))
                continue;
            var options = Pack.SwatchOptions(channel);
            if (options.Count < 2)
                continue;
            result.Add((channel, item.Swatches.GetValueOrDefault(channel, original), options));
        }
        return result.OrderBy(c => Array.IndexOf(PaintableChannels, c.Item1)).ToList();
    }

    /// <summary>Le nom d'un canal de couleur, pour l'interface.</summary>
    public static string ChannelName(string channel) => channel switch
    {
        "fabric" => "Tissu",
        "wood" => "Bois",
        "primary" => "Couleur principale",
        "secondary" => "Couleur secondaire",
        "metal" => "Métal",
        "accent" => "Détails",
        "plant" => "Feuillage",
        _ => channel,
    };

    /// <summary>
    /// Change la couleur d'un canal d'un objet du site affiché (gratuit, purement visuel). La teinte doit être une de celles
    /// que le pack propose pour ce canal ; elle est sauvegardée avec l'objet (et le suit dans les cartons).
    /// </summary>
    public CommandResult SetItemColor(int itemId, string channel, string swatch)
    {
        var item = Premises.Find(itemId);
        if (item == null)
            return CommandResult.Fail("Objet introuvable.");
        var entry = ColorChannels(item).FirstOrDefault(c => c.Channel == channel);
        if (entry.Channel == null)
            return CommandResult.Fail($"{item.Def.Name} : cette partie ne change pas de couleur.");
        if (!entry.Options.Contains(swatch))
            return CommandResult.Fail("Teinte inconnue pour cette partie.");
        item.Swatches[channel] = swatch;
        return CommandResult.Success();
    }

    /// <summary>Rend à un objet ses couleurs d'origine.</summary>
    public CommandResult ResetItemColors(int itemId)
    {
        var item = Premises.Find(itemId);
        if (item == null)
            return CommandResult.Fail("Objet introuvable.");
        foreach (string channel in PaintableChannels)
            item.Swatches.Remove(channel);
        return CommandResult.Success("Couleurs d'origine.");
    }

    private readonly Dictionary<int, WorkspaceCapacity> _capacity = new();

    private readonly Dictionary<int, Dictionary<int, List<Infrastructure.ItemIssue>>> _itemIssues = new();

    /// <summary>
    /// Les problèmes des équipements d'un site (objet → ce qui manque), gardés jusqu'au prochain changement d'aménagement :
    /// l'indicateur au-dessus de l'objet disparaît dès que le problème est réglé.
    /// </summary>
    public IReadOnlyDictionary<int, List<Infrastructure.ItemIssue>> ItemIssues(Premises site)
    {
        if (!_itemIssues.TryGetValue(site.Id, out var issues))
            _itemIssues[site.Id] = issues = Infrastructure.ItemIssues(site);
        return issues;
    }

    /// <summary>
    /// La capacité de l'espace de travail d'un site, selon les vraies règles de pose : postes complets, postes installés, et le
    /// plafond, soit celui du bail (la chambre : un bureau), soit les postes installés plus les bureaux compacts qui tiennent
    /// encore (essayés case par case avec les règles de pose, passages et cases d'accès compris). Gardé jusqu'au prochain
    /// changement d'aménagement.
    /// </summary>
    public WorkspaceCapacity Capacity(Premises site)
    {
        if (_capacity.TryGetValue(site.Id, out var cached))
            return cached with { People = People.Count(p => PostOf(p).Site == site) };
        var posts = site.Workstations.Where(p => p.Desk.Def.Kind == FurnitureKind.Desk).ToList();
        int complete = posts.Count(p => Layout.Check(site, p) == WorkstationIssue.None);
        int max;
        bool lease = site.MaxDesks > 0;
        if (lease)
            max = posts.Count + Math.Max(0, site.MaxDesks - site.Desks.Count());
        else
            max = posts.Count + SpareDesks(site);
        var capacity = new WorkspaceCapacity(complete, posts.Count, max, People.Count(p => PostOf(p).Site == site), lease);
        _capacity[site.Id] = capacity;
        return capacity;
    }

    /// <summary>Bureaux compacts qui tiennent encore dans le local, posés à l'essai un par un (puis retirés).</summary>
    private static int SpareDesks(Premises site)
    {
        var desk = FurnitureCatalog.Get("desk_compact");
        var trial = new List<PlacedItem>();
        try
        {
            for (int v = 0; v < site.Height; v++)
            for (int u = 0; u < site.Width; u++)
            {
                if (trial.Count >= 60)
                    return trial.Count;
                string facing = desk.Pack.DefaultFacing;
                if (!Layout.CanPlace(site, desk, u, v, facing).Ok)
                    continue;
                var temp = new PlacedItem { Id = -3000 - trial.Count, DefId = desk.Id, X = u, Y = v, Facing = facing };
                site.Items.Add(temp);
                trial.Add(temp);
            }
            return trial.Count;
        }
        finally
        {
            foreach (var temp in trial)
                site.Items.Remove(temp);
        }
    }
}
