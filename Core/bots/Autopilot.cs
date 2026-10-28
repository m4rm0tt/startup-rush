using Core.campaign;
using Core.finance;
using Core.products;
using Core.space;

namespace Core.bots;

/// <summary>
/// Une façon de jouer, décrite par quelques réglages. Elle ne sert qu'aux mesures d'équilibrage et aux captures :
/// elle utilise les mêmes commandes que l'interface, sans accès privilégié.
/// </summary>
public record Strategy(string Name, string Description)
{
    /// <summary>Projets à créer, dans l'ordre : public visé et composants de départ (null = ceux par défaut).</summary>
    public (string Segment, string[]? Starters)[] Projects { get; init; } = [("freelances", null)];

    /// <summary>Jours entre deux nouveaux projets (0 : tous dès le départ).</summary>
    public int ProjectInterval { get; init; } = 120;

    public decimal Marketing { get; init; } = 300;

    /// <summary>Mots tapés au terminal par jour ouvré (moins d'une minute de saisie : l'équivalent de l'ancien fondateur seul).</summary>
    public int WordsPerDay { get; init; } = 30;

    /// <summary>Le fondateur arrête de taper dès qu'il a autant d'employés (0 : il tape toujours).</summary>
    public int StopTypingWithStaff { get; init; }

    /// <summary>Délègue dès que l'arbre le permet : lancements par un employé, petites mises à jour préparées par l'équipe.</summary>
    public bool Delegate { get; init; }

    /// <summary>
    /// Héberge soi-même au-delà de cette trésorerie (0 : jamais) : loue une salle serveur, l'équipe d'une baie, y passe ses
    /// projets et vend en offre cloud ce qui reste.
    /// </summary>
    public decimal SelfHostAbove { get; init; }

    /// <summary>
    /// Se lance dans le matériel au-delà de cette trésorerie (0 : jamais) : un atelier, un établi et un chercheur, puis un
    /// produit de ce public, une ligne d'assemblage, un opérateur, des réserves automatiques et un stock cible.
    /// </summary>
    public decimal HardwareAbove { get; init; }

    public string HardwareSegment { get; init; } = "makers";

    /// <summary>
    /// Se lance dans l'IA au-delà de cette trésorerie (0 : jamais) : un petit modèle d'analyse entraîné sur des données publiques,
    /// évalué, publié, puis un produit d'IA de ce public ; une seconde version sur données achetées dès que l'argent le permet.
    /// </summary>
    public decimal AiAbove { get; init; }

    public string AiSegment { get; init; } = "ai_analytics";

    /// <summary>Équipe le siège d'un studio audiovisuel complet au-delà de cette trésorerie (0 : jamais).</summary>
    public decimal StudioAbove { get; init; }

    /// <summary>Équipe le siège d'une zone de test XR complète au-delà de cette trésorerie (0 : jamais).</summary>
    public decimal XrAbove { get; init; }

    /// <summary>
    /// Le quantique au-delà de cette trésorerie (0 : jamais) : un laboratoire avancé, le cryostat ouvert et ses supports, un
    /// chercheur sur « Calibration cryogénique », puis le cryostat fermé, sa mise en route et le service quantique vendu.
    /// </summary>
    public decimal QuantumAbove { get; init; }

    public decimal PreLaunchMarketing { get; init; }
    public int MaxStaff { get; init; }
    public decimal HireAbove { get; init; } = 30_000;

    /// <summary>N'embauche que si les revenus et la réserve couvrent le nouveau salaire.</summary>
    public bool CarefulHiring { get; init; } = true;

    /// <summary>Lit les avis et améliore ce qu'ils signalent.</summary>
    public bool ListenToReviews { get; init; } = true;

    public bool Recover { get; init; } = true;
    public bool Borrow { get; init; } = true;

    /// <summary>Débloque des nœuds de l'arbre dès que possible, dans cet ordre.</summary>
    public string[] TreeOrder { get; init; } = ["advanced", "cloud_m", "search_ads", "optimization", "hiring", "loyalty", "leads", "quality_lab"];
}

/// <summary>Joue une stratégie : une décision par jour ouvré à 8 h.</summary>
public class Autopilot(Strategy strategy)
{
    public Strategy Strategy { get; } = strategy;
    private long _lastProjectDay = -10_000;
    private int _created;
    private readonly HashSet<int> _marketed = new();

    /// <summary>Nombre de décisions prises (pour mesurer la quantité d'actions routinières).</summary>
    public int Actions { get; private set; }

    /// <summary>Mots tapés au terminal depuis le début (pour mesurer le travail de saisie demandé).</summary>
    public int Words { get; private set; }

    /// <summary>Jour où chaque étape du lot 6 a été atteinte pour la première fois (IA publiée, studio complet, quantique en service…).</summary>
    public Dictionary<string, int> Milestones { get; } = new();

    /// <summary>Score joué aux mini-jeux d'évaluation et de mise en route : correct, pas parfait.</summary>
    public const double MiniGameScore = 0.75;

    private void Mark(Campaign campaign, string milestone) => Milestones.TryAdd(milestone, (int)campaign.Day);

    public void Play(Campaign campaign, int days)
    {
        for (int d = 0; d < days && campaign.Status == CampaignStatus.Running; d++)
        {
            if (GameTime.IsWorkday(campaign.Minute))
                Decide(campaign);
            campaign.AdvanceDays(1);
        }
    }

    private void Act(CommandResult result)
    {
        if (result.Ok)
            Actions++;
    }

    public void Decide(Campaign campaign)
    {
        CreateProjects(campaign);
        Type(campaign);
        foreach (var product in campaign.Products.ToList())
            RunProject(campaign, product);
        Tree(campaign);
        Infrastructure(campaign);
        Hardware(campaign);
        Ai(campaign);
        Studio(campaign);
        Quantum(campaign);
        Hiring(campaign);
        if (Strategy.Recover)
            Recovery(campaign);
    }

    private void CreateProjects(Campaign campaign)
    {
        if (_created >= Strategy.Projects.Length)
            return;
        if (_created > 0 && campaign.Day - _lastProjectDay < Strategy.ProjectInterval)
            return;
        var (segment, starters) = Strategy.Projects[_created];
        var type = ProductCatalog.Segment(segment).TypeId;
        if (!campaign.TypeAvailable(type))
            return;
        var (result, product) = campaign.CreateProject("", segment, ProductCatalog.Type(type).Models[0], starters: starters);
        Act(result);
        if (product != null)
        {
            _created++;
            _lastProjectDay = campaign.Day;
            // Le nouveau projet passe en tête du portefeuille : l'équipe en automatique s'y met
            while (campaign.Products.OrderBy(p => p.Priority).First().Id != product.Id && campaign.MoveProject(product.Id, -1).Ok)
            {
            }
            if (Strategy.PreLaunchMarketing > 0)
                campaign.SetMarketing(product.Id, Strategy.PreLaunchMarketing);
        }
    }

    private void RunProject(Campaign campaign, Product product)
    {
        if (!product.IsLive)
        {
            if (product.Queue.Count == 0 && campaign.CanLaunch(product))
            {
                // En délégation, un employé le lancera demain matin ; sinon on lance soi-même
                if (Strategy.Delegate && campaign.CanDelegateLaunch(product).Ok)
                    campaign.DelegateLaunches = true;
                else if (campaign.Launch(product.Id).Ok)
                {
                    Actions++;
                    _marketed.Add(product.Id);
                    Act(campaign.SetMarketing(product.Id, Strategy.Marketing));
                }
            }
            return;
        }
        if (_marketed.Add(product.Id))
            Act(campaign.SetMarketing(product.Id, Strategy.Marketing));
        if (product.Queue.Count > 0)
            return;
        // L'automatisation avancée prépare elle-même les petites mises à jour (le lundi) : on la laisse faire
        if (Strategy.Delegate && campaign.IsUnlocked("automation") && campaign.Staff.Any())
            return;

        // Une amélioration à la fois : d'après les avis, sinon ce qui manque le plus
        string? link = null;
        if (Strategy.ListenToReviews)
            link = Reviews.Trends(product, campaign.Minute, campaign.IsUnlocked).FirstOrDefault(t => !t.Positive)?.Link;
        switch (link)
        {
            case "hosting":
                // Relever le plafond ne sert que s'il reste une offre plus grande au-dessus
                if (ProductCatalog.Hosting.Any(h => h.MonthlyCost > product.HostingCap && campaign.PlanAvailable(h)))
                {
                    Act(campaign.SetHosting(product.Id, true, product.HostingCap * 2));
                    return;
                }
                break;
            case "upkeep":
                if (product.Upkeep != Upkeep.High)
                {
                    Act(campaign.SetUpkeep(product.Id, Upkeep.High));
                    return;
                }
                break;
            case "price":
                Act(campaign.SetPrice(product.Id, Math.Max(1, Math.Round(product.Price * 0.85m))));
                return;
            case { } component when component.StartsWith("component:"):
                var result = campaign.QueueComponent(product.Id, component["component:".Length..]);
                if (result.Ok)
                {
                    Act(result);
                    return;
                }
                break;
        }
        var (_, _, fix) = Design.WeakestNeed(product, campaign.IsUnlocked);
        if (fix != null)
            Act(campaign.QueueComponent(product.Id, fix.Id));
        else
        {
            // Plus rien de mieux pour le besoin le plus faible : on monte d'un niveau le meilleur composant
            var next = product.Components.OrderBy(c => c.Level).Select(c => c.DefId).FirstOrDefault(id => Design.CanQueue(product, id, campaign.IsUnlocked).Ok);
            if (next != null)
                Act(campaign.QueueComponent(product.Id, next));
        }
    }

    private void Tree(Campaign campaign)
    {
        foreach (string id in Strategy.TreeOrder)
        {
            if (campaign.IsUnlocked(id))
                continue;
            var node = progress.DevTree.Node(id);
            if (campaign.CanUnlock(node).Ok)
                Act(campaign.Unlock(id));
            break;
        }
        // Les types de projet prévus par la stratégie se débloquent avant le reste
        foreach (var (segment, _) in Strategy.Projects)
            if (ProductCatalog.Type(ProductCatalog.Segment(segment).TypeId).UnlockNode is { } node && !campaign.IsUnlocked(node))
            {
                var def = progress.DevTree.Node(node);
                if (def.Requires != null && !campaign.IsUnlocked(def.Requires) && campaign.CanUnlock(progress.DevTree.Node(def.Requires)).Ok)
                    Act(campaign.Unlock(def.Requires));
                else if (campaign.CanUnlock(def).Ok)
                    Act(campaign.Unlock(node));
            }
    }

    private static readonly string[] RackModules =
        ["module_network_switch_1u", "module_patch_panel_1u", "module_cable_manager_1u", "module_server_1u", "module_server_1u", "module_storage_2u", "module_ups_2u"];

    private void Infrastructure(Campaign campaign)
    {
        if (Strategy.SelfHostAbove <= 0 || !campaign.IsUnlocked("server_room") || !campaign.IsUnlocked("multisite"))
            return;
        var room = campaign.Sites.FirstOrDefault(s => s.ListingId == "server_room");
        if (room == null)
        {
            if (campaign.Ledger.Cash < Strategy.SelfHostAbove || campaign.Solvency.State != SolvencyState.Healthy)
                return;
            var home = campaign.SiteId;
            Act(campaign.RentSite("server_room"));
            room = campaign.Premises;
            if (room.ListingId == "server_room" && campaign.FindSpot(FurnitureCatalog.Get("rack_standard")) is var (x, y, f) && campaign.BuyAndPlace("rack_standard", x, y, f).Ok)
            {
                var rack = room.Items.Last();
                foreach (string module in RackModules)
                    Act(campaign.BuyAndAttach(module, rack.Id));
            }
            campaign.ViewSite(home);
        }
        foreach (var product in campaign.Products.Where(p => p.IsLive && !p.HostingInternal))
            Act(campaign.SetInternalHosting(product.Id, true));
        // Toute la capacité libre est mise en vente
        foreach (var offer in campaign.CloudOffers.Where(o => !o.Paused && campaign.CloudAvailable(o) > o.Reserved + 1))
            Act(campaign.SetCloudOffer(offer.Id, offer.Price, campaign.CloudAvailable(offer)));
        // Une offre saturée : un module de plus, s'il reste de la place dans la baie et de l'argent
        var full = campaign.CloudOffers.FirstOrDefault(o => o.Demand > o.Served * 1.1 && o.Kind is CloudKind.Storage or CloudKind.Hosting);
        if (full != null && campaign.Ledger.Cash > Strategy.SelfHostAbove * 0.75m && campaign.Solvency.State == SolvencyState.Healthy
            && room.Items.FirstOrDefault(i => i.DefId == "rack_standard") is { } rackHost)
        {
            var home = campaign.SiteId;
            campaign.ViewSite(room.Id);
            if (campaign.BuyAndAttach(full.Kind == CloudKind.Storage ? "module_storage_jbod_4u" : "module_server_1u", rackHost.Id).Ok)
                Actions++;
            campaign.ViewSite(home);
            Act(campaign.SetCloudOffer(full.Id, full.Price, double.MaxValue));
        }
        foreach (var kind in new[] { CloudKind.Storage, CloudKind.Hosting })
            if (campaign.CloudOffers.All(o => o.Kind != kind) && campaign.IsUnlocked("cloud_offers"))
                Act(campaign.OpenCloudOffer(kind));
    }

    private void Hardware(Campaign campaign)
    {
        if (Strategy.HardwareAbove <= 0 || !campaign.IsUnlocked("hardware") || !campaign.IsUnlocked("multisite"))
            return;
        var shop = campaign.Sites.FirstOrDefault(s => s.ListingId == "hw_workshop");
        var type = ProductCatalog.Type(ProductCatalog.Segment(Strategy.HardwareSegment).TypeId);
        string research = type.UnlockNode!;
        if (shop == null)
        {
            if (campaign.Ledger.Cash < Strategy.HardwareAbove || campaign.Solvency.State != SolvencyState.Healthy)
                return;
            var home = campaign.SiteId;
            Act(campaign.RentSite("hw_workshop"));
            shop = campaign.Premises;
            if (shop.ListingId == "hw_workshop")
            {
                Act(campaign.InstallLayout("assembly"));
                if (campaign.FindSpot(FurnitureCatalog.Get("workbench_electronics")) is var (x, y, f) && campaign.BuyAndPlace("workbench_electronics", x, y, f).Ok)
                {
                    var bench = shop.Items.Last();
                    foreach (string tool in new[] { "chair_office", "oscilloscope", "soldering_station" })
                        Act(campaign.BuyAndAttach(tool, bench.Id));
                }
                if (campaign.FindSpot(FurnitureCatalog.Get("storage_rack_industrial")) is var (rx, ry, rf))
                    Act(campaign.BuyAndPlace("storage_rack_industrial", rx, ry, rf));
            }
            campaign.ViewSite(home);
            Act(campaign.StartResearch(research));
        }
        // Une personne à la recherche (assise à l'établi), puis comme opérateur une fois la famille débloquée
        var bench0 = shop.Workstations.FirstOrDefault(p => p.Desk.DefId == "workbench_electronics");
        var worker = campaign.Staff.OrderBy(p => p.Id).FirstOrDefault();
        if (worker == null || bench0 == null)
            return;
        if (!campaign.IsUnlocked(research))
        {
            if (worker.Assignment.Kind != Core.team.AssignmentKind.Research)
            {
                campaign.Seat(worker, bench0);
                Act(campaign.AssignTo(worker.Id, Core.team.AssignmentKind.Research));
            }
            return;
        }
        var product = campaign.Products.FirstOrDefault(p => p.IsHardware);
        if (product == null)
        {
            var (result, created) = campaign.CreateProject("", Strategy.HardwareSegment, RevenueModel.Purchase);
            Act(result);
            if (created != null)
                while (campaign.Products.OrderBy(p => p.Priority).First().Id != created.Id && campaign.MoveProject(created.Id, -1).Ok)
                {
                }
            if (worker.Assignment.Kind == Core.team.AssignmentKind.Research)
                Act(campaign.Assign(worker.Id, 0));
            return;
        }
        if (!product.IsLive)
        {
            if (product.Queue.Count == 0 && campaign.CanLaunch(product))
            {
                Act(campaign.LaunchWith(product.Id, 0.5));
                _marketed.Add(product.Id);
                Act(campaign.SetMarketing(product.Id, Strategy.Marketing));
            }
            return;
        }
        if (worker.Assignment.Kind != Core.team.AssignmentKind.Production)
        {
            Act(campaign.AssignTo(worker.Id, Core.team.AssignmentKind.Production, shop.Id));
            foreach (var (item, qty) in Core.industry.IndustryCatalog.Bom(product))
                Act(campaign.SetReserve(shop.Id, item, qty * 25));
            Act(campaign.SetProcurementCap(4_000));
            Act(campaign.CreateOrder(shop.Id, $"product:{product.Id}", OrderMode.TargetStock, 30));
        }
        // Une marge d'au moins 30 % : le prix suit le coût des matières, qui monte avec les composants
        decimal cost = Core.industry.IndustryCatalog.UnitCost(product);
        if (product.Price < cost * 1.45m)
            Act(campaign.SetPrice(product.Id, Math.Ceiling(cost * 1.5m)));
        // La nomenclature change avec les améliorations : les réserves suivent
        foreach (var order in campaign.Orders.Where(o => o.SiteId == shop.Id))
            campaign.SupplyOrder(order.Id);
        if (campaign.IsUnlocked("online_shop") && !product.SalesChannels.Contains("shop"))
            Act(campaign.SetChannel(product.Id, "shop", true));
    }

    private bool CanSpend(Campaign campaign, decimal above) =>
        above > 0 && campaign.Ledger.Cash >= above && campaign.Solvency.State == SolvencyState.Healthy;

    /// <summary>Pose au sol, là où il y a de la place sur le site affiché ; les objets de bureau vont sur le premier bureau.</summary>
    private void Equip(Campaign campaign, IEnumerable<string> defs)
    {
        foreach (string id in defs)
        {
            var def = FurnitureCatalog.Get(id);
            if (def.Attached)
            {
                if (campaign.Premises.Desks.FirstOrDefault() is { } desk)
                    Act(campaign.BuyAndAttach(id, desk.Id));
            }
            else if (campaign.FindSpot(def) is var (x, y, f))
                Act(campaign.BuyAndPlace(id, x, y, f));
        }
    }

    private void Ai(Campaign campaign)
    {
        if (Strategy.AiAbove <= 0 || !campaign.IsUnlocked("ai_models"))
            return;
        var model = campaign.AiModels.FirstOrDefault();
        if (model == null)
        {
            if (!CanSpend(campaign, Strategy.AiAbove))
                return;
            var goal = ai.AiCatalog.Goals.First(g => g.SegmentId == Strategy.AiSegment).Goal;
            var (result, created) = campaign.CreateAiModel("", goal, "small");
            Act(result);
            if (created == null)
                return;
            model = created;
        }
        var totals = campaign.Infra();
        bool rent = totals.Gpu + totals.Compute <= 0;
        if (model.Training == null && model.Candidate == null && model.Live == null)
            Act(campaign.StartAiTraining(model.Id, "public", rent));
        if (model.Candidate is { Evaluated: false })
            Act(campaign.EvaluateAi(model.Id, MiniGameScore));
        if (model.Candidate is { Evaluated: true })
        {
            Act(campaign.PublishAi(model.Id));
            Mark(campaign, "ia_publiee");
        }
        // Une seconde version sur de meilleures données, quand l'argent le permet ; l'ancienne sert pendant ce temps
        if (model.Live is { DatasetId: "public" } && model.Training == null && model.Candidate == null && CanSpend(campaign, Strategy.AiAbove))
            Act(campaign.StartAiTraining(model.Id, "bought", rent));
        if (model.Live == null)
            return;
        if (campaign.Products.All(p => p.ArchetypeId != "ai"))
        {
            var (result, product) = campaign.CreateProject("", Strategy.AiSegment, RevenueModel.Subscription);
            Act(result);
            if (product != null)
            {
                Mark(campaign, "produit_ia");
                while (campaign.Products.OrderBy(p => p.Priority).First().Id != product.Id && campaign.MoveProject(product.Id, -1).Ok)
                {
                }
            }
        }
        // L'inférence louée suit la charge, avec de la marge : sinon l'audience servie plafonne
        foreach (var product in campaign.Products.Where(p => p.AiModelId != 0 && p.IsLive))
        {
            decimal need = Math.Ceiling((decimal)(campaign.AiLoad(product) / 0.75 * 1.25) * ai.AiCatalog.ExternalInferencePrice);
            if (product.InferenceCap < need)
                Act(campaign.SetInference(product.Id, true, Math.Max(300, need)));
        }
    }

    private static readonly string[] StudioSet =
        ["camera_tripod", "greenscreen_backdrop", "softbox_light", "softbox_light", "mic_stand", "studio_monitors", "partition_acoustic"];

    private static readonly string[] XrSet = ["xr_headset_stand", "xr_base_station", "xr_base_station", "xr_play_mat"];

    /// <summary>Studio et zone XR au siège, là où l'équipe travaille (un studio sans personne sur place ne rend qu'à moitié).</summary>
    private void Studio(Campaign campaign)
    {
        if (campaign.SiteId != campaign.Headquarters.Id)
            return;
        var home = campaign.Headquarters;
        if (Strategy.StudioAbove > 0 && campaign.IsUnlocked("studio") && !Milestones.ContainsKey("studio_pose") && CanSpend(campaign, Strategy.StudioAbove))
        {
            Equip(campaign, StudioSet);
            Mark(campaign, "studio_pose");
        }
        if (Strategy.XrAbove > 0 && campaign.IsUnlocked("xr") && !Milestones.ContainsKey("zone_xr_posee") && CanSpend(campaign, Strategy.XrAbove))
        {
            Equip(campaign, XrSet);
            Mark(campaign, "zone_xr_posee");
        }
        if (campaign.StudioOf(home).Missing.Count == 0)
            Mark(campaign, "studio_complet");
        if (campaign.XrOf(home).Missing.Count == 0)
            Mark(campaign, "zone_xr_complete");
    }

    /// <summary>Le labo de calibration : énergie et froid, un établi pour le chercheur, le cryostat ouvert et ses deux supports.</summary>
    private static readonly string[] LabSet =
        ["power_distribution_cabinet", "crac_unit", "workbench_electronics", "quantum_control_rack", "cryo_support_unit", "quantum_cryostat_open"];

    /// <summary>Réserve gardée après chaque achat du labo (loyer, salaires).</summary>
    private const decimal LabMargin = 30_000;

    private void Quantum(Campaign campaign)
    {
        if (Strategy.QuantumAbove <= 0 || !campaign.IsUnlocked("advanced_lab") || !campaign.IsUnlocked("multisite"))
            return;
        var lab = campaign.Sites.FirstOrDefault(s => s.ListingId == "advanced_lab");
        var home = campaign.SiteId;
        if (lab == null)
        {
            if (!CanSpend(campaign, Strategy.QuantumAbove))
                return;
            Act(campaign.RentSite("advanced_lab"));
            lab = campaign.Premises;
            campaign.ViewSite(home);
            if (lab.ListingId != "advanced_lab")
                return;
            Mark(campaign, "labo_avance");
            Act(campaign.StartResearch("res_cryo"));
        }
        // Ce qui manque au labo s'achète dès que l'argent le permet, avec une marge (les gros objets peuvent attendre des mois)
        foreach (string id in LabSet)
        {
            int wanted = LabSet.Count(x => x == id);
            if (lab.Items.Count(i => i.DefId == id) >= wanted || !CanSpend(campaign, FurnitureCatalog.Get(id).Price + LabMargin))
                continue;
            campaign.ViewSite(lab.Id);
            Equip(campaign, [id]);
            if (id == "workbench_electronics" && lab.Items.LastOrDefault(i => i.DefId == id) is { } bench)
                Act(campaign.BuyAndAttach("chair_office", bench.Id));
            campaign.ViewSite(home);
        }
        // Un chercheur assis au labo tant que la calibration n'est pas finie
        var researcher = campaign.Staff.OrderBy(p => p.Id).FirstOrDefault();
        var post = lab.Workstations.FirstOrDefault(p => p.Desk.DefId == "workbench_electronics");
        if (!campaign.ResearchDone.Contains("res_cryo"))
        {
            if (researcher != null && post != null && researcher.Assignment.Kind != Core.team.AssignmentKind.Research)
            {
                campaign.Seat(researcher, post);
                Act(campaign.AssignTo(researcher.Id, Core.team.AssignmentKind.Research));
            }
            return;
        }
        Mark(campaign, "calibration_cryo");
        // La calibration finie, le chercheur retourne aux projets
        if (researcher is { Assignment.Kind: Core.team.AssignmentKind.Research })
            Act(campaign.Assign(researcher.Id, 0));
        if (lab.Items.All(i => i.DefId != "quantum_cryostat_closed"))
        {
            if (!CanSpend(campaign, FurnitureCatalog.Get("quantum_cryostat_closed").Price + LabMargin))
                return;
            campaign.ViewSite(lab.Id);
            Equip(campaign, ["quantum_cryostat_closed"]);
            campaign.ViewSite(home);
        }
        if (campaign.QuantumSites.FirstOrDefault(q => q.SiteId == lab.Id) is { WarmupLeft: > 0, Stabilized: false })
            Act(campaign.StabilizeQuantum(lab.Id, MiniGameScore));
        if (campaign.QuantumUnitsOnline == 0)
            return;
        Mark(campaign, "quantique_en_service");
        if (campaign.IsUnlocked("quantum_offers") && campaign.CloudOffers.All(o => o.Kind != CloudKind.Quantum))
            Act(campaign.OpenCloudOffer(CloudKind.Quantum));
        foreach (var offer in campaign.CloudOffers.Where(o => o.Kind == CloudKind.Quantum && !o.Paused && campaign.CloudAvailable(o) > o.Reserved + 0.5))
            Act(campaign.SetCloudOffer(offer.Id, offer.Price, campaign.CloudAvailable(offer)));
    }

    private void Hiring(Campaign campaign)
    {
        if (campaign.Staff.Count() >= Strategy.MaxStaff || campaign.Ledger.Cash < Strategy.HireAbove || campaign.Solvency.State != SolvencyState.Healthy)
            return;
        var candidate = campaign.Candidates.OrderByDescending(c => c.Person.Skill(c.Person.BestDiscipline) / (double)c.Person.Salary).FirstOrDefault();
        if (candidate == null)
            return;
        if (Strategy.CarefulHiring && campaign.RecurringRevenue30 + campaign.Ledger.Cash / 8 < campaign.MonthlyFixedCosts + candidate.Person.Salary)
            return;
        if (!EnsureWorkstation(campaign))
            return;
        Act(campaign.Hire(candidate.Person.Id));
    }

    /// <summary>Trouve la place d'un poste complet ; déménage si le local est plein et que l'argent le permet.</summary>
    public static bool EnsureWorkstation(Campaign campaign)
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            if (campaign.InstallWorkstation(buy: true) != null)
                return true;
            var offices = RealEstate.Listings.Where(l => !l.Technical && l.UnlockNode == null).ToList();
            var next = offices.SkipWhile(l => l.Id != campaign.Premises.ListingId).Skip(1).FirstOrDefault();
            if (next == null || !campaign.MoveTo(next.Id).Ok)
                return false;
            Reinstall(campaign);
        }
        return false;
    }

    /// <summary>Après un déménagement : réinstalle les postes depuis les cartons (et achète ce qui manque).</summary>
    public static void Reinstall(Campaign campaign) => campaign.SeatEveryone(buy: true);

    /// <summary>
    /// Le fondateur travaille au terminal : un nombre fixe de mots par jour ouvré, tapés à vitesse normale (environ une minute
    /// de saisie). C'est la même commande que l'interface ; le bot n'a aucun travail gratuit.
    /// </summary>
    private void Type(Campaign campaign)
    {
        if (Strategy.StopTypingWithStaff > 0 && campaign.Staff.Count() >= Strategy.StopTypingWithStaff)
            return;
        int words = Strategy.WordsPerDay;
        while (words > 0)
        {
            var product = campaign.Products.OrderBy(p => p.Priority).FirstOrDefault(p => campaign.CanOpenTerminal(p.Id).Ok);
            if (product == null || !campaign.OpenTerminal(product.Id).Ok)
                return;
            while (words > 0 && campaign.Terminal is { Finished: false } session)
            {
                campaign.SubmitWord(session.Word, session.Word.Length / 4.0);
                words--;
                Words++;
            }
            campaign.CloseTerminal();
        }
    }

    private void Recovery(Campaign campaign)
    {
        if (campaign.Solvency.State == SolvencyState.Healthy)
            return;
        // Dès que l'argent devient tendu : couper le marketing qui coûte plus qu'il ne rapporte
        foreach (var product in campaign.Products.Where(p => p.MarketingBudget > Strategy.Marketing / 4 && p.MarketingBudget > 500))
            Act(campaign.SetMarketing(product.Id, Math.Min(product.MarketingBudget, 300)));
        if (Strategy.Borrow && campaign.LoanTerms().Max > 0)
            Act(campaign.TakeLoan(campaign.LoanTerms().Max));
        if (campaign.Solvency.State is not (SolvencyState.Overdue or SolvencyState.Notice))
            return;
        if (campaign.RecoveryPlanTerms().Allowed)
        {
            Act(campaign.RequestRecoveryPlan());
            return;
        }
        foreach (var product in campaign.Products.Where(p => p.MarketingBudget > 0))
            Act(campaign.SetMarketing(product.Id, 0));
        if (campaign.Solvency.State == SolvencyState.Notice && campaign.Staff.Any())
            Act(campaign.Fire(campaign.Staff.OrderByDescending(p => p.Salary).First().Id));
    }
}
