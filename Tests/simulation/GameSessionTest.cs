using Core.simulation;

namespace Tests.simulation;

[TestFixture]
public class GameSessionTest
{
    [Test]
    public void TenSimulatedSecondsAdvanceTheClock()
    {
        var session = new GameSession(new GameConfig { BaseOperatingCost = 0 });

        for (int i = 0; i < 100; i++)
            session.Update(0.1);

        Assert.That(session.ElapsedTime, Is.EqualTo(10).Within(1e-9));
    }

    [Test]
    public void BalanceDropsWithOperatingCosts()
    {
        var session = new GameSession(new GameConfig
        {
            StartingCapital = 1000, BaseOperatingCost = 10, OperatingCostGrowthPerMinute = 0
        });

        for (int i = 0; i < 100; i++)
            session.Update(0.1);

        Assert.That(session.Company.WalletBalance, Is.EqualTo(900).Within(1e-6));
    }
}
