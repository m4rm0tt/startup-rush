using Core.campaign;
using Core.persistence;
using static Tests.campaign.CampaignTestKit;

namespace Tests.campaign;

/// <summary>Lot 5 : objectif final (2,5 M de valorisation, tenu 30 jours), objectifs progressifs (prochains jalons), bilan de partie.</summary>
public class GoalsTest
{
    private static (Campaign Campaign, Core.products.Product Product) Live()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);
        FinishQueue(campaign, product);
        Assert.That(campaign.Launch(product.Id).Ok, Is.True);
        return (campaign, product);
    }

    [Test]
    public void TheFinalGoalIsNotReachedAtTheStart()
    {
        var campaign = NewCampaign();
        campaign.AdvanceDays(3);
        Assert.That(campaign.FinalGoalReachedAt, Is.Null);
        Assert.That(campaign.Valuation(), Is.LessThan(Campaign.FinalGoalValuation));
        Assert.That(campaign.LowestCash, Is.Not.Null.And.LessThanOrEqualTo(campaign.Ledger.Cash));
    }

    [Test]
    public void MoneyAloneWithoutAProductIsNotEnough()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 5_000_000;
        campaign.AdvanceDays(2);
        Assert.That(campaign.FinalGoalReachedAt, Is.Null, "il faut au moins un produit en ligne");
    }

    [Test]
    public void TheFinalGoalIsReachedOnceAndTheGameGoesOn()
    {
        var (campaign, _) = Live();
        campaign.Ledger.Cash = Campaign.FinalGoalValuation + 100_000;
        campaign.AdvanceDays(Campaign.FinalGoalHoldDays + 2);
        Assert.That(campaign.FinalGoalReachedAt, Is.Not.Null);
        long reached = campaign.FinalGoalReachedAt!.Value;
        Assert.That(campaign.Notices.Any(n => n.Title.StartsWith("Objectif atteint")), Is.True);
        Assert.That(campaign.Status, Is.EqualTo(CampaignStatus.Running), "la partie continue");
        campaign.FinalGoalAcknowledged = true;
        campaign.AdvanceDays(5);
        Assert.That(campaign.FinalGoalReachedAt, Is.EqualTo(reached), "ne se redéclenche pas");
        Assert.That(campaign.Notices.Count(n => n.Title.StartsWith("Objectif atteint")), Is.EqualTo(1));
    }

    [Test]
    public void GoalsSurviveAReload()
    {
        var (campaign, _) = Live();
        campaign.Ledger.Cash = Campaign.FinalGoalValuation + 100_000;
        campaign.AdvanceDays(Campaign.FinalGoalHoldDays + 2);
        campaign.FinalGoalAcknowledged = true;
        var loaded = SaveStore.Deserialize(SaveStore.Serialize(campaign));
        Assert.That(loaded.FinalGoalReachedAt, Is.EqualTo(campaign.FinalGoalReachedAt));
        Assert.That(loaded.FinalGoalAcknowledged, Is.True);
        Assert.That(loaded.LowestCash, Is.EqualTo(campaign.LowestCash));
    }

    [Test]
    public void TheNextGoalsFollowTheMilestones()
    {
        var campaign = NewCampaign();
        var first = campaign.NextGoals();
        Assert.That(first, Has.Count.EqualTo(3));
        Assert.That(first[0].Id, Is.EqualTo(Campaign.MilestoneDefs[0].Id));
        var product = NewProject(campaign);
        FinishQueue(campaign, product);
        campaign.Launch(product.Id);
        campaign.AdvanceDays(1);
        Assert.That(campaign.NextGoals().Select(g => g.Id), Has.None.EqualTo("first_launch"), "un jalon atteint laisse la place au suivant");
        Assert.That(campaign.NextGoals(), Has.Count.EqualTo(3));
    }

    [Test]
    public void TheSummaryListsTheMainProducts()
    {
        var (campaign, product) = Live();
        campaign.AdvanceDays(20);
        var summary = campaign.Summary();
        Assert.That(summary.Days, Is.EqualTo(campaign.Day));
        Assert.That(summary.Products.Select(p => p.Name), Has.Member(product.Name));
        Assert.That(summary.Users, Is.EqualTo(campaign.Products.Sum(p => p.Users)));
        Assert.That(summary.LowestCash, Is.Not.Null);
    }
}
