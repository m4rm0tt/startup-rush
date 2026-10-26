using Core.requests;
using Core.simulation;
using Core.tasks;

namespace Tests.tasks;

[TestFixture]
public class WorkTaskTest
{
    private static readonly GameConfig Config = new() { WorkSecondsPerDifficulty = 10, LateRewardMultiplier = 0.5 };

    private static WorkTask MakeTask(int difficulty = 1, double? deadline = null, GameConfig? config = null) =>
        new(new ClientRequest(1, "Client", RequestType.Website, 200, difficulty, deadline, 10), config ?? Config);

    [Test]
    public void DurationDependsOnDifficulty()
    {
        Assert.That(MakeTask(difficulty: 3).Duration, Is.EqualTo(30));
    }

    [Test]
    public void ProgressIsProportionalToTime()
    {
        var task = MakeTask(difficulty: 2); // 20 s

        task.Update(5);

        Assert.That(task.Progress, Is.EqualTo(0.25));
        Assert.That(task.TimeRemaining, Is.EqualTo(15));
        Assert.That(task.Status, Is.EqualTo(WorkTaskStatus.InProgress));
    }

    [Test]
    public void TaskIsCompletedAtFullProgressWithFullReward()
    {
        var task = MakeTask(difficulty: 1);

        task.Update(10);

        Assert.That(task.Progress, Is.EqualTo(1));
        Assert.That(task.Status, Is.EqualTo(WorkTaskStatus.Completed));
        Assert.That(task.EarnedReward, Is.EqualTo(200));
    }

    [Test]
    public void TaskFinishedOnDeadlineIsNotLate()
    {
        var task = MakeTask(difficulty: 1, deadline: 10);

        task.Update(10);

        Assert.That(task.EarnedReward, Is.EqualTo(200));
    }

    [Test]
    public void LateTaskEarnsReducedReward()
    {
        var task = MakeTask(difficulty: 1, deadline: 4);

        task.Update(5);
        Assert.That(task.IsLate, Is.True);
        Assert.That(task.Status, Is.EqualTo(WorkTaskStatus.InProgress));

        task.Update(5);
        Assert.That(task.Status, Is.EqualTo(WorkTaskStatus.Completed));
        Assert.That(task.EarnedReward, Is.EqualTo(100));
    }

    [Test]
    public void LateTaskFailsWhenPenaltyIsFail()
    {
        var config = new GameConfig { WorkSecondsPerDifficulty = 10, LatePenalty = LatePenalty.Fail };
        var task = MakeTask(difficulty: 1, deadline: 4, config: config);

        task.Update(5);

        Assert.That(task.Status, Is.EqualTo(WorkTaskStatus.Failed));
        Assert.That(task.EarnedReward, Is.EqualTo(0));
    }

    [Test]
    public void WillBeLateWhenRemainingWorkExceedsDeadline()
    {
        Assert.That(MakeTask(difficulty: 1, deadline: 8).WillBeLate(), Is.True);
        Assert.That(MakeTask(difficulty: 1, deadline: 12).WillBeLate(), Is.False);
        Assert.That(MakeTask(difficulty: 1, deadline: null).WillBeLate(), Is.False);
    }

    [Test]
    public void FinishedTaskNoLongerChanges()
    {
        var task = MakeTask(difficulty: 1);
        task.Update(10);

        task.Update(10);

        Assert.That(task.WorkDone, Is.EqualTo(10));
    }
}
