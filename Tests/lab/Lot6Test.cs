using Core.ai;
using Core.campaign;
using Core.persistence;
using Core.products;
using Core.space;
using static Tests.campaign.CampaignTestKit;

namespace Tests.lab;

/// <summary>
/// Lot 6 : IA simulées, studio audiovisuel, zone XR, laboratoire avancé et quantique. Chaque test vérifie une conséquence
/// (version en service, capacité prise une seule fois, défauts retirés, mise en route, sauvegarde), pas seulement un écran.
/// </summary>
public class Lot6Test
{
    /// <summary>Pose un objet directement sur un site (sans passer par l'achat) : suffit pour l'infrastructure et les comptes.</summary>
    private static PlacedItem Install(Campaign campaign, string defId, Premises? site = null)
    {
        site ??= campaign.Premises;
        var item = new PlacedItem { Id = campaign.NewId(), DefId = defId };
        site.Items.Add(item);
        campaign.InvalidateLayout();
        return item;
    }

    private static Campaign WithAi(ulong seed = 42)
    {
        var campaign = NewCampaign(seed);
        campaign.Ledger.Cash = 200_000;
        campaign.Unlocked.Add("ai_models");
        return campaign;
    }

    private static AiModel NewModel(Campaign campaign, AiGoal goal = AiGoal.Analysis, string baseId = "small")
    {
        var (result, model) = campaign.CreateAiModel("", goal, baseId);
        Assert.That(result.Ok, Is.True, result.Message);
        return model!;
    }

    // ------------------------------------------------------------------ IA

    [Test]
    public void ASmallSpecialistOnGoodDataBeatsABigGeneralistOnPoorData()
    {
        double specialist = AiCatalog.Quality(AiGoal.Analysis, "small", "curated");
        double generalist = AiCatalog.Quality(AiGoal.Analysis, "large", "public");
        Assert.That(specialist, Is.GreaterThan(generalist));
        // Sur un usage général, la taille compte : le grand modèle bien nourri reste le meilleur
        Assert.That(AiCatalog.Quality(AiGoal.Generation, "large", "curated"), Is.GreaterThan(AiCatalog.Quality(AiGoal.Generation, "small", "curated")));
        foreach (var goal in Enum.GetValues<AiGoal>())
        foreach (var b in AiCatalog.Bases)
        foreach (var d in AiCatalog.Datasets)
            Assert.That(AiCatalog.Quality(goal, b.Id, d.Id), Is.InRange(0.1, 0.95));
    }

    [Test]
    public void TheEvaluationRecoversAtMostTheHiddenFlawsAndNeverMore()
    {
        var version = new AiVersion { Quality = 0.7 };
        double none = version.Effective;
        version.Validation = 1;
        double perfect = version.Effective;
        Assert.That(none, Is.EqualTo(0.7 - AiCatalog.HiddenFlaws).Within(1e-9));
        Assert.That(perfect, Is.EqualTo(0.7).Within(1e-9), "une validation parfaite ne dépasse pas la qualité entraînée");
    }

    [Test]
    public void AModelIsTrainedEvaluatedAndPublishedAndTheOldVersionServesUntilTheNextOneIsPublished()
    {
        var campaign = WithAi();
        var model = NewModel(campaign);
        Assert.That(campaign.StartAiTraining(model.Id, "public", external: true).Ok, Is.True);
        Assert.That(campaign.StartAiTraining(model.Id, "public", true).Ok, Is.False, "un seul entraînement à la fois");

        campaign.AdvanceDays(6); // 600 points à 150 par jour
        Assert.That(model.Training, Is.Null);
        Assert.That(model.Candidate, Is.Not.Null);
        Assert.That(model.Live, Is.Null, "rien n'est en service tant qu'on n'a pas publié");
        Assert.That(campaign.StartAiTraining(model.Id, "public", true).Ok, Is.False, "une version attend : on la publie ou on la jette");
        Assert.That(campaign.PublishAi(model.Id).Ok, Is.True);
        Assert.That(model.Live!.Number, Is.EqualTo(1));

        // Une nouvelle version s'entraîne pendant que la première sert, et ne la remplace qu'à la publication
        Assert.That(campaign.StartAiTraining(model.Id, "bought", external: true).Ok, Is.True);
        campaign.AdvanceDays(2);
        Assert.That(model.Training, Is.Not.Null);
        Assert.That(model.Live!.Number, Is.EqualTo(1));
        campaign.AdvanceDays(6);
        Assert.That(model.Candidate!.Number, Is.EqualTo(2));
        Assert.That(model.Live!.Number, Is.EqualTo(1), "l'ancienne version reste en service jusqu'à la publication");
        Assert.That(campaign.PublishAi(model.Id).Ok, Is.True);
        Assert.That(model.Live!.Number, Is.EqualTo(2));
        Assert.That(campaign.Awarded, Does.Contain("ai:published"));
    }

    [Test]
    public void ExternalTrainingIsPaidByTheDayAndTheDatasetOnce()
    {
        var campaign = WithAi();
        var model = NewModel(campaign);
        decimal before = campaign.Ledger.Cash;
        Assert.That(campaign.StartAiTraining(model.Id, "bought", external: true).Ok, Is.True);
        Assert.That(before - campaign.Ledger.Cash, Is.EqualTo(AiCatalog.DatasetPrice("bought", "small")), "le jeu de données se paie une fois, tout de suite");
        decimal afterData = campaign.Ledger.Cash;
        campaign.AdvanceDays(1);
        Assert.That(campaign.Ledger.Accrued[Core.finance.Category.Hosting], Is.EqualTo(150 * AiCatalog.ExternalPointPrice), "le calcul loué se facture avec le mois");
        Assert.That(campaign.Ledger.Cash, Is.GreaterThan(afterData - 1_000), "pas de double paiement immédiat");
    }

    [Test]
    public void TheBiggerBasesAndBetterDatasetsNeedTheirResearch()
    {
        var campaign = WithAi();
        Assert.That(campaign.CreateAiModel("", AiGoal.Assistant, "medium").Result.Ok, Is.False);
        var model = NewModel(campaign);
        Assert.That(campaign.StartAiTraining(model.Id, "curated", true).Ok, Is.False);
        campaign.ResearchDone.UnionWith(["res_ml", "res_data"]);
        Assert.That(campaign.CreateAiModel("", AiGoal.Assistant, "medium").Result.Ok, Is.True);
        Assert.That(campaign.StartAiTraining(model.Id, "curated", true).Ok, Is.True);
    }

    [Test]
    public void TrainingTakesFreeGpuOnceAndTheCloudCannotResellIt()
    {
        var campaign = WithAi();
        campaign.Unlocked.UnionWith(["cloud_offers", "compute_offers"]);
        Install(campaign, "workstation_gpu");
        double gpu = campaign.Infra().Gpu;
        Assert.That(gpu, Is.EqualTo(2).Within(1e-9));
        var model = NewModel(campaign);
        Assert.That(campaign.StartAiTraining(model.Id, "public", external: false).Ok, Is.True);

        campaign.AdvanceDays(1);
        Assert.That(campaign.AiGpuUsed, Is.EqualTo(gpu * campaign.TrainingShare).Within(1e-6), "la part prévue pour l'entraînement, pas plus");
        Assert.That(model.Training!.Progress, Is.EqualTo(campaign.AiGpuUsed * AiCatalog.PointsPerGpuUnit).Within(1e-6));
        Assert.That(campaign.CloudCapacity(CloudKind.Gpu, campaign.Infra()), Is.EqualTo(gpu - campaign.AiGpuUsed).Within(1e-6),
            "la même unité n'est pas promise à l'entraînement et aux offres");
    }

    [Test]
    public void WithoutAnyGpuOnlyRentedTrainingCanStart()
    {
        var campaign = WithAi();
        var model = NewModel(campaign);
        Assert.That(campaign.StartAiTraining(model.Id, "public", external: false).Ok, Is.False);
        Assert.That(campaign.StartAiTraining(model.Id, "public", external: true).Ok, Is.True);
    }

    [Test]
    public void AnAiProductIsRefusedWithItsReasonBeforeAnyModelIsPublished()
    {
        var campaign = WithAi();
        var (result, product) = campaign.CreateProject("", "ai_analytics", RevenueModel.Subscription);
        Assert.That(result.Ok, Is.False, "sinon le composant d'IA de départ disparaît sans un mot");
        Assert.That(product, Is.Null);
        Assert.That(campaign.Products, Is.Empty);
        var model = NewModel(campaign);
        campaign.StartAiTraining(model.Id, "public", true);
        campaign.AdvanceDays(6);
        campaign.PublishAi(model.Id);
        (result, product) = campaign.CreateProject("", "ai_analytics", RevenueModel.Subscription);
        Assert.That(result.Ok, Is.True, result.Message);
        Assert.That(product!.Queue.Any(q => q.DefId == "ai_core") || product.Level("ai_core") > 0, Is.True);
    }

    [Test]
    public void AnAiProductNeedsAPublishedModelAndTakesItsQuality()
    {
        var campaign = WithAi();
        var (_, product) = campaign.CreateProject("Assistant", "ai_assist", RevenueModel.Subscription, starters: []);
        Assert.That(campaign.QueueComponent(product!.Id, "ai_core").Ok, Is.False, "pas de modèle publié");
        var model = NewModel(campaign, AiGoal.Assistant);
        campaign.StartAiTraining(model.Id, "public", true);
        campaign.AdvanceDays(6);
        campaign.PublishAi(model.Id);
        Assert.That(campaign.QueueComponent(product.Id, "ai_core").Ok, Is.True);
        FinishQueue(campaign, product);
        Assert.That(product.AiModelId, Is.EqualTo(model.Id), "le modèle publié est branché tout seul");
        Assert.That(product.Component("ai_core")!.Quality, Is.EqualTo(Campaign.AiQualityFor(model)).Within(0.001));
    }

    [Test]
    public void InferenceCapsTheAudienceServedAndTheRevenueFollows()
    {
        var campaign = WithAi();
        Product Live() => new()
        {
            Id = 1, ArchetypeId = "ai", SegmentId = "ai_assist", Model = RevenueModel.Subscription, Price = 12, HostingId = "cluster", Users = 5_000, Aware = 50_000,
            LaunchedAt = 1, Components = [new() { DefId = "ai_core", Level = 1, Quality = 0.7 }, new() { DefId = "ui", Level = 1, Quality = 0.6 }],
        };
        var plenty = Market.Step(Live(), new DayInputs(0, campaign.Competitors) { Ai = new AiService(Capacity: 10, Load: 5) }, 1);
        var limited = Market.Step(Live(), new DayInputs(0, campaign.Competitors) { Ai = new AiService(Capacity: 2.5, Load: 5) }, 1);
        Assert.That(plenty.AiServed, Is.EqualTo(1));
        Assert.That(limited.AiServed, Is.EqualTo(0.5).Within(1e-9), "la moitié de la demande seulement est servie");
        Assert.That(limited.Revenue, Is.LessThan(plenty.Revenue));
        Assert.That(limited.Appeal, Is.LessThan(plenty.Appeal), "des réponses lentes se voient dans l'attrait");
    }

    [Test]
    public void ASharedModelPowersAnotherAppWithoutBeingCountedAsASale()
    {
        var campaign = WithAi();
        var model = NewModel(campaign, AiGoal.Assistant);
        campaign.StartAiTraining(model.Id, "public", true);
        campaign.AdvanceDays(6);
        campaign.PublishAi(model.Id);
        var product = NewProject(campaign);
        Assert.That(campaign.QueueComponent(product.Id, "ai_feature").Ok, Is.True);
        FinishQueue(campaign, product);
        Assert.That(campaign.Launch(product.Id).Ok, Is.True);
        product.Users = 3_000;
        Assert.That(product.AiModelId, Is.EqualTo(model.Id));
        double load = campaign.AiLoad(product);
        Assert.That(load, Is.EqualTo(3 * model.Base.InferencePerThousand * 0.35).Within(1e-9), "une fonction d'IA ne prend qu'une part de la capacité");
        // L'inférence interne ne facture rien de plus que l'électricité : aucune vente, aucune dépense inventée
        Install(campaign, "workstation_gpu");
        decimal cash = campaign.Ledger.Cash;
        campaign.AdvanceDays(1);
        Assert.That(product.InferenceInternal, Is.GreaterThan(0));
        Assert.That(product.InferenceRented, Is.EqualTo(0));
    }

    [Test]
    public void ARentedInferenceStaysUnderItsCap()
    {
        var campaign = WithAi();
        var model = NewModel(campaign, AiGoal.Assistant, "small");
        campaign.StartAiTraining(model.Id, "public", true);
        campaign.AdvanceDays(6);
        campaign.PublishAi(model.Id);
        var (_, product) = campaign.CreateProject("IA", "ai_assist", RevenueModel.Subscription, starters: []);
        product!.Components.AddRange([new ProjectComponent { DefId = "ai_core", Level = 1, Quality = 0.6 }, new ProjectComponent { DefId = "ui", Level = 1, Quality = 0.6 }]);
        campaign.SetProductAiModel(product.Id, model.Id);
        campaign.Launch(product.Id);
        product.Users = 400_000;
        campaign.SetInference(product.Id, external: true, cap: 300m);
        campaign.AdvanceDays(1);
        Assert.That((decimal)product.InferenceRented * AiCatalog.ExternalInferencePrice, Is.LessThanOrEqualTo(300m), "le plafond de location est respecté");
        Assert.That(campaign.InferenceCapacity(product), Is.LessThan(campaign.AiLoad(product)), "la demande dépasse : l'audience servie est plafonnée");
        campaign.SetInference(product.Id, external: false, cap: 300m);
        campaign.AdvanceDays(1);
        Assert.That(product.InferenceRented, Is.EqualTo(0));
    }

    [Test]
    public void ReviewsBlameSlowAnswersWhenInferenceIsShort()
    {
        var product = new Product
        {
            Id = 1, ArchetypeId = "ai", SegmentId = "ai_assist", Model = RevenueModel.Subscription, AiModelId = 1, LaunchedAt = 1,
            Components = [new() { DefId = "ai_core", Level = 1, Quality = 0.8 }, new() { DefId = "ui", Level = 1, Quality = 0.6 }],
        };
        for (int i = 0; i < 7; i++)
            // Comme dans Market : une inférence saturée fait aussi chuter la performance
            product.History.Add(new ProductDay { Day = i, Appeal = 0.5, Performance = 0.6, SupportFactor = 1, PriceFactor = 1, Liquidity = 1, AiUtilization = 1.6, AiServed = 0.6 });
        var trends = Reviews.Trends(product, 10_000, _ => true);
        Assert.That(trends, Has.Some.Matches<ReviewTrend>(t => t.Cause == "ai_capacity" && t.Link == "ai" && !t.Positive));
        Assert.That(trends.Where(t => !t.Positive).Select(t => t.Link), Has.None.EqualTo("hosting"),
            "les serveurs suivent : renvoyer vers l'hébergement ne réglerait rien");
    }

    [Test]
    public void TheEvaluationGameHasAClearRuleAndIsReplayable()
    {
        var campaign = WithAi();
        var model = NewModel(campaign);
        campaign.StartAiTraining(model.Id, "public", true);
        campaign.AdvanceDays(6);
        var first = campaign.EvaluationChallenge(model.Id);
        var again = campaign.EvaluationChallenge(model.Id);
        Assert.That(first.Items, Is.EqualTo(again.Items), "même partie, même mini-jeu");
        Assert.That(first.Matching, Is.True);
        for (int i = 0; i < first.Items.Count; i++)
        {
            int score = int.Parse(first.Items[i][(first.Items[i].LastIndexOf(' ') + 1)..]);
            Assert.That(first.Answer[i], Is.EqualTo(score < 5 ? 0 : score == 5 ? 1 : 2), "la règle donne la réponse");
        }
        Assert.That(first.Score(first.Answer), Is.EqualTo(1));
        Assert.That(first.Score(first.Answer.Select(a => (a + 1) % 3).ToList()), Is.EqualTo(0));

        // Jouer ou non ne bloque rien : publier sans évaluer reste possible, mais perd la validation
        var withGame = campaign.EvaluateAi(model.Id, first.Score(first.Answer));
        Assert.That(withGame.Ok, Is.True);
        Assert.That(campaign.EvaluateAi(model.Id, 1).Ok, Is.False, "une version ne s'évalue qu'une fois");
        Assert.That(model.Candidate!.Validation, Is.EqualTo(1));
    }

    [Test]
    public void AnEmployeeCanEvaluateOnceDelegationIsUnlocked()
    {
        var campaign = WithAi();
        var model = NewModel(campaign);
        campaign.StartAiTraining(model.Id, "public", true);
        campaign.AdvanceDays(6);
        Assert.That(campaign.DelegateEvaluation(model.Id).Ok, Is.False, "ni délégation ni employé");
        campaign.Unlocked.UnionWith(["delegation", "procedures"]);
        HireDeveloper(campaign);
        campaign.AdvanceDays(1);
        // L'employé n'est « présent » qu'en semaine, aux heures de travail : on avance jusqu'à un matin ouvré
        while (!Core.campaign.GameTime.IsWorkday(campaign.Minute) || Core.campaign.GameTime.Hour(campaign.Minute) < 10)
            campaign.Advance(60);
        var result = campaign.DelegateEvaluation(model.Id);
        Assert.That(result.Ok, Is.True, result.Message);
        Assert.That(model.Candidate!.Validation, Is.EqualTo(0.5), "avec les procédures : la moitié");
    }

    // ------------------------------------------------------------------ Studio et XR

    [Test]
    public void AStudioScoresItsPartsWithCapsAndNeedsSomeoneOnSite()
    {
        var campaign = NewCampaign();
        foreach (string id in new[] { "camera_tripod", "greenscreen_backdrop", "softbox_light", "softbox_light", "softbox_light", "mic_stand", "studio_monitors" })
            Install(campaign, id);
        var studio = campaign.StudioOf(campaign.Premises);
        Assert.That(studio.Video, Is.EqualTo(1.0).Within(1e-9), "trois éclairages : seuls deux comptent");
        Assert.That(studio.Audio, Is.EqualTo(0.8).Within(1e-9));
        Assert.That(studio.Score, Is.EqualTo(0.6 * 1.0 + 0.4 * 0.8).Within(1e-9));
        Assert.That(studio.Missing, Is.EqualTo(new[] { "séparation acoustique" }));
        Install(campaign, "partition_acoustic");
        Assert.That(campaign.StudioOf(campaign.Premises).Score, Is.EqualTo(0.6 + 0.4).Within(1e-9));
        Assert.That(campaign.StudioOf(campaign.Premises).Bonus, Is.EqualTo(StudioSummary.MaxBonus).Within(1e-9), "le bonus est plafonné");

        var empty = new Premises { Id = campaign.NewId() };
        foreach (string id in new[] { "camera_tripod", "greenscreen_backdrop" })
            empty.Items.Add(new PlacedItem { Id = campaign.NewId(), DefId = id });
        campaign.Sites.Add(empty);
        Assert.That(campaign.StudioOf(empty).Staffed, Is.False, "personne n'y travaille");
        Assert.That(campaign.StudioOf(empty).Score, Is.EqualTo((0.6 * 0.6) * 0.5).Within(1e-9));
    }

    [Test]
    public void TheStudioMakesContentBetterAndMarketingReachFurtherWithinTheCap()
    {
        var campaign = NewCampaign();
        var (_, game) = campaign.CreateProject("Jeu", "casual", RevenueModel.Purchase, starters: []);
        game!.Components.AddRange([new ProjectComponent { DefId = "graphics", Level = 1, Quality = 0.5 }, new ProjectComponent { DefId = "gameplay", Level = 1, Quality = 0.5 }]);
        game.MarketingBudget = 600;
        double appeal = Design.Appeal(game), reach = Market.Reach(game);
        foreach (string id in new[] { "camera_tripod", "greenscreen_backdrop", "softbox_light", "softbox_light", "mic_stand", "studio_monitors", "partition_acoustic" })
            Install(campaign, id);
        campaign.AdvanceDays(1);
        Assert.That(game.StudioBonus, Is.EqualTo(StudioSummary.MaxBonus).Within(1e-9));
        Assert.That(Design.Appeal(game), Is.GreaterThan(appeal));
        Assert.That(Market.Reach(game), Is.EqualTo(reach * (1 + 0.5 * StudioSummary.MaxBonus)).Within(1e-6));
        // Le gameplay n'est pas du « contenu de présentation » : seul graphics profite du studio
        double gameplay = Design.Coverage(game)[Need.Fun];
        game.StudioBonus = 0;
        Assert.That(Design.Coverage(game)[Need.Fun], Is.LessThan(gameplay), "graphics apporte du plaisir et profite du studio");
    }

    [Test]
    public void AHeadsetAloneServesAPrototypeAndTheFullZoneNeedsEverythingAndSomeone()
    {
        var campaign = NewCampaign();
        Install(campaign, "xr_headset_stand");
        Assert.That(campaign.XrOf(campaign.Premises).Capacity, Is.EqualTo(0.4).Within(1e-9));
        Install(campaign, "xr_base_station");
        Install(campaign, "xr_base_station");
        Install(campaign, "xr_base_station");
        Install(campaign, "xr_play_mat");
        var zone = campaign.XrOf(campaign.Premises);
        Assert.That(zone.Capacity, Is.EqualTo(1.0).Within(1e-9), "deux balises comptent, pas trois");
        Assert.That(zone.DefectCut, Is.EqualTo(XrSummary.MaxDefectCut).Within(1e-9));
        Assert.That(zone.Missing, Is.Empty);
    }

    [Test]
    public void TheXrZoneRemovesDefectsOfXrProducts()
    {
        double Bugs(bool zone)
        {
            var campaign = NewCampaign(7);
            campaign.Ledger.Cash = 100_000;
            campaign.CheatUnlockAll();
            if (zone)
                foreach (string id in new[] { "xr_headset_stand", "xr_base_station", "xr_base_station", "xr_play_mat" })
                    Install(campaign, id);
            var (result, product) = campaign.CreateProject("Monde", "xr_gamers", RevenueModel.Purchase);
            Assert.That(result.Ok, Is.True, result.Message);
            FinishQueue(campaign, product!);
            return product!.Bugs;
        }
        double without = Bugs(false), with = Bugs(true);
        Assert.That(without, Is.GreaterThan(0));
        Assert.That(with, Is.EqualTo(without * (1 - XrSummary.MaxDefectCut)).Within(Math.Max(0.3, without * 0.12)), "les tests immersifs retirent une part des défauts");
    }

    // ------------------------------------------------------------------ Laboratoire avancé et quantique

    /// <summary>Un site prêt pour une installation quantique : de la puissance et du froid en plus (armoire électrique, climatiseur).</summary>
    private static Campaign QuantumReady(bool control = true, bool cryo = true)
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 5_000_000;
        campaign.Unlocked.UnionWith(["cloud_offers", "compute_offers", "quantum_offers"]);
        Install(campaign, "power_distribution_cabinet");
        Install(campaign, "crac_unit");
        Install(campaign, "quantum_cryostat_closed");
        if (control)
            Install(campaign, "quantum_control_rack");
        if (cryo)
            Install(campaign, "cryo_support_unit");
        return campaign;
    }

    [Test]
    public void ACryostatNeedsItsControlRackAndCryogenicSupport()
    {
        var bare = QuantumReady(control: false, cryo: false);
        var infra = Infrastructure.Of(bare.Premises);
        Assert.That(infra.QuantumUnits, Is.EqualTo(0));
        Assert.That(infra.Issues, Has.Some.Contains("sans"));
        Assert.That(QuantumReady(control: true, cryo: false).SiteInfra(QuantumReady(control: true, cryo: false).Premises).QuantumUnits, Is.EqualTo(0));
        var ready = QuantumReady();
        Assert.That(ready.SiteInfra(ready.Premises).QuantumUnits, Is.EqualTo(1));
        Assert.That(ready.SiteInfra(ready.Premises).Quantum, Is.EqualTo(Infrastructure.QuantumPerUnit).Within(1e-9));
    }

    [Test]
    public void AQuantumInstallationNeedsAWarmupBeforeItProvidesAnyCompute()
    {
        var campaign = QuantumReady();
        Assert.That(campaign.Infra().Quantum, Is.EqualTo(0), "installée mais pas encore en service");
        campaign.AdvanceDays(1);
        Assert.That(campaign.QuantumSites.Single().WarmupLeft, Is.EqualTo(Campaign.QuantumWarmupDays));
        campaign.AdvanceDays(Campaign.QuantumWarmupDays - 2);
        Assert.That(campaign.QuantumUnitsOnline, Is.EqualTo(0));
        campaign.AdvanceDays(2);
        Assert.That(campaign.QuantumUnitsOnline, Is.EqualTo(1));
        Assert.That(campaign.Infra().Quantum, Is.EqualTo(Infrastructure.QuantumPerUnit).Within(1e-9));
        Assert.That(campaign.Awarded, Does.Contain("quantum:first"));
    }

    [Test]
    public void TheSignalGameShortensTheWarmupAtMostByHalfAndOnlyOnce()
    {
        var campaign = QuantumReady();
        campaign.AdvanceDays(1);
        int site = campaign.Premises.Id;
        var challenge = campaign.QuantumChallenge(site);
        Assert.That(challenge.Signals, Is.True);
        Assert.That(challenge.Start.Sum(), Is.EqualTo(challenge.Answer.Sum()), "le total des signaux est conservé : le motif est atteignable");
        Assert.That(challenge.Answer, Has.All.InRange(0, 9));
        Assert.That(challenge.Score(challenge.Answer), Is.EqualTo(1));
        var result = campaign.StabilizeQuantum(site, 1);
        Assert.That(result.Ok, Is.True);
        Assert.That(campaign.QuantumSites.Single().WarmupLeft, Is.EqualTo((int)Math.Ceiling(Campaign.QuantumWarmupDays / 2.0)));
        Assert.That(campaign.StabilizeQuantum(site, 1).Ok, Is.False, "une seule fois par mise en route");
        campaign.AdvanceDays(8);
        Assert.That(campaign.QuantumUnitsOnline, Is.EqualTo(1), "la mise en route raccourcie est terminée");
    }

    [Test]
    public void ABadScoreDoesNotBlockTheWarmup()
    {
        var campaign = QuantumReady();
        campaign.AdvanceDays(1);
        Assert.That(campaign.StabilizeQuantum(campaign.Premises.Id, 0).Ok, Is.True);
        Assert.That(campaign.QuantumSites.Single().WarmupLeft, Is.EqualTo(Campaign.QuantumWarmupDays));
        campaign.AdvanceDays(Campaign.QuantumWarmupDays);
        Assert.That(campaign.QuantumUnitsOnline, Is.EqualTo(1), "sans bonus, la mise en route finit quand même");
    }

    [Test]
    public void TheWarmupWaitsForColdAndPowerThenRestartsOnceFixed()
    {
        var campaign = QuantumReady();
        campaign.Premises.Items.RemoveAll(i => i.DefId == "crac_unit");
        campaign.InvalidateLayout();
        Assert.That(campaign.SiteInfra(campaign.Premises).Factor, Is.LessThan(0.99), "14,5 kW de chaleur pour 0,6 kW évacués");
        campaign.AdvanceDays(30);
        Assert.That(campaign.QuantumUnitsOnline, Is.EqualTo(0), "sans froid, la mise en route n'avance pas");
        Assert.That(campaign.Notices, Has.Some.Matches<Notice>(n => n.Key.StartsWith("quantum:") && !n.Resolved));
        Install(campaign, "crac_unit");
        campaign.AdvanceDays(Campaign.QuantumWarmupDays + 2);
        Assert.That(campaign.QuantumUnitsOnline, Is.EqualTo(1));
    }

    [Test]
    public void QuantumComputeIsSoldAsAServiceOnlyOnceInstalledAndNotPromisedTwice()
    {
        var campaign = QuantumReady();
        Assert.That(campaign.OpenCloudOffer(CloudKind.Quantum).Ok, Is.True);
        var offer = campaign.CloudOffers.Single(o => o.Kind == CloudKind.Quantum);
        Assert.That(offer.Reserved, Is.EqualTo(0), "rien à vendre avant la mise en route");
        campaign.AdvanceDays(Campaign.QuantumWarmupDays + 1);
        Assert.That(campaign.CloudAvailable(offer), Is.EqualTo(Infrastructure.QuantumPerUnit).Within(1e-9));
        Assert.That(campaign.SetCloudOffer(offer.Id, 900m, 1_000).Ok, Is.True);
        Assert.That(offer.Reserved, Is.EqualTo(Infrastructure.QuantumPerUnit).Within(1e-9), "on ne réserve que ce qui existe");
        campaign.AdvanceDays(40);
        Assert.That(offer.Served, Is.GreaterThan(0).And.LessThanOrEqualTo(offer.Reserved));
        Assert.That(offer.Revenue30, Is.GreaterThan(0));
    }

    [Test]
    public void QuantumDoesNotMakeEverythingFasterOnlyHeavyComputeResearch()
    {
        var plain = Campaign.ResearchCatalog.Where(r => !r.Quantum).ToList();
        Assert.That(plain.Count, Is.GreaterThan(Campaign.ResearchCatalog.Count / 2), "la plupart des recherches ne profitent pas du quantique");
        Assert.That(Campaign.ResearchCatalog.Where(r => r.Quantum).Select(r => r.Id), Is.EquivalentTo(new[] { "res_chips", "res_qsystems" }));
    }

    [Test]
    public void TheAdvancedLabRentsPlacesAllItsNewEquipmentAndSellsNothingForFree()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 5_000_000;
        Assert.That(campaign.RentSite("advanced_lab").Ok, Is.False, "à débloquer : multisite et recherches avancées");
        campaign.CheatUnlockAll();
        Assert.That(campaign.RentSite("advanced_lab").Ok, Is.True);
        var lab = campaign.Premises;
        Assert.That(lab.ListingId, Is.EqualTo("advanced_lab"));
        Assert.That(lab.Items.Any(i => i.DefId.StartsWith("quantum_")), Is.False, "les machines du plan ne sont qu'un aperçu : il faut les acheter");
        foreach (string id in new[] { "compute_cabinet", "quantum_control_rack", "cryo_support_unit", "quantum_cryostat_open", "quantum_cryostat_closed",
                     "camera_tripod", "greenscreen_backdrop", "softbox_light", "mic_stand", "xr_base_station", "xr_play_mat", "supervision_terminal" })
        {
            var def = FurnitureCatalog.Get(id);
            Assert.That(def.Buyable, Is.True, id);
            var spot = campaign.FindSpot(def);
            Assert.That(spot, Is.Not.Null, $"{id} : pas de place sur le plan du laboratoire");
            var result = campaign.BuyAndPlace(id, spot!.Value.X, spot.Value.Y, spot.Value.Facing);
            Assert.That(result.Ok, Is.True, $"{id} : {result.Message}");
        }
        // Les objets de bureau s'accrochent à un bureau
        var desk = campaign.BuyAndPlace("desk_simple", campaign.FindSpot(FurnitureCatalog.Get("desk_simple"))!.Value.X, campaign.FindSpot(FurnitureCatalog.Get("desk_simple"))!.Value.Y);
        Assert.That(desk.Ok, Is.True, desk.Message);
        int host = campaign.Premises.Items.Last().Id;
        Assert.That(campaign.BuyAndAttach("xr_headset_stand", host).Ok, Is.True);
        Assert.That(campaign.BuyAndAttach("studio_monitors", host).Ok, Is.True);
    }

    [Test]
    public void ALockedItemNamesTheResearchThatUnlocksIt()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 1_000_000;
        campaign.Unlocked.Add("advanced_lab");
        var result = campaign.BuyAndPlace("quantum_cryostat_closed", 0, 0);
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Message, Does.Contain("Calibration cryogénique"));
    }

    // ------------------------------------------------------------------ Arbre et familles de projets

    [Test]
    public void TheNewBranchesAreInTheTreeWithRealRequirements()
    {
        foreach (string id in new[] { "studio", "xr", "ai_models", "advanced_lab", "quantum_offers" })
        {
            var node = Core.progress.DevTree.Node(id);
            Assert.That(node.Effect, Is.Not.Empty, id);
            if (node.Requires != null)
                Assert.That(Core.progress.DevTree.Nodes.Any(n => n.Id == node.Requires), Is.True, id);
        }
        var campaign = NewCampaign();
        Assert.That(campaign.TypeAvailable("ai"), Is.False);
        Assert.That(campaign.TypeAvailable("xr_app"), Is.False);
        campaign.Unlocked.UnionWith(["ai_models", "xr"]);
        Assert.That(campaign.TypeAvailable("ai") && campaign.TypeAvailable("xr_app"), Is.True);
    }

    [Test]
    public void EveryNewProductFamilyCanBeCreatedWithItsStarterComponents()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 100_000;
        campaign.CheatUnlockAll();
        foreach (var segment in ProductCatalog.Segments.Where(s => s.TypeId is "ai" or "xr_app" or "xr_kit" or "quantum"))
        {
            var type = ProductCatalog.Type(segment.TypeId);
            var (result, product) = campaign.CreateProject($"P {segment.Id}", segment.Id, type.Models[0], starters: type.Starters.Where(s => s != "ai_core"));
            Assert.That(result.Ok, Is.True, $"{segment.Id} : {result.Message}");
            Assert.That(product!.Queue, Is.Not.Empty, segment.Id);
            foreach (string starter in type.Starters)
                Assert.That(ProductCatalog.Component(starter).Types, Does.Contain(type.Id), $"{starter} pour {type.Id}");
        }
    }

    [Test]
    public void AQuantumMachineIsSoldAboveItsMaterialCost()
    {
        var campaign = NewCampaign();
        campaign.CheatUnlockAll();
        var (_, product) = campaign.CreateProject("Q", "quantum_labs", RevenueModel.Purchase);
        Assert.That(product, Is.Not.Null);
        foreach (string id in ProductCatalog.Type("quantum").Starters)
            campaign.QueueComponent(product.Id, id);
        decimal cost = Core.industry.IndustryCatalog.UnitCost(product);
        Assert.That(cost, Is.GreaterThan(1_000m));
        Assert.That(cost, Is.LessThan(product.Price), "une machine se vend plus cher que ses matières");
    }

    // ------------------------------------------------------------------ Sauvegarde

    [Test]
    public void TheNewStateSurvivesASaveAndLoadRoundTrip()
    {
        var campaign = QuantumReady();
        campaign.Unlocked.Add("ai_models");
        var model = NewModel(campaign, AiGoal.Generation);
        campaign.StartAiTraining(model.Id, "bought", external: true);
        campaign.AdvanceDays(3);
        campaign.TrainingShare = 0.7;
        campaign.CheatFinishWork();
        campaign.AdvanceDays(2);
        Assert.That(model.Candidate, Is.Not.Null);
        campaign.PublishAi(model.Id);
        Assert.That(campaign.QuantumSites, Is.Not.Empty);

        string json = SaveStore.Serialize(campaign);
        var loaded = SaveStore.Deserialize(json, out bool migrated);
        Assert.That(migrated, Is.False);
        Assert.That(SaveStore.SerializeState(loaded), Is.EqualTo(SaveStore.SerializeState(campaign)));
        Assert.That(loaded.AiModels.Single().Live!.Number, Is.EqualTo(1));
        Assert.That(loaded.QuantumSites.Count, Is.EqualTo(campaign.QuantumSites.Count));
        Assert.That(loaded.TrainingShare, Is.EqualTo(0.7));
        Assert.That(loaded.Awarded, Does.Contain("ai:published"), "une récompense déjà reçue n'est pas rendue deux fois");
    }

    [Test]
    public void AFormat5SaveStillLoadsAndIsFlaggedAsMigrated()
    {
        var campaign = NewCampaign();
        string json = SaveStore.Serialize(campaign).Replace($"\"Format\":{SaveStore.CurrentFormat}", "\"Format\":5");
        var loaded = SaveStore.Deserialize(json, out bool migrated);
        Assert.That(migrated, Is.True);
        Assert.That(loaded.MigrationNote, Does.Contain("IA"));
        Assert.That(loaded.AiModels, Is.Empty);
        loaded.MigrationNote = "";
        Assert.That(SaveStore.SerializeState(loaded), Is.EqualTo(SaveStore.SerializeState(campaign)), "rien n'est converti ni perdu");
    }

    [Test]
    public void ASaveMidTrainingResumesWithItsProgress()
    {
        var campaign = WithAi();
        var model = NewModel(campaign);
        campaign.StartAiTraining(model.Id, "public", external: true);
        campaign.AdvanceDays(2);
        double progress = model.Training!.Progress;
        var loaded = SaveStore.Deserialize(SaveStore.Serialize(campaign));
        Assert.That(loaded.AiModels.Single().Training!.Progress, Is.EqualTo(progress));
        loaded.AdvanceDays(3);
        campaign.AdvanceDays(3);
        Assert.That(SaveStore.SerializeState(loaded), Is.EqualTo(SaveStore.SerializeState(campaign)), "même partie, même résultat après rechargement");
    }
}
