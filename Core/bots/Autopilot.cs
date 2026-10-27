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
                Act(campaign.SetHosting(product.Id, true, product.HostingCap * 2));
                return;
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
            var next = RealEstate.Listings.SkipWhile(l => l.Id != campaign.Premises.ListingId).Skip(1).FirstOrDefault();
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
