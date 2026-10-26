using Core.company;
using Core.requests;
using Core.simulation;

namespace Tests.company;

[TestFixture]
public class CompanyTest
{
    [Test]
    public void StartsWithStartingCapital()
    {
        var company = new Company(new GameConfig { StartingCapital = 500 });
        Assert.That(company.WalletBalance, Is.EqualTo(500));
    }

    [Test]
    public void StartsAsFreelancer()
    {
        var company = new Company(new GameConfig());
        Assert.That(company.IsFounded, Is.False);
    }

    [Test]
    public void FoundingCostsMoneyAndRenames()
    {
        var company = new Company(new GameConfig { StartingCapital = 6000, CompanyFoundingCost = 5000 });

        bool result = company.Found("Acme");

        Assert.That(result, Is.True);
        Assert.That(company.IsFounded, Is.True);
        Assert.That(company.Name, Is.EqualTo("Acme"));
        Assert.That(company.WalletBalance, Is.EqualTo(1000));
    }

    [Test]
    public void CannotFoundWithoutEnoughMoneyOrTwice()
    {
        var poor = new Company(new GameConfig { StartingCapital = 100, CompanyFoundingCost = 5000 });
        Assert.That(poor.Found("Acme"), Is.False);

        var rich = new Company(new GameConfig { StartingCapital = 20000, CompanyFoundingCost = 5000 });
        rich.Found("Acme");
        Assert.That(rich.Found("Bis"), Is.False);
        Assert.That(rich.WalletBalance, Is.EqualTo(15000));
    }

    [Test]
    public void HustleWithoutTaskEarnsGigMoney()
    {
        var company = new Company(new GameConfig { StartingCapital = 0, ClickGigMoney = 2 });
        company.Stats.ClickPower = 3;

        var result = company.Hustle();

        Assert.That(result.Money, Is.EqualTo(6));
        Assert.That(company.WalletBalance, Is.EqualTo(6));
    }

    [Test]
    public void HustleWithTaskAdvancesIt()
    {
        var company = new Company(new GameConfig { StartingCapital = 0, ClickWork = 0.5, WorkSecondsPerDifficulty = 10 });
        company.AddRequest(new ClientRequest(1, "Client", RequestType.Logo, 100, 1, null, 10));
        company.AcceptRequest(1);

        var result = company.Hustle();

        Assert.That(result.Work, Is.EqualTo(0.5));
        Assert.That(result.Money, Is.EqualTo(0));
        Assert.That(company.ActiveTasks[0].WorkDone, Is.EqualTo(0.5));
    }

    [Test]
    public void WorkSpeedMakesTasksFinishFaster()
    {
        var company = new Company(new GameConfig { StartingCapital = 0, WorkSecondsPerDifficulty = 10 });
        company.Stats.WorkSpeed = 2;
        company.AddRequest(new ClientRequest(1, "Client", RequestType.Logo, 100, 1, null, 10));
        company.AcceptRequest(1);

        company.UpdateTasks(5);

        Assert.That(company.WalletBalance, Is.EqualTo(100));
    }

    [Test]
    public void OnTimeTaskRaisesReputation()
    {
        var company = new Company(new GameConfig { WorkSecondsPerDifficulty = 1 });
        company.AddRequest(new ClientRequest(1, "Client", RequestType.Logo, 100, 2, null, 10));
        company.AcceptRequest(1);

        company.UpdateTasks(2);

        Assert.That(company.Reputation.Points, Is.EqualTo(2));
    }

    [Test]
    public void AutoAcceptTakesOnlyFeasibleRequests()
    {
        var company = new Company(new GameConfig { WorkSecondsPerDifficulty = 10, MaxActiveTasks = 5 });
        company.Stats.AutoAccept = true;
        company.Stats.TaskCapacity = 5;
        company.AddRequest(new ClientRequest(1, "Faisable", RequestType.Logo, 100, 1, 20, 10));
        company.AddRequest(new ClientRequest(2, "Trop tard", RequestType.Logo, 100, 1, 5, 10));
        company.AddRequest(new ClientRequest(3, "Sans échéance", RequestType.Logo, 100, 1, null, 10));

        company.AutoAcceptRequests();

        Assert.That(company.ActiveTasks.Select(t => t.Request.Id), Is.EquivalentTo(new[] { 1, 3 }));
        Assert.That(company.PendingRequests.Select(r => r.Id), Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public void AutoAcceptDoesNothingWithoutSalesperson()
    {
        var company = new Company(new GameConfig());
        company.AddRequest(new ClientRequest(1, "Client", RequestType.Logo, 100, 1, null, 10));

        company.AutoAcceptRequests();

        Assert.That(company.ActiveTasks, Is.Empty);
    }
}
