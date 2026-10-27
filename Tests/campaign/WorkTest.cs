using Core.campaign;
using Core.finance;
using Core.products;
using Core.space;
using Core.team;
using Core.work;
using static Tests.campaign.CampaignTestKit;

namespace Tests.campaign;

public class WorkTest
{
    private static double Progress(Campaign campaign, Product product) =>
        campaign.WorkFor(WorkOwner.Product, product.Id).Sum(w => w.Progress);

    [Test]
    public void TheFounderWorksAutomaticallyOnTheNewProjectDuringWorkHours()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);

        campaign.Advance(60); // 8 h → 9 h
        double before = Progress(campaign, product);
        campaign.Advance(3 * 60); // 9 h → 12 h
        double morning = Progress(campaign, product);
        campaign.Advance(60); // midi
        double lunch = Progress(campaign, product);

        Assert.That(campaign.Founder.Assignment.Kind, Is.EqualTo(AssignmentKind.None), "aucune affectation manuelle");
        Assert.That(before, Is.EqualTo(0).Within(0.05));
        Assert.That(morning, Is.GreaterThan(1.5).And.LessThan(4));
        Assert.That(lunch - morning, Is.EqualTo(0).Within(0.05));
    }

    [Test]
    public void DesignThenCodeThenTestsFollowWithoutReassignment()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);

        FinishQueue(campaign, product);

        Assert.That(product.Components.Select(c => c.DefId), Is.EquivalentTo(new[] { "login", "dashboard", "subscribe" }));
        Assert.That(product.Components.All(c => c.Level == 1 && c.Quality > 0.3), Is.True);
        Assert.That(campaign.Founder.Assignment.Kind, Is.EqualTo(AssignmentKind.None));
    }

    [Test]
    public void TheFirstProjectIsReadyInAFewWeeksSolo()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);

        int days = 0;
        while (product.Queue.Count > 0 && days < 90)
        {
            campaign.AdvanceDays(1);
            days++;
        }

        Assert.That(days, Is.InRange(14, 45), "un premier projet prend quelques semaines, pas des mois");
        Assert.That(campaign.CanLaunch(product), Is.True);
    }

    [Test]
    public void WithoutAComputerNothingGetsDone()
    {
        var campaign = NewCampaign();
        campaign.Premises.Items.Remove(campaign.Premises.Items.First(i => i.DefId == "laptop"));
        campaign.InvalidateLayout();
        var product = NewProject(campaign);

        campaign.Advance(6 * 60);

        Assert.That(Progress(campaign, product), Is.EqualTo(0));
        Assert.That(campaign.Founder.Status, Does.Contain("ordinateur"));
    }

    [Test]
    public void APersonWorksOnOneProjectAtATime()
    {
        var campaign = NewCampaign();
        var first = NewProject(campaign);
        var second = NewProject(campaign, "casual");
        campaign.Assign(campaign.Founder.Id, second.Id);

        campaign.Advance(6 * 60);

        Assert.That(Progress(campaign, first), Is.EqualTo(0));
        Assert.That(Progress(campaign, second), Is.GreaterThan(2).And.LessThan(8));
    }

    [Test]
    public void AutomaticModeFollowsThePortfolioPriority()
    {
        var campaign = NewCampaign();
        var first = NewProject(campaign);
        var second = NewProject(campaign, "casual");
        campaign.MoveProject(second.Id, -1);

        campaign.Advance(6 * 60);

        Assert.That(campaign.AutoTarget(), Is.SameAs(second));
        Assert.That(Progress(campaign, first), Is.EqualTo(0));
        Assert.That(Progress(campaign, second), Is.GreaterThan(2));
    }

    [Test]
    public void AnOversizedTeamLosesEfficiencyWithoutCoordination()
    {
        var campaign = NewCampaign();
        double alone = campaign.WorkRate(campaign.Founder, Discipline.Code, teamSize: 1);
        double crowd = campaign.WorkRate(campaign.Founder, Discipline.Code, teamSize: 8);
        double led = campaign.WorkRate(campaign.Founder, Discipline.Code, teamSize: 8, lead: true);

        Assert.That(crowd, Is.LessThan(alone * 0.65));
        Assert.That(led, Is.GreaterThan(crowd));
    }

    [Test]
    public void BetterComputersHelpWithDiminishingReturns()
    {
        var campaign = NewCampaign();
        var computer = campaign.Premises.ComputerOn(campaign.Premises.DeskOf(campaign.Founder.Id)!)!;
        double laptop = campaign.WorkRate(campaign.Founder, Discipline.Code);
        computer.DefId = "pc";
        double pc = campaign.WorkRate(campaign.Founder, Discipline.Code);
        computer.DefId = "workstation";
        double pro = campaign.WorkRate(campaign.Founder, Discipline.Code);

        Assert.That(pc, Is.GreaterThan(laptop));
        Assert.That(pro - pc, Is.LessThan(pc - laptop));
    }

    [Test]
    public void ANewHireWithoutAWorkstationDoesNotProduce()
    {
        var campaign = NewCampaign();
        var candidate = campaign.Candidates.First();
        candidate.NoticeWorkdays = 0;
        campaign.Hire(candidate.Person.Id);
        var product = NewProject(campaign);
        campaign.Assign(campaign.Founder.Id, 0);
        // Le fondateur part en congé : seule la recrue pourrait travailler
        campaign.Vacation(campaign.Founder.Id);

        campaign.Advance(6 * 60);

        Assert.That(Progress(campaign, product), Is.EqualTo(0));
        Assert.That(candidate.Person.Activity, Is.EqualTo(Activity.Idle));
        Assert.That(candidate.Person.Status, Does.Contain("poste"));
    }

    [Test]
    public void SeatedPeopleAreAtTheirDesk()
    {
        var campaign = NewCampaign();
        NewProject(campaign);
        campaign.Advance(90);

        var seat = Premises.SeatCell(campaign.Premises.DeskOf(campaign.Founder.Id)!);
        Assert.That(campaign.Founder.Activity, Is.EqualTo(Activity.Working));
        Assert.That(campaign.Founder.Position, Is.EqualTo(seat));
    }

    [Test]
    public void SalariesAndRentAreBilledOncePerMonth()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 100_000;
        campaign.MoveTo("small");
        campaign.AdvanceDays(70);

        var rents = campaign.Ledger.Bills.Where(b => b.Category == Category.Rent).ToList();
        var draws = campaign.Ledger.Bills.Where(b => b.Category == Category.FounderDraw).ToList();
        Assert.That(rents.Select(b => GameTime.MonthKey(b.IssuedAt)).Distinct().Count(), Is.EqualTo(rents.Count));
        Assert.That(draws.Select(b => GameTime.MonthKey(b.IssuedAt)).Distinct().Count(), Is.EqualTo(draws.Count));
        Assert.That(rents, Has.Count.EqualTo(2));
    }
}
