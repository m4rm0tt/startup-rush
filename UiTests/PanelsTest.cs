using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Core.campaign;
using Core.persistence;
using Core.products;
using UI;
using UI.Hud;
using UI.Hud.Panels;

namespace UiTests;

/// <summary>
/// Le vrai jeu sans fenêtre visible (Avalonia.Headless) : une campagne avancée, puis chaque panneau ouvert dans l'écran de
/// jeu. Vérifie qu'aucun ne plante et que les nouveaux écrans montrent ce qu'ils promettent.
/// </summary>
public class PanelsTest
{
    private static (MainWindow Window, UI.Screens.GameScreen Game, Campaign Campaign) Advanced()
    {
        string root = Path.Combine(Path.GetTempPath(), "startuprush_uitests", Guid.NewGuid().ToString("N"));
        var window = new MainWindow(new SaveStore(root), new Settings(), Path.Combine(root, "settings.json"));
        var campaign = Campaign.New("Tests UI", "Alex", "Studio Test", FounderProfile.Generalist, 42);
        campaign.Ledger.Cash = 3_000_000;
        campaign.CheatUnlockAll();
        Assert.That(campaign.RentSite("hw_workshop").Ok, Is.True);
        Assert.That(campaign.InstallLayout("assembly").Ok, Is.True);
        campaign.CreateProject("Appli", "freelances", RevenueModel.Subscription);
        campaign.CreateProject("Jeu", "casual", RevenueModel.Purchase);
        campaign.CreateProject("Carte", "makers", RevenueModel.Purchase);
        campaign.CheatFinishWork();
        foreach (var product in campaign.Products.ToList())
            campaign.Launch(product.Id);
        campaign.AdvanceDays(35);
        window.Show();
        var game = window.StartGame(campaign, null);
        Dispatcher.UIThread.RunJobs();
        return (window, game, campaign);
    }

    private static List<string> Texts(Window window) =>
        window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").Where(t => t != "").ToList();

    [AvaloniaTest]
    public void EveryPanelOpensOnAnAdvancedCampaign()
    {
        var (window, game, campaign) = Advanced();
        var panels = new List<GamePanel>
        {
            new ProjectsPanel(), new NewProjectPanel(), new TeamPanel(), new BuildPanel(), new TreePanel(), new FinancePanel(),
            new SitesPanel(), new CloudPanel(), new IndustryPanel(), new ResearchPanel(), new AiPanel(), new LabPanel(),
            new NoticesPanel(), new CheatPanel(), new PersonPanel(campaign.Founder.Id),
        };
        panels.AddRange(campaign.Products.Select(p => (GamePanel)new ProjectPanel(p.Id)));
        foreach (var panel in panels)
        {
            Assert.DoesNotThrow(() =>
            {
                game.OpenRoot(panel);
                Dispatcher.UIThread.RunJobs();
            }, panel.GetType().Name);
            Assert.That(Texts(window), Does.Contain(panel.Title), $"{panel.GetType().Name} : le titre est affiché");
        }
        window.Close();
    }

    [AvaloniaTest]
    public void TheNewScreensShowWhatTheyPromise()
    {
        var (window, game, campaign) = Advanced();

        game.OpenRoot(new FinancePanel());
        Dispatcher.UIThread.RunJobs();
        var texts = Texts(window);
        Assert.That(texts, Has.Some.Contains("Résultat par site").IgnoreCase);
        Assert.That(texts, Does.Contain("Commun à l'entreprise"));
        Assert.That(texts, Does.Contain(campaign.Sites[1].Name), "l'atelier porte son loyer et ses achats");

        game.OpenRoot(new IndustryPanel());
        Dispatcher.UIThread.RunJobs();
        Assert.That(Texts(window), Has.Some.StartsWith("Vers "), "la colonne de transfert vers l'autre site");

        var game2 = campaign.Products.First(p => p.ArchetypeId == "game");
        Ui.SetDetailsOpen("Dépendances entre composants", true);
        game.OpenRoot(new ProjectPanel(game2.Id).Show(ProjectPanel.Tab.Components));
        Dispatcher.UIThread.RunJobs();
        var graph = window.GetVisualDescendants().OfType<DependencyGraph>().Single();
        Assert.That(graph.Bounds.Width, Is.GreaterThan(0));
        Assert.That(Texts(window), Does.Contain("Menus et options").And.Contain("Inventaire"));
        Ui.SetDetailsOpen("Dépendances entre composants", false);

        game.OpenRoot(new PersonPanel(campaign.Founder.Id));
        Dispatcher.UIThread.RunJobs();
        Assert.That(Texts(window), Has.Some.Contains("Tenue"));
        window.Close();
    }
}
