using Core.campaign;

namespace Core.products;

/// <summary>Une tendance des avis : combien en parlent, ce qu'ils disent, et où aller pour agir.</summary>
public record ReviewTrend(string Cause, bool Positive, double Share, string Text, string Link);

/// <summary>
/// Les avis des utilisateurs, tirés des facteurs réels de la simulation (performances, bugs, support, prix,
/// besoins mal couverts, mises à jour récentes). Ils ne donnent pas d'ordres : ils signalent des tendances.
/// </summary>
public static class Reviews
{
    public const int MaxStored = 24;

    /// <summary>Les tendances actuelles, les plus fortes d'abord (3 négatives et 2 positives au plus).</summary>
    public static List<ReviewTrend> Trends(Product product, long now, Func<string, bool> unlocked)
    {
        var trends = new List<ReviewTrend>();
        if (!product.IsLive || product.History.Count < 3)
            return trends;
        var recent = product.History.Skip(Math.Max(0, product.History.Count - 7)).ToList();
        double perf = recent.Average(d => d.Performance);
        double downtime = recent.Sum(d => d.DowntimeHours);
        double bugs = recent.Average(d => d.BugPenalty);
        double support = recent.Average(d => d.SupportFactor);
        double price = recent.Average(d => d.PriceFactor);
        double liquidity = recent.Average(d => d.Liquidity);
        string players = product.ArchetypeId == "game" ? "joueurs" : "utilisateurs";

        if (perf < 0.97 || downtime > 0)
            trends.Add(new("perf", false, Math.Clamp((1 - perf) * 2 + downtime / 48, 0.1, 0.8),
                downtime > 0 ? "« Ça ne marche plus du tout par moments. »" : "« C'est lent à charger. »", "hosting"));
        if (bugs > 0.06)
            trends.Add(new("bugs", false, Math.Clamp(bugs * 1.5, 0.1, 0.7), "« Ça plante trop souvent. »", "upkeep"));
        if (support < 0.85 && product.Archetype.TicketFactor > 0.55)
            trends.Add(new("support", false, Math.Clamp((1 - support) * 1.5, 0.1, 0.6), "« Personne ne répond à mes questions. »", "upkeep"));
        if (price < 0.7 && product.Model != RevenueModel.Ads)
            trends.Add(new("price", false, Math.Clamp((1 - price) * 0.8, 0.1, 0.6), "« Trop cher pour ce que c'est. »", "price"));
        // IA : la capacité d'inférence plafonne l'audience servie (délai et coût d'usage)
        double aiLoad = recent.Average(d => d.AiUtilization);
        if (product.AiModelId != 0 && aiLoad > 0.95)
            trends.Add(new("ai_capacity", false, Math.Clamp((aiLoad - 0.9) * 0.8, 0.1, 0.7),
                "« Les réponses mettent trop de temps, parfois elles échouent. »", "ai"));
        if (product.ArchetypeId == "marketplace" && liquidity < 0.6)
            trends.Add(new("sellers", false, Math.Clamp(1 - liquidity, 0.1, 0.7), "« Pas assez d'offres, je ne trouve rien. »", "component:listings"));

        var (need, coverage, fix) = Design.WeakestNeed(product, unlocked);
        if (coverage < 0.35 && product.Archetype.Needs.GetValueOrDefault(need) >= 0.15)
            trends.Add(new($"need:{need}", false, Math.Clamp((0.35 - coverage) * 1.2 + 0.1, 0.1, 0.6),
                NeedComplaint(need, fix?.Name), need == Need.Intelligence ? "ai" : fix != null ? $"component:{fix.Id}" : "components"));

        // Ce qui plaît
        if (product.LastUpdateAt is long update && now - update < 14L * GameTime.MinutesPerDay)
            trends.Add(new("update", true, 0.3, "« La dernière mise à jour est vraiment bien. »", "components"));
        var best = product.Components.Where(c => c.Level > 0 && c.Def.Needs.Count > 0)
            .OrderByDescending(c => ProductCatalog.LevelValue(c.Level) * c.Quality * c.Def.Needs.Sum(n => product.Archetype.Needs.GetValueOrDefault(n.Key) * n.Value))
            .FirstOrDefault();
        if (best != null && best.Quality >= 0.55)
            trends.Add(new("praise", true, Math.Clamp(Design.Appeal(product), 0.1, 0.8), $"« J'adore : {best.Def.Name.ToLowerInvariant()}. »", $"component:{best.DefId}"));

        var negatives = trends.Where(t => !t.Positive).OrderByDescending(t => t.Share).Take(3);
        var positives = trends.Where(t => t.Positive).OrderByDescending(t => t.Share).Take(2);
        return negatives.Concat(positives).ToList();
    }

    private static string NeedComplaint(Need need, string? fix) => need switch
    {
        Need.Fun => "« On s'ennuie vite. »",
        Need.Discovery => "« On fait vite le tour. »",
        Need.Engagement => "« Rien ne me donne envie de revenir. »",
        Need.Trust => "« Ça fait un peu amateur. »",
        Need.Conversion => "« Je ne vois pas pourquoi payer. »",
        Need.Productivity => "« Pratique, mais il manque l'essentiel pour bosser. »",
        Need.Power => "« Pas assez puissant pour le prix. »",
        Need.Durability => "« Je doute que ça tienne longtemps. »",
        Need.Portability => "« Trop lourd, et la batterie ne tient pas. »",
        Need.Immersion => "« On ne s'y croit pas, et ça donne un peu mal au cœur. »",
        Need.Intelligence => "« Les réponses sont souvent à côté de la plaque. »",
        _ => "« Pas assez d'offres. »"
    };

    /// <summary>Note moyenne sur 5, d'après la satisfaction de la dernière semaine.</summary>
    public static double Rating(Product product)
    {
        if (product.History.Count == 0)
            return 0;
        double satisfaction = product.History.Skip(Math.Max(0, product.History.Count - 7)).Average(Market.Satisfaction);
        return Math.Round(1 + 4 * Math.Clamp((satisfaction - 0.08) / 0.45, 0, 1), 1);
    }

    /// <summary>Ajoute quelques avis datés (chaque semaine), tirés des tendances : le fil reste court et varié.</summary>
    public static void Collect(Product product, long now, SimRandom random, Func<string, bool> unlocked)
    {
        var trends = Trends(product, now, unlocked);
        if (trends.Count == 0 || product.Users < 5)
            return;
        int count = product.Users < 100 ? 1 : product.Users < 2_000 ? 2 : 3;
        double rating = Rating(product);
        for (int i = 0; i < count; i++)
        {
            var trend = trends[random.Next(0, trends.Count)];
            int stars = trend.Positive ? Math.Clamp((int)Math.Round(rating) + 1, 3, 5) : Math.Clamp((int)Math.Round(rating) - 1, 1, 4);
            product.Reviews.Add(new Review { Day = GameTime.Day(now), Stars = stars, Text = trend.Text, Cause = trend.Cause, Link = trend.Link });
        }
        if (product.Reviews.Count > MaxStored)
            product.Reviews.RemoveRange(0, product.Reviews.Count - MaxStored);
    }
}
