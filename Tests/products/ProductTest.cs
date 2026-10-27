using Core.campaign;
using Core.products;
using Core.team;
using Core.work;
using static Tests.campaign.CampaignTestKit;

namespace Tests.products;

public class ProductTest
{
    private static Product NewSaas(Campaign campaign, decimal price = 19m)
    {
        var (result, product) = campaign.CreateProduct(campaign.MainCompany.Id, "Factura", "freelances", RevenueModel.Subscription, price);
        Assert.That(result.Ok, Is.True, result.Message);
        return product!;
    }

    private static void AddModules(Product product, params string[] modules)
    {
        var home = product.Draft.Page(product.Draft.HomePageId)!;
        foreach (string id in modules)
            home.Modules.Add(ProductDesign.NewModule(product, id));
    }

    private static double Appeal(Product product) => ProductDesign.Appeal(product, product.Draft, _ => 0.7);

    [Test]
    public void VisualChoicesDoNotChangeTheSimulation()
    {
        var campaign = NewCampaign();
        var product = NewSaas(campaign);
        AddModules(product, "login", "dashboard", "catalog");
        double before = Appeal(product);
        var page = product.Draft.Pages[0];

        page.Theme = "sombre";
        page.Accent = 8;
        page.Layout = "grille";
        page.Modules.First(m => m.DefId == "catalog").Options["columns"] = "4";

        Assert.That(Appeal(product), Is.EqualTo(before));
    }

    [Test]
    public void UsefulModulesRaiseAppealWithDiminishingReturns()
    {
        var campaign = NewCampaign();
        var product = NewSaas(campaign);
        double start = Appeal(product);
        AddModules(product, "login", "dashboard");
        double one = Appeal(product);
        AddModules(product, "export");
        double two = Appeal(product);
        AddModules(product, "stats");
        double three = Appeal(product);

        Assert.That(one, Is.GreaterThan(start));
        Assert.That(two - one, Is.GreaterThan(three - two), "chaque module de productivité ajoute moins que le précédent");
    }

    [Test]
    public void ModulesOnAnUnlinkedPageAreInvisibleToUsers()
    {
        var campaign = NewCampaign();
        var product = NewSaas(campaign);
        var page = ProductDesign.NewPage(product, "Espace client");
        page.Modules.Add(ProductDesign.NewModule(product, "login"));
        page.Modules.Add(ProductDesign.NewModule(product, "dashboard"));
        product.Draft.Pages.Add(page);
        double unlinked = Appeal(product);
        Assert.That(ProductDesign.Warnings(product, product.Draft).Any(w => w.Text.Contains("liée")), Is.True);

        product.Draft.Pages[0].Links.Add(page.Id);

        Assert.That(Appeal(product), Is.GreaterThan(unlinked));
    }

    [Test]
    public void TooManyModulesOrIrrelevantOnesHurt()
    {
        var campaign = NewCampaign();
        var product = NewSaas(campaign);
        AddModules(product, "login", "dashboard", "export");
        double focused = Appeal(product);
        AddModules(product, "feed", "listings", "messaging", "reviews");

        Assert.That(Appeal(product), Is.LessThan(focused + 0.02));
        Assert.That(ProductDesign.Warnings(product, product.Draft).Count(w => w.Text.Contains("n'intéresse pas")), Is.GreaterThan(0));
    }

    [Test]
    public void TheDraftBecomesWorkThenAPublishedVersion()
    {
        var campaign = NewCampaign();
        var product = NewSaas(campaign);
        AddModules(product, "login", "dashboard", "subscribe");
        campaign.Assign(campaign.Founder.Id, AssignmentKind.Product, product.Id);

        var started = campaign.StartBuild(product.Id);
        Assert.That(started.Ok, Is.True, started.Message);
        Assert.That(campaign.CanEditDraft(product), Is.False, "le brouillon est figé pendant le développement");
        Assert.That(product.Build!.Forecast, Is.Not.Null);
        var module = product.Draft.AllModules.First(m => m.DefId == "dashboard");
        Assert.That(campaign.ModuleStage(product, module), Is.EqualTo("En développement"));

        for (int i = 0; i < 60 && !campaign.BuildReady(product); i++)
            campaign.AdvanceDays(1);
        Assert.That(campaign.BuildReady(product), Is.True);

        var published = campaign.Publish(product.Id);
        Assert.That(published.Ok, Is.True, published.Message);
        Assert.That(product.IsLive, Is.True);
        Assert.That(product.LiveVersion, Is.EqualTo(1));
        Assert.That(product.ModuleQuality, Has.Count.EqualTo(4));
        Assert.That(campaign.ModuleStage(product, product.Draft.AllModules.First(m => m.DefId == "dashboard")), Is.EqualTo("Publié"));
        Assert.That(campaign.WorkFor(WorkOwner.Product, product.Id), Is.Empty);
    }

    [Test]
    public void OnlyTheChangesAreRebuilt()
    {
        var campaign = NewCampaign();
        var product = NewSaas(campaign);
        AddModules(product, "login", "dashboard", "subscribe");
        product.Live = product.Draft.Clone();
        product.Draft = product.Live.Clone();
        Assert.That(campaign.PlanDraft(product), Is.Empty);

        product.Draft.AllModules.First(m => m.DefId == "dashboard").Options["widgets"] = "9";
        var plan = campaign.PlanDraft(product);

        Assert.That(plan, Is.Not.Empty);
        Assert.That(plan.All(w => w.Title.Contains("Tableau de bord")), Is.True);
        double full = ProductDesign.Effort(product.Draft.AllModules.First(m => m.DefId == "dashboard"), Discipline.Code);
        Assert.That(plan.Where(w => w.Discipline == Discipline.Code).Sum(w => w.Effort), Is.LessThan(full));
    }

    private static Product LiveProduct(Campaign campaign, params string[] modules)
    {
        var product = NewSaas(campaign);
        AddModules(product, modules);
        product.Live = product.Draft.Clone();
        product.LiveVersion = 1;
        foreach (var m in product.Live.AllModules)
            product.ModuleQuality[m.Id] = 0.7;
        return product;
    }

    [Test]
    public void MarketingBringsUsersWithDiminishingReturns()
    {
        decimal[] budgets = [0, 500, 2_000, 8_000];
        var users = new List<double>();
        foreach (decimal budget in budgets)
        {
            var campaign = NewCampaign();
            var product = LiveProduct(campaign, "login", "dashboard", "subscribe", "support");
            product.HostingId = "cluster";
            product.MarketingBudget = budget;
            product.MarketingChannelId = "search";
            for (int d = 0; d < 60; d++)
                Market.Step(product, new DayInputs(50, campaign.Competitors), d);
            users.Add(product.Users);
        }

        Assert.That(users[1], Is.GreaterThan(users[0]));
        Assert.That(users[2], Is.GreaterThan(users[1]));
        double gainPerChfLow = (users[1] - users[0]) / 500;
        double gainPerChfHigh = (users[3] - users[2]) / 6_000;
        Assert.That(gainPerChfHigh, Is.LessThan(gainPerChfLow / 2));
    }

    [Test]
    public void InsufficientHostingCausesSlownessThenOutagesThatCostUsers()
    {
        var campaign = NewCampaign();
        var product = LiveProduct(campaign, "login", "dashboard", "subscribe", "feed", "search");
        product.Users = 3_000;
        product.Aware = 5_000;

        product.HostingId = "cluster";
        var fine = Market.Step(product, new DayInputs(500, campaign.Competitors), 0);
        product.HostingId = "shared";
        var overloaded = Market.Step(product, new DayInputs(500, campaign.Competitors), 1);

        Assert.That(fine.Performance, Is.EqualTo(1));
        Assert.That(fine.DowntimeHours, Is.EqualTo(0));
        Assert.That(overloaded.Performance, Is.LessThan(0.6));
        Assert.That(overloaded.DowntimeHours, Is.GreaterThan(0));
        Assert.That(overloaded.Churned, Is.GreaterThan(fine.Churned));
    }

    [Test]
    public void WithoutPaymentModuleTheProductCannotCharge()
    {
        var campaign = NewCampaign();
        var product = LiveProduct(campaign, "login", "dashboard");
        product.Users = 500;

        var day = Market.Step(product, new DayInputs(100, campaign.Competitors), 0);

        Assert.That(day.Revenue, Is.EqualTo(0));
        product.History.AddRange(Enumerable.Range(0, 14).Select(i => new ProductDay { Day = i, Users = 500, Appeal = 0.3, Share = 0.5, PriceFactor = 1, Performance = 1, SupportFactor = 1, Liquidity = 1 }));
        Assert.That(Market.Explain(product).Any(r => r.Contains("Abonnement")), Is.True);
    }

    [Test]
    public void TheMarketplaceNeedsSellersForItsBuyers()
    {
        var campaign = NewCampaign();
        var (_, product) = campaign.CreateProduct(campaign.MainCompany.Id, "Terroir", "crafts", RevenueModel.Commission, 10m);
        var home = product!.Draft.Pages[0];
        foreach (var id in new[] { "login", "catalog", "listings", "checkout", "reviews" })
            home.Modules.Add(ProductDesign.NewModule(product, id));
        product.Live = product.Draft.Clone();
        foreach (var m in product.Live.AllModules)
            product.ModuleQuality[m.Id] = 0.7;
        product.HostingId = "cluster";
        product.Users = 4_000;
        product.Sellers = 10;

        var starved = Market.Step(product, new DayInputs(100, campaign.Competitors), 0);
        product.Sellers = 400;
        var balanced = Market.Step(product, new DayInputs(100, campaign.Competitors), 1);

        Assert.That(starved.Liquidity, Is.LessThan(0.1));
        Assert.That(balanced.Liquidity, Is.GreaterThan(starved.Liquidity));
        Assert.That(balanced.Revenue, Is.GreaterThan(starved.Revenue * 3));
    }

    [Test]
    public void PriceMattersAccordingToTheSegment()
    {
        var campaign = NewCampaign();
        var cheap = LiveProduct(campaign, "login", "dashboard", "subscribe");
        var expensive = LiveProduct(campaign, "login", "dashboard", "subscribe");
        cheap.Price = 15;
        expensive.Price = 60;

        Assert.That(Market.PriceFactor(expensive), Is.LessThan(Market.PriceFactor(cheap) / 3));
    }

    [Test]
    public void ForecastGivesARangeNotACertainty()
    {
        var campaign = NewCampaign();
        var product = NewSaas(campaign);
        AddModules(product, "login", "dashboard", "subscribe", "support");
        product.MarketingBudget = 600;

        var forecast = campaign.ForecastDraft(product);

        Assert.That(forecast.UsersHigh, Is.GreaterThan(forecast.UsersLow));
        Assert.That(forecast.ExpectedBugsHigh, Is.GreaterThan(forecast.ExpectedBugsLow));
    }
}
