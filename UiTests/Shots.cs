using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Core.campaign;
using Core.persistence;
using Avalonia.VisualTree;
using UI;

namespace UiTests;

/// <summary>
/// Captures de l'interface rendue sans fenêtre (vrai rendu Skia), pour regarder un écran sans Windows. Explicites : elles ne
/// tournent que sur demande (<c>dotnet test UiTests --filter TestCategory=Shots</c>) et écrivent dans le dossier donné par la
/// variable STARTUPRUSH_SHOTS (sinon un dossier temporaire).
/// </summary>
[Explicit, Category("Shots")]
public class Shots
{
    internal static string Folder
    {
        get
        {
            string folder = Environment.GetEnvironmentVariable("STARTUPRUSH_SHOTS") ?? Path.Combine(Path.GetTempPath(), "startuprush_shots");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    /// <summary>Enregistre l'image actuelle de la fenêtre.</summary>
    internal static void Save(Avalonia.Controls.Window window, string name)
    {
        Dispatcher.UIThread.RunJobs();
        var frame = window.CaptureRenderedFrame();
        frame?.Save(Path.Combine(Folder, name + ".png"));
    }

    /// <summary>Une partie neuve, telle qu'un joueur la commence (chambre, tutoriel).</summary>
    internal static (MainWindow Window, UI.Screens.GameScreen Game, Campaign Campaign) Fresh(bool tutorial = true)
    {
        string root = Path.Combine(Path.GetTempPath(), "startuprush_uitests", Guid.NewGuid().ToString("N"));
        var window = new MainWindow(new SaveStore(root), new Settings(), Path.Combine(root, "settings.json")) { Width = 1600, Height = 900 };
        var campaign = Campaign.New("Shots", "Alex", "Studio Léman", FounderProfile.Generalist, 7);
        campaign.TutorialOn = tutorial;
        window.Show();
        var game = window.StartGame(campaign, null);
        Dispatcher.UIThread.RunJobs();
        return (window, game, campaign);
    }
}

[Explicit, Category("Shots")]
public class Lot1Shots
{
    [AvaloniaTest]
    public void TutorialNightAndNewGame()
    {
        var (window, game, campaign) = Shots.Fresh();
        Shots.Save(window, "lot1_tutorial_start");
        game.Session.Select(UI.Game.SelectionKind.Person, campaign.Founder.Id);
        game.Scene.Pan(10, 0);
        game.Tick(0.3);
        Shots.Save(window, "lot1_tutorial_step3");
        campaign.Advance(16 * 60);
        game.Session.ClearSelection();
        game.Tick(0.3);
        Shots.Save(window, "lot1_night");
        window.Close();

        string root = Path.Combine(Path.GetTempPath(), "startuprush_uitests", Guid.NewGuid().ToString("N"));
        var menu = new MainWindow(new SaveStore(root), new Settings(), Path.Combine(root, "settings.json")) { Width = 1600, Height = 1000 };
        menu.Show();
        menu.ShowNewCampaign();
        Dispatcher.UIThread.RunJobs();
        var start = menu.GetVisualDescendants().OfType<Avalonia.Controls.Button>().First(b => b.GetVisualDescendants().OfType<Avalonia.Controls.TextBlock>().Any(t => t.Text == "Commencer"));
        Thread.Sleep(400);
        start.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
        Shots.Save(menu, "lot1_newgame_error");
        menu.Close();
    }
}

[Explicit, Category("Shots")]
public class Lot2Shots
{
    [AvaloniaTest]
    public void BuildingAndCustomization()
    {
        var (window, game, campaign) = Shots.Fresh(tutorial: false);
        campaign.Ledger.Cash = 2_000_000;
        campaign.CheatUnlockAll();
        campaign.MoveTo("small");
        foreach (var bin in campaign.Premises.Items.Where(i => i.DefId is "trash_bin" or "recycling_station").ToList())
            campaign.Premises.Items.Remove(bin);
        campaign.InvalidateLayout();
        campaign.SeatEveryone(buy: true);
        for (int i = 0; i < 6; i++)
        {
            campaign.Premises.Litter.Add(new Core.space.LitterSpot { X = 2 + i % 3, Y = 3 + i / 3, Kind = i % 3 });
        }
        game.Tick(0.3);
        game.OpenRoot(new UI.Hud.Panels.BuildPanel());
        game.Tick(0.3);
        Shots.Save(window, "lot2_build_panel");

        // Aperçu : parquet sur tout le sol, couleur noyer
        game.Session.SetFinish(new UI.Game.FinishTool("floor_parquet", "all", "walnut"));
        game.RefreshPanel();
        game.Tick(0.3);
        Shots.Save(window, "lot2_preview_floor");
        game.Session.SetFinish(new UI.Game.FinishTool("pattern_brick", "both", "sage"));
        game.RefreshPanel();
        game.Tick(0.3);
        Shots.Save(window, "lot2_preview_walls");
        game.Session.SetFinish(null);

        // Un canapé recoloré et sa fiche
        var (x, y, f) = campaign.FindSpot(Core.space.FurnitureCatalog.Get("sofa"))!.Value;
        campaign.BuyAndPlace("sofa", x, y, f);
        var sofa = campaign.Premises.Items.Last();
        campaign.SetItemColor(sofa.Id, "fabric", "rose");
        game.ClosePanels();
        game.Session.Select(UI.Game.SelectionKind.Item, sofa.Id);
        game.Tick(0.3);
        Shots.Save(window, "lot2_sofa_colors");

        // Une baie sans switch dans une salle serveur
        campaign.RentSite("server_room");
        var spot = campaign.FindSpot(Core.space.FurnitureCatalog.Get("rack_standard"))!.Value;
        campaign.BuyAndPlace("rack_standard", spot.X, spot.Y, spot.Facing);
        var rack = campaign.Premises.Items.Last();
        campaign.BuyAndAttach("module_server_1u", rack.Id);
        game.Session.Select(UI.Game.SelectionKind.Item, rack.Id);
        game.Scene.ResetCamera();
        game.Tick(0.3);
        Shots.Save(window, "lot2_rack_issue");
        window.Close();
    }
}

[Explicit, Category("Shots")]
public class Lot3Shots
{
    [AvaloniaTest]
    public void ProjectsVersionsAndMiniGames()
    {
        var (window, game, campaign) = Shots.Fresh(tutorial: false);
        campaign.Ledger.Cash = 500_000;
        campaign.CheatUnlockAll();
        var (_, product) = campaign.CreateProject("Planio", "freelances", Core.products.RevenueModel.Subscription);
        campaign.CheatFinishWork();
        campaign.Launch(product!.Id);
        campaign.AdvanceDays(10);
        campaign.QueueComponent(product.Id, "export");
        campaign.QueueComponent(product.Id, "ui", Core.products.Approaches.Polish);
        campaign.CheatFinishWork();
        campaign.QueueComponent(product.Id, "search");
        campaign.QueueComponent(product.Id, "notifications", Core.products.Approaches.Quick);
        game.OpenRoot(new UI.Hud.Panels.ProjectsPanel());
        game.PushPanel(new UI.Hud.Panels.ProjectPanel(product.Id));
        game.Tick(0.3);
        Shots.Save(window, "lot3_project_overview");
        game.PushPanel(new UI.Hud.Panels.ProjectPanel(product.Id).Show(UI.Hud.Panels.ProjectPanel.Tab.Components));
        game.Tick(0.3);
        Shots.Save(window, "lot3_components");
        game.ClosePanels();
        // Chaque genre de mini-jeu de publication, tiré pour des versions successives
        for (int v = 0; v < 5; v++)
        {
            UI.Hud.LaunchView.ShowRelease(game, product.Id);
            game.Tick(0.3);
            Shots.Save(window, $"lot3_release_{campaign.ReleaseChallenge(product.Id).Kind}");
            game.CloseModal();
            product.Version++;
        }
        game.OpenRoot(UI.Hud.Panels.ProjectsPanel.At("stock"));
        game.Tick(0.3);
        Shots.Save(window, "lot3_projects_stock");
        window.Close();
    }
}
