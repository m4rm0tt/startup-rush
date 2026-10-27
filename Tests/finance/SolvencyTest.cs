using Core.campaign;
using Core.finance;
using Core.team;
using static Tests.campaign.CampaignTestKit;

namespace Tests.finance;

public class SolvencyTest
{
    [Test]
    public void ASlightlyNegativeBalanceIsNotAGameOver()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 300;

        campaign.AdvanceDays(25); // la rémunération du 25 passe dans le découvert

        Assert.That(campaign.Ledger.Cash, Is.LessThan(0));
        Assert.That(campaign.Status, Is.EqualTo(CampaignStatus.Running));
        Assert.That(campaign.Ledger.Overdue(campaign.Minute), Is.Empty);
        Assert.That(campaign.Solvency.State, Is.EqualTo(SolvencyState.Tight));
    }

    [Test]
    public void PersistentUnpaidObligationsLeadToNoticeThenLiquidation()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 0;
        campaign.Ledger.Overdraft = 0;
        // Un employé payé mais aucun revenu : la paie ne passe pas
        var candidate = campaign.Candidates.First();
        campaign.Hire(candidate.Person.Id);

        SolvencyState? sawNotice = null;
        for (int day = 0; day < 140 && campaign.Status == CampaignStatus.Running; day++)
        {
            campaign.AdvanceDays(1);
            if (campaign.Solvency.State == SolvencyState.Notice)
                sawNotice = SolvencyState.Notice;
        }

        Assert.That(sawNotice, Is.EqualTo(SolvencyState.Notice), "la mise en demeure précède toujours la liquidation");
        Assert.That(campaign.Status, Is.EqualTo(CampaignStatus.Liquidated));
        Assert.That(campaign.Solvency.LiquidationReport, Does.Contain("Impayés"));
        Assert.That(campaign.Notices.Any(n => n.Key == "notice"), Is.True, "compte à rebours annoncé");
        Assert.That(campaign.Notices.Count(n => n.Key == "unpaid"), Is.LessThanOrEqualTo(2), "les impayés sont regroupés");
    }

    [Test]
    public void NothingHappensAfterLiquidation()
    {
        var campaign = NewCampaign();
        campaign.Status = CampaignStatus.Liquidated;
        long minute = campaign.Minute;

        campaign.AdvanceDays(10);

        Assert.That(campaign.Minute, Is.EqualTo(minute));
    }

    [Test]
    public void PayingTheArrearsLiftsTheNotice()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 0;
        campaign.Ledger.Overdraft = 0;
        for (int day = 0; day < 60 && campaign.Solvency.State != SolvencyState.Notice; day++)
            campaign.AdvanceDays(1);
        Assert.That(campaign.Solvency.State, Is.EqualTo(SolvencyState.Notice));

        // Un gros client paie enfin : tout est régularisé à l'échéance suivante
        campaign.Post(20_000, Category.Funding, "Apport du fondateur");
        campaign.AdvanceDays(1);

        Assert.That(campaign.Ledger.Overdue(campaign.Minute), Is.Empty);
        Assert.That(campaign.Solvency.State, Is.Not.EqualTo(SolvencyState.Notice));
        campaign.AdvanceDays(40);
        Assert.That(campaign.Status, Is.EqualTo(CampaignStatus.Running));
    }

    [Test]
    public void TheRecoveryPlanNeedsRecurringRevenueAndIsNotReusable()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 0;
        campaign.Ledger.Overdraft = 0;
        campaign.AdvanceDays(27);
        Assert.That(campaign.Ledger.Overdue(campaign.Minute), Is.Not.Empty);

        var refused = campaign.RecoveryPlanTerms();
        Assert.That(refused.Allowed, Is.False);
        Assert.That(refused.Condition, Does.Contain("revenus récurrents"));

        // Un produit qui rapporte assez rend le plan possible
        var (_, product) = campaign.CreateProject("Agenda", "smb", Core.products.RevenueModel.Subscription);
        product!.History.AddRange(Enumerable.Range(0, 30).Select(i => new Core.products.ProductDay { Day = i, Revenue = 60 }));
        var accepted = campaign.RequestRecoveryPlan();
        Assert.That(accepted.Ok, Is.True, accepted.Message);
        Assert.That(campaign.Ledger.Overdue(campaign.Minute), Is.Empty);
        Assert.That(campaign.Ledger.Loans.Single().Kind, Is.EqualTo(LoanKind.RecoveryPlan));

        campaign.Ledger.Cash = 0;
        campaign.AdvanceDays(35);
        Assert.That(campaign.RecoveryPlanTerms().Allowed, Is.False, "un seul plan tous les deux ans");
    }

    [Test]
    public void FiringCostsSeveranceAndFreesTheDesk()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 20_000;
        var person = HireDeveloper(campaign);
        var chair = campaign.Premises.ChairOf(campaign.Premises.SeatOf(person.Id)!)!;

        campaign.Fire(person.Id);

        Assert.That(campaign.People, Has.Count.EqualTo(1));
        Assert.That(chair.AssignedPersonId, Is.Null);
        Assert.That(campaign.Ledger.Bills.Any(b => b.Category == Category.Severance && b.Amount == person.Salary), Is.True);
    }

    [Test]
    public void TheBankWantsHistoryAndRevenue()
    {
        var campaign = NewCampaign();
        Assert.That(campaign.LoanTerms().Max, Is.EqualTo(0));

        campaign.AdvanceDays(100);
        foreach (var month in new[] { "2027-01", "2027-02", "2027-03" })
            campaign.Ledger.Months[month] = new() { [Category.Subscriptions] = 6_000 };

        var (max, _) = campaign.LoanTerms();
        Assert.That(max, Is.EqualTo(18_000));
        Assert.That(campaign.TakeLoan(max + 1).Ok, Is.False);
        Assert.That(campaign.TakeLoan(10_000).Ok, Is.True);
        Assert.That(campaign.LoanTerms().Max, Is.EqualTo(0), "un seul prêt à la fois");
    }

    [Test]
    public void RunwayStatesItsAssumptions()
    {
        var campaign = NewCampaign();
        var (days, assumptions) = campaign.Runway();

        Assert.That(days, Is.GreaterThan(200).And.LessThan(320), "environ 8 mois d'autonomie au départ");
        Assert.That(assumptions, Does.Contain("Dépenses"));
    }
}
