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

    /// <summary>Nombre de décisions prises (pour mesurer la quantité d'actions routinières).</summary>
    public int Actions { get; private set; }

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
                Act(campaign.Launch(product.Id));
                Act(campaign.SetMarketing(product.Id, Strategy.Marketing));
            }
            return;
        }
        if (product.Queue.Count > 0)
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
        if (campaign.Premises.Desks.Any(d => d.AssignedPersonId == null && Layout.Check(campaign.Premises, d) == WorkstationIssue.None))
            return true;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            if (FindDeskSpot(campaign.Premises) is var (x, y))
                return campaign.BuyAndPlace("chair", x, y - 1).Ok && campaign.BuyAndPlace("desk", x, y).Ok && campaign.BuyAndPlace("pc", x, y).Ok;
            var next = RealEstate.Listings.SkipWhile(l => l.Id != campaign.Premises.ListingId).Skip(1).FirstOrDefault();
            if (next == null || !campaign.MoveTo(next.Id).Ok)
                return false;
            Reinstall(campaign);
        }
        return false;
    }

    /// <summary>Après un déménagement : réinstalle les postes depuis les cartons.</summary>
    public static void Reinstall(Campaign campaign)
    {
        foreach (var desk in campaign.Storage.Where(s => FurnitureCatalog.Get(s.DefId).Kind == FurnitureKind.Desk).ToList())
        {
            if (FindDeskSpot(campaign.Premises) is not var (x, y))
                break;
            var chair = campaign.Storage.FirstOrDefault(s => FurnitureCatalog.Get(s.DefId).Kind == FurnitureKind.Chair);
            if (chair == null || !campaign.PlaceFromStorage(chair.Id, x, y - 1).Ok)
                break;
            campaign.PlaceFromStorage(desk.Id, x, y);
            var computer = campaign.Storage.FirstOrDefault(s => FurnitureCatalog.Get(s.DefId).Kind == FurnitureKind.Computer);
            if (computer != null)
                campaign.PlaceFromStorage(computer.Id, x, y);
        }
        foreach (var person in campaign.People)
        {
            var free = campaign.Premises.Desks.FirstOrDefault(d => d.AssignedPersonId == null && Layout.Check(campaign.Premises, d) == WorkstationIssue.None);
            if (free != null && campaign.Premises.DeskOf(person.Id) == null)
                campaign.AssignDesk(free.Id, person.Id);
        }
    }

    public static (int X, int Y)? FindDeskSpot(Premises premises)
    {
        for (int y = 2; y < premises.Height - 1; y += 3)
        for (int x = Math.Max(premises.LivingWidth, 1); x + 1 < premises.Width; x += 3)
        {
            if (!Layout.CanPlace(premises, FurnitureCatalog.Get("chair"), x, y - 1).Ok)
                continue;
            var chair = new PlacedItem { Id = -99, DefId = "chair", X = x, Y = y - 1 };
            premises.Items.Add(chair);
            bool ok = Layout.CanPlace(premises, FurnitureCatalog.Get("desk"), x, y).Ok;
            premises.Items.Remove(chair);
            if (ok)
                return (x, y);
        }
        return null;
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
