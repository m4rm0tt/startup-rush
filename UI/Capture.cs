using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Core.bots;
using Core.campaign;
using Core.persistence;
using Core.products;
using Core.space;
using Core.team;
using UI.Audio;
using UI.Hud.Panels;
using UI.Screens;

namespace UI;

/// <summary>
/// Parcours scripté lancé par « --capture &lt;dossier&gt; » : il joue dans le vrai jeu (mêmes écrans, mêmes commandes)
/// et enregistre des captures de l'interface rendue, à plusieurs tailles de fenêtre. Les sauvegardes vont dans un dossier
/// temporaire séparé. Un rapport texte liste ce qui a été vérifié (ressources chargées, sons déclenchés, rechargement).
/// </summary>
public static class Capture
{
    public static void Run(MainWindow window, string folder)
    {
        Directory.CreateDirectory(folder);
        window.Opened += async (_, _) =>
        {
            var report = new List<string>();
            try
            {
                await Script(window, folder, report);
            }
            catch (Exception e)
            {
                report.Add("ÉCHEC : " + e);
            }
            File.WriteAllLines(Path.Combine(folder, "capture_report.txt"), report);
            window.Close();
        };
    }

    private static async Task Wait(double seconds) => await Task.Delay(TimeSpan.FromSeconds(seconds));

    private static async Task Shot(MainWindow window, string folder, string name, List<string> report, int width = 1600, int height = 900)
    {
        if (window.WindowState != WindowState.Normal)
            window.WindowState = WindowState.Normal;
        window.Width = width;
        window.Height = height;
        await Wait(0.6);
        if (window.Current is GameScreen game)
            game.ApplyScale();
        await Wait(0.4);
        var size = window.ClientSize;
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)size.Width, (int)size.Height), new Vector(96, 96));
        bitmap.Render(window);
        string path = Path.Combine(folder, name + ".png");
        bitmap.Save(path);
        report.Add($"{name}.png : {size.Width}×{size.Height}");
    }

    private static async Task Script(MainWindow window, string folder, List<string> report)
    {
        report.Add($"Audio : {Sfx.LoadedSounds} sons et {Sfx.LoadedTracks} musiques chargés depuis le build.");
        report.Add($"Atlas : {UI.Pixel.Atlas.Sprites.Count} sprites dans furniture.json.");
        await Wait(1);
        await Shot(window, folder, "01_menu_principal", report);

        // Nouvelle campagne : l'appartement au premier matin
        var campaign = Campaign.New("Capture", "Alex", "Studio Léman", FounderProfile.Developer, 2027);
        window.Store.Save(campaign, SaveKind.Manual, "Début");
        var game = window.StartGame(campaign, null);
        campaign.Advance(95); // 9 h 35 : au travail
        await Wait(1.2);
        game.Session.TogglePause();
        await Shot(window, folder, "02_appartement_initial", report);

        // Aménagement : outil bureau, survol d'une case valide puis d'une case invalide
        game.OpenRoot(new BuildPanel());
        game.Session.SetTool(new Game.BuildTool("desk"));
        game.Scene.SetHover(new Cell(10, 5));
        await Shot(window, folder, "03_amenagement_valide", report);
        game.Scene.SetHover(new Cell(2, 6));
        await Shot(window, folder, "04_amenagement_invalide", report);
        var placed = campaign.BuyAndPlace("chair", 11, 4);
        var desk = campaign.BuyAndPlace("desk", 11, 5);
        var pc = campaign.BuyAndPlace("pc", 11, 5);
        report.Add($"Poste posé : chaise {placed.Ok}, bureau {desk.Ok}, PC {pc.Ok} ; postes complets : {Layout.CompleteWorkstations(campaign.Premises).Count()}");
        game.Session.SetTool(null);
        game.ClosePanels();

        // Contrat accepté, puis un produit avec deux pages personnalisées
        var offer = campaign.Offers.First();
        campaign.AcceptContract(offer.Id, Core.work.DeadlineOption.Safe, false);
        var (_, product) = campaign.CreateProduct(campaign.MainCompany.Id, "Factura", "freelances", RevenueModel.Subscription, 19);
        var home = product!.Draft.Pages[0];
        home.Subtitle = "La facturation des indépendants, sans prise de tête";
        home.Accent = 12;
        foreach (string id in new[] { "login", "subscribe" })
            home.Modules.Add(ProductDesign.NewModule(product, id));
        var app = ProductDesign.NewPage(product, "Espace client");
        app.Layout = "deux";
        app.Theme = "sombre";
        app.Accent = 10;
        var dashboard = ProductDesign.NewModule(product, "dashboard");
        dashboard.Options["widgets"] = "9";
        app.Modules.Add(dashboard);
        app.Modules.Add(ProductDesign.NewModule(product, "export"));
        app.Modules.Add(ProductDesign.NewModule(product, "support"));
        app.Links.Add(home.Id);
        product.Draft.Pages.Add(app);
        home.Links.Add(app.Id);

        game.OpenRoot(new ProductsPanel());
        var panel = new ProductPanel(product.Id);
        game.PushPanel(panel);
        await Shot(window, folder, "05_editeur_pages", report);
        report.Add($"Brouillon : {product.Draft.Pages.Count} pages, {product.Draft.AllModules.Count()} modules, attrait estimé {ProductDesign.Appeal(product, product.Draft, _ => 0.6):P0}");

        // Version : plan de travail et prévision
        campaign.Assign(campaign.Founder.Id, AssignmentKind.Product, product.Id);
        game.ClosePanels();
        game.OpenRoot(new ProductsPanel());
        var versionPanel = new ProductPanel(product.Id);
        game.PushPanel(versionPanel);
        SetTab(versionPanel, 1);
        game.RefreshPanel();
        await Shot(window, folder, "06_version_prevision", report);
        game.ClosePanels();

        // Sauvegarde, rechargement : même état
        string state = SaveStore.SerializeState(campaign);
        game.Session.Save(SaveKind.Manual, "Avant reprise");
        var reloaded = window.Store.Load(game.Session.LastSavePath!);
        report.Add($"Rechargement identique : {SaveStore.SerializeState(reloaded) == state}");

        // Campagne avancée : 14 mois joués par une stratégie mixte, puis reprise dans l'interface
        var advanced = Campaign.New("Capture avancée", "Alex", "Studio Léman", FounderProfile.Developer, 7);
        var pilot = new Autopilot(Strategies.All.First(s => s.Name == "mixte") with { MaxStaff = 5, HireAbove = 20_000, MarketingBudget = 1_500 });
        pilot.Play(advanced, 420);
        while (GameTime.IsWorkday(advanced.Minute) == false)
            advanced.AdvanceDays(1);
        advanced.Advance(10 * 60 + 15 - GameTime.MinuteOfDay(advanced.Minute));
        report.Add($"Campagne avancée : {GameTime.FormatDate(advanced.Minute)}, {advanced.People.Count} personnes, {advanced.Premises.Name}, trésorerie {advanced.Ledger.Cash:N0}, état {advanced.Solvency.State}, produits {advanced.Products.Count(p => p.IsLive)}");
        window.Store.Save(advanced, SaveKind.Manual, "Avancée");
        var advancedGame = window.StartGame(advanced, null);
        await Wait(1);
        advancedGame.Session.TogglePause();
        await Shot(window, folder, "07_campagne_avancee", report);

        advancedGame.OpenRoot(new FinancePanel());
        await Shot(window, folder, "08_finances", report);
        advancedGame.ClosePanels();

        if (advanced.Products.FirstOrDefault() is { } live)
        {
            advancedGame.OpenRoot(new ProductsPanel());
            var results = new ProductPanel(live.Id);
            advancedGame.PushPanel(results);
            SetTab(results, 2);
            advancedGame.RefreshPanel();
            await Shot(window, folder, "09_resultats_produit", report);
            advancedGame.ClosePanels();
        }

        advancedGame.OpenRoot(new TeamPanel());
        await Shot(window, folder, "10_equipe", report);
        advancedGame.ClosePanels();
        advancedGame.OpenRoot(new ProjectsPanel());
        await Shot(window, folder, "11_petite_fenetre_projets", report, 960, 540);
        advancedGame.ClosePanels();
        await Shot(window, folder, "12_1080p", report, 1920, 1080);

        advancedGame.HandleKey(Avalonia.Input.Key.Escape, Avalonia.Input.KeyModifiers.None);
        await Shot(window, folder, "13_pause", report);
        advancedGame.HandleKey(Avalonia.Input.Key.Escape, Avalonia.Input.KeyModifiers.None);

        // Défaite : une campagne sans revenus menée jusqu'à la liquidation, sauvegardes intactes
        var doomed = Campaign.New("Capture défaite", "Alex", "Studio Perdu", FounderProfile.Designer, 3);
        doomed.Ledger.Cash = 0;
        doomed.Ledger.Overdraft = 0;
        doomed.Hire(doomed.Candidates.First().Person.Id);
        window.Store.Save(doomed, SaveKind.Manual, "Avant la chute");
        for (int i = 0; i < 160 && doomed.Status == CampaignStatus.Running; i++)
            doomed.AdvanceDays(1);
        var lost = window.StartGame(doomed, null);
        await Wait(1.5);
        await Shot(window, folder, "14_liquidation", report);
        report.Add($"Défaite : {doomed.Status}, sauvegardes de la campagne encore présentes : {window.Store.List(doomed.Id).Count}");
        lock (Sfx.Played)
            report.Add("Sons déclenchés (derniers) : " + string.Join(", ", Sfx.Played.Distinct()));
        report.Add("Sauvegardes de capture : " + window.Store.Root);
        lost.Leave();
    }

    private static void SetTab(ProductPanel panel, int tab)
    {
        var field = typeof(ProductPanel).GetField("_tab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        field.SetValue(panel, Enum.ToObject(field.FieldType, tab));
    }
}
