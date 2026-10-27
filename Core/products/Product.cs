namespace Core.products;

/// <summary>Un composant construit dans un projet, avec son niveau et la qualité du travail qui l'a fait.</summary>
public class ProjectComponent
{
    public string DefId { get; set; } = "";
    public int Level { get; set; }
    public double Quality { get; set; } = 0.6;

    public ComponentDef Def => ProductCatalog.Component(DefId);
}

/// <summary>Une amélioration en file : ajouter un composant ou le monter d'un niveau. L'équipe la réalise toute seule.</summary>
public class QueuedWork
{
    public int Id { get; set; }
    public string DefId { get; set; } = "";
    public int ToLevel { get; set; }
    public List<int> WorkItemIds { get; set; } = new();
}

public enum Upkeep { Low, Normal, High }

/// <summary>Un avis d'utilisateur, toujours tiré d'un résultat réel de la simulation.</summary>
public class Review
{
    public long Day { get; set; }
    public int Stars { get; set; }
    public string Text { get; set; } = "";

    /// <summary>perf, bugs, support, price, need:Fun, praise, update, sellers</summary>
    public string Cause { get; set; } = "";

    /// <summary>Composant ou réglage concerné (pour y aller en un clic).</summary>
    public string Link { get; set; } = "";
}

public class Forecast
{
    public double Appeal { get; set; }
    public double UsersLow { get; set; }
    public double UsersHigh { get; set; }
    public decimal RevenueLow { get; set; }
    public decimal RevenueHigh { get; set; }
}

/// <summary>Un jour de résultats d'un projet, avec les facteurs qui les expliquent.</summary>
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

/// <summary>
/// Un projet du joueur (application, jeu...). Il garde la même identité de la conception à l'exploitation :
/// nom, icône, couleur, composants, versions, avis et résultats.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = "";
    public string ArchetypeId { get; set; } = "";
    public string SegmentId { get; set; } = "";
    public RevenueModel Model { get; set; }

    /// <summary>Prix mensuel, prix de vente, ou commission en % selon le modèle.</summary>
    public decimal Price { get; set; }

    /// <summary>Couleur de la carte (indice de la palette Endesga) : purement visuel.</summary>
    public int Color { get; set; } = 17;

    public long CreatedAt { get; set; }

    /// <summary>Ordre dans le portefeuille : l'équipe en automatique travaille d'abord sur le premier.</summary>
    public int Priority { get; set; }

    public List<ProjectComponent> Components { get; set; } = new();
    public List<QueuedWork> Queue { get; set; } = new();
    public int Version { get; set; }
    public long? LaunchedAt { get; set; }
    public long? LastUpdateAt { get; set; }

    /// <summary>Jeu : intérêt accumulé avant la sortie (liste de souhaits), converti en ventes au lancement.</summary>
    public double Wishlist { get; set; }

    public Upkeep Upkeep { get; set; } = Upkeep.Normal;

    // Marché
    public double Aware { get; set; }
    public double Users { get; set; }
    public double Paying { get; set; }
    public double Sellers { get; set; }
    public double TotalBuyers { get; set; }
    public double Bugs { get; set; }
    public double SupportBacklog { get; set; }
    public double MaintenanceDebt { get; set; }
    public double MaintenanceDone { get; set; }
    public double SupportDone { get; set; }

    public string HostingId { get; set; } = "shared";

    /// <summary>L'hébergement suit la charge tout seul, sans dépasser le plafond mensuel.</summary>
    public bool HostingAuto { get; set; } = true;

    public decimal HostingCap { get; set; } = 400m;

    public decimal MarketingBudget { get; set; }
    public string MarketingChannelId { get; set; } = "social";
    public double SellerMarketingShare { get; set; } = 0.3;

    public decimal AdsReceivable { get; set; }
    public List<ProductDay> History { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
    public Forecast? LastForecast { get; set; }
    public int NextLocalId { get; set; } = 1;

    public ProjectType Archetype => ProductCatalog.Type(ArchetypeId);
    public Segment Segment => ProductCatalog.Segment(SegmentId);
    public HostingPlan Hosting => ProductCatalog.Plan(HostingId);
    public bool IsLive => LaunchedAt != null;

    public ProjectComponent? Component(string defId) => Components.FirstOrDefault(c => c.DefId == defId);

    public int Level(string defId) => Component(defId)?.Level ?? 0;

    /// <summary>Niveau visé en comptant la file (pour savoir quelle amélioration proposer ensuite).</summary>
    public int PlannedLevel(string defId) => Math.Max(Level(defId), Queue.Where(q => q.DefId == defId).Select(q => q.ToLevel).DefaultIfEmpty(0).Max());

    public int NewLocalId() => NextLocalId++;
}
