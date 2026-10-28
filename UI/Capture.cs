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
using UI.Game;
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
        // Les avertissements d'Avalonia (mise en page, liaisons) vont dans le dossier des captures
        System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Path.Combine(folder, "avalonia_trace.txt")));
        System.Diagnostics.Trace.AutoFlush = true;
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
        // Mise en page complète avant l'image : un panneau tout juste ouvert peut ne pas encore être disposé
        window.UpdateLayout();
        await Wait(0.2);
        window.UpdateLayout();
        var size = window.ClientSize;
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)size.Width, (int)size.Height), new Vector(96, 96));
        bitmap.Render(window);
        string path = Path.Combine(folder, name + ".png");
        bitmap.Save(path);
        report.Add($"{name}.png : {size.Width}×{size.Height}");
    }

    /// <summary>Le fondateur tape au plus « max » mots justes, à vitesse normale, sur ce projet (comme les bots).</summary>
    private static int TypeDay(Campaign campaign, Product product, int max)
    {
        int typed = 0;
        while (typed < max && campaign.OpenTerminal(product.Id).Ok)
        {
            while (typed < max && campaign.Terminal is { Finished: false } session)
            {
                campaign.SubmitWord(session.Word, session.Word.Length / 4.0);
                typed++;
            }
            campaign.CloseTerminal();
        }
        return typed;
    }

    private static async Task Script(MainWindow window, string folder, List<string> report)
    {
        report.Add($"Audio : {Sfx.LoadedSounds} sons et {Sfx.LoadedTracks} musiques chargés depuis le build.");
        report.Add($"Pack : {System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames().Count(n => n.StartsWith("pack/"))} ressources embarquées (sprites, masques, manifeste, plans).");
        await Wait(1);
        await Shot(window, folder, "01_menu_principal", report);

        // Début de partie : la chambre, aucun projet
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

        // Premier projet : le fondateur travaille au terminal, mot par mot
        var (_, app) = campaign.CreateProject("", "freelances", RevenueModel.Subscription);
        campaign.Advance(60);
        game.Session.TogglePause();
        game.OpenTerminal(app!.Id);
        await Wait(0.5);
        // Frappe par le terminal lui-même (journal des mots, barre de progression), une faute au milieu
        for (int i = 0; i < 9 && campaign.Terminal is { Finished: false } session; i++)
        {
            game.Terminal?.SetInput(i == 4 ? "zut" : session.Word);
            game.Terminal?.Submit();
            if (i == 4)
                game.Terminal?.SetInput("");
        }
        game.Terminal?.SetInput(campaign.Terminal!.Word[..3]);
        game.Terminal?.Repaint();
        await Shot(window, folder, "05_terminal", report);
        game.CloseTerminal();
        game.Session.TogglePause();
        game.OpenRoot(new ProjectsPanel());
        game.PushPanel(new ProjectPanel(app.Id));
        await Shot(window, folder, "05b_projet_en_developpement", report);
        game.ClosePanels();
        await Shot(window, folder, "06_scene_travail", report);
        int days = 0, words = 9;
        while (app.Queue.Count > 0 && days++ < 90)
        {
            words += TypeDay(campaign, app, 120);
            campaign.AdvanceDays(1);
        }
        report.Add($"Premier projet prêt en {days} jours et {words} mots au terminal (fondateur seul, sans employé).");
        game.OpenRoot(new ProjectsPanel());
        await Shot(window, folder, "07_pret_a_sortir", report);
        game.ClosePanels();
        Hud.LaunchView.Show(game, app.Id);
        await Shot(window, folder, "07b_lancement", report);
        game.CloseModal();
        var challenge = campaign.Challenge(app.Id);
        report.Add($"Mini-jeu de lancement : « {challenge.Rule} » ({challenge.Items.Count} éléments), joué parfaitement.");
        campaign.LaunchWith(app.Id, challenge.Score(challenge.Answer));
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
        // Courbes du projet (graphiques du pack), dans la section dépliable de l'aperçu
        Hud.Ui.SetDetailsOpen("Détails : tendances, courbes, concurrents", true);
        game.OpenRoot(new ProjectsPanel());
        game.PushPanel(new ProjectPanel(app.Id));
        await Shot(window, folder, "11b_courbes_projet", report);
        game.ClosePanels();
        Hud.Ui.SetDetailsOpen("Détails : tendances, courbes, concurrents", false);

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
        foreach (string def in new[] { "plant_floor_medium", "plant_floor_tall", "sofa", "whiteboard_mobile" })
            if (advanced.FindSpot(FurnitureCatalog.Get(def)) is var (x, y, f))
                advanced.BuyAndPlace(def, x, y, f);
        if (advanced.Premises.Desks.FirstOrDefault() is { } desk)
            advanced.BuyAndAttach("mug", desk.Id);
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
                     ("16_finances", () => new FinancePanel()), ("17_journal", () => new NoticesPanel()), ("17b_journal_bonnes_nouvelles", () => new NoticesPanel().ShowGoodNews()), ("18_amenagement", () => new BuildPanel()),
                 })
        {
            later.OpenRoot(open());
            await Shot(window, folder, name, report);
            later.ClosePanels();
        }
        // Une section dépliée reste ouverte malgré le rafraîchissement automatique du menu (chaque seconde)
        Hud.Ui.SetDetailsOpen("Ce qui y contribue", true);
        later.OpenRoot(new BuildPanel());
        await Wait(2.5);
        await Shot(window, folder, "18c_section_ouverte", report);
        later.ClosePanels();
        Hud.Ui.SetDetailsOpen("Ce qui y contribue", false);
        // Aménagement : poser un bureau, fantôme orienté sur une case libre
        later.OpenRoot(new BuildPanel());
        later.Session.SetTool(new BuildTool("desk_simple", Facing: "sw"));
        if (advanced.FindSpot(FurnitureCatalog.Get("desk_simple")) is var (gx, gy, _))
            later.Scene.SetHover(new Cell(gx, gy));
        await Shot(window, folder, "18b_amenagement_pose", report);
        later.Session.SetTool(null);
        later.Scene.SetHover(null);
        later.ClosePanels();
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
        later.HandleKey(Avalonia.Input.Key.F12, Avalonia.Input.KeyModifiers.None);
        await Shot(window, folder, "22b_menu_test", report);
        later.HandleKey(Avalonia.Input.Key.F12, Avalonia.Input.KeyModifiers.None);
        // Dialogue de confirmation (composition « hud_confirm_dialog » du pack)
        if (advanced.Staff.FirstOrDefault() is { } leaving)
        {
            later.Confirm($"Licencier {leaving.Name} ?", $"{leaving.Name} quittera l'entreprise à la fin de la journée.", "Licencier", () => { });
            await Shot(window, folder, "22c_confirmation", report);
            later.CloseModal();
        }

        // Lot 4 : une salle serveur en plus, deux baies équipées, le projet principal sur ses serveurs, une offre cloud
        advanced.Ledger.Cash += 150_000;
        advanced.Unlocked.UnionWith(["multisite", "server_room", "high_density", "cloud_offers"]);
        var rented = advanced.RentSite("server_room");
        string[][] racks =
        [
            ["module_patch_panel_1u", "module_network_switch_1u", "module_cable_manager_1u", "module_server_1u", "module_server_1u", "module_server_1u",
             "module_compute_2u", "module_storage_2u", "module_storage_jbod_4u", "module_ups_2u", "module_blank_1u", "module_blank_1u"],
            ["module_patch_panel_1u", "module_network_switch_1u", "module_gpu_4u", "module_server_1u", "module_server_1u", "module_blank_1u"],
        ];
        foreach (var modules in racks)
            if (advanced.FindSpot(FurnitureCatalog.Get("rack_standard")) is var (rx, ry, rf) && advanced.BuyAndPlace("rack_standard", rx, ry, rf).Ok)
            {
                var rack = advanced.Premises.Items.Last();
                foreach (string module in modules)
                    advanced.BuyAndAttach(module, rack.Id);
            }
        if (advanced.FindSpot(FurnitureCatalog.Get("ups_floor")) is var (ux, uy, uf))
            advanced.BuyAndPlace("ups_floor", ux, uy, uf);
        var main = advanced.Products.Where(p => p.IsLive).OrderBy(p => p.Priority).First();
        advanced.AdvanceDays(1);
        var hosted = advanced.SetInternalHosting(main.Id, true);
        advanced.OpenCloudOffer(CloudKind.Storage);
        advanced.AdvanceDays(20);
        var infra = advanced.SiteInfra(advanced.Premises);
        report.Add($"Salle serveur : {rented.Ok} ; hébergement {infra.Hosting:0.#}, calcul {infra.Compute:0.#}, GPU {infra.Gpu:0.#}, stockage {infra.Storage:0} To, " +
                   $"{infra.PowerDraw:0.#}/{infra.PowerSupply:0.#} kW, chaleur {infra.Heat:0.#}/{infra.Cooling:0.#} kW, problèmes : {infra.Issues.Count}. " +
                   $"{main.Name} en interne : {hosted.Ok} ({main.InternalCapacity:0.#} unités). Stockage vendu : {advanced.CloudOffers.FirstOrDefault()?.Revenue30:N0} CHF sur 30 j.");
        later.Scene.ResetCamera();
        await Wait(0.5);
        await Shot(window, folder, "26_salle_serveur", report);
        later.OpenRoot(new BuildPanel());
        await Shot(window, folder, "26b_infrastructure", report);
        later.PushPanel(new SitesPanel());
        await Shot(window, folder, "27_sites", report);
        later.ClosePanels();
        later.OpenRoot(new ProjectsPanel());
        later.PushPanel(new CloudPanel());
        await Shot(window, folder, "28_cloud", report);
        later.ClosePanels();

        // Lot 5 : un atelier, un établi et un chercheur, une ligne d'assemblage et un opérateur, une carte calibrée puis vendue
        advanced.Ledger.Cash += 120_000;
        advanced.Unlocked.UnionWith(["hardware", "online_shop"]);
        advanced.ResearchDone.Add("res_boards");
        var workshop = advanced.RentSite("hw_workshop");
        var layout = advanced.InstallLayout("assembly");
        if (advanced.FindSpot(FurnitureCatalog.Get("workbench_electronics")) is var (bx, by, bf) && advanced.BuyAndPlace("workbench_electronics", bx, by, bf).Ok)
        {
            var bench = advanced.Premises.Items.Last();
            advanced.BuyAndAttach("chair_office", bench.Id);
            advanced.BuyAndAttach("oscilloscope", bench.Id);
            advanced.BuyAndAttach("soldering_station", bench.Id);
            if (advanced.Staff.Skip(1).FirstOrDefault() is { } researcher && advanced.Premises.Workstations.FirstOrDefault(p => p.Desk.Id == bench.Id) is { } post)
            {
                advanced.Seat(researcher, post);
                advanced.AssignTo(researcher.Id, Core.team.AssignmentKind.Research);
            }
        }
        foreach (string lab in new[] { "test_bench_pc", "storage_rack_industrial" })
            if (advanced.FindSpot(FurnitureCatalog.Get(lab)) is var (lx, ly, lf))
                advanced.BuyAndPlace(lab, lx, ly, lf);
        advanced.StartResearch("res_pc");
        var (_, board) = advanced.CreateProject("", "makers", Core.products.RevenueModel.Purchase);
        while (advanced.Products.OrderBy(p => p.Priority).First().Id != board!.Id && advanced.MoveProject(board.Id, -1).Ok)
        {
        }
        for (int d = 0; d < 40 && board.Queue.Count > 0; d++)
            advanced.AdvanceDays(1);
        later.OpenRoot(new ProjectsPanel());
        later.PushPanel(new ProjectPanel(board.Id));
        Hud.LaunchView.Show(later, board.Id);
        await Shot(window, folder, "29_calibration", report);
        later.CloseModal();
        later.ClosePanels();
        var calibration = advanced.Challenge(board.Id);
        advanced.LaunchWith(board.Id, calibration.Score(calibration.Answer));
        if (advanced.Staff.FirstOrDefault() is { } op)
            advanced.AssignTo(op.Id, Core.team.AssignmentKind.Production, advanced.Premises.Id);
        foreach (var (item, qty) in Core.industry.IndustryCatalog.Bom(board))
            advanced.BuyMaterial(advanced.Premises.Id, item, qty * 30);
        advanced.SetReserve(advanced.Premises.Id, "kit", 100);
        advanced.SetChannel(board.Id, "shop", true);
        advanced.CreateOrder(advanced.Premises.Id, $"product:{board.Id}", Core.campaign.OrderMode.TargetStock, 40);
        advanced.AdvanceDays(14);
        while (!GameTime.IsWorkday(advanced.Minute))
            advanced.AdvanceDays(1);
        advanced.Advance(10 * 60 + 15 - GameTime.MinuteOfDay(advanced.Minute));
        var week = board.History.TakeLast(7).ToList();
        report.Add($"Atelier : {workshop.Ok}, ligne : {layout.Message} ; {board.Name} : {board.Produced} fabriquées, stock {advanced.TotalStock($"p:{board.Id}")}, " +
                   $"{week.Sum(d => d.Sold):0} vendues et {week.Sum(d => d.Lost):0} perdues en 7 j, pannes {board.DefectRate:P1}, " +
                   $"recherche « Validation de PC » {advanced.Research.FirstOrDefault()?.Progress:0}/{advanced.Research.FirstOrDefault()?.Def.Effort:0}.");
        later.Scene.ResetCamera();
        await Wait(0.5);
        await Shot(window, folder, "30_atelier", report);
        later.OpenRoot(new BuildPanel());
        later.PushPanel(new IndustryPanel());
        await Shot(window, folder, "31_production", report);
        later.ClosePanels();
        later.OpenRoot(new TreePanel());
        later.PushPanel(new ResearchPanel());
        await Shot(window, folder, "32_recherche", report);
        later.ClosePanels();
        later.OpenRoot(new ProjectsPanel());
        later.PushPanel(new ProjectPanel(board.Id));
        await Shot(window, folder, "33_materiel", report);
        later.ClosePanels();

        // Lot 6 : une IA entraînée puis publiée, un studio, une zone XR et une installation quantique
        advanced.Ledger.Cash += 6_000_000;
        advanced.CheatUnlockAll();
        void Place(string def)
        {
        {
            if (advanced.FindSpot(FurnitureCatalog.Get(def)) is not var (px, py, pf))
                report.Add($"Pas de place pour {def}");
            else if (advanced.BuyAndPlace(def, px, py, pf) is { Ok: false } failed)
                report.Add($"Pose de {def} refusée : {failed.Message}");
        }
        }
        var aiModel = advanced.CreateAiModel("Assistant Léman", Core.ai.AiGoal.Assistant, "small").Model;
        later.OpenRoot(new AiPanel());
        await Shot(window, folder, "34_ia_nouveau_modele", report);
        later.ClosePanels();
        if (aiModel != null)
        {
            var training = advanced.StartAiTraining(aiModel.Id, "public", external: true);
            later.OpenRoot(new AiPanel());
            await Shot(window, folder, "35_ia_entrainement", report);
            later.ClosePanels();
            advanced.AdvanceDays(8);
            Hud.ChallengeView.ShowEvaluation(later, aiModel.Id);
            await Shot(window, folder, "36_ia_evaluation", report);
            later.CloseModal();
            var evaluation = advanced.EvaluationChallenge(aiModel.Id);
            var evaluated = advanced.EvaluateAi(aiModel.Id, evaluation.Score(evaluation.Answer));
            var published = advanced.PublishAi(aiModel.Id);
            later.OpenRoot(new AiPanel());
            await Shot(window, folder, "37_ia_publiee", report);
            later.ClosePanels();
            report.Add($"IA : entraînement {training.Ok}, évaluation {evaluated.Ok}, publication {published.Ok}, version en service {aiModel.Live?.Number}, qualité {Campaign.AiQualityFor(aiModel):0.00}.");
        }
        var labSite = advanced.RentSite("advanced_lab");
        report.Add($"Laboratoire avancé loué : {labSite.Ok} {labSite.Message}");
        foreach (string def in new[] { "desk_simple", "mic_stand", "camera_tripod", "greenscreen_backdrop", "softbox_light", "softbox_light", "partition_acoustic",
                                       "xr_base_station", "xr_base_station", "xr_play_mat",
                                       "power_distribution_cabinet", "crac_unit", "quantum_cryostat_closed", "quantum_control_rack", "cryo_support_unit" })
            Place(def);
        if (advanced.Premises.Items.FirstOrDefault(i => i.DefId == "desk_simple") is { } labDesk)
            foreach (string def in new[] { "studio_monitors", "xr_headset_stand" })
                if (advanced.BuyAndAttach(def, labDesk.Id) is { Ok: false } refused)
                    report.Add($"Pose de {def} sur le bureau refusée : {refused.Message}");
        advanced.AdvanceDays(1);
        var studio = advanced.StudioOf(advanced.Premises);
        var xr = advanced.XrOf(advanced.Premises);
        report.Add($"Studio : score {studio.Score:0.00}, bonus {studio.Bonus:P0}, manque {string.Join(", ", studio.Missing)} ; XR : capacité {xr.Capacity:0.0}, défauts retirés {xr.DefectCut:P0}, manque {string.Join(", ", xr.Missing)} ; " +
                   $"unités quantiques prises en charge {advanced.SiteInfra(advanced.Premises).QuantumUnits}.");
        later.Scene.ResetCamera();
        await Wait(0.5);
        await Shot(window, folder, "38_scene_studio_xr", report);
        later.OpenRoot(new LabPanel());
        await Shot(window, folder, "39_laboratoire", report);
        if (advanced.QuantumSites.FirstOrDefault() is { } quantum)
        {
            Hud.ChallengeView.ShowQuantum(later, quantum.SiteId);
            await Shot(window, folder, "40_quantique_signaux", report);
            later.CloseModal();
        }
        later.ClosePanels();

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
        window.ShowSettings(() => window.ShowTitle());
        await Shot(window, folder, "25b_reglages", report);
        lock (Sfx.Played)
            report.Add("Sons déclenchés : " + string.Join(", ", Sfx.Played.Distinct()));
        report.Add("Sauvegardes de capture : " + window.Store.Root);
    }
}
