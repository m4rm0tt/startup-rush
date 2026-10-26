using Core.progression;
using Core.staff;
using Core.upgrades;
using Core.simulation;

namespace Tests.simulation;

[TestFixture]
public class GameSessionTest
{
    private static void Run(GameSession session, double seconds)
    {
        for (int i = 0; i < seconds * 10; i++)
            session.Update(0.1);
    }

    [Test]
    public void TenSimulatedSecondsAdvanceTheClock()
    {
        var session = new GameSession(new GameConfig(), new Random(0));

        Run(session, 10);

        Assert.That(session.ElapsedTime, Is.EqualTo(10).Within(1e-9));
    }

    [Test]
    public void BillIsPaidAtEndOfMonth()
    {
        var session = new GameSession(new GameConfig { StartingCapital = 1000, MonthDuration = 10 }, new Random(0));
        double rent = session.CurrentBill.Total;

        Run(session, 9.5);
        Assert.That(session.Company.WalletBalance, Is.EqualTo(1000));

        Run(session, 1);
        Assert.That(session.Company.WalletBalance, Is.EqualTo(1000 - rent));
        Assert.That(session.Bills.MissedPayments, Is.EqualTo(0));
    }

    [Test]
    public void TwoUnpaidBillsInARowMeanBankruptcy()
    {
        var session = new GameSession(new GameConfig { StartingCapital = 0, MonthDuration = 10 }, new Random(0));

        Run(session, 10.5);
        Assert.That(session.Bills.MissedPayments, Is.EqualTo(1));
        Assert.That(session.IsOver, Is.False);

        Run(session, 10);
        Assert.That(session.IsOver, Is.True);
        Assert.That(session.Result!.IsVictory, Is.False);
    }

    [Test]
    public void PayingBackAfterUnpaidBillResetsTheWarning()
    {
        var session = new GameSession(new GameConfig { StartingCapital = 0, MonthDuration = 10, ClickGigMoney = 100 },
            new Random(0));
        Run(session, 10.5);

        for (int i = 0; i < 20; i++)
            session.Hustle(); // +2 000 CHF de petits boulots
        Run(session, 10);

        Assert.That(session.Bills.MissedPayments, Is.EqualTo(0));
        Assert.That(session.IsOver, Is.False);
    }

    [Test]
    public void ComputerUpgradeIncreasesWorkSpeed()
    {
        var session = new GameSession(new GameConfig { StartingCapital = 1000 }, new Random(0));
        double before = session.Stats.WorkSpeed;

        bool bought = session.BuyUpgrade(UpgradeTrackId.Computer);

        Assert.That(bought, Is.True);
        Assert.That(session.Stats.WorkSpeed, Is.GreaterThan(before));
        Assert.That(session.Company.WalletBalance, Is.EqualTo(600));
    }

    [Test]
    public void FeaturesAreLockedUntilTheirStage()
    {
        var session = new GameSession(new GameConfig { StartingCapital = 1_000_000_000, RoguelikeEnabled = false }, new Random(0));

        Assert.That(session.IsUnlocked(Feature.FoundCompany), Is.False);
        Assert.That(session.FoundCompany("Acme"), Is.False);
        Assert.That(session.InvestInBusiness(0), Is.False);
        Assert.That(session.BuyAsset(0, 100), Is.False);
        Assert.That(session.BuyHolding(), Is.False);
    }

    [Test]
    public void StoryAdvancesWithNetWorthAndFounding()
    {
        var session = new GameSession(new GameConfig { StartingCapital = 50_000, RoguelikeEnabled = false }, new Random(0));
        var reached = new List<CareerStage>();
        session.Career.StageReached += s => reached.Add(s.Stage);

        session.Update(0.1);
        Assert.That(session.Career.Stage, Is.EqualTo(CareerStage.Freelancer));

        session.FoundCompany("Acme");
        session.Update(0.1);

        Assert.That(session.Career.Stage, Is.EqualTo(CareerStage.Founder));
        Assert.That(reached, Is.EqualTo(new[] { CareerStage.Freelancer, CareerStage.Founder }));
    }

    [Test]
    public void HiringNeedsAFoundedCompanyAndAddsCapacity()
    {
        var session = new GameSession(new GameConfig { StartingCapital = 50_000, RoguelikeEnabled = false }, new Random(0));
        session.Update(0.1);
        Assert.That(session.Hire(EmployeeRole.Intern), Is.False);

        session.FoundCompany("Acme");
        session.Update(0.1);
        int before = session.Stats.TaskCapacity;

        Assert.That(session.Hire(EmployeeRole.Intern), Is.True);
        Assert.That(session.Stats.TaskCapacity, Is.EqualTo(before + 1));
        Assert.That(session.CurrentBill.Salaries, Is.GreaterThan(0));
    }

    [Test]
    public void VictoryWhenKingOfTheWorld()
    {
        var config = new GameConfig { StartingCapital = 2_000_000_000, RoguelikeEnabled = false };
        var session = new GameSession(config, new Random(0));

        session.Update(0.1); // Freelance
        session.FoundCompany("Acme");
        session.Update(0.1); // Fondateur
        for (int i = 0; i < config.EmployeesForBoss; i++)
            session.Hire(EmployeeRole.Intern);
        session.Update(0.1); // Patron, Investisseur, Magnat
        Assert.That(session.Career.Stage, Is.EqualTo(CareerStage.Tycoon));

        session.BuyHolding();
        session.Update(0.1);

        Assert.That(session.IsOver, Is.True);
        Assert.That(session.Result!.IsVictory, Is.True);
        Assert.That(session.Result.StageTitle, Is.EqualTo("Roi du monde"));
    }

    [Test]
    public void NetWorthCountsCashMarketAndBusinesses()
    {
        var session = new GameSession(new GameConfig { StartingCapital = 1_000_000_000, RoguelikeEnabled = false }, new Random(0));
        session.Update(0.1);
        session.FoundCompany("Acme");
        session.Update(0.1);
        for (int i = 0; i < 3; i++)
            session.Hire(EmployeeRole.Intern);
        session.Update(0.1);
        double before = session.NetWorth;

        session.InvestInBusiness(0);
        session.BuyAsset(1, 10_000);

        Assert.That(session.NetWorth, Is.EqualTo(before).Within(1));
    }
}
