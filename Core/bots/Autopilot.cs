using Core.campaign;
using Core.finance;
using Core.products;
using Core.space;
using Core.team;
using Core.work;

namespace Core.bots;

/// <summary>
/// Une stratégie de joueur, décrite par quelques réglages. Elle ne sert qu'aux mesures d'équilibrage et aux captures :
/// elle joue avec les mêmes commandes que l'interface, sans aucun accès privilégié.
/// </summary>
public record Strategy(string Name, string Description)
{
    /// <summary>Accepte des prestations (et combien en parallèle par personne libre).</summary>
    public bool TakeContracts { get; init; } = true;

    public int MaxContracts { get; init; } = 1;
    public DeadlineOption Deadline { get; init; } = DeadlineOption.Standard;

    /// <summary>Plus petite et plus grosse demande acceptée (points).</summary>
    public double MaxContractSize { get; init; } = 400;

    public bool Capitalize { get; init; }

    /// <summary>Jour où commence le produit (-1 : jamais).</summary>
    public int ProductDay { get; init; } = -1;

    public string Segment { get; init; } = "freelances";
    public RevenueModel Model { get; init; } = RevenueModel.Subscription;
    public string[] Modules { get; init; } = ["login", "dashboard", "subscribe", "support"];

    /// <summary>Modules ajoutés version après version.</summary>
    public string[] LaterModules { get; init; } = ["export", "notifications", "stats", "search"];

    /// <summary>Trésorerie au-dessus de laquelle on embauche, et taille maximale de l'équipe.</summary>
    public decimal HireAbove { get; init; } = 40_000;

    public int MaxStaff { get; init; }
    public decimal MarketingBudget { get; init; }
    public bool KeepSupport { get; init; } = true;
    public bool FixBugs { get; init; } = true;
    public bool UpgradeComputers { get; init; }

    /// <summary>Le fondateur reste sur les prestations même quand le produit existe.</summary>
    public bool FounderOnContracts { get; init; } = true;

    /// <summary>Utilise les leviers de redressement (plan, coupes) quand ça va mal.</summary>
    public bool Recover { get; init; } = true;

    public decimal PriceFactor { get; init; } = 1;

    /// <summary>N'embauche que si les recettes du mois écoulé couvrent les charges fixes après l'embauche.</summary>
    public bool CarefulHiring { get; init; } = true;

    /// <summary>Emprunte à la banque quand la trésorerie devient tendue.</summary>
    public bool Borrow { get; init; } = true;
}

/// <summary>Joue une stratégie, une décision par jour ouvré à 8 h.</summary>
public class Autopilot(Strategy strategy)
{
    public Strategy Strategy { get; } = strategy;
    public int? ProductId { get; private set; }
    private int _laterIndex;

    /// <summary>Fait tourner la campagne pendant un nombre de jours, en décidant chaque matin.</summary>
    public void Play(Campaign campaign, int days)
    {
        for (int d = 0; d < days && campaign.Status == CampaignStatus.Running; d++)
        {
            if (GameTime.IsWorkday(campaign.Minute))
                Decide(campaign);
            campaign.AdvanceDays(1);
        }
    }

    public void Decide(Campaign campaign)
    {
        if (Strategy.UpgradeComputers)
            UpgradeComputers(campaign);
        Contracts(campaign);
        Product(campaign);
        Hiring(campaign);
        Assign(campaign);
        if (Strategy.Recover)
            Recovery(campaign);
    }

    private void Contracts(Campaign campaign)
    {
        if (!Strategy.TakeContracts)
            return;
        // Fondateur concentré sur le produit : de nouvelles prestations seulement si la trésorerie l'exige
        if (!Strategy.FounderOnContracts && ProductId != null && campaign.Ledger.Cash > 8_000 && !campaign.Staff.Any())
            return;
        int capacity = Math.Max(1, ContractWorkers(campaign).Count) * Strategy.MaxContracts;
        foreach (var offer in campaign.Offers.OrderByDescending(o => o.Price / (decimal)Math.Max(1, o.TotalEffort)).ToList())
        {
            if (campaign.ActiveContracts.Count() >= capacity)
                break;
            if (offer.TotalEffort > Strategy.MaxContractSize)
                continue;
            campaign.AcceptContract(offer.Id, Strategy.Deadline, Strategy.Capitalize);
        }
    }

    private List<Person> ContractWorkers(Campaign campaign) =>
        campaign.People.Where(p => p.IsFounder ? Strategy.FounderOnContracts || ProductId == null : p.Assignment.Kind is AssignmentKind.Contract or AssignmentKind.None).ToList();

    private void Product(Campaign campaign)
    {
        if (Strategy.ProductDay < 0 || campaign.Day < Strategy.ProductDay)
            return;
        var product = ProductId is int id ? campaign.FindProduct(id) : null;
        if (product == null)
        {
            var segment = ProductCatalog.Segment(Strategy.Segment);
            decimal price = Strategy.Model == RevenueModel.Commission ? 10 : segment.ReferencePrice * Strategy.PriceFactor;
            var (_, created) = campaign.CreateProduct(campaign.MainCompany.Id, "Produit " + Strategy.Name, Strategy.Segment, Strategy.Model, price);
            product = created!;
            ProductId = product.Id;
            var home = product.Draft.Pages[0];
            foreach (string module in Strategy.Modules)
                home.Modules.Add(ProductDesign.NewModule(product, module));
            campaign.StartBuild(product.Id);
            return;
        }

        if (product.Build != null && campaign.BuildReady(product))
            campaign.Publish(product.Id);

        if (product.IsLive)
        {
            product.MarketingBudget = Strategy.MarketingBudget;
            product.MarketingChannelId = product.ArchetypeId == "saas" ? "search" : "social";
            // Hébergement ajusté à la charge, avec une marge
            double load = Market.Load(product, product.Live!);
            var plan = ProductCatalog.Hosting.FirstOrDefault(h => load < h.Capacity * 0.7) ?? ProductCatalog.Hosting[^1];
            product.HostingId = plan.Id;
            if (Strategy.FixBugs && product.Bugs >= 6)
                campaign.PlanBugFixes(product.Id);

            // Version suivante quand la précédente est publiée depuis un mois
            if (product.Build == null && _laterIndex < Strategy.LaterModules.Length && campaign.Minute - (product.PublishedAt ?? 0) > 30L * GameTime.MinutesPerDay)
            {
                var page = product.Draft.Pages[0];
                if (page.Modules.Count >= 5)
                {
                    var next = ProductDesign.NewPage(product, $"Page {product.Draft.Pages.Count + 1}");
                    product.Draft.Pages.Add(next);
                    page.Links.Add(next.Id);
                    page = next;
                }
                page.Modules.Add(ProductDesign.NewModule(product, Strategy.LaterModules[_laterIndex++]));
                campaign.StartBuild(product.Id);
            }
        }
    }

    private void Hiring(Campaign campaign)
    {
        if (campaign.Staff.Count() >= Strategy.MaxStaff || campaign.Ledger.Cash < Strategy.HireAbove || campaign.Solvency.State != SolvencyState.Healthy)
            return;
        if (Strategy.CarefulHiring)
        {
            string month = GameTime.ToDate(campaign.Minute).AddMonths(-1).ToString("yyyy-MM");
            decimal income = campaign.Ledger.Months.GetValueOrDefault(month)?
                .Where(t => t.Key is Category.Contracts or Category.Subscriptions or Category.Ads or Category.Commissions).Sum(t => t.Value) ?? 0;
            // Il faut que les recettes, plus six mois de réserve, couvrent les charges avec le nouveau salaire
            decimal salary = campaign.Candidates.FirstOrDefault()?.Person.Salary ?? 7_000;
            if (income + campaign.Ledger.Cash / 6 < campaign.MonthlyFixedCosts + salary)
                return;
        }
        var candidate = campaign.Candidates.OrderByDescending(c => c.Person.Skill(c.Person.BestDiscipline) / (double)c.Person.Salary).FirstOrDefault();
        if (candidate == null)
            return;
        if (!EnsureWorkstation(campaign))
            return;
        campaign.Hire(candidate.Person.Id);
    }

    /// <summary>Trouve une place pour un poste complet ; déménage si le local est plein et que la trésorerie le permet.</summary>
    public static bool EnsureWorkstation(Campaign campaign)
    {
        if (campaign.Premises.Desks.Any(d => d.AssignedPersonId == null && Layout.Check(campaign.Premises, d) == WorkstationIssue.None))
            return true;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            var spot = FindDeskSpot(campaign.Premises);
            if (spot != null)
            {
                var (x, y) = spot.Value;
                if (!campaign.BuyAndPlace("chair", x, y - 1).Ok)
                    return false;
                if (!campaign.BuyAndPlace("desk", x, y).Ok || !campaign.BuyAndPlace("pc", x, y).Ok)
                    return false;
                return true;
            }
            // Plus de place : le local suivant
            var next = RealEstate.Listings.SkipWhile(l => l.Id != campaign.Premises.ListingId).Skip(1).FirstOrDefault();
            if (next == null || !campaign.MoveTo(next.Id).Ok)
                return false;
            Reinstall(campaign);
        }
        return false;
    }

    /// <summary>Après un déménagement : réinstalle les postes depuis les cartons, puis le reste.</summary>
    public static void Reinstall(Campaign campaign)
    {
        var desks = campaign.Storage.Where(s => FurnitureCatalog.Get(s.DefId).Kind == FurnitureKind.Desk).ToList();
        foreach (var desk in desks)
        {
            var spot = FindDeskSpot(campaign.Premises);
            if (spot == null)
                break;
            var (x, y) = spot.Value;
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

    /// <summary>Une place de bureau (x, y) dont la chaise (x, y−1) est libre, en rangées régulières pour garder des allées.</summary>
    public static (int X, int Y)? FindDeskSpot(Premises premises)
    {
        for (int y = 2; y < premises.Height - 1; y += 3)
        for (int x = Math.Max(premises.LivingWidth, 1); x + 1 < premises.Width; x += 3)
        {
            if (Layout.CanPlace(premises, FurnitureCatalog.Get("chair"), x, y - 1).Ok)
            {
                // Vérifie aussi le bureau en simulant la chaise posée
                var chair = new PlacedItem { Id = -99, DefId = "chair", X = x, Y = y - 1 };
                premises.Items.Add(chair);
                bool ok = Layout.CanPlace(premises, FurnitureCatalog.Get("desk"), x, y).Ok;
                premises.Items.Remove(chair);
                if (ok)
                    return (x, y);
            }
        }
        return null;
    }

    private static void UpgradeComputers(Campaign campaign)
    {
        if (campaign.Ledger.Cash < 15_000)
            return;
        var laptop = campaign.Premises.Items.FirstOrDefault(i => i.DefId == "laptop");
        if (laptop == null)
            return;
        var desk = campaign.Premises.Find(laptop.HostId ?? -1);
        if (desk == null)
            return;
        campaign.Store(laptop.Id);
        campaign.BuyAndPlace("pc", desk.X, desk.Y);
    }

    private void Assign(Campaign campaign)
    {
        var product = ProductId is int id ? campaign.FindProduct(id) : null;
        var contracts = campaign.ActiveContracts.OrderBy(c => c.Deadline).ToList();
        int i = 0;
        bool supportTaken = false;
        foreach (var person in campaign.People)
        {
            // Les engagements passent d'abord : le fondateur finit ses contrats avant de retourner au produit
            bool contractsCovered = contracts.All(c => campaign.People.Any(p => !p.IsFounder && p.Assignment.Is(AssignmentKind.Contract, c.Id)));
            bool productPerson = product != null && (!person.IsFounder || contracts.Count == 0 || ProductBlocked(campaign, product) && contractsCovered);
            if (product != null && product.IsLive && Strategy.KeepSupport && !supportTaken && !person.IsFounder && product.Users > 300)
            {
                campaign.Assign(person.Id, AssignmentKind.Support, product.Id);
                supportTaken = true;
                continue;
            }
            if (productPerson && (product!.Build != null || campaign.WorkFor(WorkOwner.Product, product.Id).Any(w => !w.Done)))
            {
                campaign.Assign(person.Id, AssignmentKind.Product, product.Id);
                continue;
            }
            if (productPerson && product!.IsLive && !person.IsFounder)
            {
                campaign.Assign(person.Id, AssignmentKind.Maintenance, product.Id);
                continue;
            }
            if (contracts.Count > 0)
            {
                campaign.Assign(person.Id, AssignmentKind.Contract, contracts[i++ % contracts.Count].Id);
                continue;
            }
            if (product != null)
                campaign.Assign(person.Id, product.IsLive ? AssignmentKind.Maintenance : AssignmentKind.Product, product.Id);
        }
    }

    /// <summary>L'équipe produit ne peut rien faire (compétence manquante) : le fondateur vient débloquer.</summary>
    private static bool ProductBlocked(Campaign campaign, Product product) =>
        campaign.WorkFor(WorkOwner.Product, product.Id).Any(w => !w.Done)
        && campaign.People.Where(p => !p.IsFounder && p.Assignment.Is(AssignmentKind.Product, product.Id))
            .All(p => p.Status.StartsWith("Bloqué") || p.Status.StartsWith("Aucune tâche"));

    private void Recovery(Campaign campaign)
    {
        if (Strategy.Borrow && campaign.Solvency.State != SolvencyState.Healthy && campaign.LoanTerms().Max > 0)
            campaign.TakeLoan(campaign.LoanTerms().Max);
        if (campaign.Solvency.State is not (SolvencyState.Overdue or SolvencyState.Notice))
            return;
        if (campaign.RecoveryPlanTerms().Allowed)
        {
            campaign.RequestRecoveryPlan();
            return;
        }
        foreach (var product in campaign.Products)
            product.MarketingBudget = 0;
        if (campaign.Solvency.State == SolvencyState.Notice && campaign.Staff.Any())
            campaign.Fire(campaign.Staff.OrderByDescending(p => p.Salary).First().Id);
    }
}
