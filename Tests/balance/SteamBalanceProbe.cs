using Core.bots;

namespace Tests.balance;

/// <summary>
/// Lot 4 de la version Steam : les parcours proposés au joueur, mesurés sur les mêmes graines et la même durée que
/// BalanceReportTest (Play), sans tout le rapport (~45 min sur le VPS). Lancée à la main ; écrit un tableau en Markdown
/// dans le dossier STEAM_PROBE_OUT (sinon le dossier temporaire).
/// </summary>
public class SteamBalanceProbe
{
    public static readonly string[] Paths =
    [
        "un_projet", "plusieurs_petits", "studio_xr", "materiel", "materiel_industrie", "materiel_pc", "materiel_gpu",
        "ia", "ia_salle_serveur", "quantique", "marketing_excessif", "exces_puis_correction",
    ];

    public static readonly ulong[] ProbeSeeds = [11, 23, 37];

    [Test, Explicit]
    public void Paths18Months()
    {
        var runs = Paths.SelectMany(name => ProbeSeeds.Select(seed => (name, seed))).AsParallel().AsOrdered()
            .Select(x => BalanceReportTest.Play(Strategies.All.First(s => s.Name == x.name), x.seed)).ToList();
        var md = new System.Text.StringBuilder();
        md.AppendLine($"Graines {string.Join(", ", ProbeSeeds)} · {BalanceReportTest.Months} mois · fondateur polyvalent");
        md.AppendLine();
        md.AppendLine("| Parcours | Faillites | Argent final méd. | min – max | 1re sortie (j) | 1er mois bénéficiaire (j) | Revenus projets / mois méd. | Argent par trimestre (graine 11) |");
        md.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var g in runs.GroupBy(r => r.Strategy))
        {
            var l = g.ToList();
            var first = l.First(r => r.Seed == ProbeSeeds[0]);
            md.AppendLine($"| {g.Key} | {l.Count(r => r.Liquidated)}/{l.Count} | {Med(l.Select(r => r.FinalCash))} | {l.Min(r => r.FinalCash):N0} – {l.Max(r => r.FinalCash):N0} | " +
                          $"{MedDay(l.Select(r => r.FirstLaunchDay))} | {MedDay(l.Select(r => r.ProfitableMonthDay))} ({l.Count(r => r.ProfitableMonthDay >= 0)}/{l.Count}) | {Med(l.Select(r => r.Revenue30))} | " +
                          $"{string.Join(" → ", first.CashByQuarter.Select(c => $"{c:N0}"))} |");
        }
        string folder = Environment.GetEnvironmentVariable("STEAM_PROBE_OUT") ?? Path.GetTempPath();
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, $"steam_probe_{Environment.GetEnvironmentVariable("STEAM_PROBE_TAG") ?? "run"}.md");
        File.WriteAllText(file, md.ToString());
        TestContext.Out.WriteLine(md.ToString());
    }

    /// <summary>
    /// La carte d'entrée de gamme (makers) sur 36 mois : argent, revenus, et l'économie de la carte en fin de partie (prix,
    /// coût unitaire, ventes des 30 derniers jours). Écrit steam_makers_&lt;tag&gt;.md.
    /// </summary>
    [Test, Explicit]
    public void MakersCard36Months()
    {
        var rows = ProbeSeeds.AsParallel().AsOrdered().Select(seed =>
        {
            var strategy = Strategies.All.First(s => s.Name == "materiel");
            var campaign = Core.campaign.Campaign.New("Bilan", "Bot", "Bot SA", Core.campaign.FounderProfile.Generalist, seed);
            var pilot = new Autopilot(strategy);
            for (int day = 0; day < BalanceReportTest.LongMonths * 30 && campaign.Status == Core.campaign.CampaignStatus.Running; day++)
                pilot.Play(campaign, 1);
            var card = campaign.Products.FirstOrDefault(p => p.IsHardware);
            string economy = card == null ? "pas de carte" :
                $"prix {card.Price:0} · coût {Core.industry.IndustryCatalog.UnitCost(card):0} · vendues 30 j {card.History.TakeLast(30).Sum(d => d.Sold):0} · fabriquées {card.Produced} · en ligne {card.IsLive}";
            return $"| {seed} | {(campaign.Status == Core.campaign.CampaignStatus.Liquidated ? "faillite" : "en activité")} | {campaign.Ledger.Cash:N0} | {campaign.RecurringRevenue30:N0} | {economy} |";
        }).ToList();
        var md = new System.Text.StringBuilder();
        md.AppendLine($"materiel (carte makers), {BalanceReportTest.LongMonths} mois");
        md.AppendLine();
        md.AppendLine("| Graine | Issue | Argent final | Revenus / mois | La carte en fin de partie |");
        md.AppendLine("|---|---|---|---|---|");
        foreach (string row in rows)
            md.AppendLine(row);
        string folder = Environment.GetEnvironmentVariable("STEAM_PROBE_OUT") ?? Path.GetTempPath();
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, $"steam_makers_{Environment.GetEnvironmentVariable("STEAM_PROBE_TAG") ?? "run"}.md"), md.ToString());
        TestContext.Out.WriteLine(md.ToString());
    }

    private static string Med(IEnumerable<decimal> values)
    {
        var list = values.OrderBy(x => x).ToList();
        return list.Count == 0 ? "—" : $"{list[list.Count / 2]:N0}";
    }

    private static string MedDay(IEnumerable<int> values)
    {
        var list = values.Where(x => x >= 0).OrderBy(x => x).ToList();
        return list.Count == 0 ? "jamais" : $"{list[list.Count / 2]}";
    }
}
