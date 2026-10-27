using Core.finance;
using Core.products;
using Core.team;
using Core.work;

namespace Core.campaign;

/// <summary>Projets : créer, choisir et améliorer les composants, lancer, puis régler prix, marketing, serveurs et entretien.</summary>
public partial class Campaign
{
    public static readonly int[] ProjectColors = [17, 12, 9, 28, 10, 18, 13, 27];

    public bool TypeAvailable(string typeId) => ProductCatalog.Type(typeId).UnlockNode is not { } node || IsUnlocked(node);

    public (CommandResult Result, Product? Product) CreateProject(string name, string segmentId, RevenueModel model, decimal? price = null, IEnumerable<string>? starters = null)
    {
        var segment = ProductCatalog.Segment(segmentId);
        var type = ProductCatalog.Type(segment.TypeId);
        if (!TypeAvailable(type.Id))
            return (CommandResult.Fail($"{type.Name} : à débloquer dans Développement."), null);
        if (!type.Models.Contains(model))
            model = type.Models[0];
        var product = new Product
        {
            Id = NewId(),
            CompanyId = MainCompany.Id,
            Name = string.IsNullOrWhiteSpace(name) ? SuggestName(type.Id) : name.Trim(),
            ArchetypeId = type.Id,
            SegmentId = segment.Id,
            Model = model,
            Price = price ?? ProductCatalog.DefaultPrice(segment, model),
            Color = ProjectColors[Products.Count % ProjectColors.Length],
            CreatedAt = Minute,
            Priority = Products.Count == 0 ? 0 : Products.Max(p => p.Priority) + 1,
        };
        Products.Add(product);
        foreach (string defId in starters ?? type.Starters)
            QueueComponent(product.Id, defId);
        Notify(Severity.Info, "projet", $"{product.Name} : c'est parti", "L'équipe s'y met toute seule.", $"product:{product.Id}");
        return (CommandResult.Success(), product);
    }

    private string SuggestName(string typeId)
    {
        string[] apps = ["Factura", "Planio", "Notably", "Tempo", "Ledgr", "Fokus"];
        string[] games = ["Pixel Quest", "Lac Rush", "Moon Garden", "Tiny Kingdom", "Sky Courier"];
        string[] other = ["Agora", "Bazar", "Cercle", "Vitrine"];
        var pool = typeId == "game" ? games : typeId == "saas" ? apps : other;
        return pool.FirstOrDefault(n => Products.All(p => p.Name != n)) ?? $"Projet {Products.Count + 1}";
    }

    /// <summary>Met en file l'ajout d'un composant, ou son niveau suivant. Les tâches (design → développement → tests) sont créées tout de suite.</summary>
    public CommandResult QueueComponent(int productId, string defId)
    {
        var product = FindProduct(productId);
        if (product == null)
            return CommandResult.Fail("Projet inconnu.");
        var (ok, reason, level) = Design.CanQueue(product, defId, IsUnlocked);
        if (!ok)
            return CommandResult.Fail(reason);

        var def = ProductCatalog.Component(defId);
        var (design, code, test) = Design.Effort(def, level);
        var queued = new QueuedWork { Id = NewId(), DefId = defId, ToLevel = level };
        string title = level == 1 ? def.Name : $"{def.Name} niv. {level}";
        var previous = new List<int>();
        foreach (var (discipline, points) in new[] { (Discipline.Design, design), (Discipline.Code, code), (Discipline.Test, test) })
        {
            if (points < 0.25)
                continue;
            var ids = AddChunks(product.Id, title, discipline, points, previous, queued.Id);
            queued.WorkItemIds.AddRange(ids);
            previous = ids;
        }
        product.Queue.Add(queued);
        return CommandResult.Success(level == 1 ? $"{def.Name} ajouté à la file." : $"{def.Name} niveau {level} en file.");
    }

    /// <summary>Découpe un volume de travail en tâches d'au plus 10 points, dépendantes de l'étape précédente.</summary>
    public List<int> AddChunks(int productId, string title, Discipline discipline, double points, List<int> dependsOn, int queueId)
    {
        var ids = new List<int>();
        int chunks = Math.Max(1, (int)Math.Ceiling(points / 10));
        for (int i = 0; i < chunks; i++)
        {
            var item = new WorkItem
            {
                Id = NewId(),
                Owner = WorkOwner.Product,
                OwnerId = productId,
                Title = title,
                Discipline = discipline,
                Effort = Math.Round(points / chunks, 2),
                DependsOn = dependsOn.ToList(),
                Kind = "component",
                ModuleId = queueId,
            };
            Work.Add(item);
            ids.Add(item.Id);
        }
        return ids;
    }

    public CommandResult CancelQueued(int productId, int queuedId)
    {
        var product = FindProduct(productId);
        var queued = product?.Queue.FirstOrDefault(q => q.Id == queuedId);
        if (product == null || queued == null)
            return CommandResult.Fail("Rien à annuler.");
        // Ce qui en dépend dans la file (niveau suivant, composant qui l'exige) part aussi
        var removed = product.Queue.Where(q => q.Id == queuedId
                                               || q.DefId == queued.DefId && q.ToLevel > queued.ToLevel
                                               || queued.ToLevel == 1 && ProductCatalog.Component(q.DefId).Requires == queued.DefId && product.Level(queued.DefId) == 0).ToList();
        foreach (var q in removed)
        {
            Work.RemoveAll(w => q.WorkItemIds.Contains(w.Id));
            product.Queue.Remove(q);
        }
        return CommandResult.Success("Retiré de la file.");
    }

    public bool CanLaunch(Product product) => !product.IsLive && product.Components.Count(c => c.Level > 0) >= 2;

    public CommandResult Launch(int productId)
    {
        var product = FindProduct(productId);
        if (product == null || product.IsLive)
            return CommandResult.Fail("Déjà lancé.");
        if (!CanLaunch(product))
            return CommandResult.Fail("Il faut au moins deux composants terminés.");
        product.LaunchedAt = Minute;
        product.Version = 1;
        double buyers = Market.Launch(product);
        if (buyers > 0)
        {
            decimal sales = Math.Round((decimal)buyers * product.Price * (1 - (decimal)Tuning.Products.PaymentFees), 2);
            Post(sales, Category.Sales, $"{product.Name} : ventes du lancement");
        }
        product.LastForecast = Market.Forecast(product, Competitors, 0);
        Resolve($"ready:{product.Id}");
        ProposeLaunches(quiet: true);
        Notify(Severity.Good, "projet", $"{product.Name} est sorti !",
            buyers > 0 ? $"{buyers:N0} joueurs l'ont acheté le premier jour." : "Les premiers utilisateurs arrivent.", $"product:{product.Id}");
        return CommandResult.Success();
    }

    /// <summary>Une amélioration de la file est terminée : le composant gagne son niveau (et une mise à jour sort si le projet est en ligne).</summary>
    private void CompleteQueued(Product product, QueuedWork queued)
    {
        var items = queued.WorkItemIds.Select(FindWork).Where(w => w != null).Select(w => w!).ToList();
        double quality = items.Sum(w => w.QualityPoints) / Math.Max(1e-6, items.Sum(w => w.Progress));
        var component = product.Component(queued.DefId);
        if (component == null)
        {
            component = new ProjectComponent { DefId = queued.DefId, Level = 0, Quality = quality };
            product.Components.Add(component);
        }
        // La qualité s'améliore avec chaque niveau (moyenne pondérée vers le travail le plus récent)
        component.Quality = component.Level == 0 ? quality : Math.Round(component.Quality * 0.4 + quality * 0.6, 3);
        component.Level = Math.Max(component.Level, queued.ToLevel);

        // Bugs : code écrit × complexité × (1,2 − qualité), moins ce que les tests ont attrapé
        double code = items.Where(w => w.Discipline == Discipline.Code).Sum(w => w.Effort);
        double tests = items.Where(w => w.Discipline == Discipline.Test).Sum(w => w.Effort);
        double caught = Math.Min(0.85, tests / Math.Max(1, code * 0.35) * 0.7 * quality / 0.6);
        double bugs = code * 0.1 * component.Def.Complexity * (1.2 - quality) * (1 - caught) * Random.Range(0.6, 1.4);
        product.Bugs += Math.Round(bugs, 1);

        Work.RemoveAll(w => queued.WorkItemIds.Contains(w.Id));
        product.Queue.Remove(queued);

        string what = queued.ToLevel == 1 ? component.Def.Name : $"{component.Def.Name} niveau {queued.ToLevel}";
        if (product.IsLive)
        {
            product.Version++;
            product.LastUpdateAt = Minute;
            Market.UpdateBoost(product);
            Notify(Severity.Good, "projet", $"{product.Name} : mise à jour {product.Version}", $"{what} est en ligne.", $"product:{product.Id}", key: $"update:{product.Id}");
        }
        else if (product.Queue.Count == 0)
            ProposeLaunches();
    }

    /// <summary>Termine les améliorations dont tout le travail est déjà fait (après une migration, par exemple).</summary>
    public void CheckQueue(Product product)
    {
        foreach (var queued in product.Queue.ToList())
            if (queued.WorkItemIds.All(id => FindWork(id) is not { Done: false }))
                CompleteQueued(product, queued);
    }

    public CommandResult SetPrice(int productId, decimal price)
    {
        var product = FindProduct(productId);
        if (product == null || price <= 0)
            return CommandResult.Fail("Prix invalide.");
        if (product.IsLive && price > product.Price && product.Model is RevenueModel.Subscription or RevenueModel.Freemium)
        {
            double increase = (double)((price - product.Price) / product.Price);
            product.Users -= product.Users * Math.Min(0.35, increase * product.Segment.PriceSensitivity * 0.25);
        }
        product.Price = price;
        return CommandResult.Success();
    }

    public CommandResult SetMarketing(int productId, decimal monthlyBudget, string? channelId = null)
    {
        var product = FindProduct(productId);
        if (product == null || monthlyBudget < 0)
            return CommandResult.Fail("Budget invalide.");
        var channel = ProductCatalog.Channel(channelId ?? product.MarketingChannelId);
        if (channel.UnlockNode != null && !IsUnlocked(channel.UnlockNode))
            return CommandResult.Fail("Canal à débloquer dans Développement.");
        product.MarketingBudget = monthlyBudget;
        product.MarketingChannelId = channel.Id;
        return CommandResult.Success();
    }

    public CommandResult SetUpkeep(int productId, Upkeep upkeep)
    {
        var product = FindProduct(productId);
        if (product == null)
            return CommandResult.Fail("Projet inconnu.");
        product.Upkeep = upkeep;
        return CommandResult.Success();
    }

    public bool PlanAvailable(HostingPlan plan) => plan.UnlockNode == null || IsUnlocked(plan.UnlockNode);

    public CommandResult SetHosting(int productId, bool auto, decimal cap, string? planId = null)
    {
        var product = FindProduct(productId);
        if (product == null)
            return CommandResult.Fail("Projet inconnu.");
        product.HostingAuto = auto;
        product.HostingCap = product.HostingInternal && cap == 0 ? 0 : Math.Max(15, cap);
        if (planId != null)
        {
            var plan = ProductCatalog.Plan(planId);
            if (!PlanAvailable(plan))
                return CommandResult.Fail("Offre à débloquer dans Développement.");
            product.HostingId = plan.Id;
        }
        if (auto)
            AdjustHosting(product);
        return CommandResult.Success();
    }

    /// <summary>Hébergement automatique : la plus petite offre débloquée qui tient la charge avec une marge, sous le plafond.</summary>
    private void AdjustHosting(Product product)
    {
        if (!product.HostingAuto)
            return;
        double load = Market.Load(product, IsUnlocked("optimization"));
        var fitting = ProductCatalog.Hosting.Where(PlanAvailable).Where(h => h.MonthlyCost <= product.HostingCap).ToList();
        if (product.HostingInternal)
        {
            // Sur tes serveurs : l'offre externe ne prend que ce qui dépasse, et seulement sous le plafond
            double missing = load - product.InternalCapacity * 0.75;
            var overflow = missing <= 0 ? null : fitting.FirstOrDefault(h => missing < h.Capacity * 0.75) ?? fitting.LastOrDefault();
            product.OverflowActive = overflow != null;
            if (overflow != null)
                product.HostingId = overflow.Id;
            if (product.IsLive && load > product.Capacity * 0.85)
                Notify(Severity.Warning, "projet", $"{product.Name} manque de serveurs", "Tes serveurs sont pleins et le débordement externe ne suffit pas : ajoute des serveurs, ou relève le plafond.",
                    $"product:{product.Id}", key: $"hosting:{product.Id}");
            else
                Resolve($"hosting:{product.Id}");
            return;
        }
        var plan = fitting.FirstOrDefault(h => load < h.Capacity * 0.75) ?? fitting.LastOrDefault();
        if (plan != null)
            product.HostingId = plan.Id;
        if (product.IsLive && load > product.Hosting.Capacity * 0.85)
            Notify(Severity.Warning, "projet", $"{product.Name} : serveurs presque pleins",
                fitting.Count < ProductCatalog.Hosting.Count(PlanAvailable) ? "Le plafond d'hébergement empêche de monter d'offre." : "Débloque une offre plus grande dans Développement.",
                $"product:{product.Id}", key: $"hosting:{product.Id}");
        else
            Resolve($"hosting:{product.Id}");
    }

    public CommandResult MoveProject(int productId, int delta)
    {
        var ordered = Products.OrderBy(p => p.Priority).ToList();
        int index = ordered.FindIndex(p => p.Id == productId);
        int target = index + delta;
        if (index < 0 || target < 0 || target >= ordered.Count)
            return CommandResult.Fail("");
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (int i = 0; i < ordered.Count; i++)
            ordered[i].Priority = i;
        return CommandResult.Success();
    }

    public CommandResult Rename(int productId, string name)
    {
        var product = FindProduct(productId);
        if (product == null || string.IsNullOrWhiteSpace(name))
            return CommandResult.Fail("Nom invalide.");
        product.Name = name.Trim();
        return CommandResult.Success();
    }

    /// <summary>Ferme un projet : plus de coûts ni de revenus. Les composants et les avis disparaissent avec lui.</summary>
    public CommandResult Retire(int productId)
    {
        var product = FindProduct(productId);
        if (product == null)
            return CommandResult.Fail("Projet inconnu.");
        Work.RemoveAll(w => w.OwnerId == productId);
        foreach (var person in People.Where(p => p.Assignment.Is(AssignmentKind.Project, productId)))
            person.Assignment = Assignment.None;
        Products.Remove(product);
        foreach (var notice in Notices.Where(n => n.Target == $"product:{productId}"))
            notice.Resolved = true;
        Notify(Severity.Info, "projet", $"{product.Name} est fermé", "Plus de serveurs ni de marketing à payer.", "projects");
        return CommandResult.Success();
    }

    /// <summary>Projet sur lequel travaillent les personnes en automatique : le premier du portefeuille qui a du travail en file.</summary>
    public Product? AutoTarget() =>
        Products.OrderBy(p => p.Priority).FirstOrDefault(p => p.Queue.Count > 0) ?? Products.OrderBy(p => p.Priority).FirstOrDefault(p => p.IsLive);

    /// <summary>Les employés qui travaillent sur ce projet (affectés, ou en automatique quand c'est la cible). Le fondateur n'en fait pas partie : il travaille au terminal.</summary>
    public IEnumerable<Person> TeamOf(Product product)
    {
        var auto = AutoTarget();
        return Staff.Where(p => p.Assignment.Is(AssignmentKind.Project, product.Id) || p.Assignment.Kind == AssignmentKind.None && auto?.Id == product.Id);
    }

    /// <summary>Points restants dans la file et jours ouvrés estimés au rythme de l'équipe actuelle.</summary>
    public (double Remaining, double Days) QueueEstimate(Product product)
    {
        double remaining = product.Queue.SelectMany(q => q.WorkItemIds).Select(FindWork).Where(w => w != null).Sum(w => w!.Remaining);
        double share = product.IsLive ? 1 - UpkeepShare(product) : 1;
        double speed = TeamOf(product).Sum(p => WorkRate(p, p.BestDiscipline) * 60 * 7) * share;
        return (remaining, speed > 0 ? remaining / speed : double.PositiveInfinity);
    }

    public static double UpkeepShare(Product product) => product.Upkeep switch
    {
        Upkeep.Low => Tuning.Projects.UpkeepLow,
        Upkeep.High => Tuning.Projects.UpkeepHigh,
        _ => Tuning.Projects.UpkeepNormal
    };

    public double QueueProgress(QueuedWork queued)
    {
        var items = queued.WorkItemIds.Select(FindWork).Where(w => w != null).ToList();
        return items.Sum(w => w!.Progress) / Math.Max(1e-6, items.Sum(w => w!.Effort));
    }

    public Forecast ForecastProject(Product product) =>
        Market.Forecast(product, Competitors, TeamOf(product).Sum(p => WorkRate(p, Discipline.Support) * 60 * 7 * UpkeepShare(product)));

    /// <summary>Résultat des 30 derniers jours d'un projet : revenus moins hébergement et marketing.</summary>
    public static decimal Result30(Product product)
    {
        var days = product.History.Skip(Math.Max(0, product.History.Count - 30)).ToList();
        return days.Sum(d => d.Revenue) - days.Sum(d => d.Costs);
    }
}
