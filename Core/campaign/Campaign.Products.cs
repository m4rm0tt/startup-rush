using Core.finance;
using Core.products;
using Core.team;
using Core.work;

namespace Core.campaign;

/// <summary>Produits : créer, concevoir les pages, développer une version, publier, piloter prix, serveurs et marketing.</summary>
public partial class Campaign
{
    public const decimal CompanyRegistrationCost = 800m;

    public CommandResult CreateCompany(string name, string sector)
    {
        if (string.IsNullOrWhiteSpace(name))
            return CommandResult.Fail("Il faut un nom.");
        var paid = Spend(CompanyRegistrationCost, Category.Registration, $"Inscription au registre du commerce : {name}");
        if (!paid.Ok)
            return paid;
        Companies.Add(new Company { Id = NewId(), Name = name.Trim(), Sector = sector, FoundedAt = Minute, Reputation = MainCompany.Reputation * 0.3 });
        return CommandResult.Success();
    }

    public CommandResult RenameCompany(int companyId, string name)
    {
        var company = FindCompany(companyId);
        if (company == null || string.IsNullOrWhiteSpace(name))
            return CommandResult.Fail("Nom invalide.");
        company.Name = name.Trim();
        return CommandResult.Success();
    }

    public (CommandResult Result, Product? Product) CreateProduct(int companyId, string name, string segmentId, RevenueModel model, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (CommandResult.Fail("Il faut un nom."), null);
        var segment = ProductCatalog.Segment(segmentId);
        var archetype = ProductCatalog.Archetype(segment.ArchetypeId);
        if (!archetype.Models.Contains(model))
            return (CommandResult.Fail($"{ProductCatalog.ModelName(model)} ne convient pas à ce type de produit."), null);
        var product = new Product
        {
            Id = NewId(),
            CompanyId = FindCompany(companyId)?.Id ?? MainCompany.Id,
            Name = name.Trim(),
            ArchetypeId = archetype.Id,
            SegmentId = segment.Id,
            Model = model,
            Price = price,
            CreatedAt = Minute,
        };
        product.Draft = ProductDesign.Starter(product);
        Products.Add(product);
        return (CommandResult.Success(), product);
    }

    /// <summary>Le brouillon est modifiable tant qu'aucune version n'est en fabrication.</summary>
    public bool CanEditDraft(Product product) => product.Build == null;

    /// <summary>Qualité moyenne que l'équipe affectée au produit produirait (pour les prévisions).</summary>
    public double AssumedQuality(Product product)
    {
        var team = People.Where(p => p.Assignment.Is(AssignmentKind.Product, product.Id)).ToList();
        if (team.Count == 0)
            team = [Founder];
        return team.Average(p => WorkQuality(p, Discipline.Code) * 0.6 + WorkQuality(p, Discipline.Design) * 0.4);
    }

    public List<PlannedWork> PlanDraft(Product product) =>
        ProductDesign.PlanWork(product, (FindCompany(product.CompanyId) ?? MainCompany).ReuseLevels);

    public Forecast ForecastDraft(Product product)
    {
        var plan = PlanDraft(product);
        double quality = AssumedQuality(product);
        double bugs = ProductDesign.ExpectedBugs(plan, quality);
        double support = People.Where(p => p.Assignment.Is(AssignmentKind.Support, product.Id)).Sum(p => WorkRate(p, Discipline.Support) * 60 * 7);
        return Market.Forecast(product, product.Draft, quality, bugs, support, Competitors);
    }

    public CommandResult StartBuild(int productId)
    {
        var product = FindProduct(productId);
        if (product == null)
            return CommandResult.Fail("Produit inconnu.");
        if (product.Build != null)
            return CommandResult.Fail("Une version est déjà en cours.");
        var blocking = ProductDesign.Warnings(product, product.Draft).FirstOrDefault(w => w.Blocking);
        if (blocking != null)
            return CommandResult.Fail(blocking.Text);
        var plan = PlanDraft(product);
        if (plan.Count == 0)
            return CommandResult.Fail("Rien n'a changé depuis la version publiée.");

        var build = new ProductBuild
        {
            Version = product.LiveVersion + 1,
            Spec = product.Draft.Clone(),
            StartedAt = Minute,
            Forecast = ForecastDraft(product),
        };

        // Par module : design → développement → tests. Les retouches de pages sont indépendantes.
        foreach (var group in plan.GroupBy(w => (w.ModuleId, w.Group)))
        {
            var previous = new List<int>();
            foreach (var discipline in new[] { Discipline.Design, Discipline.Code, Discipline.Test })
            {
                var works = group.Where(w => w.Discipline == discipline).ToList();
                if (works.Count == 0)
                    continue;
                var ids = new List<int>();
                foreach (var w in works)
                    ids.AddRange(AddChunks(WorkOwner.Product, product.Id, w.Title, discipline, w.Effort, previous, "module", w.ModuleId));
                previous = ids;
                build.WorkItemIds.AddRange(ids);
            }
        }
        product.Build = build;
        Notify(Severity.Info, "produit", $"{product.Name} v{build.Version} : développement lancé",
            $"{plan.Sum(w => w.Effort):0} points de travail. Affecte des personnes au produit dans Équipe.", $"product:{product.Id}");
        return CommandResult.Success();
    }

    public CommandResult CancelBuild(int productId)
    {
        var product = FindProduct(productId);
        if (product?.Build == null)
            return CommandResult.Fail("Aucune version en cours.");
        Work.RemoveAll(w => product.Build.WorkItemIds.Contains(w.Id));
        product.Build = null;
        return CommandResult.Success("Version annulée : le travail fait est perdu, le brouillon est de nouveau modifiable.");
    }

    public bool BuildReady(Product product) =>
        product.Build != null && product.Build.WorkItemIds.All(id => FindWork(id) is not { Done: false });

    /// <summary>Étape d'un module dans la version en cours : brouillon, en développement, en test, prêt ou publié.</summary>
    public string ModuleStage(Product product, ModuleInstance module)
    {
        if (product.Build != null)
        {
            var items = product.Build.WorkItemIds.Select(FindWork).Where(w => w != null && w.ModuleId == module.Id).ToList();
            if (items.Count > 0)
            {
                if (items.All(w => w!.Done))
                    return "Prêt";
                if (items.Where(w => w!.Discipline != Discipline.Test).All(w => w!.Done))
                    return "En test";
                return "En développement";
            }
        }
        if (product.Live?.AllModules.Any(m => m.Id == module.Id) == true)
        {
            var live = product.Live.AllModules.First(m => m.Id == module.Id);
            bool changed = !live.Options.OrderBy(o => o.Key).SequenceEqual(module.Options.OrderBy(o => o.Key)) || live.Visible != module.Visible;
            return changed ? "Modifié (brouillon)" : "Publié";
        }
        return "Brouillon";
    }

    public CommandResult Publish(int productId)
    {
        var product = FindProduct(productId);
        if (product?.Build == null)
            return CommandResult.Fail("Aucune version à publier.");
        if (!BuildReady(product))
            return CommandResult.Fail("La version n'est pas terminée.");

        var build = product.Build;
        var items = build.WorkItemIds.Select(FindWork).Where(w => w != null).Select(w => w!).ToList();
        foreach (var group in items.Where(w => w.ModuleId != null).GroupBy(w => w.ModuleId!.Value))
            product.ModuleQuality[group.Key] = Math.Round(group.Sum(w => w.QualityPoints) / Math.Max(1e-6, group.Sum(w => w.Progress)), 3);

        // Bugs livrés : code écrit, complexité et qualité, moins ce que les tests ont attrapé
        double bugs = 0;
        foreach (var group in items.Where(w => w.ModuleId != null).GroupBy(w => w.ModuleId!.Value))
        {
            var module = build.Spec.AllModules.FirstOrDefault(m => m.Id == group.Key);
            if (module == null)
                continue;
            double code = group.Where(w => w.Discipline == Discipline.Code).Sum(w => w.Effort);
            double codeQuality = group.Where(w => w.Discipline == Discipline.Code).Sum(w => w.QualityPoints) / Math.Max(1e-6, code);
            var tests = group.Where(w => w.Discipline == Discipline.Test).ToList();
            double testQuality = tests.Sum(w => w.QualityPoints) / Math.Max(1e-6, tests.Sum(w => w.Effort));
            double caught = Math.Min(0.85, tests.Sum(w => w.Effort) / Math.Max(1, code * 0.35) * 0.7 * testQuality / 0.6);
            bugs += code * 0.12 * module.Def.Complexity * (1.2 - codeQuality) * (1 - caught);
        }
        double expected = bugs;
        bugs = Math.Max(0, bugs * Random.Range(0.6, 1.5));
        product.Bugs += Math.Round(bugs);

        bool first = product.Live == null;
        product.Live = build.Spec.Clone();
        product.LiveVersion = build.Version;
        product.PublishedAt = Minute;
        product.LastForecast = build.Forecast;
        product.Draft = product.Live.Clone();
        product.Build = null;
        var liveIds = product.Live.AllModules.Select(m => m.Id).ToHashSet();
        foreach (var id in product.ModuleQuality.Keys.Where(k => !liveIds.Contains(k)).ToList())
            product.ModuleQuality.Remove(id);
        Work.RemoveAll(w => build.WorkItemIds.Contains(w.Id));

        Notify(Severity.Good, "produit", first ? $"{product.Name} est en ligne !" : $"{product.Name} v{product.LiveVersion} publiée",
            $"Qualité moyenne {product.ModuleQuality.Values.DefaultIfEmpty(0).Average():P0}. Bugs introduits : {Math.Round(bugs)} (attendus ~{expected:0}). " +
            (build.Forecast != null ? $"Prévision à 90 jours : {build.Forecast.UsersLow:N0} à {build.Forecast.UsersHigh:N0} utilisateurs." : ""), $"product:{product.Id}");
        return CommandResult.Success();
    }

    public CommandResult SetPrice(int productId, decimal price)
    {
        var product = FindProduct(productId);
        if (product == null || price <= 0)
            return CommandResult.Fail("Prix invalide.");
        if (product.IsLive && price > product.Price && product.Model != RevenueModel.Ads)
        {
            // Une hausse fait partir une partie des clients sensibles au prix
            double increase = (double)((price - product.Price) / product.Price);
            double lost = product.Users * Math.Min(0.35, increase * product.Segment.PriceSensitivity * 0.25);
            product.Users -= lost;
            if (lost >= 1)
                Notify(Severity.Info, "produit", $"{product.Name} : hausse de prix", $"{lost:0} utilisateurs sont partis tout de suite.", $"product:{product.Id}");
        }
        product.Price = price;
        return CommandResult.Success();
    }

    public CommandResult SetHosting(int productId, string planId)
    {
        var product = FindProduct(productId);
        if (product == null)
            return CommandResult.Fail("Produit inconnu.");
        product.HostingId = ProductCatalog.Plan(planId).Id;
        return CommandResult.Success();
    }

    public CommandResult SetMarketing(int productId, decimal monthlyBudget, string channelId, double sellerShare)
    {
        var product = FindProduct(productId);
        if (product == null || monthlyBudget < 0)
            return CommandResult.Fail("Budget invalide.");
        product.MarketingBudget = monthlyBudget;
        product.MarketingChannelId = ProductCatalog.Channel(channelId).Id;
        product.SellerMarketingShare = Math.Clamp(sellerShare, 0, 1);
        return CommandResult.Success();
    }

    /// <summary>Planifie une série de correctifs : 3 points de code et 1 de test par bug, 10 bugs au plus.</summary>
    public CommandResult PlanBugFixes(int productId)
    {
        var product = FindProduct(productId);
        if (product is not { IsLive: true } || product.Bugs < 1)
            return CommandResult.Fail("Aucun bug connu.");
        if (WorkFor(WorkOwner.Product, product.Id).Any(w => w.Kind == "fix"))
            return CommandResult.Fail("Des correctifs sont déjà prévus.");
        int count = (int)Math.Min(10, Math.Ceiling(product.Bugs));
        string title = $"Correctifs ({count} bugs)";
        var code = AddChunks(WorkOwner.Product, product.Id, title, Discipline.Code, count * 3, [], "fix");
        AddChunks(WorkOwner.Product, product.Id, title, Discipline.Test, count, code, "fix");
        return CommandResult.Success();
    }

    public CommandResult PlanOptimization(int productId)
    {
        var product = FindProduct(productId);
        if (product is not { IsLive: true })
            return CommandResult.Fail("Produit non publié.");
        if (product.Optimizations >= 3)
            return CommandResult.Fail("Déjà optimisé au maximum.");
        if (WorkFor(WorkOwner.Product, product.Id).Any(w => w.Kind == "optimize"))
            return CommandResult.Fail("Une optimisation est déjà prévue.");
        AddChunks(WorkOwner.Product, product.Id, $"Optimisation n° {product.Optimizations + 1}", Discipline.Code, 18 * (1 + product.Optimizations * 0.5), [], "optimize");
        return CommandResult.Success();
    }

    public CommandResult SetAutoFix(int productId, int threshold)
    {
        var product = FindProduct(productId);
        if (product == null)
            return CommandResult.Fail("Produit inconnu.");
        product.AutoFixThreshold = Math.Max(0, threshold);
        return CommandResult.Success();
    }

    /// <summary>Arrête un produit : plus de coûts, plus de revenus. Les utilisateurs partent.</summary>
    public CommandResult Retire(int productId)
    {
        var product = FindProduct(productId);
        if (product == null)
            return CommandResult.Fail("Produit inconnu.");
        Work.RemoveAll(w => w.Owner == WorkOwner.Product && w.OwnerId == productId);
        foreach (var person in People.Where(p => p.Assignment.TargetId == productId && p.Assignment.Kind is AssignmentKind.Product or AssignmentKind.Maintenance or AssignmentKind.Support))
            person.Assignment = Assignment.None;
        Products.Remove(product);
        Notify(Severity.Info, "produit", $"{product.Name} est arrêté", "Plus de serveurs ni de marketing à payer.", "products");
        return CommandResult.Success();
    }
}
