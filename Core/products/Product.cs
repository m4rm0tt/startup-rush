using System.Text.Json;

namespace Core.products;

/// <summary>Un module posé sur une page, avec ses réglages.</summary>
public class ModuleInstance
{
    public int Id { get; set; }
    public string DefId { get; set; } = "";
    public string Label { get; set; } = "";
    public bool Visible { get; set; } = true;
    public Dictionary<string, string> Options { get; set; } = new();

    public ModuleDef Def => ProductCatalog.Module(DefId);

    public ModuleChoice Choice(ModuleOption option) =>
        option.Choices.FirstOrDefault(c => Options.TryGetValue(option.Key, out var v) && c.Value == v) ?? option.Choices[Math.Min(1, option.Choices.Length - 1)];

    /// <summary>Produit des facteurs de tous les choix (hors réglages purement visuels pour la valeur).</summary>
    public double Factor(Func<ModuleChoice, double> pick, bool includeVisual = false)
    {
        double factor = 1;
        foreach (var option in Def.Options)
            if (includeVisual || !option.VisualOnly)
                factor *= pick(Choice(option));
        return factor;
    }
}

/// <summary>Une page du produit : son apparence (libre) et ses modules (qui font la valeur et le coût).</summary>
public class ProductPage
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";

    /// <summary>colonne, deux, grille : purement visuel.</summary>
    public string Layout { get; set; } = "colonne";

    /// <summary>clair, sombre : purement visuel.</summary>
    public string Theme { get; set; } = "clair";

    /// <summary>Indice d'une couleur de la palette Endesga : purement visuel.</summary>
    public int Accent { get; set; } = 17;

    public bool Visible { get; set; } = true;

    /// <summary>Pages accessibles depuis le menu de celle-ci.</summary>
    public List<int> Links { get; set; } = new();

    public List<ModuleInstance> Modules { get; set; } = new();
}

/// <summary>Le contenu d'une version : les pages, leur navigation et leurs modules.</summary>
public class ProductSpec
{
    public int HomePageId { get; set; }
    public List<ProductPage> Pages { get; set; } = new();

    public ProductSpec Clone() => JsonSerializer.Deserialize<ProductSpec>(JsonSerializer.Serialize(this))!;

    public ProductPage? Page(int id) => Pages.FirstOrDefault(p => p.Id == id);

    public IEnumerable<ModuleInstance> AllModules => Pages.SelectMany(p => p.Modules);

    /// <summary>Pages qu'un visiteur peut atteindre depuis l'accueil en suivant les menus (pages masquées exclues).</summary>
    public HashSet<int> ReachablePages()
    {
        var reached = new HashSet<int>();
        var home = Page(HomePageId);
        if (home == null || !home.Visible)
            return reached;
        var queue = new Queue<ProductPage>();
        queue.Enqueue(home);
        reached.Add(home.Id);
        while (queue.Count > 0)
        {
            var page = queue.Dequeue();
            foreach (int link in page.Links)
            {
                var target = Page(link);
                if (target != null && target.Visible && reached.Add(target.Id))
                    queue.Enqueue(target);
            }
        }
        return reached;
    }

    /// <summary>Les modules que les utilisateurs voient vraiment : visibles, sur une page atteignable.</summary>
    public IEnumerable<ModuleInstance> ActiveModules()
    {
        var reachable = ReachablePages();
        return Pages.Where(p => reachable.Contains(p.Id)).SelectMany(p => p.Modules).Where(m => m.Visible);
    }
}

/// <summary>La version en cours de fabrication : ce qui sera publié, et les tâches qui la produisent.</summary>
public class ProductBuild
{
    public int Version { get; set; }
    public ProductSpec Spec { get; set; } = new();
    public List<int> WorkItemIds { get; set; } = new();
    public long StartedAt { get; set; }

    /// <summary>Estimation faite au lancement, pour comparer ensuite avec les résultats mesurés.</summary>
    public Forecast? Forecast { get; set; }
}

public class Forecast
{
    public double Appeal { get; set; }
    public double UsersLow { get; set; }
    public double UsersHigh { get; set; }
    public decimal RevenueLow { get; set; }
    public decimal RevenueHigh { get; set; }
    public double ExpectedBugsLow { get; set; }
    public double ExpectedBugsHigh { get; set; }
}

/// <summary>Un jour de résultats d'un produit, avec les facteurs qui les expliquent.</summary>
public class ProductDay
{
    public long Day { get; set; }
    public double NewAware { get; set; }
    public double Signups { get; set; }
    public double Churned { get; set; }
    public double Users { get; set; }
    public double Paying { get; set; }
    public double Sellers { get; set; }
    public decimal Revenue { get; set; }
    public decimal Costs { get; set; }
    public double Appeal { get; set; }
    public double Share { get; set; }
    public double PriceFactor { get; set; }
    public double BugPenalty { get; set; }
    public double Performance { get; set; }
    public double SupportFactor { get; set; }
    public double Liquidity { get; set; }
    public double DowntimeHours { get; set; }
    public double MarketingReach { get; set; }
    public double Tickets { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = "";
    public string ArchetypeId { get; set; } = "";
    public string SegmentId { get; set; } = "";
    public RevenueModel Model { get; set; }

    /// <summary>Prix mensuel de l'abonnement, ou commission en % pour une place de marché.</summary>
    public decimal Price { get; set; }

    public long CreatedAt { get; set; }

    // Versions
    public ProductSpec? Live { get; set; }
    public int LiveVersion { get; set; }
    public long? PublishedAt { get; set; }
    public ProductSpec Draft { get; set; } = new();
    public ProductBuild? Build { get; set; }

    /// <summary>Prévision faite pour la dernière version publiée, à comparer aux résultats mesurés.</summary>
    public Forecast? LastForecast { get; set; }

    /// <summary>Qualité mesurée de chaque module publié (0 à 1), d'après le travail qui l'a produit.</summary>
    public Dictionary<int, double> ModuleQuality { get; set; } = new();

    // Marché
    public double Aware { get; set; }
    public double Users { get; set; }
    public double Paying { get; set; }
    public double Sellers { get; set; }
    public double Bugs { get; set; }
    public double SupportBacklog { get; set; }
    public double MaintenanceDebt { get; set; }

    /// <summary>Correctifs et maintenance faits cette semaine (en points).</summary>
    public double MaintenanceDone { get; set; }

    public string HostingId { get; set; } = "shared";

    /// <summary>Optimisations de performance réalisées (chacune réduit la charge de 15 %, 3 au plus).</summary>
    public int Optimizations { get; set; }

    public decimal MarketingBudget { get; set; }
    public string MarketingChannelId { get; set; } = "social";

    /// <summary>Part du budget marketing consacrée aux vendeurs (place de marché).</summary>
    public double SellerMarketingShare { get; set; } = 0.3;

    /// <summary>Délégation : quand les bugs dépassent ce seuil, une tâche de correctifs est créée automatiquement (0 = jamais).</summary>
    public int AutoFixThreshold { get; set; }

    public decimal AdsReceivable { get; set; }
    public List<ProductDay> History { get; set; } = new();
    public int NextLocalId { get; set; } = 1;

    public Archetype Archetype => ProductCatalog.Archetype(ArchetypeId);
    public Segment Segment => ProductCatalog.Segment(SegmentId);
    public HostingPlan Hosting => ProductCatalog.Plan(HostingId);
    public bool IsLive => Live != null;

    public int NewLocalId() => NextLocalId++;
}
