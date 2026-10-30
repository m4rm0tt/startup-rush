using Core.campaign;
using Core.finance;
using Core.products;
using Core.space;
using static Tests.campaign.CampaignTestKit;

namespace Tests.campaign;

/// <summary>Fonctions de la spécification ajoutées pour la version complète (terminal de recherche, avis, composants...).</summary>
public class FeaturesTest
{
    private static void WorkHours(Campaign campaign)
    {
        while (!GameTime.IsWorkday(campaign.Minute) || GameTime.Hour(campaign.Minute) < 10)
            campaign.Advance(60);
    }

    [Test]
    public void TheFounderCanPrepareResearchAtTheTerminalWhereTheEquipmentIs()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 200_000;
        campaign.Unlocked.Add("hardware");
        Assert.That(campaign.CanOpenTerminal(Campaign.ResearchTerminal).Ok, Is.False, "aucune recherche en cours");
        Assert.That(campaign.StartResearch("res_boards").Ok, Is.True);
        WorkHours(campaign);
        var (ok, why) = campaign.CanOpenTerminal(Campaign.ResearchTerminal);
        Assert.That(ok, Is.False);
        Assert.That(why, Does.Contain("équipement"));

        var spot = campaign.FindSpot(FurnitureCatalog.Get("workbench_electronics"))!.Value;
        Assert.That(campaign.BuyAndPlace("workbench_electronics", spot.X, spot.Y, spot.Facing).Ok, Is.True);
        Assert.That(campaign.BuyAndAttach("oscilloscope", campaign.Premises.Items.Last().Id).Ok, Is.True);
        Assert.That(campaign.SiteOf(campaign.Founder), Is.EqualTo(campaign.Premises));
        Assert.That(campaign.OpenTerminal(Campaign.ResearchTerminal).Ok, Is.True);
        Assert.That(campaign.TerminalTitle(Campaign.ResearchTerminal), Does.Contain("Conception de cartes"));
        Assert.That(Core.campaign.TerminalWords.Research, Does.Contain(campaign.Terminal!.Word));

        var project = campaign.Research.Single();
        Assert.That(campaign.SubmitWord("faux", 1).Correct, Is.False, "une faute ne coûte rien");
        Assert.That(project.Progress, Is.EqualTo(0));
        double total = 0;
        for (int i = 0; i < 5; i++)
            total += campaign.SubmitWord(campaign.Terminal!.Word, 1).Points;
        Assert.That(total, Is.GreaterThan(0));
        Assert.That(project.Progress, Is.EqualTo(total).Within(1e-9), "le travail va à la recherche, une fois");
        Assert.That(project.Today, Is.EqualTo(total).Within(1e-9), "le budget du jour sera dû");
        Assert.That(campaign.Terminal!.Stage, Does.StartWith("Recherche"));
        campaign.CloseTerminal();

        // Bornée à ce qu'il reste : une recherche presque finie se termine sans dépasser
        project.Progress = project.Def.Effort - 0.01;
        Assert.That(campaign.OpenTerminal(Campaign.ResearchTerminal).Ok, Is.True);
        var last = campaign.SubmitWord(campaign.Terminal!.Word, 1);
        Assert.That(last.Points, Is.EqualTo(0.01).Within(1e-9));
        Assert.That(campaign.ResearchDone, Does.Contain("res_boards"));
        Assert.That(campaign.Terminal!.Finished, Is.True);
        campaign.CloseTerminal();
        Assert.That(campaign.CanOpenTerminal(Campaign.ResearchTerminal).Ok, Is.False);
    }

    // ------------------------------------------------------------------ Avis : causes réelles (spec §8)

    private static Product Live(string archetype, string segment, params ProjectComponent[] components)
    {
        var product = new Product { Id = 1, ArchetypeId = archetype, SegmentId = segment, Model = RevenueModel.Purchase, LaunchedAt = 1, Components = components.ToList() };
        for (int i = 0; i < 7; i++)
            product.History.Add(new ProductDay { Day = i, Appeal = 0.5, Performance = 1, SupportFactor = 1, PriceFactor = 1, Liquidity = 1 });
        return product;
    }

    [Test]
    public void HardwareReviewsBlameStockoutsAndBreakdownsAndPointToProduction()
    {
        var board = Live("board", "makers", new ProjectComponent { DefId = "sensors", Level = 1, Quality = 0.6 });
        Assert.That(board.IsHardware, Is.True);
        foreach (var day in board.History)
            (day.Sold, day.Lost) = (2, 6);
        board.DefectRate = 0.09;
        var trends = Reviews.Trends(board, 10_000, _ => true);
        Assert.That(trends, Has.Some.Matches<ReviewTrend>(t => t.Cause == "stockout" && t.Link == "industry"));
        Assert.That(trends, Has.Some.Matches<ReviewTrend>(t => t.Cause == "defects" && t.Link == "industry"));

        foreach (var day in board.History)
            (day.Sold, day.Lost) = (4, 0);
        board.DefectRate = 0.01;
        trends = Reviews.Trends(board, 10_000, _ => true);
        Assert.That(trends.Select(t => t.Cause), Has.None.EqualTo("stockout").And.None.EqualTo("defects"));
        Assert.That(trends, Has.Some.Matches<ReviewTrend>(t => t.Cause == "reliable" && t.Positive), "les retours positifs comptent aussi");
    }

    [Test]
    public void AiReviewsJudgeTheQualityOfTheAnswersFromTheModel()
    {
        var weak = Live("ai", "ai_assist", new ProjectComponent { DefId = "ai_core", Level = 1, Quality = 0.25 }, new ProjectComponent { DefId = "ui", Level = 1, Quality = 0.6 });
        weak.AiModelId = 1;
        var trends = Reviews.Trends(weak, 10_000, _ => true);
        Assert.That(trends, Has.Some.Matches<ReviewTrend>(t => t.Cause == "ai_quality" && t.Link == "ai"));
        Assert.That(trends.Select(t => t.Cause), Has.None.EqualTo("need:Intelligence"), "une seule plainte pour une seule cause");

        var good = Live("ai", "ai_assist", new ProjectComponent { DefId = "ai_core", Level = 1, Quality = 0.85 }, new ProjectComponent { DefId = "ui", Level = 1, Quality = 0.6 });
        good.AiModelId = 1;
        trends = Reviews.Trends(good, 10_000, _ => true);
        Assert.That(trends.Select(t => t.Cause), Has.None.EqualTo("ai_quality"));
        Assert.That(trends, Has.Some.Matches<ReviewTrend>(t => t.Cause == "ai_good" && t.Positive));
    }
}
