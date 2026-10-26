using Core.progression;
using Core.simulation;

namespace Tests.simulation;

/// <summary>
/// Joue plusieurs parties avec le bot « Tycoon » et vérifie que l'histoire dure
/// une vraie partie, ni trop courte ni interminable. Le détail est écrit dans la sortie du test.
/// </summary>
[TestFixture]
public class BalanceReportTest
{
    private const int Games = 10;

    [Test]
    public void StoryTakesAProperGameToComplete()
    {
        var bots = new List<(GameBot Bot, GameSession Session)>();
        for (int seed = 0; seed < Games; seed++)
        {
            var bot = new GameBot(BotStrategy.Tycoon);
            bots.Add((bot, bot.Run(new GameSession(new GameConfig(), new Random(seed)))));
        }

        foreach (var stage in Enum.GetValues<CareerStage>().Skip(1))
        {
            var times = bots.Where(b => b.Bot.StageTimes.ContainsKey(stage))
                .Select(b => b.Bot.StageTimes[stage] / 60).OrderBy(t => t).ToList();
            string detail = times.Count == 0
                ? "jamais atteint"
                : $"min {times[0]:F1} | médiane {times[times.Count / 2]:F1} | max {times[^1]:F1} min";
            TestContext.Out.WriteLine($"{stage,-15} {times.Count,2}/{Games}  {detail}");
        }

        int victories = bots.Count(b => b.Session.Result?.IsVictory == true);
        var victoryTimes = bots.Where(b => b.Session.Result?.IsVictory == true)
            .Select(b => b.Session.ElapsedTime / 60).OrderBy(t => t).ToList();

        Assert.That(victories, Is.GreaterThanOrEqualTo(Games - 1), "un bon joueur doit pouvoir gagner");
        Assert.That(victoryTimes[victoryTimes.Count / 2], Is.InRange(40, 120), "durée médiane d'une partie gagnée (min)");
    }

    /// <summary>Déroulé minute par minute d'une partie du bot, pour comprendre où l'équilibrage coince.</summary>
    [Test, Explicit("Outil d'équilibrage, à lancer à la main")]
    public void TraceOneGame()
    {
        var session = new GameSession(new GameConfig(), new Random(0));
        var bot = new GameBot(BotStrategy.Tycoon);

        for (int minute = 5; minute <= 180 && !session.IsOver; minute += 5)
        {
            bot.Run(session, minute * 60);
            var company = session.Company;
            TestContext.Out.WriteLine(
                $"{minute,3} min | patrimoine {session.NetWorth,15:N0} | {session.Career.Current.Title,-12} | " +
                $"réputation {company.Reputation.Current.Title,-20} | vitesse ×{session.Stats.WorkSpeed,6:F1} | " +
                $"employés {session.Staff.TotalCount,3} | factures {session.CurrentBill.Total,11:N0}/mois | " +
                $"boîtes {session.Businesses.IncomePerSecond,11:N0}/s [{string.Join(",", session.Businesses.Businesses.Select(b => b.Level))}] | " +
                $"matériel [{string.Join(",", session.Upgrades.Tracks.Select(t => t.Level))}]");
        }
    }
}
