using System.Collections.Concurrent;
using System.Text;
using Core.progression;
using Core.roguelike;
using Core.simulation;

namespace Tests.simulation;

/// <summary>
/// Campagne d'équilibrage reproductible : plusieurs façons de jouer, graines fixes, pression 0 et 3,
/// sans Héritage (débutant) et avec tout l'Héritage (joueur avancé). Le tableau sort dans la console
/// et dans le fichier indiqué par la variable BALANCE_OUT (par défaut dans le dossier temporaire).
/// </summary>
[TestFixture]
public class BalanceCampaignTest
{
    private const int Seeds = 20;

    private static readonly LegacyBonuses FullLegacy = new(
        ExtraStartingCash: 1_000, StartingReputation: 60, ExtraDraftChoices: 1, FreeRerolls: 2, StartingShields: 1,
        LegendaryPerksUnlocked: true);

    private sealed record Run(BotPolicy Policy, int Pressure, bool Legacy, int Seed, GameResult? Result, double Minutes,
        Dictionary<CareerStage, double> Stages);

    [Test, Explicit("Campagne d'équilibrage (quelques minutes), à lancer à la main")]
    public void PlaystylesAcrossPressures()
    {
        var configs = new List<(BotPolicy Policy, int Pressure, bool Legacy)>();
        foreach (var policy in BotPolicy.Playstyles)
            foreach (int pressure in new[] { 0, 3 })
                configs.Add((policy, pressure, false));
        configs.Add((BotPolicy.Mixed, 0, true));
        configs.Add((BotPolicy.Mixed, 3, true));
        configs.Add((BotPolicy.TasksOnly, 0, false));

        var runs = new ConcurrentBag<Run>();
        var jobs = configs.SelectMany(c => Enumerable.Range(0, Seeds).Select(seed => (c, seed))).ToList();
        Parallel.ForEach(jobs, job =>
        {
            var (config, seed) = job;
            var setup = new RunSetup(CharacterCatalog.All[0], config.Pressure, seed,
                config.Legacy ? FullLegacy : new LegacyBonuses());
            var bot = new GameBot(config.Policy);
            var session = bot.Run(new GameSession(setup: setup), maxTime: 2 * 3600);
            runs.Add(new Run(config.Policy, config.Pressure, config.Legacy, seed, session.Result, session.ElapsedTime / 60,
                new Dictionary<CareerStage, double>(bot.StageTimes)));
        });

        var report = new StringBuilder();
        report.AppendLine($"Alex, {Seeds} graines (0 à {Seeds - 1}), 2 h de jeu au plus par run.");
        report.AppendLine();
        report.AppendLine("| Style | Pression | Héritage | Victoires | Durée médiane (min) | Min–max | Freelance | Fondateur | Patron | Investisseur | Magnat | Roi | Défaites |");
        report.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");

        foreach (var config in configs)
        {
            var group = runs.Where(r => r.Policy == config.Policy && r.Pressure == config.Pressure && r.Legacy == config.Legacy)
                .OrderBy(r => r.Seed).ToList();
            var wins = group.Where(r => r.Result?.IsVictory == true).Select(r => r.Minutes).OrderBy(m => m).ToList();
            string duration = wins.Count > 0 ? $"{Median(wins):F0}" : "–";
            string range = wins.Count > 0 ? $"{wins[0]:F0}–{wins[^1]:F0}" : "–";

            var stages = new[] { CareerStage.Freelancer, CareerStage.Founder, CareerStage.Boss, CareerStage.Investor,
                CareerStage.Tycoon, CareerStage.KingOfTheWorld }
                .Select(stage =>
                {
                    var times = group.Where(r => r.Stages.ContainsKey(stage)).Select(r => r.Stages[stage] / 60).ToList();
                    return times.Count == 0 ? "–" : times.Count == group.Count ? $"{Median(times):F0}" : $"{Median(times):F0} ({times.Count})";
                });

            var losses = group.Where(r => r.Result?.IsVictory != true)
                .GroupBy(r => r.Result == null ? "2 h sans finir" : Shorten(r.Result.CauseOfDeath))
                .Select(g => $"{g.Count()} {g.Key}");

            report.AppendLine($"| {config.Policy.Name} | {config.Pressure} | {(config.Legacy ? "complet" : "aucun")} | " +
                              $"{wins.Count}/{group.Count} | {duration} | {range} | {string.Join(" | ", stages)} | " +
                              $"{string.Join(", ", losses)} |");
        }

        report.AppendLine();
        report.AppendLine("Statuts : minute médiane d'arrivée (entre parenthèses, le nombre de runs qui l'atteignent quand ce ne sont pas toutes).");

        string text = report.ToString();
        TestContext.Out.WriteLine(text);
        string path = Environment.GetEnvironmentVariable("BALANCE_OUT") ?? Path.Combine(Path.GetTempPath(), "startuprush-balance.md");
        File.WriteAllText(path, text);
        TestContext.Out.WriteLine($"Rapport écrit dans {path}");
    }

    private static double Median(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        return sorted[sorted.Count / 2];
    }

    private static string Shorten(string? cause) =>
        cause == null ? "abandon" : cause.Length > 40 ? cause[..40] + "…" : cause;
}
