using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using UI.Hud.Panels;

namespace UiTests;

/// <summary>Lot 5 : bilan à la réussite (une seule fois, on continue) et à la faillite, objectifs dans Finances.</summary>
public class GoalsUiTest
{
    private static List<string> Texts(Window window) =>
        window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").Where(t => t != "").ToList();

    internal static (UI.MainWindow Window, UI.Screens.GameScreen Game, Core.campaign.Campaign Campaign) Rich()
    {
        var (window, game, campaign) = Shots.Fresh(tutorial: false);
        var (_, product) = campaign.CreateProject("Planio", "freelances", Core.products.RevenueModel.Subscription);
        campaign.CheatFinishWork();
        campaign.Launch(product!.Id);
        campaign.AdvanceDays(20);
        campaign.Ledger.Cash = Core.campaign.Campaign.FinalGoalValuation + 250_000;
        campaign.AdvanceDays(Core.campaign.Campaign.FinalGoalHoldDays + 1);
        return (window, game, campaign);
    }

    [AvaloniaTest]
    public void ReachingTheFinalGoalShowsTheSummaryOnceAndTheGameContinues()
    {
        var (window, game, campaign) = Rich();
        Assert.That(campaign.FinalGoalReachedAt, Is.Not.Null);
        game.Tick(0.3);
        Dispatcher.UIThread.RunJobs();
        var texts = Texts(window);
        Assert.That(texts, Has.Some.Contains("Objectif atteint"));
        Assert.That(texts, Has.Some.Contains("Principaux produits"));
        Assert.That(texts, Has.Some.Contains("Planio"));
        Assert.That(campaign.FinalGoalAcknowledged, Is.True);

        Thread.Sleep(400);
        window.GetVisualDescendants().OfType<Button>().First(b => b.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Continuer la partie"))
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        game.Tick(0.3);
        Dispatcher.UIThread.RunJobs();
        Assert.That(Texts(window), Has.None.Contains("Objectif atteint !"), "le bilan ne revient pas");
        Assert.That(campaign.Status, Is.EqualTo(Core.campaign.CampaignStatus.Running));

        game.OpenRoot(new FinancePanel());
        Dispatcher.UIThread.RunJobs();
        Assert.That(Texts(window), Has.Some.Contains("Objectif final").And.Some.Contains("la partie continue"));
        window.Close();
    }

    [AvaloniaTest]
    public void FinanceShowsTheFinalGoalAndTheNextMilestones()
    {
        var (window, game, campaign) = Shots.Fresh(tutorial: false);
        game.OpenRoot(new FinancePanel());
        Dispatcher.UIThread.RunJobs();
        var texts = Texts(window);
        Assert.That(texts, Has.Some.Contains("Objectif final"));
        foreach (var (_, name, _) in campaign.NextGoals())
            Assert.That(texts, Has.Some.Contains(name));
        window.Close();
    }
}

[Explicit, Category("Shots")]
public class GoalsShots
{
    [AvaloniaTest]
    public void VictorySummaryAndFinanceGoals()
    {
        var (window, game, campaign) = GoalsUiTest.Rich();
        game.Tick(0.3);
        Shots.Save(window, "goals_victory");
        game.CloseModal();
        game.OpenRoot(new FinancePanel());
        game.Tick(0.3);
        Shots.Save(window, "goals_finance");
        window.Close();
    }
}
