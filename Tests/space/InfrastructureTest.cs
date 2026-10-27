using Core.campaign;
using Core.finance;
using Core.space;
using static Tests.campaign.CampaignTestKit;

namespace Tests.space;

/// <summary>
/// Lot 4 : baies, modules, énergie, froid, réseau ; hébergement interne des projets ; offres cloud vendues. Chaque test
/// vérifie une conséquence (capacité, coût, limite) et l'absence de double promesse d'une même unité.
/// </summary>
public class InfrastructureTest
{
    /// <summary>Une campagne avec une salle serveur louée (climatiseur et armoire électrique livrés), affichée.</summary>
    private static Campaign WithServerRoom(ulong seed = 42)
    {
        var campaign = NewCampaign(seed);
        campaign.Ledger.Cash = 500_000;
        campaign.Unlocked.UnionWith(["multisite", "server_room", "high_density", "cloud_offers", "compute_offers"]);
        Assert.That(campaign.RentSite("server_room").Ok, Is.True);
        return campaign;
    }

    private static double Server => FurnitureCatalog.Get("module_server_1u").Hosting;

    private static PlacedItem BuyRack(Campaign campaign, string rack = "rack_standard")
    {
        var spot = campaign.FindSpot(FurnitureCatalog.Get(rack))!.Value;
        var result = campaign.BuyAndPlace(rack, spot.X, spot.Y, spot.Facing);
        Assert.That(result.Ok, Is.True, result.Message);
        return campaign.Premises.Items.Last();
    }

    private static void Mount(Campaign campaign, PlacedItem rack, params string[] modules)
    {
        foreach (string module in modules)
        {
            var result = campaign.BuyAndAttach(module, rack.Id);
            Assert.That(result.Ok, Is.True, $"{module} : {result.Message}");
        }
    }

    [Test]
    public void ModulesFillRackUnitsAndNeedASwitchAndAController()
    {
        var campaign = WithServerRoom();
        // Sans les chemins de câbles livrés avec la salle, pour voir l'effet du câblage
        campaign.Premises.Items.RemoveAll(i => i.DefId.StartsWith("cable_tray"));
        campaign.InvalidateLayout();
        var rack = BuyRack(campaign, "rack_compact");
        Mount(campaign, rack, "module_server_1u", "module_storage_jbod_4u");
        var infra = Infrastructure.Of(campaign.Premises);
        Assert.That(infra.Hosting, Is.EqualTo(0), "sans switch, rien n'est relié");
        Assert.That(infra.Issues, Has.Some.Contains("switch"));

        Mount(campaign, rack, "module_network_switch_1u", "module_storage_2u");
        Assert.That(campaign.BuyAndAttach("module_server_1u", rack.Id).Message, Does.Contain("place"), "8U pleins : 1 + 4 + 1 + 2");
        infra = Infrastructure.Of(campaign.Premises);
        Assert.That(infra.Hosting, Is.EqualTo(Server * 0.9).Within(1e-9), "câblage en désordre : −10 %");
        double storage = FurnitureCatalog.Get("module_storage_2u").Storage + FurnitureCatalog.Get("module_storage_jbod_4u").Storage;
        Assert.That(infra.Storage, Is.EqualTo(storage * 0.9).Within(1e-9), "le tiroir compte avec son contrôleur");

        // Deux baies : 4 points d'organisation pour un câblage parfait
        Mount(campaign, BuyRack(campaign), "module_patch_panel_1u", "module_cable_manager_1u", "module_patch_panel_1u", "module_cable_manager_1u");
        Assert.That(Infrastructure.Of(campaign.Premises).Organization, Is.EqualTo(1), "2 points par baie : câblage parfait");
        Assert.That(Infrastructure.Of(campaign.Premises).Hosting, Is.EqualTo(Server).Within(1e-9));
    }

    [Test]
    public void PowerCoolingAndNetworkLimitWhatASiteDelivers()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 100_000;
        var room = campaign.Premises;
        void Tower()
        {
            var spot = campaign.FindSpot(FurnitureCatalog.Get("server_tower_classic"))!.Value;
            Assert.That(campaign.BuyAndPlace("server_tower_classic", spot.X, spot.Y, spot.Facing).Ok, Is.True);
        }
        Tower();
        double tower = FurnitureCatalog.Get("server_tower_classic").Hosting;
        var one = Infrastructure.Of(room);
        Assert.That(one.Hosting, Is.EqualTo(tower).Within(1e-9), "un serveur tour dans la chambre");
        Assert.That(one.Issues, Is.Empty);

        Tower();
        var two = Infrastructure.Of(room);
        Assert.That(two.Heat, Is.EqualTo(0.7).Within(1e-9));
        Assert.That(two.Hosting, Is.EqualTo(2 * tower * 0.6 / 0.7).Within(1e-9), "la chambre n'évacue que 0,6 kW");
        Assert.That(two.Issues, Has.Some.Contains("refroidissement"));

        // Réseau : 50 de débit dans la chambre, 2 par unité d'hébergement servie
        Assert.That(Infrastructure.Base(RealEstate.Apartment).Network, Is.EqualTo(50));
        var server = WithServerRoom();
        Assert.That(Infrastructure.Of(server.Premises).Network, Is.EqualTo(300 + 1_000), "la salle serveur est livrée avec la fibre");
        server.Premises.Items.RemoveAll(i => i.DefId == "fiber_box");
        server.InvalidateLayout();
        var rack = BuyRack(server);
        Mount(server, rack, "module_network_switch_1u", "module_patch_panel_1u", "module_cable_manager_1u");
        for (int i = 0; i < 17; i++)
            Mount(server, rack, "module_server_1u");
        var full = Infrastructure.Of(server.Premises);
        Assert.That(full.NetworkNeeded, Is.EqualTo(17 * Server * 2).Within(1e-9));
        Assert.That(full.Hosting, Is.EqualTo(17 * Server * 300.0 / full.NetworkNeeded).Within(1e-9), "300 de débit : limité");
        Assert.That(full.Issues, Has.Some.Contains("réseau"));
        double heat = 0.1 + 17 * 0.3;
        Assert.That(full.PowerDraw, Is.EqualTo(4 * Math.Max(0.25, heat / 24) + heat).Within(1e-9), "le climatiseur consomme selon la chaleur (un quart au moins)");
    }

    [Test]
    public void HighDensityRacksAndBlanksRunCooler()
    {
        var campaign = WithServerRoom();
        var standard = BuyRack(campaign);
        Mount(campaign, standard, "module_network_switch_1u", "module_gpu_4u");
        double heat = Infrastructure.Of(campaign.Premises).Heat;
        Mount(campaign, standard, "module_blank_1u", "module_blank_1u", "module_blank_1u", "module_blank_1u");
        Assert.That(Infrastructure.Of(campaign.Premises).Heat, Is.EqualTo(heat * 0.91).Within(1e-9), "obturateurs : 9 % au plus");

        var dense = WithServerRoom(9);
        Mount(dense, BuyRack(dense, "rack_high_density"), "module_network_switch_1u", "module_gpu_4u");
        Assert.That(Infrastructure.Of(dense.Premises).Heat, Is.EqualTo(heat * 0.8).Within(1e-9));
    }

    [Test]
    public void UpsAndSupervisionMakeIncidentsRarer()
    {
        var campaign = WithServerRoom();
        Mount(campaign, BuyRack(campaign), "module_network_switch_1u", "module_server_1u");
        double bare = Infrastructure.Of(campaign.Premises).IncidentChance;
        var spot = campaign.FindSpot(FurnitureCatalog.Get("ups_floor"))!.Value;
        Assert.That(campaign.BuyAndPlace("ups_floor", spot.X, spot.Y, spot.Facing).Ok, Is.True);
        double ups = Infrastructure.Of(campaign.Premises).IncidentChance;
        Assert.That(ups, Is.EqualTo(bare * 0.1).Within(1e-9), "toute la consommation est couverte");
        spot = campaign.FindSpot(FurnitureCatalog.Get("supervision_console"))!.Value;
        Assert.That(campaign.BuyAndPlace("supervision_console", spot.X, spot.Y, spot.Facing).Ok, Is.True);
        Assert.That(Infrastructure.Of(campaign.Premises).IncidentChance, Is.LessThan(ups * 0.5 + 1e-12));
    }

    [Test]
    public void ProjectsHostedInsideUseTheirShareThenOverflowUnderTheCap()
    {
        var campaign = WithServerRoom();
        Mount(campaign, BuyRack(campaign), "module_network_switch_1u", "module_patch_panel_1u", "module_cable_manager_1u", "module_server_1u");
        var product = NewProject(campaign);
        FinishQueue(campaign, product);
        Assert.That(campaign.Launch(product.Id).Ok, Is.True);
        Assert.That(campaign.SetInternalHosting(product.Id, true).Ok, Is.True);
        campaign.AdvanceDays(2);

        Assert.That(product.InternalCapacity, Is.GreaterThan(0));
        Assert.That(product.OverflowActive, Is.False, "la charge tient sur le serveur");
        Assert.That(product.ExternalMonthlyCost, Is.EqualTo(0));
        Assert.That(campaign.Ledger.Accrued.GetValueOrDefault(Category.Energy), Is.GreaterThan(0), "l'électricité est comptée");

        // Beaucoup plus d'utilisateurs : la part interne est pleine, l'externe prend le reste sous le plafond
        product.Users = 20_000;
        campaign.AdvanceDays(1);
        Assert.That(product.InternalCapacity, Is.LessThanOrEqualTo(Server + 1e-9));
        Assert.That(product.OverflowActive, Is.True);
        Assert.That(product.Hosting.MonthlyCost, Is.LessThanOrEqualTo(product.HostingCap));
        Assert.That(product.Capacity, Is.EqualTo(product.InternalCapacity + product.Hosting.Capacity).Within(1e-9));
    }

    [Test]
    public void TheSameUnitIsNeverPromisedTwice()
    {
        var campaign = WithServerRoom();
        Mount(campaign, BuyRack(campaign), "module_network_switch_1u", "module_patch_panel_1u", "module_cable_manager_1u",
            "module_server_1u", "module_server_1u");
        var first = NewProject(campaign);
        var second = NewProject(campaign, "casual");
        foreach (var p in new[] { first, second })
        {
            FinishQueue(campaign, p);
            campaign.Launch(p.Id);
            p.HostingInternal = true;
            p.Users = 5_000;
        }
        Assert.That(campaign.OpenCloudOffer(CloudKind.Hosting).Ok, Is.True);
        campaign.AdvanceDays(1);

        double pool = campaign.Infra().Hosting;
        var offer = campaign.CloudOffers.Single();
        Assert.That(first.InternalCapacity + second.InternalCapacity + offer.Reserved, Is.LessThanOrEqualTo(pool + 1e-9));
        Assert.That(first.InternalCapacity, Is.GreaterThanOrEqualTo(second.InternalCapacity), "l'ordre du portefeuille décide");
        Assert.That(campaign.SetCloudOffer(offer.Id, offer.Price, 10_000).Message, Does.Contain("reste"));
        Assert.That(offer.Reserved, Is.LessThanOrEqualTo(campaign.HostingLeft + 1e-9));
    }

    [Test]
    public void CloudOffersSellOnlyWhatIsAvailableAndDemanded()
    {
        var plain = NewCampaign();
        Assert.That(plain.OpenCloudOffer(CloudKind.Storage).Ok, Is.False, "il faut l'arbre");

        var campaign = WithServerRoom();
        Mount(campaign, BuyRack(campaign), "module_network_switch_1u", "module_storage_2u", "module_patch_panel_1u", "module_cable_manager_1u");
        Assert.That(campaign.OpenCloudOffer(CloudKind.Storage).Ok, Is.True);
        var offer = campaign.CloudOffers.Single();
        decimal cash = campaign.Ledger.Cash;
        campaign.AdvanceDays(30);

        Assert.That(offer.Demand, Is.GreaterThan(0));
        Assert.That(offer.Served, Is.LessThanOrEqualTo(offer.Reserved + 1e-9));
        Assert.That(offer.Revenue30, Is.GreaterThan(0));
        Assert.That(campaign.Ledger.Months.Values.Sum(m => m.GetValueOrDefault(Category.Cloud)), Is.EqualTo(offer.Revenue30), "chaque recette comptée une fois");

        campaign.SetCloudOffer(offer.Id, offer.Price * 3, offer.Reserved);
        Assert.That(campaign.CloudTrends(offer).Any(t => t.Lever == "price" && !t.Positive), Is.True);
        double demand = offer.Demand;
        campaign.AdvanceDays(10);
        Assert.That(offer.Demand, Is.LessThan(demand), "trop cher : les clients partent");

        campaign.SetCloudOffer(offer.Id, offer.Price, offer.Reserved, paused: true);
        campaign.AdvanceDays(1);
        Assert.That(offer.Served, Is.EqualTo(0));
        Assert.That(offer.Revenue.Last(), Is.EqualTo(0m));
    }

    [Test]
    public void EnergyIsBilledWithTheMonth()
    {
        var campaign = WithServerRoom();
        Mount(campaign, BuyRack(campaign), "module_network_switch_1u", "module_gpu_4u");
        campaign.AdvanceDays(35);
        var bill = campaign.Ledger.Bills.FirstOrDefault(b => b.Category == Category.Energy);
        Assert.That(bill, Is.Not.Null);
        double kw = Infrastructure.Of(campaign.Premises).PowerDraw;
        Assert.That((double)bill!.Amount, Is.GreaterThan(kw * 24 * 0.25 * 20), "au moins 20 jours d'électricité du site");
    }
}
