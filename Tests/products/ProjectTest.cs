using Core.campaign;
using Core.products;
using static Tests.campaign.CampaignTestKit;

namespace Tests.products;

public class ProjectTest
{
    private static Product Live(Campaign campaign, string segment, params (string Id, int Level)[] components)
    {
        var product = NewProject(campaign, segment, []);
        foreach (var (id, level) in components)
            product.Components.Add(new ProjectComponent { DefId = id, Level = level, Quality = 0.65 });
        product.LaunchedAt = campaign.Minute;
        product.Version = 1;
        return product;
    }

    [Test]
    public void ComponentsRaiseAppealWithDiminishingReturns()
    {
        var campaign = NewCampaign();
        var product = Live(campaign, "freelances", ("login", 1));
        double base0 = Design.Appeal(product);
        product.Components.Add(new ProjectComponent { DefId = "dashboard", Level = 1, Quality = 0.65 });
        double one = Design.Appeal(product);
        product.Component("dashboard")!.Level = 2;
        double two = Design.Appeal(product);
        product.Component("dashboard")!.Level = 3;
        double three = Design.Appeal(product);

        Assert.That(one, Is.GreaterThan(base0));
        Assert.That(two - one, Is.GreaterThan(three - two), "chaque niveau rapporte moins que le précédent");
    }

    [Test]
    public void TheProjectColorDoesNotChangeTheSimulation()
    {
        var campaign = NewCampaign();
        var product = Live(campaign, "freelances", ("login", 1), ("dashboard", 1));
        double before = Design.Appeal(product);
        product.Color = 9;

        Assert.That(Design.Appeal(product), Is.EqualTo(before));
    }

    [Test]
    public void QueueRulesExplainWhatIsMissing()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign, "freelances", []);

        var needsLogin = Design.CanQueue(product, "messaging", campaign.IsUnlocked);
        var wrongType = Design.CanQueue(product, "gameplay", campaign.IsUnlocked);
        Assert.That(needsLogin.Ok, Is.False);
        Assert.That(needsLogin.Reason, Does.Contain("Comptes"));
        Assert.That(wrongType.Ok, Is.False);

        campaign.QueueComponent(product.Id, "login");
        Assert.That(Design.CanQueue(product, "messaging", campaign.IsUnlocked).Ok, Is.True, "la file compte comme prévue");

        campaign.QueueComponent(product.Id, "search");
        campaign.QueueComponent(product.Id, "search");
        var level3 = Design.CanQueue(product, "search", campaign.IsUnlocked);
        Assert.That(level3.Ok, Is.False);
        Assert.That(level3.Reason, Does.Contain("Niveau 3"));
    }

    [Test]
    public void CancellingAComponentAlsoCancelsWhatDependsOnIt()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign, "freelances", []);
        campaign.QueueComponent(product.Id, "login");
        campaign.QueueComponent(product.Id, "messaging");

        campaign.CancelQueued(product.Id, product.Queue[0].Id);

        Assert.That(product.Queue, Is.Empty);
        Assert.That(campaign.WorkFor(Core.work.WorkOwner.Product, product.Id), Is.Empty);
    }

    [Test]
    public void AnUpdateAfterLaunchRaisesTheVersion()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);
        FinishQueue(campaign, product);
        Assert.That(campaign.Launch(product.Id).Ok, Is.True);
        Assert.That(product.Version, Is.EqualTo(1));

        campaign.QueueComponent(product.Id, "export");
        FinishQueue(campaign, product);

        Assert.That(product.Version, Is.EqualTo(2));
        Assert.That(product.Level("export"), Is.EqualTo(1));
        Assert.That(campaign.Notices.Any(n => n.Title.Contains("mise à jour")), Is.True);
    }

    [Test]
    public void AGameSellsItsWishlistOnLaunchDay()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign, "core");
        campaign.SetMarketing(product.Id, 800);
        FinishQueue(campaign, product);
        double wishlist = product.Wishlist;
        decimal cash = campaign.Ledger.Cash;

        campaign.Launch(product.Id);

        Assert.That(wishlist, Is.GreaterThan(200));
        Assert.That(product.TotalBuyers, Is.GreaterThan(wishlist * 0.1));
        Assert.That(campaign.Ledger.Cash, Is.GreaterThan(cash), "les ventes du lancement sont encaissées");
    }

    [Test]
    public void ALaunchNeedsAtLeastTwoComponents()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign, "freelances", ["login"]);
        FinishQueue(campaign, product);

        Assert.That(campaign.Launch(product.Id).Ok, Is.False);
    }

    [Test]
    public void MarketingBringsUsersWithDiminishingReturns()
    {
        decimal[] budgets = [0, 500, 2_000, 8_000];
        var users = new List<double>();
        foreach (decimal budget in budgets)
        {
            var campaign = NewCampaign();
            var product = Live(campaign, "freelances", ("login", 1), ("dashboard", 2), ("subscribe", 1));
            product.HostingId = "cluster";
            product.MarketingBudget = budget;
            for (int d = 0; d < 60; d++)
                Market.Step(product, new DayInputs(50, campaign.Competitors), d);
            users.Add(product.Users);
        }

        Assert.That(users[1], Is.GreaterThan(users[0]));
        Assert.That(users[2], Is.GreaterThan(users[1]));
        Assert.That((users[3] - users[2]) / 6_000, Is.LessThan((users[1] - users[0]) / 500 / 2));
    }

    [Test]
    public void AutomaticHostingFollowsTheLoadUnderItsCap()
    {
        var campaign = NewCampaign();
        var product = Live(campaign, "consumers", ("login", 1), ("dashboard", 1), ("search", 1));
        product.Users = 6_000;

        campaign.SetHosting(product.Id, true, 100);
        Assert.That(product.HostingId, Is.EqualTo("cloud_s"), "la plus grosse offre sous le plafond");

        campaign.Unlock("cloud_m");
        campaign.SetHosting(product.Id, true, 400);
        Assert.That(product.HostingId, Is.EqualTo("cloud_m"));
    }

    [Test]
    public void InsufficientHostingCausesOutagesThatCostUsers()
    {
        var campaign = NewCampaign();
        var product = Live(campaign, "consumers", ("login", 1), ("dashboard", 1), ("search", 1), ("messaging", 1));
        product.Users = 3_000;
        product.Aware = 5_000;

        product.HostingId = "cluster";
        var fine = Market.Step(product, new DayInputs(500, campaign.Competitors), 0);
        product.HostingId = "shared";
        var overloaded = Market.Step(product, new DayInputs(500, campaign.Competitors), 1);

        Assert.That(fine.DowntimeHours, Is.EqualTo(0));
        Assert.That(overloaded.DowntimeHours, Is.GreaterThan(0));
        Assert.That(overloaded.Churned, Is.GreaterThan(fine.Churned));
    }

    [Test]
    public void UpkeepIsAutomaticAndKeepsALiveProjectHealthy()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);
        FinishQueue(campaign, product);
        campaign.Launch(product.Id);
        product.Users = 400;
        product.Bugs = 6;

        campaign.AdvanceDays(30);

        Assert.That(product.Bugs, Is.LessThan(6), "le fondateur corrige tout seul quand il n'y a rien d'autre à faire");
        Assert.That(product.SupportBacklog, Is.LessThan(product.History[^1].Tickets * 2 + 1));
    }

    [Test]
    public void PriceMattersAccordingToTheAudience()
    {
        var campaign = NewCampaign();
        var cheap = Live(campaign, "freelances", ("login", 1));
        var expensive = Live(campaign, "freelances", ("login", 1));
        cheap.Price = 10;
        expensive.Price = 40;

        Assert.That(Market.PriceFactor(expensive), Is.LessThan(Market.PriceFactor(cheap) / 3));
    }

    [Test]
    public void ForecastGivesARange()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);

        var forecast = campaign.ForecastProject(product);

        Assert.That(forecast.UsersHigh, Is.GreaterThan(forecast.UsersLow));
    }

    [Test]
    public void ClosingAProjectStopsItsCosts()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);
        campaign.SetMarketing(product.Id, 500);
        decimal before = campaign.MonthlyFixedCosts;

        campaign.Retire(product.Id);

        Assert.That(campaign.MonthlyFixedCosts, Is.EqualTo(before - 500));
        Assert.That(campaign.Products, Is.Empty);
    }
}
