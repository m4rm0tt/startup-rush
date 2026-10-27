using Core.campaign;
using Core.finance;
using Core.work;
using static Tests.campaign.CampaignTestKit;

namespace Tests.campaign;

public class ContractTest
{
    [Test]
    public void AnInvoiceIsAReceivableUntilTheClientPays()
    {
        var campaign = NewCampaign();
        var offer = AddOffer(campaign, price: 3_000m, reliability: 1);
        decimal cash = campaign.Ledger.Cash;

        campaign.AcceptContract(offer.Id, DeadlineOption.Standard, false);

        var deposit = campaign.Ledger.Invoices.Single(i => i.ContractId == offer.Id);
        Assert.That(deposit.Amount, Is.EqualTo(900m));
        Assert.That(campaign.Ledger.Cash, Is.EqualTo(cash), "facturé n'est pas encaissé");
        Assert.That(campaign.Ledger.OpenReceivables, Is.EqualTo(900m));

        campaign.AdvanceDays(14);
        Assert.That(deposit.PaidAt, Is.Not.Null);
        Assert.That(campaign.Ledger.Entries.Count(e => e.Label.Contains("Acompte")), Is.EqualTo(1), "encaissé une seule fois");
    }

    [Test]
    public void DeliveringOnTimeBillsTheBalanceAndRaisesReputation()
    {
        var campaign = NewCampaign();
        var offer = AddOffer(campaign, design: 4, code: 8, test: 2, price: 2_000m, workdays: 10);
        double reputation = campaign.MainCompany.Reputation;
        campaign.AcceptContract(offer.Id, DeadlineOption.Standard, false);

        campaign.AdvanceDays(7);

        Assert.That(offer.Status, Is.EqualTo(ContractStatus.Completed), offer.Outcome);
        Assert.That(offer.Penalty, Is.EqualTo(0));
        Assert.That(campaign.Ledger.Invoices.Count(i => i.ContractId == offer.Id), Is.EqualTo(2));
        Assert.That(campaign.MainCompany.Reputation, Is.GreaterThan(reputation));
        Assert.That(campaign.WorkFor(WorkOwner.Contract, offer.Id), Is.Empty);
        Assert.That(campaign.Founder.Assignment.Kind, Is.EqualTo(Core.team.AssignmentKind.None));
    }

    [Test]
    public void ARushedPromiseCanTurnIntoPenaltiesThenACancellation()
    {
        var campaign = NewCampaign();
        // Beaucoup trop de travail pour le délai promis
        var offer = AddOffer(campaign, design: 40, code: 120, test: 40, price: 20_000m, workdays: 8);
        campaign.AcceptContract(offer.Id, DeadlineOption.Rush, false);
        double reputation = campaign.MainCompany.Reputation;

        campaign.AdvanceDays(40);

        Assert.That(offer.Status, Is.EqualTo(ContractStatus.Cancelled));
        Assert.That(offer.Outcome, Does.Contain("retard"));
        Assert.That(campaign.MainCompany.Reputation, Is.LessThan(reputation));
        Assert.That(campaign.Ledger.Invoices.Count(i => i.ContractId == offer.Id), Is.EqualTo(1), "seul l'acompte a été facturé");
    }

    [Test]
    public void NegotiatingASaferDeadlineCostsPrice()
    {
        var campaign = NewCampaign();
        var offer = AddOffer(campaign, price: 4_000m, workdays: 10);

        var safe = offer.Terms(DeadlineOption.Safe);
        var rush = offer.Terms(DeadlineOption.Rush);

        Assert.That(safe.Workdays, Is.GreaterThan(10));
        Assert.That(safe.Price, Is.LessThan(4_000m));
        Assert.That(rush.Workdays, Is.LessThan(10));
        Assert.That(rush.Price, Is.GreaterThan(4_000m));
    }

    [Test]
    public void CapitalizingCostsNowAndSavesLater()
    {
        var campaign = NewCampaign();
        var first = AddOffer(campaign, design: 0, code: 10, test: 0);
        campaign.AcceptContract(first.Id, DeadlineOption.Standard, capitalize: true);
        double firstEffort = campaign.WorkFor(WorkOwner.Contract, first.Id).Sum(w => w.Effort);
        campaign.AdvanceDays(5);
        Assert.That(first.Status, Is.EqualTo(ContractStatus.Completed));

        var second = AddOffer(campaign, design: 0, code: 10, test: 0);
        campaign.AcceptContract(second.Id, DeadlineOption.Standard, capitalize: false);
        double secondEffort = campaign.WorkFor(WorkOwner.Contract, second.Id).Sum(w => w.Effort);

        Assert.That(firstEffort, Is.EqualTo(11.5).Within(0.01));
        Assert.That(secondEffort, Is.EqualTo(9).Within(0.01));
    }

    [Test]
    public void AnUnreliableClientMayNeverPay()
    {
        int neverPaid = 0;
        for (ulong seed = 1; seed <= 200; seed++)
        {
            var campaign = NewCampaign(seed);
            var offer = AddOffer(campaign, reliability: 0.6);
            campaign.AcceptContract(offer.Id, DeadlineOption.Standard, false);
            if (campaign.Ledger.Invoices.Single(i => i.ContractId == offer.Id).PaysAt == null)
                neverPaid++;
        }
        Assert.That(neverPaid, Is.GreaterThan(0).And.LessThan(20));
    }
}
