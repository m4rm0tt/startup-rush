using Core.simulation;

namespace Tests.simulation;

/// <summary>
/// Joue plusieurs parties avec chaque stratégie et vérifie que l'économie n'est
/// ni impossible ni triviale. Le détail est écrit dans la sortie du test pour l'équilibrage.
/// </summary>
[TestFixture]
public class BalanceReportTest
{
    private const int Games = 20;

    private static List<GameSession> PlayMany(BotStrategy strategy) =>
        Enumerable.Range(0, Games)
            .Select(seed => GameBot.Play(new GameSession(new GameConfig(), new Random(seed)), strategy))
            .ToList();

    private static void Report(string name, List<GameSession> sessions)
    {
        var times = sessions.Select(s => s.ElapsedTime).OrderBy(t => t).ToList();
        TestContext.Out.WriteLine(
            $"{name,-13} survie min {times[0] / 60:F1} min | médiane {times[times.Count / 2] / 60:F1} min | " +
            $"max {times[^1] / 60:F1} min | tâches moy. {sessions.Average(s => s.Company.CompletedTaskCount):F1} | " +
            $"faillites {sessions.Count(s => s.IsOver)}/{sessions.Count}");
    }

    [Test]
    public void EconomyIsNeitherTrivialNorImpossible()
    {
        var idle = PlayMany(BotStrategy.Idle);
        var acceptAll = PlayMany(BotStrategy.AcceptAll);
        var onTime = PlayMany(BotStrategy.AcceptOnTime);

        Report("Idle", idle);
        Report("AcceptAll", acceptAll);
        Report("AcceptOnTime", onTime);

        // Sans rien faire, la faillite arrive vite
        Assert.That(idle.Max(s => s.ElapsedTime), Is.LessThan(5 * 60));

        // Un bon joueur tient une vraie partie, mais finit quand même par perdre
        double medianOnTime = onTime.Select(s => s.ElapsedTime).OrderBy(t => t).ElementAt(Games / 2);
        Assert.That(medianOnTime, Is.GreaterThan(8 * 60));
        Assert.That(onTime.All(s => s.IsOver), Is.True);
    }
}
