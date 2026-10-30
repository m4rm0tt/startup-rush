using Core.campaign;
using Core.finance;
using Core.industry;
using Core.persistence;
using Core.products;
using Core.space;
using Core.team;
using static Tests.campaign.CampaignTestKit;

namespace Tests.campaign;

/// <summary>
/// Fondations de la version complète : résultat par site réconcilié avec le total, transferts de stock sans perte ni
/// duplication, stock d'un site quitté, métiers d'opérateur et de chercheur, studio tenu par compétence, concurrents tirés
/// à la demande, migration du format 7.
/// </summary>
public class FoundationsTest
{
    private static (Campaign Campaign, Premises Workshop, Product Board) TwoSites(ulong seed = 42)
    {
        var campaign = NewCampaign(seed);
        campaign.Ledger.Cash = 1_000_000;
        campaign.Unlocked.UnionWith(["multisite", "hardware", "industry"]);
        campaign.ResearchDone.Add("res_boards");
        Assert.That(campaign.RentSite("hw_workshop").Ok, Is.True);
        var board = NewProject(campaign, "makers");
        return (campaign, campaign.Premises, board);
    }

    private static void Put(Campaign campaign, int siteId, string item, int qty)
    {
        if (!campaign.Stock.TryGetValue(siteId, out var stock))
            campaign.Stock[siteId] = stock = new();
        stock[item] = stock.GetValueOrDefault(item) + qty;
    }

    // ------------------------------------------------------------------ Résultat par site

    [Test]
    public void EachMonthTheSitesAddUpExactlyToTheCompanyTotal()
    {
        var (campaign, workshop, _) = TwoSites();
        var dev = HireDeveloper(campaign);
        Assert.That(campaign.SiteOf(dev), Is.EqualTo(workshop), "le poste est installé sur le site affiché : l'atelier");
        Assert.That(campaign.BuyMaterial(workshop.Id, "pcb", 20).Ok, Is.True);
        campaign.AdvanceDays(70);

        Assert.That(campaign.Ledger.Months.Keys, Is.EquivalentTo(campaign.Ledger.SiteMonths.Keys));
        foreach (var (month, totals) in campaign.Ledger.Months)
            foreach (var category in totals.Keys.Union(campaign.Ledger.SiteMonths[month].Values.SelectMany(s => s.Keys)))
            {
                decimal bySite = campaign.Ledger.SiteMonths[month].Values.Sum(s => s.GetValueOrDefault(category));
                Assert.That(bySite, Is.EqualTo(totals.GetValueOrDefault(category)), $"{month} {category} : la somme des sites égale le total");
            }

        var all = campaign.Ledger.SiteMonths.Values;
        decimal workshopRent = all.Sum(m => m.GetValueOrDefault(workshop.Id)?.GetValueOrDefault(Category.Rent) ?? 0);
        decimal hqRent = all.Sum(m => m.GetValueOrDefault(campaign.Headquarters.Id)?.GetValueOrDefault(Category.Rent) ?? 0);
        Assert.That(workshopRent, Is.LessThan(0), "le loyer de l'atelier est à l'atelier");
        Assert.That(-workshopRent % workshop.Rent, Is.EqualTo(0m), "des loyers entiers de l'atelier, rien d'autre");
        Assert.That(hqRent, Is.EqualTo(0m), "l'appartement de départ n'a pas de loyer");
        Assert.That(all.Sum(m => m.GetValueOrDefault(workshop.Id)?.GetValueOrDefault(Category.Salaries) ?? 0), Is.LessThan(0), "le salaire suit le poste");
        Assert.That(all.Sum(m => m.GetValueOrDefault(workshop.Id)?.GetValueOrDefault(Category.Materials) ?? 0), Is.EqualTo(-IndustryCatalog.Material("pcb").Price * 20));
        Assert.That(campaign.SiteResults(campaign.Month).Sum(r => r.Result), Is.EqualTo(Campaign.OperatingResult(campaign.Ledger.Months[campaign.Month])));
    }

    // ------------------------------------------------------------------ Transferts

    [Test]
    public void ATransferMovesStockWithoutLosingOrDuplicatingAndArrivesTheNextDay()
    {
        var (campaign, workshop, board) = TwoSites();
        int hq = campaign.Headquarters.Id;
        Put(campaign, workshop.Id, "pcb", 30);
        Put(campaign, workshop.Id, $"p:{board.Id}", 5);
        decimal cash = campaign.Ledger.Cash;

        var sent = campaign.TransferStock(workshop.Id, hq, "pcb", 12);
        Assert.That(sent.Ok, Is.True, sent.Message);
        Assert.That(campaign.StockAt(workshop.Id, "pcb"), Is.EqualTo(18), "parti tout de suite");
        Assert.That(campaign.StockAt(hq, "pcb"), Is.EqualTo(0), "pas encore arrivé");
        Assert.That(campaign.InTransit(hq, "pcb"), Is.EqualTo(12), "en route, compté une fois");
        Assert.That(campaign.StockEverywhere("pcb"), Is.EqualTo(30), "rien de perdu, rien de dupliqué");
        Assert.That(campaign.Ledger.Cash, Is.EqualTo(cash - Campaign.TransferCost("pcb", 12)), "seul le transport est payé");
        Assert.That(campaign.Ledger.Entries.Last().Category, Is.EqualTo(Category.Logistics));

        campaign.AdvanceDays(2);
        Assert.That(campaign.StockAt(hq, "pcb"), Is.EqualTo(12));
        Assert.That(campaign.StockAt(workshop.Id, "pcb") + campaign.StockAt(hq, "pcb"), Is.EqualTo(30));
        Assert.That(campaign.Purchases, Is.Empty);
        Assert.That(campaign.Ledger.Months.Values.Sum(m => m.Where(t => Ledger.IsIncome(t.Key)).Sum(t => t.Value)), Is.EqualTo(0m), "un transfert interne ne crée aucune recette");

        Assert.That(campaign.TransferStock(workshop.Id, hq, "pcb", 99).Ok, Is.False, "on ne transfère pas plus qu'on n'a");
        Assert.That(campaign.TransferStock(workshop.Id, workshop.Id, "pcb", 1).Ok, Is.False, "pas vers le même site");
        int room = campaign.StockRoom(campaign.Headquarters);
        Put(campaign, workshop.Id, "pcb", room + 5);
        Assert.That(campaign.TransferStock(workshop.Id, hq, "pcb", room + 1).Ok, Is.False, "la place d'arrivée est vérifiée à l'envoi");
        Assert.That(campaign.TransferStock(workshop.Id, hq, $"p:{board.Id}", 5).Ok, Is.True, "les produits finis voyagent aussi");
    }

    [Test]
    public void LeavingASiteSendsItsStockToTheHeadquartersAndStopsItsOrders()
    {
        var (campaign, workshop, board) = TwoSites();
        Assert.That(campaign.InstallLayout("assembly").Ok, Is.True);
        Put(campaign, workshop.Id, "pcb", 7);
        Put(campaign, workshop.Id, $"p:{board.Id}", 3);
        Assert.That(campaign.CreateOrder(workshop.Id, $"product:{board.Id}", OrderMode.Quantity, 5).Ok, Is.True);
        Assert.That(campaign.SetReserve(workshop.Id, "pcb", 50).Ok, Is.True);

        Assert.That(campaign.CloseSite(workshop.Id).Ok, Is.True);
        Assert.That(campaign.Stock.ContainsKey(workshop.Id), Is.False, "plus de stock fantôme sur un site fermé");
        Assert.That(campaign.Orders, Is.Empty);
        Assert.That(campaign.Reserves, Is.Empty);
        Assert.That(campaign.StockEverywhere("pcb"), Is.EqualTo(7));
        Assert.That(campaign.StockEverywhere($"p:{board.Id}"), Is.EqualTo(3));
        campaign.AdvanceDays(1);
        int hq = campaign.Headquarters.Id;
        Assert.That(campaign.StockAt(hq, "pcb") + campaign.InTransit(hq, "pcb"), Is.EqualTo(7), "livré au siège, ou en attente de place");
    }

    // ------------------------------------------------------------------ Métiers

    [Test]
    public void OperatorsAndResearchersAreRealJobsThatOpenWithTheirActivity()
    {
        var campaign = NewCampaign();
        Assert.That(campaign.OpenJobs(), Is.EqualTo(Recruitment.ProjectJobs), "au départ : les métiers des projets");
        Assert.That(campaign.Founder.Skills.Keys, Does.Contain(Discipline.Operations).And.Contain(Discipline.Research));

        // Sans métier en plus, les tirages sont ceux d'avant
        var a = Recruitment.Generate(new SimRandom(7), 0, 1, 10);
        var b = Recruitment.Generate(new SimRandom(7), 0, 1, 10, Recruitment.ProjectJobs);
        Assert.That(a.Person.Skills, Is.EqualTo(b.Person.Skills));
        Assert.That(a.Person.Salary, Is.EqualTo(b.Person.Salary));

        campaign.Unlocked.Add("industry");
        campaign.ResearchDone.Add("res_boards");
        Assert.That(campaign.OpenJobs(), Does.Contain(Discipline.Operations).And.Contain(Discipline.Research));
        var random = new SimRandom(11);
        var specialists = Enumerable.Range(0, 200).Select(i => Recruitment.Generate(random, 0, i, 50, campaign.OpenJobs()).Person).ToList();
        var op = specialists.First(p => p.BestDiscipline == Discipline.Operations);
        var researcher = specialists.First(p => p.BestDiscipline == Discipline.Research);
        Assert.That(op.Title, Does.Contain("opérateur"));
        Assert.That(researcher.Title, Does.Contain("chercheur"));
        Assert.That(Person.OperatorPace(80), Is.GreaterThan(Person.OperatorPace(30)));
        Assert.That(Person.OperatorPace(30), Is.EqualTo(1.03).Within(0.01), "quelqu'un d'autre au poste : à peu près le rythme de base");
    }

    [Test]
    public void ASkilledOperatorRunsTheLineFaster()
    {
        // Heures de travail pour sortir 8 unités (le stock de base tient la nomenclature de 8 cartes et leurs cartons)
        int Hours(double skill)
        {
            var (campaign, workshop, board) = TwoSites();
            Assert.That(campaign.InstallLayout("assembly").Ok, Is.True);
            var person = HireDeveloper(campaign);
            person.Skills[Discipline.Operations] = skill;
            Assert.That(campaign.AssignTo(person.Id, AssignmentKind.Production, workshop.Id).Ok, Is.True);
            foreach (var (item, qty) in IndustryCatalog.Bom(board))
                Put(campaign, workshop.Id, item, qty * 8);
            Assert.That(campaign.CreateOrder(workshop.Id, $"product:{board.Id}", OrderMode.Quantity, 8).Ok, Is.True);
            int hours = 0;
            for (; hours < 24 * 20 && campaign.StockAt(workshop.Id, $"p:{board.Id}") < 8; hours++)
                campaign.Advance(60);
            Assert.That(campaign.StockAt(workshop.Id, $"p:{board.Id}"), Is.EqualTo(8));
            return hours;
        }
        Assert.That(Hours(85), Is.LessThan(Hours(10)));
    }

    // ------------------------------------------------------------------ Concurrents et sauvegarde

    [Test]
    public void LateMarketsGetTheirCompetitorsWithTheirFirstProjectWithoutShiftingTheGame()
    {
        var plain = NewCampaign(5);
        var other = NewCampaign(5);
        var aiSegments = ProductCatalog.Segments.Where(s => !Campaign.FoundingTypes.Contains(s.TypeId)).Select(s => s.Id).ToHashSet();
        Assert.That(plain.Competitors.Any(c => aiSegments.Contains(c.SegmentId)), Is.False, "aucun concurrent IA ou XR au départ");

        other.Unlocked.Add("xr");
        var before = other.Random.State;
        var xr = NewProject(other, "xr_gamers");
        Assert.That(other.Competitors.Count(c => c.SegmentId == "xr_gamers"), Is.InRange(1, 2));
        Assert.That(other.Random.State, Is.EqualTo(before), "le hasard de la partie n'a pas bougé");

        var third = NewCampaign(5);
        third.Unlocked.Add("xr");
        third.AdvanceDays(3);
        NewProject(third, "xr_gamers");
        Assert.That(third.Competitors.Where(c => c.SegmentId == "xr_gamers").Select(c => (c.Name, c.Appeal)),
            Is.EqualTo(other.Competitors.Where(c => c.SegmentId == xr.SegmentId).Select(c => (c.Name, c.Appeal))), "mêmes concurrents, quel que soit le moment");
    }

    [Test]
    public void AFormat7SaveGetsTheNewJobSkillsAndKeepsEverythingElse()
    {
        var campaign = NewCampaign();
        HireDeveloper(campaign);
        campaign.AdvanceDays(3);
        string json = SaveStore.Serialize(campaign).Replace($"\"Format\":{SaveStore.CurrentFormat}", "\"Format\":7");
        var node = System.Text.Json.Nodes.JsonNode.Parse(json)!;
        foreach (var person in node["Campaign"]!["People"]!.AsArray())
        {
            person!["Skills"]!.AsObject().Remove("Operations");
            person["Skills"]!.AsObject().Remove("Research");
        }
        var loaded = SaveStore.Deserialize(node.ToJsonString(), out bool migrated);
        Assert.That(migrated, Is.True);
        foreach (var person in loaded.People)
        {
            var original = campaign.FindPerson(person.Id)!;
            Assert.That(person.Skill(Discipline.Operations), Is.EqualTo(original.Skill(Discipline.Operations)), "déduit des autres compétences, comme à la création");
            Assert.That(person.Skill(Discipline.Research), Is.EqualTo(original.Skill(Discipline.Research)));
            Assert.That(person.Skill(Discipline.Code), Is.EqualTo(original.Skill(Discipline.Code)));
        }
        Assert.That(loaded.Ledger.Cash, Is.EqualTo(campaign.Ledger.Cash));
        Assert.That(loaded.MigrationNote, Does.Contain("opérateur"));
    }
}
