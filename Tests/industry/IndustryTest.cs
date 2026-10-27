using Core.campaign;
using Core.finance;
using Core.industry;
using Core.products;
using Core.space;
using Core.team;
using static Tests.campaign.CampaignTestKit;

namespace Tests.industry;

/// <summary>
/// Lot 5 : lignes de production reliées port à port, stocks et achats, ventes du matériel, recherche. Chaque test vérifie
/// une conséquence : unités fabriquées, matières consommées, argent, blocages et leur reprise.
/// </summary>
public class IndustryTest
{
    /// <summary>Un atelier loué (affiché), du matériel débloqué, un projet « Cartes et kits ».</summary>
    private static (Campaign Campaign, Product Board) Workshop(ulong seed = 42)
    {
        var campaign = NewCampaign(seed);
        campaign.Ledger.Cash = 1_000_000;
        campaign.Unlocked.UnionWith(["multisite", "hardware", "industry"]);
        campaign.ResearchDone.Add("res_boards");
        Assert.That(campaign.RentSite("hw_workshop").Ok, Is.True);
        var board = NewProject(campaign, "makers");
        return (campaign, board);
    }

    private static Person Operator(Campaign campaign)
    {
        var person = HireDeveloper(campaign);
        Assert.That(campaign.AssignTo(person.Id, AssignmentKind.Production, campaign.Premises.Id).Ok, Is.True);
        return person;
    }

    private static void Fill(Campaign campaign, Dictionary<string, int> bom, int units)
    {
        var stock = campaign.Stock.TryGetValue(campaign.Premises.Id, out var s) ? s : campaign.Stock[campaign.Premises.Id] = new();
        foreach (var (item, qty) in bom)
            stock[item] = stock.GetValueOrDefault(item) + qty * units;
    }

    /// <summary>Avance jusqu'à ce que la condition soit vraie (au plus « days » jours).</summary>
    private static void Until(Campaign campaign, Func<bool> done, int days = 20)
    {
        for (int i = 0; i < days * 24 && !done(); i++)
            campaign.Advance(60);
    }

    [Test]
    public void AnAssemblyLineTurnsMaterialsIntoBoxedProductsWithoutLosingAnything()
    {
        var (campaign, board) = Workshop();
        Assert.That(campaign.InstallLayout("assembly").Ok, Is.True);
        var route = IndustryCatalog.ProductRoute(board);
        Assert.That(campaign.LineFor(campaign.Premises, route).Ok, Is.True, campaign.LineFor(campaign.Premises, route).Why);
        Operator(campaign);
        var bom = IndustryCatalog.Bom(board);
        Fill(campaign, bom, 8);
        int site = campaign.Premises.Id;

        Assert.That(campaign.CreateOrder(site, $"product:{board.Id}", OrderMode.Quantity, 5).Ok, Is.True);
        Until(campaign, () => campaign.StockAt(site, $"p:{board.Id}") == 5);

        Assert.That(campaign.StockAt(site, $"p:{board.Id}"), Is.EqualTo(5));
        foreach (var (item, qty) in bom)
            Assert.That(campaign.StockAt(site, item), Is.EqualTo(qty * 3), $"{item} : exactement 5 nomenclatures consommées");
        Assert.That(campaign.FlowUnits, Is.Empty);
        Assert.That(campaign.Orders.Single().Finished, Is.EqualTo(5));
        Assert.That(board.Produced, Is.EqualTo(5));
    }

    [Test]
    public void WithoutAnOperatorNothingMoves()
    {
        var (campaign, board) = Workshop();
        campaign.InstallLayout("assembly");
        Fill(campaign, IndustryCatalog.Bom(board), 3);
        campaign.CreateOrder(campaign.Premises.Id, $"product:{board.Id}", OrderMode.Quantity, 3);
        campaign.AdvanceDays(3);

        Assert.That(campaign.StockAt(campaign.Premises.Id, $"p:{board.Id}"), Is.EqualTo(0));
        Assert.That(campaign.Flow.Values.Any(f => f.Text.Contains("Opérateur absent")), Is.True);
    }

    [Test]
    public void StationsInTheWrongOrderAreDiagnosedNotTeleported()
    {
        var (campaign, board) = Workshop();
        // Logiciel avant l'assemblage : la ligne ne peut pas livrer
        (string Asset, int U, string Facing)[] parts =
            [("flash_station", 0, "sw"), ("conveyor_straight", 2, "se"), ("assembly_station_manual", 3, "sw"), ("conveyor_straight", 5, "se"),
             ("packing_station", 6, "sw"), ("conveyor_straight", 8, "se"), ("conveyor_end", 9, "se")];
        foreach (var (asset, u, facing) in parts)
            Assert.That(campaign.BuyAndPlace(asset, u, 3, facing).Ok, Is.True, asset);
        var result = campaign.CreateOrder(campaign.Premises.Id, $"product:{board.Id}", OrderMode.Quantity, 2);
        Assert.That(result.Message, Does.Contain("ne relie pas"));

        Operator(campaign);
        Fill(campaign, IndustryCatalog.Bom(board), 2);
        campaign.AdvanceDays(2);
        Assert.That(campaign.StockAt(campaign.Premises.Id, $"p:{board.Id}"), Is.EqualTo(0));
        Assert.That(campaign.Flow.Values.Any(f => f.Text.StartsWith("Mauvaise entrée")), Is.True);
    }

    [Test]
    public void ABlockedLineHoldsABoundedBufferAndResumes()
    {
        var (campaign, board) = Workshop();
        campaign.InstallLayout("assembly");
        Operator(campaign);
        int site = campaign.Premises.Id;
        var end = campaign.Premises.Items.Single(i => i.DefId == "conveyor_end");
        (int X, int Y, string F) spot = (end.X, end.Y, end.Facing);
        Assert.That(campaign.Store(end.Id).Ok, Is.True, "sans fin de ligne, la sortie est bloquée");
        var bom = IndustryCatalog.Bom(board);
        Fill(campaign, bom, 20);
        campaign.CreateOrder(site, $"product:{board.Id}", OrderMode.Quantity, 20);
        campaign.AdvanceDays(4);

        int inFlow = campaign.FlowUnits.Count;
        Assert.That(campaign.StockAt(site, $"p:{board.Id}"), Is.EqualTo(0));
        Assert.That(inFlow, Is.LessThanOrEqualTo(3 + 3 * IndustryCatalog.ConveyorCapacity), "trois stations, trois convoyeurs : pas plus");
        Assert.That(campaign.Orders.Single().Started, Is.EqualTo(inFlow), "rien ne se perd ni ne se crée");
        Assert.That(campaign.StockAt(site, "carton"), Is.EqualTo(20 - inFlow));
        Assert.That(campaign.Flow.Values.Any(f => f.Text.StartsWith("Sortie")), Is.True);

        var stored = campaign.Storage.Single(s => s.DefId == "conveyor_end");
        Assert.That(campaign.PlaceFromStorage(stored.Id, spot.X, spot.Y, spot.F).Ok, Is.True);
        Until(campaign, () => campaign.StockAt(site, $"p:{board.Id}") == 20, 30);
        Assert.That(campaign.StockAt(site, $"p:{board.Id}"), Is.EqualTo(20), "la ligne repart et termine tout");
        Assert.That(campaign.FlowUnits, Is.Empty);
    }

    [Test]
    public void TheSuggestedFactoryLineMakesBoardsAndPassesThroughUnneededStations()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 2_000_000;
        campaign.Unlocked.UnionWith(["multisite", "hardware", "industry"]);
        campaign.ResearchDone.UnionWith(["res_boards", "res_smt"]);
        Assert.That(campaign.RentSite("factory").Ok, Is.True);
        var (cost, count) = campaign.SuggestedLine(campaign.Premises);
        Assert.That(count, Is.GreaterThan(10));
        decimal cash = campaign.Ledger.Cash;
        Assert.That(campaign.InstallSuggestedLine().Ok, Is.True);
        Assert.That(cash - campaign.Ledger.Cash, Is.EqualTo(cost), "coût annoncé avant");
        var route = IndustryCatalog.FindRoute("board")!;
        Assert.That(campaign.LineFor(campaign.Premises, route).Ok, Is.True, "placement, refusion, inspection ; le test et l'assemblage laissent passer");

        Operator(campaign);
        Fill(campaign, route.Inputs.ToDictionary(kv => kv.Key, kv => kv.Value), 6);
        campaign.CreateOrder(campaign.Premises.Id, "board", OrderMode.Quantity, 6);
        Until(campaign, () => campaign.StockAt(campaign.Premises.Id, "carte") == 6);
        Assert.That(campaign.StockAt(campaign.Premises.Id, "carte"), Is.EqualTo(6));
    }

    [Test]
    public void ChipsComeFromALoadedToolInACleanRoomAndPrecisionSavesHalf()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 2_000_000;
        campaign.Unlocked.UnionWith(["multisite", "hardware", "industry", "cleanroom"]);
        campaign.ResearchDone.UnionWith(["res_boards", "res_smt", "res_chips"]);
        Assert.That(campaign.RentSite("factory").Ok, Is.True);
        Assert.That(Campaign.Cleanroom(campaign.Premises), Is.True, "l'usine est livrée avec sa salle propre");
        Assert.That(campaign.Premises.Items.Any(i => i.DefId == "precision_station" && i.Included), Is.True, "et son contrôle de précision");
        campaign.Premises.Items.RemoveAll(i => i.DefId == "precision_station");
        campaign.InvalidateLayout();
        var spot = campaign.FindSpot(FurnitureCatalog.Get("chip_fab_tool"))!.Value;
        Assert.That(campaign.BuyAndPlace("chip_fab_tool", spot.X, spot.Y, spot.Facing).Ok, Is.True);
        Operator(campaign);
        int site = campaign.Premises.Id;
        campaign.Stock[site] = new() { ["wafer"] = 2 };
        campaign.CreateOrder(site, "chip", OrderMode.Quantity, 1);
        Until(campaign, () => campaign.StockAt(site, "puce") > 0, 5);
        Assert.That(campaign.StockAt(site, "puce"), Is.EqualTo(10), "sans station de précision, la moitié est rebutée");

        var precision = campaign.FindSpot(FurnitureCatalog.Get("precision_station"))!.Value;
        Assert.That(campaign.BuyAndPlace("precision_station", precision.X, precision.Y, precision.Facing).Ok, Is.True);
        campaign.CreateOrder(site, "chip", OrderMode.Quantity, 1);
        Until(campaign, () => campaign.StockAt(site, "puce") > 10, 5);
        Assert.That(campaign.StockAt(site, "puce"), Is.EqualTo(30));
    }

    [Test]
    public void ProcurementRespectsTheCapCountsGoodsInTransitAndPaysOnce()
    {
        var (campaign, _) = Workshop();
        int site = campaign.Premises.Id;
        campaign.SetProcurementCap(90);
        campaign.SetReserve(site, "kit", 50);
        decimal cash = campaign.Ledger.Cash;
        campaign.AdvanceDays(1);

        var price = IndustryCatalog.Material("kit").Price;
        int ordered = campaign.Purchases.Sum(p => p.Qty);
        Assert.That(ordered, Is.EqualTo((int)(90 / price)), "pas plus que le plafond du mois");
        Assert.That(ordered, Is.LessThan(50), "le plafond, pas la réserve, limite cet achat");
        Assert.That(cash - campaign.Ledger.Cash, Is.GreaterThanOrEqualTo(ordered * price));
        decimal afterOrder = campaign.Ledger.Cash;
        campaign.AdvanceDays(1);
        Assert.That(campaign.Purchases.Sum(p => p.Qty), Is.EqualTo(ordered), "ce qui est en route compte : pas de rachat");

        campaign.AdvanceDays(IndustryCatalog.Material("kit").LeadDays + 1);
        Assert.That(campaign.StockAt(site, "kit"), Is.EqualTo(ordered));
        Assert.That(campaign.Ledger.Entries.Count(e => e.Category == Category.Materials), Is.EqualTo(1), "payé une seule fois, à la commande");
        Assert.That(campaign.ProcurementSpent, Is.LessThanOrEqualTo(90m));

        Assert.That(campaign.BuyMaterial(site, "kit", 10_000).Message, Does.Contain("place"));
    }

    [Test]
    public void HardwareSellsOnlyWhatIsInStockAndCountsEachSaleOnce()
    {
        var (campaign, board) = Workshop();
        FinishQueue(campaign, board);
        Assert.That(campaign.Launch(board.Id).Ok, Is.True);
        campaign.SetMarketing(board.Id, 3_000);
        campaign.AdvanceDays(10);
        Assert.That(board.History.TakeLast(10).Sum(d => d.Sold), Is.EqualTo(0), "rien en stock, rien de vendu");
        Assert.That(board.History.TakeLast(10).Sum(d => d.Revenue), Is.EqualTo(0m));
        Assert.That(board.History.TakeLast(10).Sum(d => d.Lost), Is.GreaterThan(0), "des clients repartent");

        int site = campaign.Premises.Id;
        campaign.Stock[site] = new() { [$"p:{board.Id}"] = 10 };
        decimal salesBefore = campaign.Ledger.Months.Values.Sum(m => m.GetValueOrDefault(Category.Sales));
        campaign.AdvanceDays(10);
        double sold = board.History.TakeLast(10).Sum(d => d.Sold);
        Assert.That(sold, Is.GreaterThan(0));
        Assert.That(campaign.TotalStock($"p:{board.Id}"), Is.EqualTo(10 - sold), "le stock baisse de ce qui est vendu");
        decimal sales = campaign.Ledger.Months.Values.Sum(m => m.GetValueOrDefault(Category.Sales)) - salesBefore;
        decimal revenue = board.History.TakeLast(10).Sum(d => d.Revenue);
        Assert.That((double)revenue, Is.EqualTo(sold * (double)board.Price).Within(0.05), "vente directe : aucune commission");
        Assert.That((double)sales, Is.EqualTo((double)revenue * (1 - (double)Tuning.Products.PaymentFees)).Within(0.5), "une vente, une recette (moins les frais de paiement)");
    }

    [Test]
    public void ResearchNeedsItsEquipmentKeepsProgressWhenPausedAndUnlocksAFamily()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 500_000;
        campaign.Unlocked.UnionWith(["multisite", "hardware"]);
        Assert.That(campaign.TypeAvailable("board"), Is.False);
        Assert.That(campaign.RentSite("hw_workshop").Ok, Is.True);
        var researcher = HireDeveloper(campaign);
        campaign.Seat(researcher, campaign.InstallWorkstation(buy: true)!);
        Assert.That(campaign.SiteOf(researcher), Is.EqualTo(campaign.Premises));
        campaign.AssignTo(researcher.Id, AssignmentKind.Research);
        Assert.That(campaign.StartResearch("res_boards").Ok, Is.True);
        campaign.AdvanceDays(3);
        var project = campaign.Research.Single();
        Assert.That(project.Progress, Is.EqualTo(0), "sans établi ni oscilloscope, rien n'avance");
        Assert.That(campaign.Ledger.Accrued.GetValueOrDefault(Category.Research), Is.EqualTo(0), "pas de budget dépensé pour rien");

        foreach (string lab in new[] { "workbench_electronics" })
        {
            var spot = campaign.FindSpot(FurnitureCatalog.Get(lab))!.Value;
            Assert.That(campaign.BuyAndPlace(lab, spot.X, spot.Y, spot.Facing).Ok, Is.True);
        }
        var bench = campaign.Premises.Items.Last();
        Assert.That(campaign.BuyAndAttach("oscilloscope", bench.Id).Ok, Is.True);
        campaign.AdvanceDays(3);
        double progress = project.Progress;
        Assert.That(progress, Is.GreaterThan(0));
        Assert.That(campaign.Ledger.Accrued.GetValueOrDefault(Category.Research), Is.GreaterThan(0));

        campaign.PauseResearch("res_boards", true);
        campaign.AdvanceDays(3);
        Assert.That(project.Progress, Is.EqualTo(progress), "suspendue : rien ne bouge, rien ne se perd");
        campaign.StartResearch("res_boards");
        for (int i = 0; i < 60 && !campaign.ResearchDone.Contains("res_boards"); i++)
            campaign.AdvanceDays(1);
        Assert.That(campaign.TypeAvailable("board"), Is.True, "la recherche débloque la famille");
    }

    [Test]
    public void TheFirstSeriesCalibrationLowersDefects()
    {
        var (good, goodBoard) = Workshop(5);
        var (bad, badBoard) = Workshop(5);
        FinishQueue(good, goodBoard);
        FinishQueue(bad, badBoard);
        var challenge = good.Challenge(goodBoard.Id);
        Assert.That(challenge.Calibration, Is.True);
        Assert.That(challenge.Score(challenge.Answer), Is.EqualTo(1));
        Assert.That(challenge.Score(challenge.Answer.Select(a => a + 1).ToList()), Is.EqualTo(0.5), "à un cran : la moitié");
        good.LaunchWith(goodBoard.Id, 1);
        bad.LaunchWith(badBoard.Id, 0);
        foreach (var (c, b) in new[] { (good, goodBoard), (bad, badBoard) })
        {
            c.InstallLayout("assembly");
            Operator(c);
            Fill(c, IndustryCatalog.Bom(b), 4);
            c.CreateOrder(c.Premises.Id, $"product:{b.Id}", OrderMode.Quantity, 4);
            Until(c, () => b.Produced == 4);
        }
        Assert.That(goodBoard.DefectRate, Is.LessThan(badBoard.DefectRate * 0.6));
    }
}
