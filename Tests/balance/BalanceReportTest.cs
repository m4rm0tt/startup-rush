using System.Text;
using Core.bots;
using Core.campaign;
using Core.finance;

namespace Tests.balance;

/// <summary>
/// Campagne d'équilibrage : chaque stratégie est jouée sur plusieurs graines pendant 18 mois de jeu.
/// Le rapport est écrit dans BALANCE_REPORT.md (dotnet test --filter Name=WriteBalanceReport).
/// </summary>
public class BalanceReportTest
{
    public const int Months = 18;
    public static readonly ulong[] Seeds = [11, 23, 37, 41, 59];

    public record RunResult(
        string Strategy, ulong Seed, bool Liquidated, int DaysPlayed, decimal FinalCash, decimal MinCash, int MonthsInTrouble,
        decimal Income, int FirstLaunchDay, int FirstRevenueDay, int ProfitableMonthDay, int Staff, double Users, decimal Revenue30,
        int Points, int Unlocked, int Actions, int Words, string Premises, decimal[] CashByQuarter);

    public static RunResult Play(Strategy strategy, ulong seed, int months = Months)
    {
        var campaign = Campaign.New("Bilan", "Bot", "Bot SA", FounderProfile.Generalist, seed);
        var pilot = new Autopilot(strategy);
        decimal minCash = campaign.Ledger.Cash;
        int trouble = 0, firstLaunch = -1, firstRevenue = -1, profitable = -1;
        var quarters = new List<decimal>();
        for (int month = 1; month <= months && campaign.Status == CampaignStatus.Running; month++)
        {
            for (int day = 0; day < 30 && campaign.Status == CampaignStatus.Running; day++)
            {
                pilot.Play(campaign, 1);
                minCash = Math.Min(minCash, campaign.Ledger.Cash);
                if (firstLaunch < 0 && campaign.Products.Any(p => p.IsLive))
                    firstLaunch = (int)campaign.Day;
                if (firstRevenue < 0 && campaign.RecurringRevenue30 > 0)
                    firstRevenue = (int)campaign.Day;
            }
            string key = GameTime.ToDate(campaign.Minute).AddMonths(-1).ToString("yyyy-MM");
            if (profitable < 0 && campaign.Ledger.Months.TryGetValue(key, out var totals) && Campaign.OperatingResult(totals) > 0)
                profitable = (int)campaign.Day;
            if (campaign.Solvency.State is SolvencyState.Overdue or SolvencyState.Notice or SolvencyState.Liquidated)
                trouble++;
            if (month % 3 == 0)
                quarters.Add(Math.Round(campaign.Ledger.Cash));
        }

        decimal income = campaign.Ledger.Months.Values.Sum(t => t.Where(x => x.Key is Category.Subscriptions or Category.Ads or Category.Commissions or Category.Sales).Sum(x => x.Value));
        int spent = campaign.Unlocked.Sum(id => Core.progress.DevTree.Node(id).Cost);
        return new RunResult(strategy.Name, seed, campaign.Status == CampaignStatus.Liquidated, (int)campaign.Day,
            Math.Round(campaign.Ledger.Cash), Math.Round(minCash), trouble, Math.Round(income), firstLaunch, firstRevenue, profitable,
            campaign.Staff.Count(), Math.Round(campaign.Products.Sum(p => p.Users)), Math.Round(campaign.RecurringRevenue30),
            spent + campaign.DevPoints, campaign.Unlocked.Count, pilot.Actions, pilot.Words, campaign.Premises.Name, quarters.ToArray());
    }

    [Test, Explicit("Long : écrit BALANCE_REPORT.md")]
    public void WriteBalanceReport()
    {
        var results = Strategies.All.SelectMany(s => Seeds.Select(seed => Play(s, seed))).ToList();
        string Med<T>(IEnumerable<T> values, Func<T, string> format) where T : IComparable<T>
        {
            var list = values.OrderBy(v => v).ToList();
            return list.Count == 0 ? "—" : format(list[list.Count / 2]);
        }

        var md = new StringBuilder();
        md.AppendLine("# Bilan d'équilibrage mesuré");
        md.AppendLine();
        md.AppendLine("Généré par `Tests/balance/BalanceReportTest.cs` (`dotnet test Tests/Tests.csproj --filter Name=WriteBalanceReport`).");
        md.AppendLine($"Protocole : {Strategies.All.Count} façons de jouer × {Seeds.Length} graines ({string.Join(", ", Seeds)}), {Months} mois de jeu, fondateur polyvalent, " +
                      "réglages de `Core/campaign/Tuning.cs`. Joué par `Core/bots/Autopilot.cs` : une décision par jour ouvré, avec les commandes de l'interface. " +
                      "Aucune prestation client n'existe dans cette version.");
        md.AppendLine();
        md.AppendLine("## Résumé");
        md.AppendLine();
        md.AppendLine("| Stratégie | Faillites | Argent final (médiane) | Pire creux | 1re sortie (jour, méd.) | 1er mois bénéficiaire (jour, méd.) | Revenus des projets / mois (méd.) | Utilisateurs (méd.) | Points gagnés (méd.) | Décisions sur 18 mois (méd.) | Mots tapés (méd.) |");
        md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var group in results.GroupBy(r => r.Strategy))
        {
            var list = group.ToList();
            md.AppendLine($"| {group.Key} | {list.Count(r => r.Liquidated)}/{list.Count} | {Med(list.Select(r => r.FinalCash), v => v.ToString("N0"))} | {list.Min(r => r.MinCash):N0} | " +
                          $"{Med(list.Where(r => r.FirstLaunchDay >= 0).Select(r => r.FirstLaunchDay), v => v.ToString())} | " +
                          $"{Med(list.Where(r => r.ProfitableMonthDay >= 0).Select(r => r.ProfitableMonthDay), v => v.ToString())} ({list.Count(r => r.ProfitableMonthDay >= 0)}/{list.Count}) | " +
                          $"{Med(list.Select(r => r.Revenue30), v => v.ToString("N0"))} | {Med(list.Select(r => r.Users), v => v.ToString("N0"))} | " +
                          $"{Med(list.Select(r => r.Points), v => v.ToString())} | {Med(list.Select(r => r.Actions), v => v.ToString())} | " +
                          $"{Med(list.Select(r => r.Words), v => v.ToString("N0"))} |");
        }
        md.AppendLine();
        md.AppendLine("## Façons de jouer");
        md.AppendLine();
        foreach (var s in Strategies.All)
            md.AppendLine($"- **{s.Name}** : {s.Description}");
        md.AppendLine();
        md.AppendLine("## Détail");
        md.AppendLine();
        md.AppendLine("| Stratégie | Graine | Issue | Jours | Argent par trimestre | Équipe | Local | Nœuds débloqués |");
        md.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var r in results)
            md.AppendLine($"| {r.Strategy} | {r.Seed} | {(r.Liquidated ? "faillite" : "en activité")} | {r.DaysPlayed} | {string.Join(" → ", r.CashByQuarter.Select(c => $"{c:N0}"))} | {r.Staff} | {r.Premises} | {r.Unlocked} |");
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
    public void AFounderAloneShipsAndEarnsWithoutClientWork()
    {
        foreach (ulong seed in Seeds.Take(3))
        {
            var r = Play(Strategies.All.First(s => s.Name == "un_projet"), seed, 9);
            Assert.That(r.Liquidated, Is.False, $"graine {seed}");
            Assert.That(r.FirstLaunchDay, Is.InRange(10, 60), $"graine {seed} : première sortie");
            Assert.That(r.FirstRevenueDay, Is.GreaterThan(0).And.LessThan(90), $"graine {seed} : premiers revenus");
        }
    }

    [Test]
    public void HiringBeforeAnyRevenueLeadsToBankruptcy()
    {
        int liquidated = Seeds.Take(3).Count(seed => Play(Strategies.All.First(s => s.Name == "recrutement_precoce"), seed, 9).Liquidated);
        Assert.That(liquidated, Is.GreaterThanOrEqualTo(2));
    }
}
