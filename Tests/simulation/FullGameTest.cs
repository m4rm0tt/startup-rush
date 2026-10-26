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
        Assert.That(session.Company.WalletBalance, Is.LessThan(0));
        Assert.That(session.Result, Is.Not.Null);
        Assert.That(session.Result!.SurvivedTime, Is.EqualTo(session.ElapsedTime));
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
    public void AcceptingGoodRequestsSurvivesLongerThanDoingNothing(int seed)
    {
        var idle = GameBot.Play(NewSession(seed), BotStrategy.Idle);
        var player = GameBot.Play(NewSession(seed), BotStrategy.AcceptOnTime);

        Assert.That(player.ElapsedTime, Is.GreaterThan(idle.ElapsedTime));
        Assert.That(player.Company.CompletedTaskCount, Is.GreaterThan(0));
    }

    [Test]
    public void ScoreCombinesSurvivedTimeAndEarnings()
    {
        var result = new GameResult(SurvivedTime: 120, FinalBalance: -1, TotalEarned: 500, CompletedTasks: 3, FailedTasks: 0);
        Assert.That(result.Score, Is.EqualTo(170));
    }
}
