using System.Text;
using Core.bots;
using Core.campaign;
using Core.finance;
using Core.work;

namespace Tests.balance;

/// <summary>
/// Campagne d'équilibrage : chaque stratégie est jouée sur plusieurs graines pendant 18 mois de jeu, et l'on mesure
/// trajectoire de trésorerie, rentabilité, défaillances, retards, rétention et investissements. Le rapport est écrit
/// dans BALANCE_REPORT.md (lancer explicitement : dotnet test --filter Name=WriteBalanceReport).
/// </summary>
public class BalanceReportTest
{
    public const int Months = 18;
    public static readonly ulong[] Seeds = [11, 23, 37, 41, 59];

    public record RunResult(
        string Strategy, ulong Seed, bool Liquidated, int DaysPlayed, decimal FinalCash, decimal MinCash, int MonthsInTrouble,
        decimal Income, decimal Expenses, int Contracts, int Cancelled, int Staff, int PeakStaff, double Users, double Paying,
        decimal Mrr, double Bugs, decimal Equipment, string Premises, decimal[] CashByQuarter, string Bottleneck);

    public static RunResult Play(Strategy strategy, ulong seed, int months = Months)
    {
        var campaign = Campaign.New("Bilan", "Bot", "Bot SA", FounderProfile.Generalist, seed);
        var pilot = new Autopilot(strategy);
        decimal minCash = campaign.Ledger.Cash;
        int trouble = 0, peakStaff = 0;
        var quarters = new List<decimal>();
        string bottleneck = "";
        for (int month = 1; month <= months && campaign.Status == CampaignStatus.Running; month++)
        {
            for (int day = 0; day < 30 && campaign.Status == CampaignStatus.Running; day++)
            {
                pilot.Play(campaign, 1);
                minCash = Math.Min(minCash, campaign.Ledger.Cash);
                peakStaff = Math.Max(peakStaff, campaign.Staff.Count());
            }
            if (campaign.Solvency.State is SolvencyState.Overdue or SolvencyState.Notice or SolvencyState.Liquidated)
                trouble++;
            if (month % 3 == 0)
                quarters.Add(Math.Round(campaign.Ledger.Cash));
            var blocked = campaign.People.Where(p => p.Status.StartsWith("Bloqué")).Select(p => p.Status).FirstOrDefault();
            if (blocked != null)
                bottleneck = blocked;
        }

        decimal income = 0, expenses = 0;
        foreach (var totals in campaign.Ledger.Months.Values)
        {
            income += totals.Where(t => t.Key is Category.Contracts or Category.Subscriptions or Category.Ads or Category.Commissions).Sum(t => t.Value);
            expenses += totals.Where(t => t.Value < 0 && t.Key is not (Category.Deposit or Category.LoanRepayment)).Sum(t => t.Value);
        }
        var product = pilot.ProductId is int id ? campaign.FindProduct(id) : null;
        return new RunResult(strategy.Name, seed, campaign.Status == CampaignStatus.Liquidated, (int)campaign.Day,
            Math.Round(campaign.Ledger.Cash), Math.Round(minCash), trouble, Math.Round(income), Math.Round(-expenses),
            campaign.Contracts.Count(c => c.Status == ContractStatus.Completed), campaign.Contracts.Count(c => c.Status == ContractStatus.Cancelled),
            campaign.Staff.Count(), peakStaff, Math.Round(product?.Users ?? 0), Math.Round(product?.Paying ?? 0),
            Math.Round(campaign.RecurringRevenue30), Math.Round(product?.Bugs ?? 0),
            campaign.Ledger.Months.Values.Sum(t => -t.GetValueOrDefault(Category.Equipment)), campaign.Premises.Name, quarters.ToArray(), bottleneck);
    }

    [Test, Explicit("Long : écrit BALANCE_REPORT.md")]
    public void WriteBalanceReport()
    {
        var results = Strategies.All.SelectMany(s => Seeds.Select(seed => Play(s, seed))).ToList();
        var md = new StringBuilder();
        md.AppendLine("# Bilan d'équilibrage mesuré");
        md.AppendLine();
        md.AppendLine("Généré par `Tests/balance/BalanceReportTest.cs` (`dotnet test Tests/Tests.csproj --filter Name=WriteBalanceReport`).");
        md.AppendLine($"Protocole : {Strategies.All.Count} stratégies × {Seeds.Length} graines ({string.Join(", ", Seeds)}), {Months} mois de jeu chacune, fondateur polyvalent, " +
                      "valeurs de `Core/campaign/Tuning.cs`. Les stratégies sont jouées par `Core/bots/Autopilot.cs`, une décision par jour ouvré, avec les mêmes commandes que l'interface.");
        md.AppendLine();
        md.AppendLine("## Résumé par stratégie");
        md.AppendLine();
        md.AppendLine("| Stratégie | Liquidations | Trésorerie finale (médiane) | Pire creux | Mois en impayés (moy.) | Recettes 18 mois (méd.) | Revenus récurrents finaux (méd.) | Équipe max | Contrats livrés / rompus |");
        md.AppendLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var group in results.GroupBy(r => r.Strategy))
        {
            var list = group.ToList();
            decimal Median(Func<RunResult, decimal> f) => list.Select(f).OrderBy(v => v).ElementAt(list.Count / 2);
            md.AppendLine($"| {group.Key} | {list.Count(r => r.Liquidated)}/{list.Count} | {Median(r => r.FinalCash):N0} | {list.Min(r => r.MinCash):N0} | " +
                          $"{list.Average(r => r.MonthsInTrouble):0.0} | {Median(r => r.Income):N0} | {Median(r => r.Mrr):N0} | {list.Max(r => r.PeakStaff)} | " +
                          $"{list.Sum(r => r.Contracts)} / {list.Sum(r => r.Cancelled)} |");
        }
        md.AppendLine();
        md.AppendLine("## Stratégies jouées");
        md.AppendLine();
        foreach (var s in Strategies.All)
            md.AppendLine($"- **{s.Name}** : {s.Description}");
        md.AppendLine();
        md.AppendLine("## Détail des parties");
        md.AppendLine();
        md.AppendLine("| Stratégie | Graine | Issue | Jours | Trésorerie par trimestre | Produit : utilisateurs / payants / bugs | Local final | Équipe finale | Matériel acheté |");
        md.AppendLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var r in results)
            md.AppendLine($"| {r.Strategy} | {r.Seed} | {(r.Liquidated ? "liquidée" : "en activité")} | {r.DaysPlayed} | {string.Join(" → ", r.CashByQuarter.Select(c => $"{c:N0}"))} | " +
                          $"{r.Users:N0} / {r.Paying:N0} / {r.Bugs:0} | {r.Premises} | {r.Staff} | {r.Equipment:N0} |");
        File.WriteAllText(Path.Combine(RootFolder(), "BALANCE_REPORT.md"), md.ToString());
        TestContext.Out.WriteLine(md.ToString());
    }

    public static string RootFolder()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "StartupRush.sln")))
            dir = dir.Parent;
        return dir!.FullName;
    }

    // ------------------------------------------------------------------ Garde-fous rapides (toujours lancés)

    [Test]
    public void APrudentFreelanceSurvivesItsFirstYear()
    {
        foreach (ulong seed in Seeds.Take(3))
        {
            var r = Play(Strategies.All.First(s => s.Name == "freelance"), seed, 12);
            Assert.That(r.Liquidated, Is.False, $"graine {seed}");
            Assert.That(r.FinalCash, Is.GreaterThan(7_000), $"graine {seed} : le freelance doit pouvoir s'en sortir");
        }
    }

    [Test]
    public void HiringAndSpendingWithoutRevenueLeadsToLiquidation()
    {
        int liquidated = Seeds.Take(3).Count(seed => Play(Strategies.All.First(s => s.Name == "croissance"), seed, 9).Liquidated);
        Assert.That(liquidated, Is.GreaterThanOrEqualTo(2), "une mauvaise gestion persistante doit pouvoir mener à la liquidation");
    }
}
