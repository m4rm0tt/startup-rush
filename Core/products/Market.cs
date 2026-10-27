using Core.campaign;

namespace Core.products;

/// <summary>Un concurrent sur un segment. Il progresse à son rythme et annonce ses nouvelles versions à l'avance.</summary>
public class Competitor
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string SegmentId { get; set; } = "";
    public double Appeal { get; set; }
    public decimal Price { get; set; }

    /// <summary>Nouvelle version annoncée : date de sortie et gain d'attrait prévu.</summary>
    public long? UpgradeAt { get; set; }

    public double UpgradeGain { get; set; }
}

/// <summary>Ce qu'un produit reçoit de l'entreprise ce jour-là (temps de support, budget de marketing).</summary>
public record DayInputs(double SupportPoints, IReadOnlyList<Competitor> Competitors);

/// <summary>
/// Le modèle de marché, un pas par jour. Toutes les étapes sont multiplicatives et gardées dans ProductDay,
/// pour que le jeu puisse expliquer d'où vient une hausse ou une baisse.
/// </summary>
public static class Market
{
    public const double TicketsPerSupportPoint = 6;
    public const decimal MarketplaceBasket = 55m;

    public static double Load(Product product, ProductSpec spec)
    {
        double perThousand = spec.ActiveModules().Sum(m => m.Def.Load * m.Factor(c => c.LoadFactor));
        double people = product.Users + product.Sellers;
        return people / 1000 * perThousand * Math.Pow(0.85, product.Optimizations);
    }

    public static double Performance(double utilization) => utilization switch
    {
        <= 0.8 => 1,
        <= 1 => 1 - (utilization - 0.8),
        _ => Math.Max(0.25, 0.8 - (utilization - 1) * 0.8)
    };

    public static double PriceFactor(Product product)
    {
        var segment = product.Segment;
        if (product.Model == RevenueModel.Ads)
            return 1;
        if (product.Model == RevenueModel.Commission)
            return Math.Clamp(Math.Pow(10.0 / Math.Max(1, (double)product.Price), segment.PriceSensitivity * 0.5), 0.2, 1.6);
        return Math.Clamp(Math.Pow((double)segment.ReferencePrice / Math.Max(1, (double)product.Price), segment.PriceSensitivity), 0.1, 1.8);
    }

    public static double Quality(Product product, ModuleInstance module) =>
        product.ModuleQuality.TryGetValue(module.Id, out double q) ? q : 0.6;

    /// <summary>Avance le produit d'un jour. Ne touche qu'au produit : l'argent est comptabilisé par la campagne.</summary>
    public static ProductDay Step(Product product, DayInputs inputs, long day)
    {
        var spec = product.Live!;
        var archetype = product.Archetype;
        var segment = product.Segment;
        var coverage = ProductDesign.Coverage(product, spec, m => Quality(product, m));

        // Attrait : conception × bugs × performances
        double baseAppeal = ProductDesign.Appeal(product, spec, m => Quality(product, m));
        double bugPenalty = Math.Min(0.45, product.Bugs * 0.012);
        double utilization = Load(product, spec) / product.Hosting.Capacity;
        double performance = Performance(utilization);
        double downtime = utilization > 1 ? Math.Min(24, (utilization - 1) * 20) : 0;
        double appeal = baseAppeal * (1 - bugPenalty) * performance;

        // Concurrence : part relative de l'attrait
        double own = Math.Pow(appeal, 3);
        double others = inputs.Competitors.Where(c => c.SegmentId == product.SegmentId).Sum(c => Math.Pow(c.Appeal, 3));
        double share = own + others <= 0 ? 0 : own / (own + others);
        double priceFactor = PriceFactor(product);

        // Notoriété : bouche-à-oreille, marketing (rendement décroissant), et un peu d'organique
        var channel = ProductCatalog.Channel(product.MarketingChannelId);
        double budgetPerDay = (double)product.MarketingBudget / 30;
        double sellerShare = archetype.Id == "marketplace" ? product.SellerMarketingShare : 0;
        double reach = budgetPerDay * channel.ReachPerChf.GetValueOrDefault(archetype.Id, 0.1) / (1 + budgetPerDay / channel.Saturation);
        double lastSatisfaction = product.History.Count > 0 ? Satisfaction(product.History[^1]) : 0.4;
        double wordOfMouth = product.Users * Tuning.Products.WordOfMouth * archetype.WordOfMouthFactor * lastSatisfaction;
        double unaware = Math.Max(0, segment.Size - product.Aware);
        double organic = Tuning.Products.OrganicAwarenessPerDay * (0.4 + 2 * appeal);
        double newAware = Math.Min(unaware, (organic + reach * (1 - sellerShare) + wordOfMouth) * unaware / segment.Size);
        product.Aware += newAware;

        bool paidOnly = product.Model == RevenueModel.Subscription;
        double trial = archetype.TrialRate * (0.4 + 2 * appeal) * (0.5 + share) * (paidOnly ? Math.Sqrt(priceFactor) : 1);
        double signups = newAware * Math.Min(0.9, trial);

        // Support : tickets du jour contre capacité de l'équipe affectée
        double ticketRate = Math.Max(0.05, spec.ActiveModules().Sum(m => m.Def.Tickets * m.Factor(c => c.TicketFactor)));
        double tickets = (product.Users + product.Sellers) / 1000 * ticketRate * archetype.TicketFactor * 10 * (1 + product.Bugs * 0.02);
        double handled = inputs.SupportPoints * TicketsPerSupportPoint;
        product.SupportBacklog = Math.Max(0, product.SupportBacklog + tickets - handled);
        double backlogDays = product.SupportBacklog / Math.Max(1, tickets);
        double supportFactor = 1 / (1 + 0.2 * Math.Max(0, backlogDays - 1));

        // Place de marché : il faut assez de vendeurs pour les acheteurs
        double liquidity = 1;
        if (archetype.Id == "marketplace")
        {
            double newSellers = (reach * sellerShare * 0.1 + newAware * 0.01) * (0.2 + coverage[Need.Liquidity]);
            double sales = product.Users * 0.015 * (0.3 + coverage[Need.Conversion]);
            double salesPerSeller = product.Sellers > 0 ? sales / product.Sellers : 0;
            double sellerChurn = product.Sellers * 0.01 * Math.Clamp(1.5 - salesPerSeller, 0.4, 1.5);
            product.Sellers = Math.Max(0, product.Sellers + newSellers - sellerChurn);
            liquidity = Math.Min(1, product.Sellers / Math.Max(1, product.Users * 0.04));
        }

        double satisfaction = appeal * supportFactor * (1 - downtime / 24 * 0.6)
                              * (archetype.Id == "marketplace" ? 0.4 + 0.6 * liquidity : 1)
                              * (product.Model == RevenueModel.Ads ? 0.92 : 1);
        double churnFactor = Math.Clamp(1.8 - 2 * satisfaction, 0.3, 2.5);
        double churned = product.Users * Tuning.Products.BaseDailyChurn * archetype.ChurnFactor * churnFactor
                         + product.Users * 0.01 * downtime / 24;
        product.Users = Math.Max(0, product.Users + signups - churned);

        // Revenus selon le modèle
        bool canCharge = ProductDesign.MissingRequired(product, spec).Count == 0;
        decimal revenue = 0;
        double paying = 0;
        if (canCharge)
        {
            switch (product.Model)
            {
                case RevenueModel.Subscription:
                    paying = product.Users * Math.Min(0.95, 0.6 * (0.3 + coverage[Need.Conversion]) * priceFactor);
                    revenue = (decimal)paying * product.Price / 30;
                    break;
                case RevenueModel.Freemium:
                    paying = product.Users * Math.Min(0.3, 0.05 * (0.4 + 1.5 * coverage[Need.Conversion]) * priceFactor);
                    revenue = (decimal)paying * product.Price / 30;
                    break;
                case RevenueModel.Ads:
                    revenue = (decimal)(product.Users * (0.004 + 0.012 * coverage[Need.Engagement]));
                    break;
                case RevenueModel.Commission:
                    paying = product.Users * 0.015 * liquidity * (0.3 + coverage[Need.Conversion]) * priceFactor;
                    revenue = (decimal)paying * MarketplaceBasket * product.Price / 100;
                    break;
            }
        }
        product.Paying = paying;

        var result = new ProductDay
        {
            Day = day,
            NewAware = newAware,
            Signups = signups,
            Churned = churned,
            Users = product.Users,
            Paying = paying,
            Sellers = product.Sellers,
            Revenue = Math.Round(revenue, 2),
            Appeal = appeal,
            Share = share,
            PriceFactor = priceFactor,
            BugPenalty = bugPenalty,
            Performance = performance,
            SupportFactor = supportFactor,
            Liquidity = liquidity,
            DowntimeHours = downtime,
            MarketingReach = reach,
            Tickets = tickets,
        };
        product.History.Add(result);
        if (product.History.Count > Tuning.Products.HistoryDays)
            product.History.RemoveAt(0);
        return result;
    }

    public static double Satisfaction(ProductDay day) => day.Appeal * day.SupportFactor * (1 - day.DowntimeHours / 24 * 0.6) * (0.4 + 0.6 * day.Liquidity);

    /// <summary>
    /// Estime les résultats d'une version sur 90 jours en faisant tourner le même modèle sur une copie du produit,
    /// avec une qualité supposée et une fourchette de ±15 % sur l'attrait (l'incertitude de la prévision).
    /// </summary>
    public static Forecast Forecast(Product product, ProductSpec spec, double assumedQuality, double expectedBugs,
        double supportPoints, IReadOnlyList<Competitor> competitors)
    {
        (double Users, decimal Revenue) Run(double qualityShift)
        {
            var copy = System.Text.Json.JsonSerializer.Deserialize<Product>(System.Text.Json.JsonSerializer.Serialize(product))!;
            copy.Live = spec.Clone();
            copy.History.Clear();
            copy.ModuleQuality = spec.AllModules.ToDictionary(m => m.Id, m =>
                Math.Clamp((product.ModuleQuality.TryGetValue(m.Id, out double q) ? q : assumedQuality) * qualityShift, 0.1, 1));
            copy.Bugs += expectedBugs;
            decimal revenue = 0;
            for (int d = 0; d < 90; d++)
            {
                var day = Step(copy, new DayInputs(supportPoints, competitors), d);
                if (d >= 60)
                    revenue += day.Revenue;
            }
            return (copy.Users, revenue);
        }

        var low = Run(0.85);
        var high = Run(1.15);
        return new Forecast
        {
            Appeal = ProductDesign.Appeal(product, spec, m => product.ModuleQuality.TryGetValue(m.Id, out double q) ? q : assumedQuality),
            UsersLow = Math.Round(low.Users),
            UsersHigh = Math.Round(high.Users),
            RevenueLow = Math.Round(low.Revenue),
            RevenueHigh = Math.Round(high.Revenue),
            ExpectedBugsLow = Math.Round(expectedBugs * 0.6, 1),
            ExpectedBugsHigh = Math.Round(expectedBugs * 1.5, 1),
        };
    }

    /// <summary>
    /// Pourquoi les revenus ou les utilisateurs bougent : compare la dernière semaine à la précédente, facteur par facteur,
    /// et renvoie les causes les plus importantes d'abord.
    /// </summary>
    public static List<string> Explain(Product product)
    {
        var reasons = new List<(double Weight, string Text)>();
        var history = product.History;
        if (history.Count < 14)
            return ["Pas encore assez d'historique (2 semaines nécessaires) pour expliquer les tendances."];

        var last = history.Skip(history.Count - 7).ToList();
        var before = history.Skip(history.Count - 14).Take(7).ToList();
        double Avg(List<ProductDay> days, Func<ProductDay, double> f) => days.Average(f);

        void Compare(string name, Func<ProductDay, double> f, string up, string down)
        {
            double a = Avg(before, f), b = Avg(last, f);
            if (a <= 1e-6 && b <= 1e-6)
                return;
            double change = (b - a) / Math.Max(1e-6, Math.Abs(a));
            if (Math.Abs(change) < 0.05)
                return;
            reasons.Add((Math.Abs(change), $"{name} {(change > 0 ? "+" : "")}{change:P0} : {(change > 0 ? up : down)}"));
        }

        double revenueBefore = (double)before.Sum(d => d.Revenue), revenueLast = (double)last.Sum(d => d.Revenue);
        string headline = revenueBefore <= 0 && revenueLast <= 0
            ? "Aucun revenu sur les deux dernières semaines."
            : $"Revenus de la semaine : {revenueLast:N0} CHF (semaine précédente : {revenueBefore:N0} CHF).";

        Compare("Notoriété nouvelle", d => d.NewAware, "plus de monde découvre le produit (marketing, bouche-à-oreille)", "moins de nouveaux curieux (marketing réduit ou marché qui s'épuise)");
        Compare("Attrait", d => d.Appeal, "la version plaît davantage", "le produit plaît moins (bugs, lenteurs, conception)");
        Compare("Part face à la concurrence", d => d.Share, "les concurrents pèsent moins", "un concurrent attire vos utilisateurs potentiels");
        Compare("Effet des bugs", d => 1 - d.BugPenalty, "moins de bugs gênants", "les bugs font fuir les utilisateurs");
        Compare("Performances", d => d.Performance, "les serveurs suivent mieux", "les serveurs saturent : pages lentes");
        Compare("Support", d => d.SupportFactor, "les demandes sont traitées plus vite", "les tickets s'accumulent sans réponse");
        Compare("Abandons", d => d.Churned, "plus d'utilisateurs partent", "moins d'utilisateurs partent");
        if (product.ArchetypeId == "marketplace")
            Compare("Liquidité", d => d.Liquidity, "plus de vendeurs pour les acheteurs", "pas assez de vendeurs pour la demande");
        if (last.Any(d => d.DowntimeHours > 0))
            reasons.Add((1, $"Pannes : {last.Sum(d => d.DowntimeHours):0} h d'indisponibilité cette semaine (capacité d'hébergement dépassée)."));
        if (ProductDesign.MissingRequired(product, product.Live!).Count > 0)
            reasons.Add((2, "Aucun encaissement possible : " + product.Archetype.RequirementText));
        if (last[^1].PriceFactor < 0.6 && product.Model != RevenueModel.Ads)
            reasons.Add((0.5, $"Prix jugé élevé par ce public (référence {product.Segment.ReferencePrice} CHF)."));

        return new[] { headline }.Concat(reasons.OrderByDescending(r => r.Weight).Select(r => r.Text).Take(5)).ToList();
    }
}
