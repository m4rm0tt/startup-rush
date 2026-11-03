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
    public const int LongMonths = 36;

    /// <summary>Le quantique est une fin de campagne longue : il est aussi suivi sur 60 mois.</summary>
    public const int QuantumMonths = 60;
    public static readonly ulong[] Seeds = [11, 23, 37, 41, 59];

    /// <summary>
    /// Cinq graines ne suffisent pas à mesurer un taux de faillite ni une dispersion : les stratégies de référence et les erreurs
    /// sont aussi jouées sur 20 graines (les 5 du bilan et 15 de plus).
    /// </summary>
    public static readonly ulong[] WideSeeds = [.. Seeds, .. Enumerable.Range(0, 15).Select(i => (ulong)(1_000 + 37 * i))];

    public static readonly string[] WideStrategies =
        ["un_projet", "plusieurs_petits", "jeu_simple", "delegation", "marketing_excessif", "exces_puis_correction", "materiel", "materiel_pc", "ia", "studio_xr", "hebergement_maison"];

    /// <summary>Stratégies rejouées sur 36 mois (diversifications et leur base).</summary>
    public static readonly string[] LongStrategies =
        ["plusieurs_petits", "hebergement_maison", "materiel", "materiel_industrie", "materiel_pc", "materiel_serveur", "materiel_gpu", "ia", "ia_salle_serveur", "studio_xr", "quantique"];

    public record RunResult(
        string Strategy, ulong Seed, bool Liquidated, int DaysPlayed, decimal FinalCash, decimal MinCash, int MonthsInTrouble,
        decimal Income, int FirstLaunchDay, int FirstRevenueDay, int ProfitableMonthDay, int Staff, double Users, decimal Revenue30,
        int Points, int Unlocked, int Actions, int Words, string Premises, decimal[] CashByQuarter, IReadOnlyDictionary<string, int> Milestones);

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
            spent + campaign.DevPoints, campaign.Unlocked.Count, pilot.Actions, pilot.Words, campaign.Premises.Name, quarters.ToArray(), pilot.Milestones);
    }

    [Test, Explicit("Long : écrit BALANCE_REPORT.md")]
    public void WriteBalanceReport()
    {
        // Nombres au format français quelle que soit la langue de la machine (sinon 87,760 sur un serveur en anglais)
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
        // Chaque partie est indépendante (son propre hasard) : jouées en parallèle, dans l'ordre du rapport
        var results = Strategies.All.SelectMany(s => Seeds.Select(seed => (s, seed))).AsParallel().AsOrdered().Select(x => Play(x.s, x.seed)).ToList();
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
        md.AppendLine("| Stratégie | Faillites | Argent final (médiane) | Argent final (min – max) | Pire creux | 1re sortie (jour, méd.) | 1er mois bénéficiaire (jour, méd.) | Revenus des projets / mois (méd.) | Utilisateurs (méd.) | Points gagnés (méd.) | Décisions sur 18 mois (méd.) | Mots tapés (méd.) |");
        md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var group in results.GroupBy(r => r.Strategy))
        {
            var list = group.ToList();
            md.AppendLine($"| {group.Key} | {list.Count(r => r.Liquidated)}/{list.Count} | {Med(list.Select(r => r.FinalCash), v => v.ToString("N0"))} | {list.Min(r => r.FinalCash):N0} – {list.Max(r => r.FinalCash):N0} | {list.Min(r => r.MinCash):N0} | " +
                          $"{Med(list.Where(r => r.FirstLaunchDay >= 0).Select(r => r.FirstLaunchDay), v => v.ToString())} | " +
                          $"{Med(list.Where(r => r.ProfitableMonthDay >= 0).Select(r => r.ProfitableMonthDay), v => v.ToString())} ({list.Count(r => r.ProfitableMonthDay >= 0)}/{list.Count}) | " +
                          $"{Med(list.Select(r => r.Revenue30), v => v.ToString("N0"))} | {Med(list.Select(r => r.Users), v => v.ToString("N0"))} | " +
                          $"{Med(list.Select(r => r.Points), v => v.ToString())} | {Med(list.Select(r => r.Actions), v => v.ToString())} | " +
                          $"{Med(list.Select(r => r.Words), v => v.ToString("N0"))} |");
        }
        md.AppendLine();
        var wide = WideStrategies.SelectMany(name => WideSeeds.Select(seed => (Strategies.All.First(st => st.Name == name), seed)))
            .AsParallel().AsOrdered().Select(x => Play(x.Item1, x.seed)).ToList();
        md.AppendLine($"## Dispersion sur {WideSeeds.Length} graines ({Months} mois)");
        md.AppendLine();
        md.AppendLine("Argent final : 10 % des parties finissent sous P10, 10 % au-dessus de P90. Une faillite compte avec son solde au jour de la liquidation.");
        md.AppendLine();
        md.AppendLine("| Stratégie | Faillites | P10 | Médiane | P90 | Revenus / mois (méd.) |");
        md.AppendLine("|---|---|---|---|---|---|");
        foreach (var group in wide.GroupBy(r => r.Strategy))
        {
            var cash = group.Select(r => r.FinalCash).OrderBy(v => v).ToList();
            decimal At(double q) => cash[(int)Math.Clamp(Math.Round(q * (cash.Count - 1)), 0, cash.Count - 1)];
            md.AppendLine($"| {group.Key} | {group.Count(r => r.Liquidated)}/{cash.Count} | {At(0.1):N0} | {At(0.5):N0} | {At(0.9):N0} | {Med(group.Select(r => r.Revenue30), v => v.ToString("N0"))} |");
        }
        md.AppendLine();
        md.AppendLine("## Façons de jouer");
        md.AppendLine();
        foreach (var s in Strategies.All)
            md.AppendLine($"- **{s.Name}** : {s.Description}");
        md.AppendLine();
        // Lot 6 : jusqu'où chaque stratégie va (IA publiée, studio et zone XR complets, quantique en service)
        md.AppendLine("## Jalons du lot 6");
        md.AppendLine();
        md.AppendLine("Jour où l'étape est atteinte pour la première fois (médiane des graines qui l'atteignent). Mini-jeux d'évaluation et de mise en route joués à 75 %.");
        md.AppendLine();
        md.AppendLine("| Stratégie | Jalon | Atteint | Jour (méd.) |");
        md.AppendLine("|---|---|---|---|");
        void Milestones(IEnumerable<RunResult> runs, string label)
        {
            var list = runs.ToList();
            foreach (string key in list.SelectMany(r => r.Milestones.Keys).Distinct())
                md.AppendLine($"| {label} | {key} | {list.Count(r => r.Milestones.ContainsKey(key))}/{list.Count} | " +
                              $"{Med(list.Where(r => r.Milestones.ContainsKey(key)).Select(r => r.Milestones[key]), v => v.ToString())} |");
        }
        foreach (var group in results.GroupBy(r => r.Strategy).Where(g => g.Any(r => r.Milestones.Count > 0)))
            Milestones(group, group.Key);
        md.AppendLine();
        // Le quantique est une fin de progression : 18 mois ne suffisent pas, on regarde aussi sur un horizon plus long
        // Les diversifications (salle serveur, matériel, IA, studio, quantique) sont des investissements de milieu de partie :
        // on les compare à leur base (plusieurs_petits) sur 36 mois aussi
        var longAll = LongStrategies.SelectMany(name => Seeds.Select(seed => (Strategies.All.First(s => s.Name == name), seed)))
            .AsParallel().AsOrdered().Select(x => Play(x.Item1, x.seed, LongMonths)).ToList();
        md.AppendLine($"## Horizon long : {LongMonths} mois");
        md.AppendLine();
        md.AppendLine("| Stratégie | Faillites | Argent final (médiane) | Argent final (min – max) | Écart à plusieurs_petits (médiane) | Revenus / mois (méd.) | Jalons atteints |");
        md.AppendLine("|---|---|---|---|---|---|---|");
        decimal baseLong = longAll.Where(r => r.Strategy == "plusieurs_petits").Select(r => r.FinalCash).OrderBy(v => v).ElementAt(Seeds.Length / 2);
        foreach (var group in longAll.GroupBy(r => r.Strategy))
        {
            var list = group.ToList();
            decimal median = list.Select(r => r.FinalCash).OrderBy(v => v).ElementAt(list.Count / 2);
            string reached = string.Join(", ", list.SelectMany(r => r.Milestones.Keys).Distinct().Select(k => $"{k} {list.Count(r => r.Milestones.ContainsKey(k))}/{list.Count}"));
            md.AppendLine($"| {group.Key} | {list.Count(r => r.Liquidated)}/{list.Count} | {median:N0} | {list.Min(r => r.FinalCash):N0} – {list.Max(r => r.FinalCash):N0} | " +
                          $"{median - baseLong:+#,0;-#,0;0} | {Med(list.Select(r => r.Revenue30), v => v.ToString("N0"))} | {(reached == "" ? "—" : reached)} |");
        }
        md.AppendLine();
        var longRuns = Seeds.AsParallel().AsOrdered().Select(seed => Play(Strategies.All.First(s => s.Name == "quantique"), seed, QuantumMonths)).ToList();
        md.AppendLine($"### Quantique sur {QuantumMonths} mois, graine par graine");
        md.AppendLine();
        md.AppendLine("| Graine | Issue | Argent par trimestre | Nœuds | Jalons |");
        md.AppendLine("|---|---|---|---|---|");
        foreach (var r in longRuns)
            md.AppendLine($"| {r.Seed} | {(r.Liquidated ? "faillite" : "en activité")} | {string.Join(" → ", r.CashByQuarter.Select(c => $"{c:N0}"))} | {r.Unlocked} | " +
                          $"{(r.Milestones.Count == 0 ? "aucun" : string.Join(", ", r.Milestones.Select(m => $"{m.Key} (j{m.Value})")))} |");
        md.AppendLine();
        md.AppendLine("## Détail");
        md.AppendLine();
        md.AppendLine("| Stratégie | Graine | Issue | Jours | Argent par trimestre | Équipe | Local | Nœuds débloqués |");
        md.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var r in results)
            md.AppendLine($"| {r.Strategy} | {r.Seed} | {(r.Liquidated ? "faillite" : "en activité")} | {r.DaysPlayed} | {string.Join(" → ", r.CashByQuarter.Select(c => $"{c:N0}"))} | {r.Staff} | {r.Premises} | {r.Unlocked} |");
        File.WriteAllText(Path.Combine(RootFolder(), "BALANCE_REPORT.md"), md.ToString());
        // Une passe nommée (BALANCE_LABEL) garde toutes ses parties en CSV pour les comparaisons avant/après (docs/equilibrage/)
        if (Environment.GetEnvironmentVariable("BALANCE_LABEL") is { Length: > 0 } label)
        {
            var csv = new StringBuilder("strategie,graine,mois,faillite,argent_final,pire_creux,revenus_30j,utilisateurs,equipe,noeuds,jalons\n");
            foreach (var r in results.Concat(longAll.Select(r => r with { Strategy = r.Strategy + "_36" })).Concat(longRuns.Select(r => r with { Strategy = r.Strategy + "_48" }))
                         .Concat(wide.Where(r => !Seeds.Contains(r.Seed)).Select(r => r with { Strategy = r.Strategy + "_w20" })))
                csv.AppendLine(string.Join(",", r.Strategy, r.Seed, r.Strategy.EndsWith("_48") ? QuantumMonths : r.Strategy.EndsWith("_36") ? LongMonths : Months, r.Liquidated ? 1 : 0,
                    r.FinalCash, r.MinCash, r.Revenue30, r.Users, r.Staff, r.Unlocked,
                    string.Join(";", r.Milestones.Select(m => $"{m.Key}@{m.Value}"))));
            File.WriteAllText(Path.Combine(RootFolder(), "docs", "equilibrage", $"runs_{label}.csv"), csv.ToString());
            File.Copy(Path.Combine(RootFolder(), "BALANCE_REPORT.md"), Path.Combine(RootFolder(), "docs", "equilibrage", $"bilan_{label}.md"), true);
        }
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
            // Le premier projet est un prototype (demande des testeurs : début trop lent) : il sort en une à trois semaines
            Assert.That(r.FirstLaunchDay, Is.InRange(5, 30), $"graine {seed} : première sortie");
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
