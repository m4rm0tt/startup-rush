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
    internal static (MainWindow Window, UI.Screens.GameScreen Game, Campaign Campaign) Advanced()
    {
        string root = Path.Combine(Path.GetTempPath(), "startuprush_uitests", Guid.NewGuid().ToString("N"));
        var window = new MainWindow(new SaveStore(root), new Settings(), Path.Combine(root, "settings.json"));
        var campaign = Campaign.New("Tests UI", "Alex", "Studio Test", FounderProfile.Generalist, 42);
        campaign.Ledger.Cash = 3_000_000;
        // Une partie avancée a déjà vu son bilan de réussite (lot 5) : il n'interrompt pas les tests des menus
        campaign.FinalGoalAcknowledged = true;
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

    private static IEnumerable<UI.Pixel.PixelPanel> Frames(Window window) => window.GetVisualDescendants().OfType<UI.Pixel.PixelPanel>();

    [AvaloniaTest]
    public void TheHudKitIsWiredIntoTheRealPanels()
    {
        var (window, game, campaign) = Advanced();

        // Aménagement : la rubrique en liste déroulante ; la recherche filtre tout le catalogue sans reconstruire le panneau
        game.OpenRoot(new BuildPanel());
        Dispatcher.UIThread.RunJobs();
        Assert.That(Frames(window).Select(p => p.Skin), Has.Some.StartsWith("pack:hud_dropdown"));
        var search = window.GetVisualDescendants().OfType<TextBox>().First(t => t.PlaceholderText == "Chercher un objet");
        search.Text = "plante";
        Dispatcher.UIThread.RunJobs();
        Assert.That(Texts(window), Has.Some.Contains("Plante").IgnoreCase, "un objet du catalogue trouvé par son nom");
        Assert.That(Texts(window), Has.None.EqualTo("Bureau compact"), "les objets qui ne correspondent pas disparaissent");
        search.Text = "zzzz";
        Dispatcher.UIThread.RunJobs();
        Assert.That(Texts(window), Does.Contain("Aucun objet ne correspond."));
        search.Text = "";

        // Finances : barres du résultat par site, mois par mois qui défile à l'horizontale
        game.OpenRoot(new FinancePanel());
        Dispatcher.UIThread.RunJobs();
        Assert.That(window.GetVisualDescendants().OfType<Image>().Count(i => (i.Tag as string)?.StartsWith("bar:") == true), Is.EqualTo(campaign.SiteResults(campaign.Ledger.Months.Keys.Max()!).Count));
        Assert.That(window.GetVisualDescendants().OfType<ScrollViewer>().Any(s => (string?)s.Tag == "horizontal"), Is.True);

        // Sites : deux jauges (électricité, froid) pour un site qui a une infrastructure
        Assert.That(campaign.RentSite("server_room").Ok, Is.True);
        var (rx, ry, rf) = campaign.FindSpot(Core.space.FurnitureCatalog.Get("rack_standard"))!.Value;
        Assert.That(campaign.BuyAndPlace("rack_standard", rx, ry, rf).Ok, Is.True);
        int rack = campaign.Premises.Items.Last().Id;
        foreach (string module in new[] { "module_patch_panel_1u", "module_network_switch_1u", "module_server_1u" })
            Assert.That(campaign.BuyAndAttach(module, rack).Ok, Is.True, module);
        campaign.AdvanceDays(1);
        Assert.That(campaign.SiteInfra(campaign.Premises).Any, Is.True);
        game.OpenRoot(new SitesPanel());
        Dispatcher.UIThread.RunJobs();
        Assert.That(window.GetVisualDescendants().OfType<Image>().Count(i => (i.Tag as string)?.StartsWith("gauge:") == true), Is.GreaterThanOrEqualTo(2));

        // Portefeuille : la puce sombre du type, le bouton « … » de chaque carte
        game.OpenRoot(new ProjectsPanel());
        Dispatcher.UIThread.RunJobs();
        Assert.That(Frames(window).Count(p => p.Skin == "pack:hud_chip_dark"), Is.EqualTo(campaign.Products.Count));

        // Fiche projet : les notes passent par la commande et restent après une sauvegarde
        var product = campaign.Products[0];
        Ui.SetDetailsOpen("Nom, notes, couleur, fermeture", true);
        game.OpenRoot(new ProjectPanel(product.Id).Show(ProjectPanel.Tab.Settings));
        Dispatcher.UIThread.RunJobs();
        var notes = window.GetVisualDescendants().OfType<TextBox>().First(t => t.AcceptsReturn);
        notes.Text = "Prix à revoir\nAjouter l'export";
        Dispatcher.UIThread.RunJobs();
        Assert.That(product.Notes, Is.EqualTo("Prix à revoir\nAjouter l'export"));
        Ui.SetDetailsOpen("Nom, notes, couleur, fermeture", false);
        window.Close();
    }

    [AvaloniaTest]
    public void ASlowOperationShowsTheBusyCursorThenRestoresIt()
    {
        string root = Path.Combine(Path.GetTempPath(), "startuprush_uitests", Guid.NewGuid().ToString("N"));
        var window = new MainWindow(new SaveStore(root), new Settings(), Path.Combine(root, "settings.json"));
        window.Show();
        bool ranWhileBusy = false;
        window.Busy(() => ranWhileBusy = window.IsBusy && window.Cursor != null);
        Assert.That(window.IsBusy, Is.True, "le curseur d'attente s'affiche avant le travail");
        Assert.That(window.Cursor, Is.Not.Null);
        Dispatcher.UIThread.RunJobs();
        Assert.That(ranWhileBusy, Is.True);
        Assert.That(window.IsBusy, Is.False);
        Assert.That(window.Cursor, Is.Null, "le curseur revient après le travail");
        window.Close();
    }
}
