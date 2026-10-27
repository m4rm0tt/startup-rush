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

        // Début de partie : l'appartement, aucun projet
        var campaign = Campaign.New("Capture", "Alex", "Studio Léman", FounderProfile.Developer, 2027);
        window.Store.Save(campaign, SaveKind.Manual, "Début");
        var game = window.StartGame(campaign, null);
        campaign.Advance(95);
        await Wait(1.2);
        game.Session.TogglePause();
        await Shot(window, folder, "02_appartement_debut", report);
        game.OpenRoot(new ProjectsPanel());
        await Shot(window, folder, "03_portefeuille_vide", report);
        game.PushPanel(new NewProjectPanel());
        await Shot(window, folder, "04_nouveau_projet", report);
        game.ClosePanels();

        // Premier projet : développement automatique
        var (_, app) = campaign.CreateProject("", "freelances", RevenueModel.Subscription);
        campaign.Advance(4 * 60);
        game.OpenRoot(new ProjectsPanel());
        game.PushPanel(new ProjectPanel(app!.Id));
        await Shot(window, folder, "05_projet_en_developpement", report);
        game.ClosePanels();
        await Shot(window, folder, "06_scene_travail", report);
        int days = 0;
        while (app.Queue.Count > 0 && days++ < 90)
            campaign.AdvanceDays(1);
        report.Add($"Premier projet prêt en {days} jours (fondateur seul, sans aucune affectation manuelle).");
        game.OpenRoot(new ProjectsPanel());
        await Shot(window, folder, "07_pret_a_sortir", report);
        campaign.Launch(app.Id);
        decimal cashAtLaunch = campaign.Ledger.Cash;
        for (int i = 0; i < 50; i++)
            campaign.AdvanceDays(1);
        report.Add($"50 jours après la sortie : {app.Users:0} utilisateurs, {app.Paying:0} payants, revenus 30 j {app.History.TakeLast(30).Sum(d => d.Revenue):N0} CHF, argent {cashAtLaunch:N0} → {campaign.Ledger.Cash:N0}.");
        var trend = Reviews.Trends(app, campaign.Minute, campaign.IsUnlocked).FirstOrDefault(t => !t.Positive);
        report.Add($"Avis principal : {trend?.Text} → {trend?.Link}");
        game.ClosePanels();
        game.OpenRoot(new ProjectsPanel());
        game.PushPanel(new ProjectPanel(app.Id).Show(ProjectPanel.Tab.Reviews));
        await Shot(window, folder, "08_avis", report);
        game.ClosePanels();
        game.OpenRoot(new ProjectsPanel());
        game.PushPanel(new ProjectPanel(app.Id).Show(ProjectPanel.Tab.Components, trend?.Link.StartsWith("component:") == true ? trend.Link["component:".Length..] : null));
        await Shot(window, folder, "09_atelier_composants", report);
        game.ClosePanels();
        game.OpenRoot(new ProjectsPanel());
        game.PushPanel(new ProjectPanel(app.Id).Show(ProjectPanel.Tab.Settings));
        await Shot(window, folder, "10_reglages_projet", report);
        game.ClosePanels();
        game.OpenRoot(new ProjectsPanel());
        game.PushPanel(new ProjectPanel(app.Id));
        await Shot(window, folder, "11_apercu_projet", report);
        game.ClosePanels();

        // Sauvegarde, rechargement : même état
        string state = SaveStore.SerializeState(campaign);
        game.Session.Save(SaveKind.Manual, "Après la sortie");
        var reloaded = window.Store.Load(game.Session.LastSavePath!);
        report.Add($"Rechargement identique : {SaveStore.SerializeState(reloaded) == state}");

        // Campagne avancée : plusieurs projets, une équipe, des déblocages
        var advanced = Campaign.New("Capture avancée", "Alex", "Studio Léman", FounderProfile.Generalist, 37);
        var pilot = new Autopilot(Strategies.All.First(s => s.Name == "plusieurs_petits") with { MaxStaff = 4, HireAbove = 15_000 });
        pilot.Play(advanced, 480);
        while (!GameTime.IsWorkday(advanced.Minute))
            advanced.AdvanceDays(1);
        advanced.Advance(10 * 60 + 15 - GameTime.MinuteOfDay(advanced.Minute));
        foreach (var (def, x, y) in new[] { ("plant", 15, 0), ("plant", 0, 9), ("coffee", 13, 0), ("sofa", 10, 0) })
            advanced.BuyAndPlace(def, x, y);
        report.Add($"Campagne avancée : {GameTime.FormatDate(advanced.Minute)}, {advanced.People.Count} personnes, {advanced.Premises.Name}, argent {advanced.Ledger.Cash:N0}, " +
                   $"{advanced.Products.Count(p => p.IsLive)} projets en ligne, {advanced.Unlocked.Count} nœuds, {pilot.Actions} décisions en 16 mois.");
        window.Store.Save(advanced, SaveKind.Manual, "Avancée");
        var later = window.StartGame(advanced, null);
        await Wait(1);
        later.Session.TogglePause();
        await Shot(window, folder, "12_campagne_avancee", report);
        foreach (var (name, open) in new (string, Func<Hud.GamePanel>)[]
                 {
                     ("13_portefeuille", () => new ProjectsPanel()), ("14_equipe", () => new TeamPanel()), ("15_arbre", () => new TreePanel()),
                     ("16_finances", () => new FinancePanel()), ("17_journal", () => new NoticesPanel()), ("18_amenagement", () => new BuildPanel()),
                 })
        {
            later.OpenRoot(open());
            await Shot(window, folder, name, report);
            later.ClosePanels();
        }
        if (advanced.Staff.FirstOrDefault() is { } someone)
        {
            later.OpenRoot(new TeamPanel());
            later.PushPanel(new PersonPanel(someone.Id));
            await Shot(window, folder, "19_fiche_personne", report);
            later.ClosePanels();
        }
        later.OpenRoot(new ProjectsPanel());
        await Shot(window, folder, "20_petite_fenetre", report, 960, 540);
        later.ClosePanels();
        await Shot(window, folder, "21_1080p", report, 1920, 1080);
        later.HandleKey(Avalonia.Input.Key.Escape, Avalonia.Input.KeyModifiers.None);
        await Shot(window, folder, "22_pause", report);
        later.HandleKey(Avalonia.Input.Key.Escape, Avalonia.Input.KeyModifiers.None);

        // Faillite : trois embauches sans revenus
        var doomed = Campaign.New("Capture faillite", "Alex", "Studio Perdu", FounderProfile.Designer, 3);
        var reckless = new Autopilot(Strategies.All.First(s => s.Name == "recrutement_precoce"));
        window.Store.Save(doomed, SaveKind.Manual, "Avant la chute");
        reckless.Play(doomed, 200);
        var lost = window.StartGame(doomed, null);
        await Wait(1.5);
        await Shot(window, folder, "23_faillite", report);
        report.Add($"Faillite : {doomed.Status} le {GameTime.FormatDate(doomed.Minute)}, sauvegardes conservées : {window.Store.List(doomed.Id).Count}");
        lost.Leave();

        window.ShowNewCampaign();
        await Shot(window, folder, "24_nouvelle_partie", report);
        window.ShowLoad();
        await Shot(window, folder, "25_charger", report);
        lock (Sfx.Played)
            report.Add("Sons déclenchés : " + string.Join(", ", Sfx.Played.Distinct()));
        report.Add("Sauvegardes de capture : " + window.Store.Root);
    }
}
