using Core.finance;
using Core.products;
using Core.space;

namespace Core.campaign;

/// <summary>Ce que l'entreprise a de capacité, tous sites réunis (les transferts entre sites sont agrégés).</summary>
public record InfraTotals(double Hosting, double Compute, double Gpu, double Storage, decimal MonthlyEnergy, double Quantum = 0);

/// <summary>
/// L'infrastructure interne : chaque site fournit des capacités (Core/space/Infrastructure.cs) ; chaque jour, elles sont
/// réparties dans un ordre fixe — d'abord tes projets hébergés en interne, dans l'ordre du portefeuille, puis les offres
/// cloud vendues (Campaign.Cloud.cs). Une même unité n'est jamais promise deux fois. L'électricité est facturée chaque mois.
/// </summary>
public partial class Campaign
{
    private readonly Dictionary<int, (Premises Site, int Items, InfraSummary Summary)> _infra = new();

    /// <summary>L'infrastructure d'un site (gardée en mémoire tant que ses objets ne changent pas).</summary>
    public InfraSummary SiteInfra(Premises site)
    {
        if (_infra.TryGetValue(site.Id, out var c) && c.Site == site && c.Items == site.Items.Count)
            return c.Summary;
        var summary = Infrastructure.Of(site);
        _infra[site.Id] = (site, site.Items.Count, summary);
        return summary;
    }

    /// <summary>Sites touchés par un incident aujourd'hui (coupure, panne) : leur capacité est réduite de moitié jusqu'à minuit.</summary>
    public HashSet<int> IncidentsToday { get; set; } = new();

    /// <summary>Jours où un incident a touché l'infrastructure (30 derniers jours) : la réputation des offres cloud s'en souvient.</summary>
    public List<long> IncidentDays { get; set; } = new();

    /// <summary>Part de l'hébergement interne gardée en réserve (marge), de 0 à 50 %.</summary>
    public double HostingReserve { get; set; } = 0.1;

    /// <summary>Hébergement interne encore libre après les projets (ce que les offres cloud peuvent vendre).</summary>
    public double HostingLeft { get; set; }

    public InfraTotals Infra()
    {
        double hosting = 0, compute = 0, gpu = 0, storage = 0, quantum = 0;
        decimal energy = 0;
        foreach (var site in Sites)
        {
            var s = SiteInfra(site);
            double incident = IncidentsToday.Contains(site.Id) ? 0.5 : 1;
            hosting += s.Hosting * incident;
            compute += s.Compute * incident;
            gpu += s.Gpu * incident;
            storage += s.Storage * incident;
            // Seules les installations quantiques déjà mises en route fournissent du calcul
            quantum += s.Quantum * incident * Math.Min(1.0, QuantumOnline(site) / Math.Max(1.0, s.QuantumUnits));
            energy += (decimal)s.KwhPerDay * 30 * Infrastructure.KwhPrice + s.MonthlyCost;
        }
        return new InfraTotals(hosting, compute, gpu, storage, Math.Round(energy), quantum);
    }

    /// <summary>Chaque nuit : incidents du jour (tirés pour chaque site équipé), puis répartition des capacités.</summary>
    private void RunInfrastructure()
    {
        IncidentsToday.Clear();
        foreach (var site in Sites)
        {
            var infra = SiteInfra(site);
            if (infra.Any && Random.Chance(infra.IncidentChance))
            {
                IncidentsToday.Add(site.Id);
                IncidentDays.Add(Day);
                Notify(Severity.Warning, "infrastructure", $"Incident sur le site {site.Name}",
                    "Une coupure : la moitié de sa capacité manque aujourd'hui. Onduleurs et supervision les rendent plus rares.", "sites", key: $"incident:{site.Id}");
            }
            else
                Resolve($"incident:{site.Id}");
            foreach (string issue in infra.Issues)
                Notify(Severity.Warning, "infrastructure", site.Name, issue, "sites", key: $"infra:{site.Id}");
            if (infra.Issues.Count == 0)
                Resolve($"infra:{site.Id}");
        }
        IncidentDays.RemoveAll(d => d < Day - 30);

        // Électricité et abonnements réseau, facturés avec le mois
        decimal energy = Sites.Sum(s => (decimal)SiteInfra(s).KwhPerDay * Infrastructure.KwhPrice + SiteInfra(s).MonthlyCost / 30);
        if (energy > 0)
            Ledger.Accrue(Category.Energy, Math.Round(energy, 2));

        // Répartition : les projets d'abord, dans l'ordre du portefeuille, avec une marge de 25 % sur leur charge
        var totals = Infra();
        double pool = totals.Hosting * (1 - Math.Clamp(HostingReserve, 0, 0.5));
        bool optimization = IsUnlocked("optimization");
        foreach (var product in Products.OrderBy(p => p.Priority))
        {
            if (!product.IsLive || !product.HostingInternal)
            {
                product.InternalCapacity = 0;
                continue;
            }
            double want = Math.Max(1, Market.Load(product, optimization) / 0.75);
            product.InternalCapacity = Math.Round(Math.Min(want, pool), 2);
            pool -= product.InternalCapacity;
        }
        HostingLeft = Math.Max(0, pool);
        // Ensuite l'IA (inférence des produits, puis entraînement), et seulement après, ce qui reste se vend
        AllocateAi(totals);
        AllocateCloud(totals);
    }

    /// <summary>Héberge un projet sur tes serveurs (avec débordement vers l'offre externe, sous le plafond), ou à l'extérieur.</summary>
    public CommandResult SetInternalHosting(int productId, bool inside)
    {
        var product = FindProduct(productId);
        if (product == null)
            return CommandResult.Fail("Projet inconnu.");
        if (inside && Sites.All(s => SiteInfra(s).Hosting <= 0))
            return CommandResult.Fail("Aucun serveur ne fournit d'hébergement : installe un serveur tour, ou une baie avec un switch et des serveurs.");
        product.HostingInternal = inside;
        // La capacité arrive à la prochaine répartition ; en attendant, le débordement externe prend le relais
        if (inside && product.IsLive && product.InternalCapacity <= 0)
        {
            product.InternalCapacity = Math.Min(HostingLeft, Math.Max(1, Market.Load(product, IsUnlocked("optimization")) / 0.75));
            HostingLeft -= product.InternalCapacity;
        }
        AdjustHosting(product);
        return CommandResult.Success(inside ? "Le projet passe sur tes serveurs." : "Le projet repasse chez un hébergeur.");
    }
}
