using Core.campaign;

namespace Core.products;

/// <summary>Un concurrent sur un public. Il progresse à son rythme et annonce ses nouvelles versions à l'avance.</summary>
public class Competitor
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string SegmentId { get; set; } = "";
    public double Appeal { get; set; }
    public decimal Price { get; set; }
    public long? UpgradeAt { get; set; }
    public double UpgradeGain { get; set; }
}

/// <summary>Ce que l'entreprise apporte au projet ce jour-là, et les déblocages qui comptent pour le marché.</summary>
public record DayInputs(double SupportPoints, IReadOnlyList<Competitor> Competitors, bool Optimization = false, bool Loyalty = false)
{
    /// <summary>Matériel : stock vendable, visibilité et capacité des canaux, commission moyenne (null pour un logiciel).</summary>
    public HardwareSales? Sales { get; init; }
}

/// <summary>Ce qui limite les ventes d'un produit matériel aujourd'hui.</summary>
public record HardwareSales(int Stock, double Visibility, double PerDay, double Commission);

/// <summary>
/// Le modèle de marché, un pas par jour. Toutes les étapes sont multiplicatives et gardées dans ProductDay,
/// pour que le jeu puisse expliquer d'où vient une hausse ou une baisse (et en tirer les avis).
/// </summary>
public static class Market
{
    public const double TicketsPerSupportPoint = 6;
    public const decimal MarketplaceBasket = 55m;

    public static double Load(Product product, bool optimization = false) =>
        (product.Users + product.Sellers) / 1000 * Design.LoadPerThousand(product, optimization);

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

    /// <summary>Marketing : visiteurs par jour, avec un rendement décroissant (chaque franc rapporte moins que le précédent).</summary>
    public static double Reach(Product product)
    {
        var channel = ProductCatalog.Channel(product.MarketingChannelId);
        double budgetPerDay = (double)product.MarketingBudget / 30;
        return budgetPerDay * channel.ReachPerChf.GetValueOrDefault(product.ArchetypeId, 0.1) / (1 + budgetPerDay / channel.Saturation);
    }

    /// <summary>Avant la sortie d'un jeu : le marketing et un peu de bouche-à-oreille remplissent la liste de souhaits.</summary>
    public static void PreLaunch(Product product, bool launches)
    {
        if (product.ArchetypeId != "game")
            return;
        double interest = (15 + Reach(product)) * (launches ? 1.5 : 1);
        product.Wishlist = Math.Min(product.Segment.Size * 0.2, product.Wishlist + interest);
    }

    /// <summary>Le jour du lancement : un jeu convertit sa liste de souhaits selon son attrait.</summary>
    public static double Launch(Product product)
    {
        if (product.ArchetypeId != "game")
            return 0;
        double appeal = Design.Appeal(product);
        double buyers = product.Wishlist * Math.Clamp(0.15 + appeal * 0.6, 0.1, 0.6) * Math.Sqrt(PriceFactor(product));
        product.Aware += product.Wishlist;
        product.Wishlist = 0;
        product.Users += buyers;
        product.TotalBuyers += buyers;
        return buyers;
    }

    /// <summary>Une mise à jour fait revenir des joueurs et reparler du projet : une partie du public déjà informé redevient curieuse.</summary>
    public static void UpdateBoost(Product product)
    {
        product.Aware = Math.Max(0, product.Aware - (product.ArchetypeId == "game" ? 150 + product.TotalBuyers * 0.05 : 50));
    }

    /// <summary>Avance le projet d'un jour. Ne touche qu'au projet : l'argent est comptabilisé par la campagne.</summary>
    public static ProductDay Step(Product product, DayInputs inputs, long day)
    {
        var archetype = product.Archetype;
        var segment = product.Segment;
        var coverage = Design.Coverage(product);

        // Attrait : composants × bugs × performances
        double baseAppeal = Design.Appeal(product);
        double bugPenalty = Math.Min(0.45, product.Bugs * 0.012);
        double utilization = Load(product, inputs.Optimization) / Math.Max(0.01, product.Capacity);
        double performance = Performance(utilization);
        double downtime = utilization > 1 ? Math.Min(24, (utilization - 1) * 20) : 0;
        // Matériel : les pannes connues pèsent sur l'attrait comme des bugs
        double appeal = baseAppeal * (1 - bugPenalty) * performance * (1 - Math.Min(0.5, product.DefectRate * 2));

        double own = Math.Pow(appeal, 3);
        double others = inputs.Competitors.Where(c => c.SegmentId == product.SegmentId).Sum(c => Math.Pow(c.Appeal, 3));
        double share = own + others <= 0 ? 0 : own / (own + others);
        double priceFactor = PriceFactor(product);

        // Notoriété : bouche-à-oreille, marketing, organique
        double sellerShare = archetype.Id == "marketplace" ? product.SellerMarketingShare : 0;
        double reach = Reach(product);
        double lastSatisfaction = product.History.Count > 0 ? Satisfaction(product.History[^1]) : 0.4;
        double wordOfMouth = product.Users * Tuning.Products.WordOfMouth * archetype.WordOfMouthFactor * lastSatisfaction;
        double unaware = Math.Max(0, segment.Size - product.Aware);
        double organic = Tuning.Products.OrganicAwarenessPerDay * (0.4 + 2 * appeal);
        double newAware = Math.Min(unaware, (organic + reach * (1 - sellerShare) + wordOfMouth) * unaware / segment.Size);
        product.Aware += newAware;

        bool paysUpfront = product.Model is RevenueModel.Subscription or RevenueModel.Purchase;
        double trial = archetype.TrialRate * (0.4 + 2 * appeal) * (0.5 + share) * (paysUpfront ? Math.Sqrt(priceFactor) : 1);
        double signups = newAware * Math.Min(0.9, trial);

        // Matériel : on ne vend que des unités entières, en stock, par des canaux qui ont de la capacité
        double sold = 0, lost = 0;
        if (inputs.Sales is { } channels)
        {
            double desired = signups * channels.Visibility + product.PendingDemand;
            double whole = Math.Floor(desired);
            sold = Math.Min(whole, Math.Min(channels.Stock, Math.Floor(channels.PerDay)));
            lost = whole - sold;
            product.PendingDemand = desired - whole;
            signups = sold;
        }

        // Support : tickets du jour contre le temps que l'équipe y consacre
        double tickets = (product.Users + product.Sellers) / 1000 * Design.TicketRate(product) * archetype.TicketFactor * 4 * (1 + product.Bugs * 0.02);
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
        double churnFactor = Math.Clamp(1.8 - 2 * satisfaction, 0.3, 2.5) * (inputs.Loyalty ? 0.85 : 1);
        double churned = product.Users * Tuning.Products.BaseDailyChurn * archetype.ChurnFactor * churnFactor
                         + product.Users * 0.01 * downtime / 24;
        product.Users = Math.Max(0, product.Users + signups - churned);

        // Revenus selon le modèle
        decimal revenue = 0;
        double paying = 0;
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
                revenue = (decimal)(product.Users * (0.008 + 0.02 * coverage[Need.Engagement]));
                break;
            case RevenueModel.Commission:
                paying = product.Users * 0.015 * liquidity * (0.3 + coverage[Need.Conversion]) * priceFactor;
                revenue = (decimal)paying * MarketplaceBasket * product.Price / 100;
                break;
            case RevenueModel.Purchase:
                // Chaque nouveau joueur achète le jeu une fois ; les joueurs actifs ne paient plus
                paying = signups;
                product.TotalBuyers += signups;
                revenue = (decimal)signups * product.Price * (1 - (decimal)(inputs.Sales?.Commission ?? 0));
                break;
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
            Sold = sold,
            Lost = lost,
        };
        product.History.Add(result);
        if (product.History.Count > Tuning.Products.HistoryDays)
            product.History.RemoveAt(0);
        return result;
    }

    public static double Satisfaction(ProductDay day) => day.Appeal * day.SupportFactor * (1 - day.DowntimeHours / 24 * 0.6) * (0.4 + 0.6 * day.Liquidity);

    /// <summary>
    /// Estime les résultats à 90 jours (après la sortie, ou à partir d'aujourd'hui), avec le même modèle sur une copie,
    /// en comptant la file d'améliorations comme faite, avec une fourchette de ±15 % sur la qualité.
    /// </summary>
    public static Forecast Forecast(Product product, IReadOnlyList<Competitor> competitors, double supportPoints)
    {
        (double Users, decimal Revenue) Run(double shift)
        {
            var copy = System.Text.Json.JsonSerializer.Deserialize<Product>(System.Text.Json.JsonSerializer.Serialize(product))!;
            foreach (var queued in copy.Queue)
            {
                var existing = copy.Component(queued.DefId);
                if (existing == null)
                    copy.Components.Add(new ProjectComponent { DefId = queued.DefId, Level = queued.ToLevel });
                else
                    existing.Level = Math.Max(existing.Level, queued.ToLevel);
            }
            foreach (var c in copy.Components)
                c.Quality = Math.Clamp(c.Quality * shift, 0.1, 1);
            copy.Queue.Clear();
            copy.History.Clear();
            decimal revenue = 0;
            if (copy.LaunchedAt == null)
                Launch(copy);
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
            Appeal = Design.Appeal(product),
            UsersLow = Math.Round(low.Users),
            UsersHigh = Math.Round(high.Users),
            RevenueLow = Math.Round(low.Revenue),
            RevenueHigh = Math.Round(high.Revenue),
        };
    }

    /// <summary>Pourquoi ça bouge : la dernière semaine comparée à la précédente, cause par cause (vue détaillée facultative).</summary>
    public static List<string> Explain(Product product)
    {
        var reasons = new List<(double Weight, string Text)>();
        var history = product.History;
        if (history.Count < 14)
            return ["Encore trop tôt : il faut deux semaines de résultats."];

        var last = history.Skip(history.Count - 7).ToList();
        var before = history.Skip(history.Count - 14).Take(7).ToList();

        void Compare(string name, Func<ProductDay, double> f, string up, string down)
        {
            double a = before.Average(f), b = last.Average(f);
            if (a <= 1e-6 && b <= 1e-6)
                return;
            double change = (b - a) / Math.Max(1e-6, Math.Abs(a));
            if (Math.Abs(change) < 0.05)
                return;
            reasons.Add((Math.Abs(change), $"{name} {(change > 0 ? "+" : "")}{change:P0} : {(change > 0 ? up : down)}"));
        }

        Compare("Nouveaux curieux", d => d.NewAware, "plus de monde découvre le projet", "moins de nouveaux curieux");
        Compare("Attrait", d => d.Appeal, "le projet plaît davantage", "le projet plaît moins");
        Compare("Face aux concurrents", d => d.Share, "les concurrents pèsent moins", "un concurrent attire du monde");
        Compare("Bugs", d => 1 - d.BugPenalty, "moins gênants", "plus gênants");
        Compare("Serveurs", d => d.Performance, "plus rapides", "saturés");
        Compare("Support", d => d.SupportFactor, "plus réactif", "débordé");
        Compare("Départs", d => d.Churned, "plus de départs", "moins de départs");
        return reasons.OrderByDescending(r => r.Weight).Select(r => r.Text).Take(5).DefaultIfEmpty("Stable par rapport à la semaine précédente.").ToList();
    }
}
