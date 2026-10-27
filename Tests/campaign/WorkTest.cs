using Core.campaign;
using Core.finance;
using Core.space;
using Core.team;
using Core.work;
using static Tests.campaign.CampaignTestKit;

namespace Tests.campaign;

public class WorkTest
{
    [Test]
    public void TheFounderProducesOnlyAtACompleteWorkstationDuringWorkHours()
    {
        var campaign = NewCampaign();
        var offer = AddOffer(campaign);
        campaign.AcceptContract(offer.Id, DeadlineOption.Standard, false);

        campaign.Advance(60); // 8 h → 9 h : pas encore au travail
        double before = campaign.WorkFor(WorkOwner.Contract, offer.Id).Sum(w => w.Progress);
        campaign.Advance(3 * 60); // 9 h → 12 h
        double morning = campaign.WorkFor(WorkOwner.Contract, offer.Id).Sum(w => w.Progress);
        campaign.Advance(60); // pause de midi
        double lunch = campaign.WorkFor(WorkOwner.Contract, offer.Id).Sum(w => w.Progress);

        Assert.That(before, Is.EqualTo(0).Within(0.05));
        Assert.That(morning, Is.GreaterThan(1.5).And.LessThan(4));
        Assert.That(lunch - morning, Is.EqualTo(0).Within(0.05), "personne ne travaille à midi");
    }

    [Test]
    public void WithoutAComputerNothingGetsDone()
    {
        var campaign = NewCampaign();
        var laptop = campaign.Premises.Items.First(i => i.DefId == "laptop");
        campaign.Premises.Items.Remove(laptop);
        campaign.InvalidateLayout();
        var offer = AddOffer(campaign);
        campaign.AcceptContract(offer.Id, DeadlineOption.Standard, false);

        campaign.Advance(6 * 60); // jusqu'à 14 h

        Assert.That(campaign.WorkFor(WorkOwner.Contract, offer.Id).Sum(w => w.Progress), Is.EqualTo(0));
        Assert.That(campaign.Founder.Status, Does.Contain("ordinateur"));
    }

    [Test]
    public void APersonHasOneAssignmentSoWorkIsNeverCountedTwice()
    {
        var campaign = NewCampaign();
        var a = AddOffer(campaign);
        var b = AddOffer(campaign);
        campaign.AcceptContract(a.Id, DeadlineOption.Standard, false);
        campaign.AcceptContract(b.Id, DeadlineOption.Standard, false);
        campaign.Assign(campaign.Founder.Id, AssignmentKind.Contract, b.Id);

        campaign.Advance(10 * 60);

        double onA = campaign.WorkFor(WorkOwner.Contract, a.Id).Sum(w => w.Progress);
        double onB = campaign.WorkFor(WorkOwner.Contract, b.Id).Sum(w => w.Progress);
        Assert.That(onA, Is.EqualTo(0));
        Assert.That(onB, Is.GreaterThan(3));
        Assert.That(onB, Is.LessThan(9), "une seule personne : au plus une journée de points");
    }

    [Test]
    public void TasksWaitForTheirDependencies()
    {
        var campaign = NewCampaign();
        var offer = AddOffer(campaign, design: 30, code: 10, test: 4);
        campaign.AcceptContract(offer.Id, DeadlineOption.Standard, false);

        campaign.Advance(10 * 60);

        var items = campaign.WorkFor(WorkOwner.Contract, offer.Id).ToList();
        Assert.That(items.Where(w => w.Discipline == Discipline.Code).Sum(w => w.Progress), Is.EqualTo(0));
        Assert.That(items.Where(w => w.Discipline == Discipline.Design).Sum(w => w.Progress), Is.GreaterThan(0));
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
        var desk = campaign.Premises.DeskOf(campaign.Founder.Id)!;
        var computer = campaign.Premises.ComputerOn(desk)!;
        double laptop = campaign.WorkRate(campaign.Founder, Discipline.Code);
        computer.DefId = "pc";
        double pc = campaign.WorkRate(campaign.Founder, Discipline.Code);
        computer.DefId = "workstation";
        double pro = campaign.WorkRate(campaign.Founder, Discipline.Code);

        Assert.That(pc, Is.GreaterThan(laptop));
        Assert.That(pro - pc, Is.LessThan(pc - laptop), "le deuxième achat rapporte moins que le premier");
    }

    [Test]
    public void ANewHireWithoutAWorkstationDoesNotProduce()
    {
        var campaign = NewCampaign();
        var candidate = campaign.Candidates.First();
        candidate.NoticeWorkdays = 0;
        campaign.Hire(candidate.Person.Id);
        var offer = AddOffer(campaign, design: 0, code: 40, test: 0);
        campaign.AcceptContract(offer.Id, DeadlineOption.Standard, false);
        campaign.Assign(campaign.Founder.Id, AssignmentKind.None, 0);
        campaign.Assign(candidate.Person.Id, AssignmentKind.Contract, offer.Id);

        campaign.Advance(6 * 60);

        Assert.That(campaign.WorkFor(WorkOwner.Contract, offer.Id).Sum(w => w.Progress), Is.EqualTo(0));
        Assert.That(candidate.Person.Activity, Is.EqualTo(Activity.Idle).Or.EqualTo(Activity.Walking));
    }

    [Test]
    public void EmployeesWalkToTheirSeatThroughFreeCells()
    {
        var campaign = NewCampaign();
        campaign.Advance(60 + 30); // 9 h 30

        var founder = campaign.Founder;
        var seat = Premises.SeatCell(campaign.Premises.DeskOf(founder.Id)!);
        Assert.That(founder.Activity, Is.EqualTo(Activity.Working));
        Assert.That(founder.Position, Is.EqualTo(seat));
    }

    [Test]
    public void SalariesAndRentAreBilledOncePerMonthEvenIfDaysAreReplayed()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 100_000;
        campaign.MoveTo("small");
        campaign.AdvanceDays(70);

        var rents = campaign.Ledger.Bills.Where(b => b.Category == Category.Rent).ToList();
        var draws = campaign.Ledger.Bills.Where(b => b.Category == Category.FounderDraw).ToList();
        Assert.That(rents.Select(b => GameTime.MonthKey(b.IssuedAt)).Distinct().Count(), Is.EqualTo(rents.Count));
        Assert.That(draws.Select(b => GameTime.MonthKey(b.IssuedAt)).Distinct().Count(), Is.EqualTo(draws.Count));
        Assert.That(rents, Has.Count.EqualTo(2), "février et mars");
    }
}
