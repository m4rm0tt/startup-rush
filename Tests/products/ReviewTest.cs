using Core.campaign;
using Core.products;
using static Tests.campaign.CampaignTestKit;

namespace Tests.products;

public class ReviewTest
{
    private static Product Live(Campaign campaign)
    {
        var product = NewProject(campaign, "freelances", []);
        product.Components.Add(new ProjectComponent { DefId = "login", Level = 1, Quality = 0.65 });
        product.Components.Add(new ProjectComponent { DefId = "subscribe", Level = 1, Quality = 0.65 });
        product.LaunchedAt = campaign.Minute;
        product.HostingId = "cluster";
        product.Users = 600;
        return product;
    }

    private static void Days(Campaign campaign, Product product, int days, double support = 200)
    {
        for (int d = 0; d < days; d++)
            Market.Step(product, new DayInputs(support, campaign.Competitors), d);
    }

    [Test]
    public void AMissingFeatureShowsUpAndPointsToARealComponent()
    {
        var campaign = NewCampaign();
        var product = Live(campaign);
        Days(campaign, product, 7);

        var trends = Reviews.Trends(product, campaign.Minute, campaign.IsUnlocked);
        var missing = trends.FirstOrDefault(t => t.Cause.StartsWith("need:"));

        Assert.That(missing, Is.Not.Null);
        Assert.That(missing!.Cause, Is.EqualTo("need:Productivity"), "une application pro sans tableau de bord");
        Assert.That(missing.Link, Is.EqualTo("component:dashboard"));
    }

    [Test]
    public void BuildingTheSuggestedComponentMakesTheComplaintGoAway()
    {
        var campaign = NewCampaign();
        var product = Live(campaign);
        Days(campaign, product, 7);
        string link = Reviews.Trends(product, campaign.Minute, campaign.IsUnlocked).First(t => t.Cause.StartsWith("need:")).Link;

        product.Components.Add(new ProjectComponent { DefId = link["component:".Length..], Level = 2, Quality = 0.7 });
        product.Components.Add(new ProjectComponent { DefId = "export", Level = 1, Quality = 0.7 });
        Days(campaign, product, 7);

        Assert.That(Reviews.Trends(product, campaign.Minute, campaign.IsUnlocked).Any(t => t.Cause == "need:Productivity"), Is.False);
    }

    [Test]
    public void SlowServersAndUnansweredUsersAreReported()
    {
        var campaign = NewCampaign();
        var product = Live(campaign);
        product.HostingId = "shared";
        product.Users = 5_000;
        Days(campaign, product, 7, support: 0);

        var causes = Reviews.Trends(product, campaign.Minute, campaign.IsUnlocked).Select(t => t.Cause).ToList();

        Assert.That(causes, Does.Contain("perf"));
        Assert.That(causes, Does.Contain("support"));
    }

    [Test]
    public void ThereIsAlsoGoodNewsAndTheFeedStaysShort()
    {
        var campaign = NewCampaign();
        var product = Live(campaign);
        product.LastUpdateAt = campaign.Minute;
        Days(campaign, product, 7);

        var trends = Reviews.Trends(product, campaign.Minute, campaign.IsUnlocked);
        Assert.That(trends.Any(t => t.Positive), Is.True);
        Assert.That(trends.Count(t => !t.Positive), Is.LessThanOrEqualTo(3));

        for (int week = 0; week < 20; week++)
            Reviews.Collect(product, campaign.Minute, campaign.Random, campaign.IsUnlocked);
        Assert.That(product.Reviews.Count, Is.LessThanOrEqualTo(Reviews.MaxStored));
    }
}
