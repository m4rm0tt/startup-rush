using Core.team;

namespace Core.products;

/// <summary>
/// Ce que valent les composants d'un projet pour son public, ce qu'ils coûtent à faire et à faire tourner.
/// L'apparence (couleur, icône) n'entre jamais dans la valeur.
/// </summary>
public static class Design
{
    /// <summary>Couverture de chaque besoin (0 à 1) : chaque composant comble une part de ce qui reste, donc rendement décroissant.</summary>
    public static Dictionary<Need, double> Coverage(Product product, IEnumerable<ProjectComponent> components)
    {
        var missing = Enum.GetValues<Need>().ToDictionary(n => n, _ => 1.0);
        var list = components.ToList();
        foreach (var component in list.Where(c => c.Level > 0))
        {
            var def = component.Def;
            // Un composant qui dépend d'un autre absent ne sert à rien (on ne peut d'ailleurs pas l'ajouter sans lui)
            if (def.Requires != null && list.All(c => c.DefId != def.Requires || c.Level == 0))
                continue;
            double value = ProductCatalog.LevelValue(component.Level) * Math.Clamp(component.Quality / 0.6, 0.3, 1.5);
            foreach (var (need, contribution) in def.Needs)
                missing[need] *= 1 - Math.Min(0.95, contribution * value);
        }
        return missing.ToDictionary(p => p.Key, p => 1 - p.Value);
    }

    public static Dictionary<Need, double> Coverage(Product product) => Coverage(product, product.Components);

    /// <summary>Attrait pour le public visé (0 à 1), avant bugs, performances et concurrence.</summary>
    public static double Appeal(Product product, IEnumerable<ProjectComponent> components)
    {
        var coverage = Coverage(product, components);
        var weights = product.Archetype.Needs;
        return weights.Sum(w => w.Value * coverage[w.Key]) / weights.Values.Sum();
    }

    public static double Appeal(Product product) => Appeal(product, product.Components);

    /// <summary>Charge serveur pour 1 000 personnes : composants (plus lourds à haut niveau), réduite par les Performances.</summary>
    public static double LoadPerThousand(Product product, bool optimizationNode)
    {
        double load = product.Components.Where(c => c.Level > 0).Sum(c => c.Def.Load * (1 + 0.2 * (c.Level - 1)));
        int performance = product.Level("performance");
        return Math.Max(0.05, load) * Math.Pow(0.75, performance) * (optimizationNode ? 0.85 : 1);
    }

    public static double TicketRate(Product product) =>
        Math.Max(0.05, product.Components.Where(c => c.Level > 0).Sum(c => c.Def.Tickets * (c.Def.Tickets < 0 ? c.Level : 1)));

    /// <summary>Maintenance par semaine (points) : chaque composant demande un peu d'entretien.</summary>
    public static double MaintenancePerWeek(Product product) =>
        product.Components.Where(c => c.Level > 0).Sum(c => c.Def.Maintenance * (0.8 + 0.2 * c.Level)) * 0.5;

    public static double Complexity(Product product) => product.Components.Where(c => c.Level > 0).Sum(c => c.Def.Complexity);

    /// <summary>Effort (design, développement, tests) pour amener un composant au niveau donné.</summary>
    public static (double Design, double Code, double Test) Effort(ComponentDef def, int level, double reuse = 1)
    {
        double total = def.Effort * campaign.Tuning.Projects.EffortScale * ProductCatalog.LevelEffort(level);
        return (Math.Round(total * def.DesignShare, 1), Math.Round(total * def.CodeShare * reuse, 1), Math.Round(total * def.TestShare, 1));
    }

    public static double TotalEffort(ComponentDef def, int level)
    {
        var (d, c, t) = Effort(def, level);
        return d + c + t;
    }

    /// <summary>Composants proposés pour ce type, dans un ordre stable (les premiers sont les plus utiles).</summary>
    public static IEnumerable<ComponentDef> ForType(string typeId) =>
        ProductCatalog.Components.Where(c => c.Types.Contains(typeId))
            .OrderByDescending(c => c.Needs.Sum(n => ProductCatalog.Type(typeId).Needs.GetValueOrDefault(n.Key) * n.Value));

    /// <summary>Peut-on mettre ce composant (ou son niveau suivant) en file ? Sinon, pourquoi.</summary>
    public static (bool Ok, string Reason, int NextLevel) CanQueue(Product product, string defId, Func<string, bool> unlocked)
    {
        var def = ProductCatalog.Component(defId);
        int next = product.PlannedLevel(defId) + 1;
        if (!def.Types.Contains(product.ArchetypeId))
            return (false, "Pas pour ce type de projet.", next);
        if (def.UnlockNode != null && !unlocked(def.UnlockNode))
            return (false, $"À débloquer dans Développement : {progress.DevTree.Node(def.UnlockNode).Name}.", next);
        if (next > ProductCatalog.MaxLevel)
            return (false, "Niveau maximum atteint.", next);
        if (next == ProductCatalog.MaxLevel && !unlocked("advanced"))
            return (false, $"Niveau 3 : à débloquer dans Développement ({progress.DevTree.Node("advanced").Name}).", next);
        if (def.Requires != null && product.PlannedLevel(def.Requires) == 0)
            return (false, $"Il faut d'abord {ProductCatalog.Component(def.Requires).Name}.", next);
        return (true, "", next);
    }

    /// <summary>
    /// Le besoin le plus mal couvert (parmi ceux qui comptent pour ce public) et le composant qui y répond le mieux.
    /// Sert aux avis : « il manque... » renvoie toujours vers quelque chose de faisable.
    /// </summary>
    public static (Need Need, double Coverage, ComponentDef? Fix) WeakestNeed(Product product, Func<string, bool> unlocked)
    {
        var coverage = Coverage(product);
        var weakest = product.Archetype.Needs.Where(n => n.Value >= 0.15).OrderBy(n => coverage[n.Key]).First().Key;
        var fix = ForType(product.ArchetypeId)
            .Where(c => c.Needs.ContainsKey(weakest) && CanQueue(product, c.Id, unlocked).Ok)
            .OrderByDescending(c => c.Needs[weakest])
            .FirstOrDefault();
        return (weakest, coverage[weakest], fix);
    }
}
