using Core.bots;
using Core.campaign;

namespace Tests.balance;

/// <summary>
/// Étape 3 de la reprise Steam : combien de temps de jeu simulé faut-il à un bot pour atteindre l'objectif final
/// (<see cref="Campaign.FinalGoalValuation"/>), avec plusieurs parcours et plusieurs graines ? Mesure le temps SIMULÉ (jours de
/// jeu), le temps de CALCUL et le nombre de décisions du bot ; la durée réelle de jeu n'est qu'une estimation tirée de ces
/// mesures et d'hypothèses de vitesse (voir docs/equilibrage/objectif_final.md). Lancée à la main :
///   GOAL_PROBE_OUT=docs/equilibrage GOAL_PROBE_TAG=avant dotnet test Tests/Tests.csproj --filter Name=GoalTime
/// Variables : GOAL_PROBE_MONTHS (défaut 72), GOAL_PROBE_PATHS (liste séparée par des virgules), GOAL_PROBE_SEEDS.
/// </summary>
public class GoalTimeProbe
{
    public static readonly string[] DefaultPaths =
    [
        "un_projet", "plusieurs_petits", "ambitieux", "studio_xr", "materiel", "materiel_pc", "ia", "ia_salle_serveur",
        "quantique", "exces_puis_correction",
    ];

    public record Run(string Strategy, ulong Seed, int GoalDay, bool Liquidated, int Days, decimal FinalCash, decimal FinalValuation,
        int Actions, long Words, double Seconds, decimal[] ValuationByHalfYear);

    [Test, Explicit("Long : mesure le délai de l'objectif final")]
    public void GoalTime()
    {
        int months = int.Parse(Environment.GetEnvironmentVariable("GOAL_PROBE_MONTHS") ?? "72");
        var paths = (Environment.GetEnvironmentVariable("GOAL_PROBE_PATHS") ?? string.Join(",", DefaultPaths)).Split(',', StringSplitOptions.RemoveEmptyEntries);
        var seeds = (Environment.GetEnvironmentVariable("GOAL_PROBE_SEEDS") ?? "11,23,37").Split(',').Select(ulong.Parse).ToArray();
        var runs = paths.SelectMany(p => seeds.Select(s => (p, s))).AsParallel().AsOrdered().Select(x => Play(Strategies.All.First(st => st.Name == x.p), x.s, months)).ToList();

        var md = new System.Text.StringBuilder();
        md.AppendLine($"Objectif mesuré : valorisation ≥ {Campaign.FinalGoalValuation:N0} CHF, sans impayés, un produit en ligne (`Campaign.FinalGoalMet`). Graines {string.Join(", ", seeds)}, jusqu'à {months} mois de jeu simulé.");
        md.AppendLine();
        md.AppendLine("| Parcours | Atteint | Jour de jeu (par graine) | Médiane (jours / mois) | Faillites | Valorisation à 6 / 12 / 24 / 36 / 48 / 72 mois (médiane) | Décisions du bot (méd.) | Calcul (s, méd.) |");
        md.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var g in runs.GroupBy(r => r.Strategy))
        {
            var l = g.ToList();
            var days = l.Where(r => r.GoalDay >= 0).Select(r => r.GoalDay).OrderBy(d => d).ToList();
            string median = days.Count == 0 ? "jamais" : $"{days[days.Count / 2]} j / {days[days.Count / 2] / 30.0:0.0} mois";
            string per = string.Join(" ; ", l.Select(r => r.GoalDay >= 0 ? $"{r.Seed}: {r.GoalDay}" : $"{r.Seed}: —"));
            string val = string.Join(" / ", new[] { 1, 2, 4, 6, 8, 12 }.Select(h => Med(l.Select(r => h <= r.ValuationByHalfYear.Length ? r.ValuationByHalfYear[h - 1] : (decimal?)null))));
            md.AppendLine($"| {g.Key} | {l.Count(r => r.GoalDay >= 0)}/{l.Count} | {per} | {median} | {l.Count(r => r.Liquidated)}/{l.Count} | {val} | " +
                          $"{l.OrderBy(r => r.Actions).ElementAt(l.Count / 2).Actions} | {l.OrderBy(r => r.Seconds).ElementAt(l.Count / 2).Seconds:0} |");
        }
        string folder = Environment.GetEnvironmentVariable("GOAL_PROBE_OUT") ?? Path.GetTempPath();
        Directory.CreateDirectory(folder);
        string tag = Environment.GetEnvironmentVariable("GOAL_PROBE_TAG") ?? "run";
        File.WriteAllText(Path.Combine(folder, $"objectif_{tag}.md"), md.ToString());
        var csv = new System.Text.StringBuilder("strategie,graine,jour_objectif,faillite,jours,argent_final,valorisation_finale,decisions,mots,secondes_calcul\n");
        foreach (var r in runs)
            csv.AppendLine(string.Join(",", r.Strategy, r.Seed, r.GoalDay, r.Liquidated ? 1 : 0, r.Days, r.FinalCash, r.FinalValuation, r.Actions, r.Words, r.Seconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)));
        File.WriteAllText(Path.Combine(folder, $"objectif_{tag}.csv"), csv.ToString());
        TestContext.Out.WriteLine(md.ToString());
    }

    public static Run Play(Strategy strategy, ulong seed, int months)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var campaign = Campaign.New("Objectif", "Bot", "Bot SA", FounderProfile.Generalist, seed);
        var pilot = new Autopilot(strategy);
        int goalDay = -1, day = 0;
        var halves = new List<decimal>();
        for (; day < months * 30 && campaign.Status == CampaignStatus.Running; day++)
        {
            pilot.Play(campaign, 1);
            if (goalDay < 0 && campaign.FinalGoalMet)
                goalDay = (int)campaign.Day;
            if ((day + 1) % 180 == 0)
                halves.Add(campaign.Valuation());
        }
        return new Run(strategy.Name, seed, goalDay, campaign.Status == CampaignStatus.Liquidated, day, campaign.Ledger.Cash, campaign.Valuation(),
            pilot.Actions, pilot.Words, watch.Elapsed.TotalSeconds, halves.ToArray());
    }

    private static string Med(IEnumerable<decimal?> values)
    {
        var list = values.Where(v => v != null).Select(v => v!.Value).OrderBy(x => x).ToList();
        return list.Count == 0 ? "—" : $"{list[list.Count / 2]:N0}";
    }
}
