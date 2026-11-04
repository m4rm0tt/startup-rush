using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Core.ai;
using Core.campaign;
using Core.industry;
using Core.products;
using Core.progress;
using Core.team;
using SimRandom = Core.campaign.SimRandom;
using UI;
using UI.Hud;
using UI.Hud.Panels;

namespace UiTests;

/// <summary>
/// Couverture de la traduction anglaise de la zone « produits » (projets, composants, publics, IA, recherche, laboratoire,
/// cloud, industrie, développement, lancements et mini-jeux). Les catalogues réels sont parcourus, les messages du moteur
/// sont déclenchés par de vraies commandes, et les panneaux sont ouverts : aucun texte d'apparence française ne doit rester.
/// </summary>
public class EnglishCoverage_ProduitsTest
{
    private string _previous = "fr";

    [SetUp]
    public void SetUp()
    {
        _previous = Loc.Language;
        Loc.Language = "en";
    }

    [TearDown]
    public void TearDown()
    {
        Loc.Language = _previous;
        Loc.RecordMisses = false;
        Loc.Misses.Clear();
    }

    /// <summary>Des noms qui s'écrivent pareil dans les deux langues (le test les accepte tels quels).</summary>
    private static readonly HashSet<string> SameInEnglish = ["Gameplay", "Immersion", "Cluster", "Cloud S", "Cloud M", "Cloud L", "Makers", "Options", "Score", "Support", "Commission", "Bugs", "Cloud", "Distributors", "Assistant", "Infrastructure", "INFRASTRUCTURE", "Innovation", "INNOVATION", "Datacenter", "Macros", "XR", "placement", "inspection", "Notifications", "Exports"];

    private static void AssertTranslated(IEnumerable<string> texts, string what)
    {
        var all = texts.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
        var untouched = all.Where(t => Loc.Translate(t) == t && !SameInEnglish.Contains(t)).ToList();
        Assert.That(untouched, Is.Empty, $"{what} : textes restés en français");
        var french = all.Select(Loc.Translate).Where(Loc.LooksFrench).ToList();
        Assert.That(french, Is.Empty, $"{what} : traductions d'apparence française");
    }

    private static readonly string[] FurnitureNames = Core.space.FurnitureCatalog.All.Select(f => f.Name).OrderByDescending(n => n.Length).ToArray();

    /// <summary>Un texte sans les noms d'objets du mobilier (traduits par la zone « aménagement »).</summary>
    private static string WithoutFurniture(string text) => FurnitureNames.Aggregate(text, (t, name) => t.Replace(name, "#"));

    /// <summary>Les messages d'une liste : traduits, ou déjà sans apparence française.</summary>
    private static void AssertNoFrench(IEnumerable<string> texts, string what, IEnumerable<string>? names = null)
    {
        // Les noms propres de la partie (sites, personnes, projets) restent tels quels : on les masque
        var masks = (names ?? []).Where(n => n.Length > 0).OrderByDescending(n => n.Length).ToList();
        string Mask(string t) => masks.Aggregate(t, (x, n) => x.Replace(n, "#"));
        var left = texts.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().Where(t => Loc.LooksFrench(Mask(Loc.Translate(t)))).ToList();
        Assert.That(left, Is.Empty, $"{what} : textes restés en français :\n" + string.Join("\n", left));
    }

    private static IEnumerable<string> GameNames(Campaign campaign) =>
        campaign.Sites.Select(s => s.Name).Concat(campaign.Products.Select(p => p.Name)).Concat(campaign.Staff.Select(p => p.Name)).Append(campaign.Founder.Name)
            .Concat(campaign.AiModels.Select(m => m.Name)).Concat(campaign.Competitors.Select(c => c.Name));

    /// <summary>Une partie avancée sans fenêtre : les mêmes projets que PanelsTest.Advanced().</summary>
    private static Campaign NewCampaign()
    {
        var campaign = Campaign.New("Tests produits", "Alex", "Studio Test", FounderProfile.Generalist, 42);
        campaign.Ledger.Cash = 3_000_000;
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
        return campaign;
    }

    // ------------------------------------------------------------------ Catalogues

    [Test]
    public void EveryCatalogEntryIsTranslated()
    {
        var texts = new List<string>();
        foreach (var type in ProductCatalog.Types)
            texts.AddRange([type.Name, type.Pitch]);
        foreach (var segment in ProductCatalog.Segments)
            texts.AddRange([segment.Name, segment.Pitch]);
        foreach (var component in ProductCatalog.Components)
            texts.AddRange([component.Name, component.Role]);
        foreach (var channel in ProductCatalog.Channels)
            texts.AddRange([channel.Name, channel.Pitch]);
        foreach (var plan in ProductCatalog.Hosting)
            texts.Add(plan.Name);
        foreach (var need in Enum.GetValues<Need>())
            texts.Add(ProductCatalog.NeedName(need));
        foreach (var model in Enum.GetValues<RevenueModel>())
            texts.Add(ProductCatalog.ModelName(model));
        foreach (string approach in Approaches.All)
            texts.AddRange([Approaches.Name(approach), Approaches.Pitch(approach)]);
        AssertTranslated(texts, "produits");

        var ai = new List<string>();
        foreach (var b in AiCatalog.Bases)
            ai.AddRange([b.Name, b.Pitch]);
        foreach (var d in AiCatalog.Datasets)
            ai.AddRange([d.Name, d.Pitch, d.Name.ToLowerInvariant()]);
        foreach (var g in AiCatalog.Goals)
            ai.AddRange([g.Name, g.Pitch]);
        AssertTranslated(ai, "IA");

        var dev = new List<string>(DevTree.Branches.Select(b => b.ToUpperInvariant()));
        dev.AddRange(DevTree.Branches);
        foreach (var node in DevTree.Nodes)
            dev.AddRange([node.Name, node.Effect]);
        AssertTranslated(dev, "arbre de développement");

        var industry = new List<string>();
        foreach (var m in IndustryCatalog.Materials)
            industry.Add(m.Name);
        foreach (var l in IndustryCatalog.Layouts)
            industry.AddRange([l.Name, l.Description]);
        foreach (var r in IndustryCatalog.Routes)
            industry.Add(r.Name);
        foreach (var c in IndustryCatalog.Channels)
            industry.AddRange([c.Name, c.Pitch]);
        foreach (var s in Enum.GetValues<Step>())
            industry.Add(IndustryCatalog.StepName(s));
        AssertTranslated(industry, "industrie");

        var research = new List<string>();
        foreach (var r in Campaign.ResearchCatalog)
            research.AddRange([r.Name, r.Field, r.Unlocks]);
        AssertTranslated(research, "recherches");

        var cloud = new List<string>();
        foreach (var kind in Enum.GetValues<CloudKind>())
        {
            var info = Campaign.CloudInfo(kind);
            cloud.AddRange([info.Name, info.Unit, Campaign.Units(kind, 1), Campaign.Units(kind, 5)]);
        }
        AssertTranslated(cloud, "cloud");
    }

    [Test]
    public void ListsBuiltFromCatalogNamesAreTranslatedPieceByPiece()
    {
        // Listes assemblées par le moteur : « A, B niveau 2 », « A ou B, C »… (les panneaux traduisent leurs morceaux avant de les assembler)
        var staged = ProductCatalog.Components.Take(3).Select(c => c.Name).ToList();
        string list = $"{staged[0]}, {staged[1]} niveau 2, {staged[2]}";
        string translated = Loc.Translate($"Prête à publier : {list}.");
        Assert.That(Loc.LooksFrench(translated), Is.False, translated);
        translated = Loc.Translate($"Version majeure : {list}.");
        Assert.That(Loc.LooksFrench(translated), Is.False, translated);
        translated = Loc.Translate($"Prêt : {staged[0]}, {staged[1]}. Encore 2 évolution(s) en file.");
        Assert.That(Loc.LooksFrench(translated), Is.False, translated);
        translated = Loc.Translate($"{staged[0]}, {staged[1]}.");
        Assert.That(Loc.LooksFrench(translated), Is.False, translated);
        translated = Loc.Translate($"Lea : {staged[0]} · Tom : {staged[1]}");
        Assert.That(Loc.LooksFrench(translated), Is.False, translated);

        // Les équipements manquants d'une recherche, tels que le moteur les assemble (groupes « A ou B », séparés par une virgule)
        var campaign = NewCampaign();
        foreach (var def in Campaign.ResearchCatalog)
        {
            var missing = Campaign.MissingEquipment(campaign.Premises, def);
            if (missing.Count == 0)
                continue;
            translated = WithoutFurniture(Loc.Translate($"Il manque : {string.Join(", ", missing)}"));
            Assert.That(Loc.LooksFrench(translated), Is.False, translated);
            translated = WithoutFurniture(Loc.Translate($"Recherche lancée : {def.Name}. Il manque sur tes sites : {string.Join(", ", missing)}."));
            Assert.That(Loc.LooksFrench(translated), Is.False, translated);
            string required = "Équipement : " + string.Join(" + ", def.Equipment.Select(g => string.Join(" ou ", g.Select(id => Core.space.FurnitureCatalog.Get(id).Name))));
            translated = WithoutFurniture(Loc.Translate(required));
            Assert.That(Loc.LooksFrench(translated), Is.False, translated);
            Assert.That(translated, Does.Not.Contain(" ou "), translated);
        }

        // Les manques du studio et de la zone XR
        foreach (var site in campaign.Sites)
        {
            translated = Loc.Translate($"Il manque : {string.Join(", ", campaign.StudioOf(site).Missing.Select(Loc.Translate))}.");
            Assert.That(Loc.LooksFrench(translated), Is.False, translated);
            translated = Loc.Translate($"Il manque : {string.Join(", ", campaign.XrOf(site).Missing.Select(Loc.Translate))}. Un casque seul sert au prototype (40 %).");
            Assert.That(Loc.LooksFrench(translated), Is.False, translated);
        }
    }

    // ------------------------------------------------------------------ Messages du moteur

    [Test]
    public void EngineMessagesAndNotificationsAreTranslated()
    {
        var campaign = NewCampaign();
        var messages = new List<string>();
        void Say(CommandResult result) => messages.Add(result.Message);
        int before = campaign.Notices.Count;

        var app = campaign.Products.First(p => p.ArchetypeId == "saas");
        var game = campaign.Products.First(p => p.ArchetypeId == "game");
        var board = campaign.Products.First(p => p.IsHardware);

        // Projets, composants, file, lancement
        Say(campaign.QueueComponent(app.Id, "ui", "inconnu"));
        Say(campaign.QueueComponent(9999, "ui"));
        Say(campaign.QueueComponent(app.Id, "gameplay"));
        Say(campaign.QueueComponent(app.Id, "ai_core"));
        Say(campaign.QueueComponent(app.Id, "dashboard"));
        Say(campaign.QueueComponent(app.Id, "ui"));
        Say(campaign.QueueComponent(app.Id, "ui"));
        Say(campaign.QueueComponent(game.Id, "multiplayer"));
        Say(campaign.CancelQueued(app.Id, 9999));
        Say(campaign.MoveQueued(app.Id, app.Queue[0].Id, -1));
        Say(campaign.MoveQueued(app.Id, app.Queue[^1].Id, 1));
        Say(campaign.CancelQueued(app.Id, app.Queue[^1].Id));
        Say(campaign.Launch(app.Id));
        Say(campaign.SetPrice(app.Id, -1));
        Say(campaign.SetMarketing(app.Id, -1));
        Say(campaign.Rename(app.Id, ""));
        Say(campaign.Retire(9999));
        Say(campaign.SetChannel(9999, "shop", true));
        Say(campaign.PublishRelease(app.Id, 0));
        var (fresh, product) = campaign.CreateProject("Neuf", "freelances", RevenueModel.Subscription);
        Say(fresh);
        Say(campaign.Launch(product!.Id));
        Say(campaign.QueueComponent(product.Id, "login"));
        Say(campaign.PublishRelease(product.Id, 0));
        campaign.CheatFinishWork();
        Say(campaign.SetHosting(product.Id, true, 100, "cloud_l"));
        Say(campaign.CreateProject("IA", "ai_assist", RevenueModel.Subscription).Result);

        // IA : modèles, entraînement, évaluation, publication
        Say(campaign.StartAiTraining(9999, "public", true));
        Say(campaign.EvaluateAi(9999, 0.5));
        Say(campaign.PublishAi(9999));
        Say(campaign.CancelAiTraining(9999));
        Say(campaign.DiscardAiCandidate(9999));
        Say(campaign.DelegateEvaluation(9999));
        Say(campaign.SetProductAiModel(app.Id, 9999));
        var (created, model) = campaign.CreateAiModel("", AiGoal.Assistant, "small");
        Say(created);
        Say(campaign.CreateAiModel("Essai", AiGoal.Assistant, "large").Result);
        Say(campaign.StartAiTraining(model!.Id, "curated", true));
        Say(campaign.StartAiTraining(model.Id, "public", true));
        Say(campaign.StartAiTraining(model.Id, "public", true));
        Say(campaign.PublishAi(model.Id));
        Say(campaign.DiscardAiCandidate(model.Id));
        campaign.AdvanceDays(40);
        Say(campaign.EvaluateAi(model.Id, 0.7));
        Say(campaign.EvaluateAi(model.Id, 0.7));
        Say(campaign.DelegateEvaluation(model.Id));
        Say(campaign.PublishAi(model.Id));
        Say(campaign.SetProductAiModel(app.Id, model.Id));
        Say(campaign.SetInference(app.Id, true, 100));
        campaign.AdvanceDays(3);

        // Recherche, laboratoire, cloud
        Say(campaign.StartResearch("res_boards"));
        Say(campaign.StartResearch("res_boards"));
        Say(campaign.StartResearch("res_laptop"));
        Say(campaign.PauseResearch("inconnue", true));
        Say(campaign.PauseResearch("res_boards", true));
        Say(campaign.PauseResearch("res_boards", false));
        Say(campaign.AssignTo(campaign.Founder.Id, AssignmentKind.Research, campaign.Premises.Id));
        Say(campaign.StabilizeQuantum(campaign.Premises.Id, 0.5));
        foreach (var kind in Enum.GetValues<CloudKind>())
        {
            Say(campaign.OpenCloudOffer(kind));
            Say(campaign.OpenCloudOffer(kind));
        }
        Say(campaign.SetCloudOffer(9999, 10, 1));
        foreach (var offer in campaign.CloudOffers.ToList())
        {
            Say(campaign.SetCloudOffer(offer.Id, offer.Price, 1_000_000));
            Say(campaign.SetCloudOffer(offer.Id, offer.Price, 1));
        }

        // Industrie : achats, réserves, transferts, ordres, lignes
        var other = campaign.Sites.First(s => s.Id != campaign.Premises.Id);
        Say(campaign.BuyMaterial(campaign.Premises.Id, "kit", 0));
        Say(campaign.BuyMaterial(campaign.Premises.Id, "kit", 100_000));
        Say(campaign.BuyMaterial(campaign.Premises.Id, "kit", 10));
        Say(campaign.SetReserve(campaign.Premises.Id, "kit", 20));
        Say(campaign.SetReserve(9999, "kit", 20));
        Say(campaign.SetProcurementCap(1_000));
        Say(campaign.TransferStock(campaign.Premises.Id, campaign.Premises.Id, "kit", 1));
        Say(campaign.TransferStock(campaign.Premises.Id, other.Id, "kit", 0));
        Say(campaign.TransferStock(campaign.Premises.Id, other.Id, "kit", 10_000));
        Say(campaign.TransferStock(campaign.Premises.Id, other.Id, "kit", 2));
        Say(campaign.CreateOrder(9999, "board", OrderMode.Quantity, 5));
        Say(campaign.CreateOrder(campaign.Premises.Id, "inconnue", OrderMode.Quantity, 5));
        Say(campaign.CreateOrder(campaign.Premises.Id, "board", OrderMode.Quantity, 5));
        Say(campaign.CreateOrder(campaign.Premises.Id, $"product:{board.Id}", OrderMode.Quantity, 5));
        Say(campaign.CreateOrder(campaign.Premises.Id, $"product:{board.Id}", OrderMode.Continuous, 5));
        foreach (var order in campaign.Orders.ToList())
        {
            Say(campaign.SupplyOrder(order.Id));
            Say(campaign.SetOrder(order.Id, true));
            Say(campaign.SetOrder(order.Id, false));
        }
        Say(campaign.SupplyOrder(9999));
        Say(campaign.SetOrder(9999, true));
        Say(campaign.CancelOrder(9999));
        foreach (var layout in IndustryCatalog.Layouts)
            Say(campaign.InstallLayout(layout.Id));
        Say(campaign.InstallSuggestedLine());
        Say(campaign.SetChannel(board.Id, "shop", true));
        campaign.AdvanceDays(60);
        foreach (var order in campaign.Orders.ToList())
            Say(campaign.CancelOrder(order.Id));

        // Délégation et lancement
        Say(campaign.DelegateLaunch(9999));
        Say(campaign.DelegateLaunch(product.Id));
        Say(campaign.DelegateLaunch(app.Id));
        Say(campaign.LaunchWith(product.Id, 0.5));
        campaign.AdvanceDays(20);

        AssertNoFrench(messages, "messages des commandes", GameNames(campaign));

        // Notifications créées par ces actions et par les jours qui ont passé
        var notices = campaign.Notices.Skip(before).Where(n => n.Category is "cloud" or "ia" or "labo" or "projet" or "recherche" or "production").SelectMany(n => new[] { n.Title, n.Detail }).ToList();
        Assert.That(notices, Is.Not.Empty);
        AssertNoFrench(notices, "notifications", GameNames(campaign));

        // Les statuts des stations de la ligne
        AssertNoFrench(campaign.Flow.Values.Select(f => f.Text), "statuts des stations", GameNames(campaign));
    }

    [Test]
    public void MiniGamesAreTranslated()
    {
        var campaign = NewCampaign();
        var challenges = new List<LaunchChallenge>();
        foreach (var product in campaign.Products)
            challenges.Add(campaign.Challenge(product.Id));
        var random = new SimRandom(7);
        var board = campaign.Products.First(p => p.IsHardware);
        var app = campaign.Products.First(p => p.ArchetypeId == "saas");
        for (int difficulty = 0; difficulty < 4; difficulty++)
        {
            challenges.Add(Core.campaign.MiniGames.Logs(random, difficulty));
            challenges.Add(Core.campaign.MiniGames.Firewall(random, difficulty));
            challenges.Add(Core.campaign.MiniGames.Merge(random, difficulty));
            challenges.Add(Core.campaign.MiniGames.Command(random, app, 2, difficulty));
        }
        // Le déploiement et la recette (privés au moteur), la calibration, l'évaluation d'un modèle d'IA et la mise en route quantique
        foreach (string name in new[] { "Deploy", "Review" })
            challenges.Add((LaunchChallenge)typeof(Campaign).GetMethod(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.Invoke(null, [random])!);
        challenges.Add(campaign.Challenge(board.Id));
        var model = campaign.CreateAiModel("Essai", AiGoal.Assistant, "small").Model!;
        challenges.Add(campaign.EvaluationChallenge(model.Id));
        challenges.Add(campaign.QuantumChallenge(campaign.Premises.Id));

        var texts = new List<string>();
        foreach (var challenge in challenges)
        {
            texts.Add(challenge.Rule);
            texts.AddRange(challenge.Options);
            // Les lignes des journaux sont affichées avec leur case : « [ ] ligne », « [x] ligne »
            texts.AddRange(challenge.Items.Select(i => challenge.Kind == "logs" ? $"[ ] {i}" : i));
        }
        foreach (string kind in new[] { "logs", "firewall", "merge", "command", "deploy", "review", "calibrate", "match", "signals" })
            texts.Add(Core.campaign.MiniGames.Name(kind));
        AssertNoFrench(texts, "mini-jeux");
        Assert.That(Loc.Translate("[ ] [ERREUR] connexion à la base refusée"), Is.EqualTo("[ ] [ERROR] database connection refused"));
        Assert.That(Loc.Translate("[x] [OK] cache vidé"), Is.EqualTo("[x] [OK] cache cleared"));
    }

    // ------------------------------------------------------------------ Panneaux et écrans

    [AvaloniaTest]
    public void PanelsAndViewsShowNoUntranslatedFrench()
    {
        var (window, game, campaign) = PanelsTest.Advanced();
        Loc.Language = "en";   // la fenêtre applique la langue des réglages (français) en se créant
        Dispatcher.UIThread.RunJobs();
        // Ce que l'écran de jeu affiche déjà (barres, menus d'autres zones) est mis en cache : seuls les textes nouveaux sont relevés
        Loc.Misses.Clear();
        Loc.RecordMisses = true;

        var app = campaign.Products.First(p => p.ArchetypeId == "saas");
        var arcade = campaign.Products.First(p => p.ArchetypeId == "game");
        var board = campaign.Products.First(p => p.IsHardware);

        // Un projet de chaque type, des files, une nouvelle version, un modèle d'IA publié
        foreach (var type in ProductCatalog.Types.Where(t => campaign.TypeAvailable(t.Id)))
        {
            var segment = ProductCatalog.Segments.First(s => s.TypeId == type.Id);
            var model = type.Models[0];
            campaign.CreateProject($"Essai {type.Id}", segment.Id, model);
        }
        var aiModel = campaign.CreateAiModel("Modèle essai", AiGoal.Assistant, "small").Model!;
        campaign.StartAiTraining(aiModel.Id, "public", true);
        campaign.QueueComponent(app.Id, "dashboard");
        campaign.QueueComponent(app.Id, "ui");
        campaign.CheatFinishWork();
        campaign.QueueComponent(app.Id, "login");
        campaign.StartResearch("res_boards");
        campaign.OpenCloudOffer(CloudKind.Hosting);
        campaign.OpenCloudOffer(CloudKind.Storage);
        campaign.CreateOrder(campaign.Premises.Id, $"product:{board.Id}", OrderMode.Quantity, 5);
        campaign.AdvanceDays(45);

        var panels = new List<GamePanel>
        {
            new ProjectsPanel(), new NewProjectPanel(), new NewProjectPanel("ai"), new NewProjectPanel("board"), new TreePanel(), new CloudPanel(),
            new IndustryPanel(), new ResearchPanel(), new AiPanel(), new LabPanel(),
        };
        foreach (var product in campaign.Products.ToList())
            foreach (var tab in Enum.GetValues<ProjectPanel.Tab>())
                panels.Add(new ProjectPanel(product.Id).Show(tab));
        // Les infobulles ne sont affichées qu'au survol : on les relève sur les contrôles (celles de l'écran de jeu d'avant sont écartées)
        var tips = new HashSet<string>();
        var knownTips = window.GetVisualDescendants().OfType<Control>().Select(c => ToolTip.GetTip(c) as string).Where(t => t != null).ToHashSet();
        void HarvestTips() => tips.UnionWith(window.GetVisualDescendants().OfType<Control>().Select(c => ToolTip.GetTip(c) as string).Where(t => t != null && !knownTips.Contains(t))!);
        foreach (var panel in panels)
        {
            game.OpenRoot(panel);
            Dispatcher.UIThread.RunJobs();
            HarvestTips();
        }
        // Deuxième état : le modèle d'IA est évalué, publié, branché sur un produit, et une nouvelle version s'entraîne
        campaign.EvaluateAi(aiModel.Id, 0.8);
        campaign.PublishAi(aiModel.Id);
        foreach (var aiProduct in campaign.Products.Where(p => p.ArchetypeId == "ai" || p.Level("ai_feature") > 0 || p.Level("ai_core") > 0).ToList())
            campaign.SetProductAiModel(aiProduct.Id, aiModel.Id);
        campaign.StartAiTraining(aiModel.Id, "public", true);
        campaign.AdvanceDays(3);
        var second = new List<GamePanel> { new AiPanel(), new ProjectsPanel(), new IndustryPanel(), new CloudPanel(), new ResearchPanel(), new LabPanel() };
        foreach (var product in campaign.Products.ToList())
            foreach (var tab in Enum.GetValues<ProjectPanel.Tab>())
                second.Add(new ProjectPanel(product.Id).Show(tab));
        foreach (var panel in second)
        {
            game.OpenRoot(panel);
            Dispatcher.UIThread.RunJobs();
            HarvestTips();
        }

        // La preuve que la traduction s'applique à l'écran : le panneau d'un projet est en anglais
        game.OpenRoot(new ProjectPanel(app.Id));
        Dispatcher.UIThread.RunJobs();
        var shown = window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
        Assert.That(shown, Has.Some.EqualTo("Overview"), string.Join(" / ", shown.Take(80)));
        Assert.That(shown, Has.Some.EqualTo("Settings"));

        // Le détail d'un nœud de l'arbre, les onglets de la production, les écrans de lancement et d'évaluation
        var selected = typeof(TreePanel).GetField("_selected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        foreach (var node in DevTree.Nodes)
        {
            var tree = new TreePanel();
            selected.SetValue(tree, node.Id);
            game.OpenRoot(tree);
            Dispatcher.UIThread.RunJobs();
        }
        foreach (var product in campaign.Products.ToList())
        {
            if (!product.IsLive)
                LaunchView.Show(game, product.Id);
            else
                LaunchView.ShowRelease(game, product.Id);
            Dispatcher.UIThread.RunJobs();
            game.CloseModal();
        }
        ChallengeView.ShowEvaluation(game, aiModel.Id);
        Dispatcher.UIThread.RunJobs();
        game.CloseModal();
        ChallengeView.ShowQuantum(game, campaign.Premises.Id);
        Dispatcher.UIThread.RunJobs();
        game.CloseModal();

        HarvestTips();
        // Les jalons (Campaign.Daily), le terminal (Campaign.Terminal), les gains de points (Campaign.cs) et les noms de lieux relèvent d'autres zones
        var foreign = GameNames(campaign).Concat(Campaign.MilestoneDefs.SelectMany(m => new[] { m.Name, m.Description })).Append("Rien en file sur ce projet : ajoute ou améliore un composant.").ToHashSet();
        var misses = Loc.Misses.Keys.Where(k => !foreign.Contains(k) && !k.EndsWith("de développement")).OrderBy(k => k).ToList();
        Loc.RecordMisses = false;
        var names = GameNames(campaign).ToList();
        window.Close();
        Assert.That(misses, Is.Empty, "textes d'apparence française affichés sans traduction :\n" + string.Join("\n", misses));
        Assert.That(tips, Is.Not.Empty);
        AssertNoFrench(tips.Where(t => !t.StartsWith("Argent ")), "infobulles", names);   // « Argent… » : la barre du haut (autre zone)
    }
}
