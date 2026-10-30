using Core.campaign;
using Core.finance;
using Core.space;
using static Tests.campaign.CampaignTestKit;

namespace Tests.space;

/// <summary>
/// Les deux plus grands locaux : le datacenter (salle technique à grande échelle) et le siège (étage complet). Ce que chacun
/// livre, ce qu'il permet de plus qu'un local plus petit, et ce qu'il coûte.
/// </summary>
public class DatacenterAndHqTest
{
    private static Campaign Rich()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 2_000_000;
        campaign.Unlocked.UnionWith(["multisite", "server_room", "datacenter", "high_density", "cloud_offers"]);
        return campaign;
    }

    /// <summary>Pose autant de baies standard que possible (au plus « max »), chacune avec son réseau et deux serveurs.</summary>
    private static int FillWithRacks(Campaign campaign, int max)
    {
        int racks = 0;
        while (racks < max && campaign.FindSpot(FurnitureCatalog.Get("rack_standard")) is var (x, y, f) && campaign.BuyAndPlace("rack_standard", x, y, f).Ok)
        {
            int rack = campaign.Premises.Items.Last().Id;
            foreach (string module in new[] { "module_patch_panel_1u", "module_network_switch_1u", "module_server_1u", "module_server_1u" })
                Assert.That(campaign.BuyAndAttach(module, rack).Ok, Is.True, module);
            racks++;
        }
        return racks;
    }

    [Test]
    public void TheDatacenterNeedsItsNodeAndDeliversColdPowerAndSecurityButNoRacks()
    {
        var campaign = Rich();
        campaign.Unlocked.Remove("datacenter");
        Assert.That(campaign.RentSite("datacenter").Ok, Is.False, "le nœud « Datacenter » d'abord");
        campaign.Unlocked.Add("datacenter");
        decimal cash = campaign.Ledger.Cash;
        Assert.That(campaign.RentSite("datacenter").Ok, Is.True);
        var listing = RealEstate.Get("datacenter");
        Assert.That(cash - campaign.Ledger.Cash, Is.EqualTo(Campaign.OpeningCost(listing)));

        var site = campaign.Premises;
        Assert.That(site.Items.Any(i => i.Def.Kind is FurnitureKind.Rack or FurnitureKind.RackModule), Is.False, "les baies du plan ne sont qu'un aperçu");
        Assert.That(site.Items.Where(i => i.Included).Select(i => i.Def.Kind), Does.Contain(FurnitureKind.Utility), "climatiseurs et armoires livrés");
        var infra = Infrastructure.Of(site);
        Assert.That(infra.Cooling, Is.GreaterThan(0), "froid livré");
        Assert.That(infra.PowerSupply, Is.GreaterThan(0), "alimentation livrée");
        Assert.That(infra.Security, Is.GreaterThan(0), "cage de sécurité livrée");
        // La chambre (où travaille le fondateur) ne déménage pas dans une salle technique
        Assert.That(campaign.ViewSite(campaign.Headquarters.Id).Ok, Is.True);
        Assert.That(campaign.MoveTo("datacenter").Ok, Is.False, "personne n'y a de bureau");
    }

    [Test]
    public void TheDatacenterHoldsMoreRacksThanAServerRoomAndHostsThemWithoutOverheating()
    {
        var small = Rich();
        Assert.That(small.RentSite("server_room").Ok, Is.True);
        int inServerRoom = FillWithRacks(small, 30);

        var big = Rich();
        Assert.That(big.RentSite("datacenter").Ok, Is.True);
        int inDatacenter = FillWithRacks(big, 30);

        TestContext.Out.WriteLine($"baies : salle serveur {inServerRoom}, datacenter {inDatacenter}");
        Assert.That(inServerRoom, Is.GreaterThanOrEqualTo(8), "une dizaine de baies tiennent dans la salle serveur");
        // Sa vraie limite est le froid livré : environ quatre baies pleines de serveurs 1U (6 kW chacune)
        double fullRack = 20 * FurnitureCatalog.Get("module_server_1u").Power;
        double cooled = Infrastructure.Of(small.Premises).Cooling / fullRack;
        Assert.That(cooled, Is.InRange(3.5, 5.0), "la climatisation livrée refroidit environ quatre baies pleines");
        Assert.That(inDatacenter, Is.GreaterThanOrEqualTo(2 * inServerRoom), "le datacenter tient au moins deux fois plus de baies");
        big.AdvanceDays(1);
        var infra = big.SiteInfra(big.Premises);
        Assert.That(infra.Hosting, Is.GreaterThan(small.SiteInfra(small.Premises).Hosting * 1.9), "deux fois plus d'hébergement");
        Assert.That(infra.Heat, Is.LessThanOrEqualTo(infra.Cooling), "le froid livré suit");
        Assert.That(infra.PowerDraw, Is.LessThanOrEqualTo(infra.PowerSupply), "l'alimentation livrée suit");
        Assert.That(infra.Issues, Is.Empty, "ni surchauffe, ni manque de courant ou de réseau");
        Assert.That(infra.Factor, Is.GreaterThan(0.95), "au plus le léger malus d'un câblage peu organisé (score par site)");
    }

    [Test]
    public void TheHqDeliversItsSharedSpacesAndTheirEffects()
    {
        var campaign = Rich();
        decimal before = campaign.MonthlyFixedCosts;
        Assert.That(campaign.RentSite("hq").Ok, Is.True);
        var hq = campaign.Premises;
        var listing = RealEstate.Get("hq");
        Assert.That(campaign.MonthlyFixedCosts - before, Is.GreaterThanOrEqualTo(listing.Rent), "le loyer du siège");
        Assert.That(hq.Items.Any(i => i.Def.Kind == FurnitureKind.Desk), Is.False, "les postes du plan ne sont qu'un aperçu");

        // Accueil, cafétéria, sanitaires, réunions, cloisons et lambris : chacun agit sur son indicateur
        var comfort = Layout.Comfort(hq, 30);
        Assert.That(comfort.Recruits, Is.GreaterThan(0), "accueil : candidats en plus");
        Assert.That(comfort.Break, Is.GreaterThan(Layout.Comfort(campaign.Headquarters, 30).Break), "cafétéria : meilleures pauses que la chambre");
        Assert.That(comfort.HygieneCoverage, Is.GreaterThan(0), "sanitaires");
        Assert.That(comfort.Coordination, Is.GreaterThanOrEqualTo(1), "salles de réunion");
        Assert.That(Finishes.Ambiance(hq), Is.GreaterThan(0), "lambris du plan");
    }

    [Test]
    public void TheHqSeatsAFarLargerTeamThanTheOpenSpace()
    {
        int Posts(string listing)
        {
            var campaign = Rich();
            Assert.That(campaign.RentSite(listing).Ok, Is.True);
            int posts = 0;
            // Un poste installé est occupé (chaise attribuée) : le suivant demande un nouveau bureau
            while (posts < 120 && campaign.InstallWorkstation(true) is { } post)
            {
                campaign.Premises.ChairOf(post)!.AssignedPersonId = 10_000 + posts;
                posts++;
            }
            TestContext.Out.WriteLine($"{listing} : {posts} postes");
            return posts;
        }
        int hq = Posts("hq"), large = Posts("large");
        Assert.That(hq, Is.GreaterThan(large), "le siège tient plus de postes que le grand open space");
        Assert.That(hq, Is.GreaterThanOrEqualTo(40), "un étage complet");
    }
}
