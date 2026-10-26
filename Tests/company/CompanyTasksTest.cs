using Core.company;
using Core.requests;
using Core.simulation;
using Core.tasks;

namespace Tests.company;

[TestFixture]
public class CompanyTasksTest
{
    private Company _company = null!;

    [SetUp]
    public void SetUp()
    {
        var config = new GameConfig
        {
            StartingCapital = 1000,
            WorkSecondsPerDifficulty = 10,
            MaxActiveTasks = 2
        };
        _company = new Company(config);
    }

    private static ClientRequest MakeRequest(int id, double value = 300, double? deadline = null) =>
        new(id, "Client", RequestType.MobileApp, value, 1, deadline, 30);

    [Test]
    public void AcceptedRequestBecomesActiveTask()
    {
        _company.AddRequest(MakeRequest(1));

        _company.AcceptRequest(1);

        Assert.That(_company.ActiveTasks, Has.Count.EqualTo(1));
        Assert.That(_company.ActiveTasks[0].Request.Id, Is.EqualTo(1));
    }

    [Test]
    public void CompletedTaskPaysItsValue()
    {
        _company.AddRequest(MakeRequest(1, value: 300));
        _company.AcceptRequest(1);
        WorkTask? completed = null;
        _company.TaskCompleted += t => completed = t;

        _company.UpdateTasks(5);
        Assert.That(_company.WalletBalance, Is.EqualTo(1000));

        _company.UpdateTasks(5);
        Assert.That(_company.WalletBalance, Is.EqualTo(1300));
        Assert.That(_company.ActiveTasks, Is.Empty);
        Assert.That(completed?.Request.Id, Is.EqualTo(1));
    }

    [Test]
    public void LateTaskPaysReducedValue()
    {
        _company.AddRequest(MakeRequest(1, value: 300, deadline: 6));
        _company.AcceptRequest(1);

        _company.UpdateTasks(10);

        Assert.That(_company.WalletBalance, Is.EqualTo(1150));
    }

    [Test]
    public void AcceptIsRefusedWhenCapacityIsReached()
    {
        _company.AddRequest(MakeRequest(1));
        _company.AddRequest(MakeRequest(2));
        _company.AddRequest(MakeRequest(3));
        _company.AcceptRequest(1);
        _company.AcceptRequest(2);

        bool result = _company.AcceptRequest(3);

        Assert.That(result, Is.False);
        Assert.That(_company.HasFreeCapacity, Is.False);
        Assert.That(_company.ActiveTasks, Has.Count.EqualTo(2));
        Assert.That(_company.PendingRequests.Select(r => r.Id), Is.EqualTo(new[] { 3 }));
    }

    [Test]
    public void CapacityIsFreedWhenTaskIsCompleted()
    {
        _company.AddRequest(MakeRequest(1));
        _company.AddRequest(MakeRequest(2));
        _company.AddRequest(MakeRequest(3));
        _company.AcceptRequest(1);
        _company.AcceptRequest(2);

        _company.UpdateTasks(10);

        Assert.That(_company.AcceptRequest(3), Is.True);
    }

    [Test]
    public void FailedTaskIsRemovedWithoutPayment()
    {
        var company = new Company(new GameConfig
        {
            StartingCapital = 1000, WorkSecondsPerDifficulty = 10, LatePenalty = LatePenalty.Fail
        });
        company.AddRequest(MakeRequest(1, deadline: 2));
        company.AcceptRequest(1);
        WorkTask? failed = null;
        company.TaskFailed += t => failed = t;

        company.UpdateTasks(3);

        Assert.That(company.ActiveTasks, Is.Empty);
        Assert.That(company.WalletBalance, Is.EqualTo(1000));
        Assert.That(failed, Is.Not.Null);
    }
}
