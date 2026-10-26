using Core.progression;
using Core.simulation;

namespace Tests.simulation;

/// <summary>Tests d'intégration : des parties complètes jouées sans interface.</summary>
[TestFixture]
public class FullGameTest
{
    private static GameSession NewSession(int seed) => new(new GameConfig(), new Random(seed));

    [Test]
    public void IdleGameEndsInBankruptcy()
    {
        var session = GameBot.Play(NewSession(1), BotStrategy.Idle);

        Assert.That(session.IsOver, Is.True);
        Assert.That(session.Result!.IsVictory, Is.False);
        Assert.That(session.Result.SurvivedTime, Is.LessThanOrEqualTo(3 * session.Config.MonthDuration));
        Assert.That(session.Result.CompletedTasks, Is.EqualTo(0));
    }

    [Test]
    public void GameOverIsRaisedOnceAndSessionStops()
    {
        var session = NewSession(1);
        int gameOverCount = 0;
        session.GameOver += _ => gameOverCount++;

        GameBot.Play(session, BotStrategy.Idle);
        double endTime = session.ElapsedTime;
        session.Update(0.1);

        Assert.That(gameOverCount, Is.EqualTo(1));
        Assert.That(session.ElapsedTime, Is.EqualTo(endTime));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void HardWorkPaysTheRentAtStart(int seed)
    {
        var session = GameBot.Play(NewSession(seed), BotStrategy.TasksOnly, maxTime: 10 * 60);

        Assert.That(session.IsOver, Is.False);
        Assert.That(session.Company.CompletedTaskCount, Is.GreaterThan(20));
    }

    [TestCase(1)]
    [TestCase(2)]
    public void WithoutInvestingYouNeverBecomeKing(int seed)
    {
        var session = GameBot.Play(NewSession(seed), BotStrategy.TasksOnly, maxTime: 2 * 3600);

        Assert.That(session.Career.Stage, Is.LessThan(CareerStage.Boss));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void GoodPlayerBecomesKingOfTheWorld(int seed)
    {
        var session = GameBot.Play(NewSession(seed), BotStrategy.Tycoon);

        Assert.That(session.IsOver, Is.True);
        Assert.That(session.Result!.IsVictory, Is.True);
    }
}
