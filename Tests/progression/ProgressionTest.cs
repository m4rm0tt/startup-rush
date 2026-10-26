using Core.progression;
using Core.simulation;

namespace Tests.progression;

[TestFixture]
public class ProgressionTest
{
    private static Career NewCareer() => new(new GameConfig().CareerStages, employeesForBoss: 3);

    [Test]
    public void CareerStartsBroke()
    {
        Assert.That(NewCareer().Stage, Is.EqualTo(CareerStage.Broke));
    }

    [Test]
    public void FounderNeedsAFoundedCompany()
    {
        var career = NewCareer();

        career.Update(new CareerSnapshot(1_000_000, CompanyFounded: false, EmployeeCount: 0, HasHolding: false));

        Assert.That(career.Stage, Is.EqualTo(CareerStage.Freelancer));
    }

    [Test]
    public void SeveralStagesCanBeReachedAtOnce()
    {
        var career = NewCareer();
        var reached = new List<CareerStage>();
        career.StageReached += s => reached.Add(s.Stage);

        career.Update(new CareerSnapshot(5_000_000, CompanyFounded: true, EmployeeCount: 3, HasHolding: false));

        Assert.That(career.Stage, Is.EqualTo(CareerStage.Investor));
        Assert.That(reached, Has.Count.EqualTo(4));
    }

    [Test]
    public void StageIsNeverLost()
    {
        var career = NewCareer();
        career.Update(new CareerSnapshot(10_000, true, 0, false));

        career.Update(new CareerSnapshot(0, true, 0, false));

        Assert.That(career.Stage, Is.EqualTo(CareerStage.Founder));
    }

    [Test]
    public void KingOfTheWorldNeedsAHolding()
    {
        var career = NewCareer();

        career.Update(new CareerSnapshot(5_000_000_000, true, 10, HasHolding: false));
        Assert.That(career.Stage, Is.EqualTo(CareerStage.Tycoon));

        career.Update(new CareerSnapshot(5_000_000_000, true, 10, HasHolding: true));
        Assert.That(career.Stage, Is.EqualTo(CareerStage.KingOfTheWorld));
        Assert.That(career.Next, Is.Null);
    }

    [Test]
    public void FeaturesUnlockInStoryOrder()
    {
        Assert.That(Feature.FoundCompany.RequiredStage(), Is.LessThan(Feature.Hiring.RequiredStage()));
        Assert.That(Feature.Hiring.RequiredStage(), Is.LessThan(Feature.Businesses.RequiredStage()));
        Assert.That(Feature.Businesses.RequiredStage(), Is.LessThan(Feature.Market.RequiredStage()));
        Assert.That(Feature.Market.RequiredStage(), Is.LessThan(Feature.Holding.RequiredStage()));
    }

    [Test]
    public void ReputationLevelFollowsPoints()
    {
        var levels = new List<ReputationLevel> { new("A", 0, 2, 1), new("B", 10, 3, 2), new("C", 30, 4, 3) };
        var reputation = new Reputation(levels);
        var changes = new List<string>();
        reputation.LevelChanged += l => changes.Add(l.Title);

        reputation.Add(12);
        Assert.That(reputation.Current.Title, Is.EqualTo("B"));
        Assert.That(reputation.ProgressToNext, Is.EqualTo(0.1).Within(1e-9));

        reputation.Add(-50);
        Assert.That(reputation.Points, Is.EqualTo(0));
        Assert.That(reputation.Current.Title, Is.EqualTo("A"));
        Assert.That(changes, Is.EqualTo(new[] { "B", "A" }));
    }
}
