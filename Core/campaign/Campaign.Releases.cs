using Core.products;
using Core.work;

namespace Core.campaign;

/// <summary>
/// Les versions d'un produit en ligne. Ce que l'équipe termine ne part plus tout seul chez les utilisateurs : chaque évolution
/// attend la prochaine version. Le joueur choisit ce qui entre dans la version (la file, dans l'ordre qu'il veut), la prépare,
/// puis la publie (avec un mini-jeu facultatif qui réduit les bugs de la sortie). Une version qui apporte plusieurs évolutions
/// fait plus parler d'elle ; des versions régulières construisent une communauté qui retient les utilisateurs. Une semaine
/// après la sortie, le bilan de la version dit ce qu'elle a changé.
/// </summary>
public partial class Campaign
{
    /// <summary>Publier aussitôt chaque évolution terminée (sans préparer de version) : réglage du joueur, désactivé par défaut.</summary>
    public bool AutoPublish { get; set; }

    /// <summary>Jours sans nouvelle version avant que la communauté ne s'effrite, et ce qu'elle perd ensuite par jour.</summary>
    public const int CommunityGraceDays = 45;

    public const double CommunityDecay = 0.004;

    /// <summary>Une évolution terminée sur un projet en ligne : elle attend la prochaine version.</summary>
    private void StageChange(Product product, QueuedWork queued, List<WorkItem> items, double quality)
    {
        var def = ProductCatalog.Component(queued.DefId);
        product.Staged.RemoveAll(s => s.DefId == queued.DefId && s.Level <= queued.ToLevel);
        product.Staged.Add(new StagedChange
        {
            DefId = queued.DefId, Level = queued.ToLevel, Quality = quality, Bugs = Math.Round(WorkBugs(product, def, items, quality), 1),
            FinishedAt = Minute, Approach = queued.Approach,
        });
        Work.RemoveAll(w => queued.WorkItemIds.Contains(w.Id));
        product.Queue.Remove(queued);
        if (AutoPublish)
        {
            PublishRelease(product.Id, 0);
            return;
        }
        string list = string.Join(", ", product.Staged.Select(ChangeName));
        Notify(Severity.Good, "projet", $"{product.Name} : version {product.Version + 1} en préparation",
            product.Queue.Count == 0 ? $"Prête à publier : {list}." : $"Prêt : {list}. Encore {product.Queue.Count} évolution(s) en file.",
            $"release:{product.Id}", key: $"release:{product.Id}");
    }

    public static string ChangeName(StagedChange change) => change.Level <= 1 ? change.Def.Name : $"{change.Def.Name} niveau {change.Level}";

    /// <summary>Peut-on publier une version de ce projet ? Sinon, pourquoi.</summary>
    public (bool Ok, string Reason) CanPublish(Product product)
    {
        if (!product.IsLive)
            return (false, "Le projet n'est pas encore sorti : lance-le d'abord.");
        if (product.Staged.Count == 0)
            return (false, product.Queue.Count > 0 ? "Rien n'est encore terminé pour la prochaine version." : "Rien de neuf à publier : ajoute ou améliore un composant.");
        return (true, "");
    }

    /// <summary>L'attrait qu'aurait le produit avec la version préparée (pour comparer avant de publier).</summary>
    public static double AppealWithStaged(Product product) => Design.Appeal(product, PlannedComponents(product));

    /// <summary>
    /// Ce que le niveau suivant d'un composant ajouterait à l'attrait (en points), par rapport à ce qui est déjà prêt ou en file,
    /// pour une qualité moyenne : le chiffre affiché sur la carte du composant.
    /// </summary>
    public static double AppealGain(Product product, string defId)
    {
        var planned = PlannedComponents(product, withQueue: true);
        double before = Design.Appeal(product, planned);
        var existing = planned.FirstOrDefault(c => c.DefId == defId);
        if (existing == null)
            planned.Add(new ProjectComponent { DefId = defId, Level = 1, Quality = 0.6 });
        else
            existing.Level = Math.Min(ProductCatalog.MaxLevel, existing.Level + 1);
        return Design.Appeal(product, planned) - before;
    }

    /// <summary>Les composants avec ce qui est prêt (et, au besoin, ce qui est en file) : la version telle qu'elle sera.</summary>
    private static List<ProjectComponent> PlannedComponents(Product product, bool withQueue = false)
    {
        var components = product.Components.Select(c => new ProjectComponent { DefId = c.DefId, Level = c.Level, Quality = c.Quality }).ToList();
        foreach (var change in product.Staged)
        {
            var existing = components.FirstOrDefault(c => c.DefId == change.DefId);
            if (existing == null)
                components.Add(new ProjectComponent { DefId = change.DefId, Level = change.Level, Quality = change.Quality });
            else
            {
                existing.Quality = existing.Level == 0 ? change.Quality : Math.Round(existing.Quality * 0.4 + change.Quality * 0.6, 3);
                existing.Level = Math.Max(existing.Level, change.Level);
            }
        }
        if (withQueue)
            foreach (var queued in product.Queue)
            {
                var existing = components.FirstOrDefault(c => c.DefId == queued.DefId);
                if (existing == null)
                    components.Add(new ProjectComponent { DefId = queued.DefId, Level = queued.ToLevel, Quality = 0.6 });
                else
                    existing.Level = Math.Max(existing.Level, queued.ToLevel);
            }
        return components;
    }

    /// <summary>
    /// Publie la version préparée : les évolutions entrent en service, la version monte, les utilisateurs en entendent parler
    /// (plus fort quand la version apporte beaucoup), la communauté grandit. « score » : mini-jeu de publication (0 à 1), qui
    /// retire jusqu'à la moitié des bugs que la version apporte.
    /// </summary>
    public CommandResult PublishRelease(int productId, double score)
    {
        var product = FindProduct(productId);
        if (product == null)
            return CommandResult.Fail("Projet inconnu.");
        var (ok, reason) = CanPublish(product);
        if (!ok)
            return CommandResult.Fail(reason);
        score = Math.Clamp(score, 0, 1);
        var release = new Release
        {
            Version = product.Version + 1, Minute = Minute, UsersBefore = product.Users, AppealBefore = Design.Appeal(product), Score = score,
            Changes = product.Staged.Select(ChangeName).ToList(),
        };
        double bugs = 0;
        bool ai = false;
        foreach (var change in product.Staged)
        {
            var component = product.Component(change.DefId);
            if (component == null)
            {
                component = new ProjectComponent { DefId = change.DefId, Level = 0, Quality = change.Quality };
                product.Components.Add(component);
            }
            component.Quality = component.Level == 0 ? change.Quality : Math.Round(component.Quality * 0.4 + change.Quality * 0.6, 3);
            component.Level = Math.Max(component.Level, change.Level);
            bugs += change.Bugs;
            ai |= change.DefId is "ai_core" or "ai_feature";
        }
        int changes = product.Staged.Count;
        product.Staged.Clear();
        product.Bugs += Math.Round(bugs * (1 - LaunchBugReduction * score), 1);
        if (ai)
            AutoLinkAi(product);
        product.Version = release.Version;
        product.LastUpdateAt = Minute;
        // Une version qui apporte beaucoup fait plus parler d'elle (une évolution : l'effet d'avant ; cinq : trois fois plus)
        double echo = 1 + 0.5 * Math.Min(4, changes - 1);
        for (int i = 0; i < Math.Round(echo * 2); i++)
            Market.UpdateBoost(product, 0.5);
        product.Community = Math.Min(1, product.Community + Math.Min(0.25, 0.06 + 0.04 * changes) * (1 - product.Community));
        release.AppealAfter = Design.Appeal(product);
        release.Bugs = product.Bugs;
        product.Releases.Add(release);
        Resolve($"release:{product.Id}");
        // Un produit qui dure : des points pour les versions publiées (une fois par produit)
        int updates = product.Releases.Count(r => r.Version > 1);
        if (updates >= 3)
            Award($"releases3:{product.Id}", 1, $"{product.Name} : trois mises à jour publiées. Un produit qui grandit fidélise.");
        if (updates >= 8)
            Award($"releases8:{product.Id}", 2, $"{product.Name} : huit mises à jour publiées. Une vraie communauté.");
        string major = changes >= 3 ? "Version majeure : " : "";
        Notify(Severity.Good, "projet", $"{product.Name} {release.Version} est en ligne", $"{major}{string.Join(", ", release.Changes)}.",
            $"product:{product.Id}", key: $"update:{product.Id}");
        return CommandResult.Success($"Version {release.Version} publiée.");
    }

    /// <summary>
    /// Change l'ordre de la file : l'équipe (et le terminal) travaille d'abord sur ce qui est en tête. Une évolution ne passe pas
    /// devant ce dont elle dépend (le niveau d'avant du même composant, ou le composant qu'elle demande).
    /// </summary>
    public CommandResult MoveQueued(int productId, int queuedId, int delta)
    {
        var product = FindProduct(productId);
        int index = product?.Queue.FindIndex(q => q.Id == queuedId) ?? -1;
        if (product == null || index < 0)
            return CommandResult.Fail("Rien à déplacer.");
        int target = Math.Clamp(index + delta, 0, product.Queue.Count - 1);
        if (target == index)
            return CommandResult.Fail(delta < 0 ? "Déjà en tête de file." : "Déjà en fin de file.");
        var moving = product.Queue[index];
        var list = product.Queue.ToList();
        list.RemoveAt(index);
        list.Insert(target, moving);
        // Chaque évolution doit rester après celles dont elle dépend
        for (int i = 0; i < list.Count; i++)
            for (int j = i + 1; j < list.Count; j++)
                if (DependsOn(list[i], list[j]))
                    return CommandResult.Fail($"{ProductCatalog.Component(list[i].DefId).Name} attend {QueuedName(list[j])} : il ne peut pas passer devant.");
        product.Queue.Clear();
        product.Queue.AddRange(list);
        foreach (var person in People.Where(p => p.CurrentWorkItemId is int w && FindWork(w)?.OwnerId == product.Id))
            person.CurrentWorkItemId = null;
        return CommandResult.Success();
    }

    private static string QueuedName(QueuedWork q) => q.ToLevel > 1 ? $"{ProductCatalog.Component(q.DefId).Name} niveau {q.ToLevel}" : ProductCatalog.Component(q.DefId).Name;

    /// <summary>« a » dépend-elle de « b » (même composant à un niveau plus bas, ou composant demandé pas encore là) ?</summary>
    private static bool DependsOn(QueuedWork a, QueuedWork b) =>
        a.DefId == b.DefId && a.ToLevel > b.ToLevel
        || b.ToLevel == 1 && ProductCatalog.Component(a.DefId).Requires == b.DefId;

    /// <summary>Chaque nuit : bilan des versions sorties il y a une semaine, et la communauté qui s'effrite sans nouveauté.</summary>
    private void RunReleases()
    {
        foreach (var product in Products.Where(p => p.IsLive))
        {
            foreach (var release in product.Releases.Where(r => r.UsersAfter == null && Minute - r.Minute >= 7L * GameTime.MinutesPerDay))
            {
                release.UsersAfter = product.Users;
                if (release.Version <= 1)
                    continue;
                double delta = product.Users - release.UsersBefore;
                Notify(delta >= 0 ? Severity.Good : Severity.Info, "projet", $"Bilan de {product.Name} {release.Version}",
                    $"{(delta >= 0 ? "+" : "−")}{Math.Abs(delta):N0} utilisateurs en une semaine ; attrait {release.AppealBefore:P0} → {release.AppealAfter:P0}.",
                    $"product:{product.Id}", key: $"bilan:{product.Id}");
            }
            long since = Minute - (product.LastUpdateAt ?? product.LaunchedAt ?? Minute);
            if (since > CommunityGraceDays * (long)GameTime.MinutesPerDay)
                product.Community = Math.Max(0, product.Community - CommunityDecay);
        }
    }

    /// <summary>
    /// Ce que fait l'équipe sans le joueur : en mode « déléguer par défaut » ou avec l'automatisation, un employé publie chaque
    /// matin la version prête dont la file est vide (avec les procédures de publication s'il les a).
    /// </summary>
    private void AutoPublishReady()
    {
        bool delegated = DelegateLaunches && IsUnlocked("delegation");
        bool automated = AutoImprove && IsUnlocked("automation");
        if (!delegated && !automated || Delegate() == null)
            return;
        foreach (var product in Products.Where(p => p.IsLive && p.Staged.Count > 0 && p.Queue.Count == 0).ToList())
            PublishRelease(product.Id, IsUnlocked("procedures") ? 0.5 : 0);
    }
}
