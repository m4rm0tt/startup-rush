using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using UI.Hud.Panels;

namespace UiTests;

/// <summary>Demandes des testeurs vérifiées dans le vrai écran de jeu (sans fenêtre) : pause, tutoriel.</summary>
public class TesterRequestsUiTest
{
    private static List<string> Texts(Window window) =>
        window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").Where(t => t != "").ToList();

    [AvaloniaTest]
    public void LargeMenusNoLongerPauseByDefault()
    {
        var (window, game, _) = Shots.Fresh(tutorial: false);
        game.OpenRoot(new FinancePanel());
        Dispatcher.UIThread.RunJobs();
        Assert.That(game.Session.AutoPause, Is.Null, "le temps continue pendant la navigation");
        Assert.That(game.Session.Running, Is.True);
        window.Close();
    }

    [AvaloniaTest]
    public void SpaceRestartsTimeStoppedByALargeMenu()
    {
        var (window, game, _) = Shots.Fresh(tutorial: false);
        game.Session.Settings.PauseInLargeMenus = true;
        game.OpenRoot(new FinancePanel());
        Dispatcher.UIThread.RunJobs();
        Assert.That(game.Session.AutoPause, Is.Not.Null, "réglage choisi : le grand menu arrête le temps");

        game.HandleKey(Avalonia.Input.Key.Space, Avalonia.Input.KeyModifiers.None);
        Assert.That(game.Session.AutoPause, Is.Null, "Espace relance le temps au lieu de poser une pause invisible");
        Assert.That(game.Session.Running, Is.True);
        game.RefreshPanel();
        Assert.That(game.Session.Running, Is.True, "le menu ouvert ne remet pas en pause");

        game.HandleKey(Avalonia.Input.Key.Space, Avalonia.Input.KeyModifiers.None);
        Assert.That(game.Session.PausedByPlayer, Is.True, "Espace remet en pause");
        window.Close();
    }

    [AvaloniaTest]
    public void TheTutorialShowsItsCurrentStepAndFollowsThePlayer()
    {
        var (window, game, campaign) = Shots.Fresh();
        game.Tick(0.3);
        Assert.That(Texts(window), Has.Some.Contains("Tutoriel 1/"));
        game.Scene.Pan(20, 0);
        game.Tick(0.3);
        Assert.That(Texts(window), Has.Some.Contains("Tutoriel 2/"), "déplacer la vue valide la première étape");
        game.Session.Select(UI.Game.SelectionKind.Person, campaign.Founder.Id);
        game.Tick(0.3);
        Assert.That(Texts(window), Has.Some.Contains("Ouvre Projets"));
        campaign.SkipTutorial();
        game.Tick(0.3);
        Assert.That(Texts(window), Has.None.Contains("Tutoriel "));
        window.Close();
    }
}

public class ProjectsUiTest
{
    private static List<string> Texts(Window window) =>
        window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").Where(t => t != "").ToList();

    [AvaloniaTest]
    public void AVersionIsPreparedThenPublishedFromTheProjectPanel()
    {
        var (window, game, campaign) = Shots.Fresh(tutorial: false);
        campaign.Ledger.Cash = 100_000;
        var (_, product) = campaign.CreateProject("Planio", "freelances", Core.products.RevenueModel.Subscription);
        campaign.CheatFinishWork();
        campaign.Launch(product!.Id);
        campaign.QueueComponent(product.Id, "export");
        campaign.CheatFinishWork();
        Assert.That(product.Staged, Has.Count.EqualTo(1));
        game.OpenRoot(new ProjectPanel(product.Id));
        Dispatcher.UIThread.RunJobs();
        Assert.That(Texts(window), Has.Some.Contains("Version 2 en préparation").IgnoreCase);
        UI.Hud.LaunchView.ShowRelease(game, product.Id);
        Dispatcher.UIThread.RunJobs();
        Assert.That(Texts(window), Has.Some.Contains("Publier Planio 2"));
        var publish = window.GetVisualDescendants().OfType<Button>().First(b => b.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Publier sans jouer"));
        Thread.Sleep(400);
        publish.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.That(product.Version, Is.EqualTo(2));
        Assert.That(product.Level("export"), Is.EqualTo(1));
        window.Close();
    }

    [AvaloniaTest]
    public void ProductionAndStockLiveInProjects()
    {
        var (window, game, campaign) = Shots.Fresh(tutorial: false);
        campaign.CheatUnlockAll();
        game.OpenRoot(new ProjectsPanel());
        Dispatcher.UIThread.RunJobs();
        Assert.That(Texts(window), Does.Contain("Production").And.Contain("Stock").And.Contain("Développement"));
        game.OpenRoot(ProjectsPanel.At("stock"));
        Dispatcher.UIThread.RunJobs();
        Assert.That(Texts(window), Has.Some.Contains("Stock et achats").IgnoreCase);
        game.OpenRoot(ProjectsPanel.At("dev"));
        window.Close();
    }
}
