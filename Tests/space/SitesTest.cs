using System.Text.Json.Nodes;
using Core.campaign;
using Core.finance;
using Core.persistence;
using Core.space;
using static Tests.campaign.CampaignTestKit;

namespace Tests.space;

/// <summary>Lot 4 : une entreprise, plusieurs sites. Louer, déménager, fermer, regarder un site sans rien changer.</summary>
public class SitesTest
{
    private static Campaign Rich(ulong seed = 42)
    {
        var campaign = NewCampaign(seed);
        campaign.Ledger.Cash = 200_000;
        return campaign;
    }

    [Test]
    public void ASecondSiteNeedsTheNodeAndAddsItsRentAndDeposit()
    {
        var campaign = Rich();
        Assert.That(campaign.RentSite("small").Message, Does.Contain("Plusieurs sites"));
        campaign.Unlocked.Add("multisite");
        decimal cash = campaign.Ledger.Cash;
        int offers = RealEstate.Offers(campaign.Sites).Count(o => o.Listing.Id == "small");

        var result = campaign.RentSite("small", "Ouchy");

        Assert.That(result.Ok, Is.True, result.Message);
        Assert.That(campaign.Sites, Has.Count.EqualTo(2));
        Assert.That(campaign.Premises.Name, Is.EqualTo("Petit bureau, Ouchy"), "le nouveau site s'affiche");
        Assert.That(campaign.Headquarters.ListingId, Is.EqualTo(RealEstate.Apartment), "la chambre reste le premier site");
        var listing = RealEstate.Get("small");
        Assert.That(cash - campaign.Ledger.Cash, Is.EqualTo(Campaign.OpeningCost(listing)));
        Assert.That(campaign.Ledger.DepositHeld, Is.EqualTo(listing.Deposit));
        Assert.That(RealEstate.Offers(campaign.Sites).Count(o => o.Listing.Id == "small"), Is.EqualTo(offers - 1), "une adresse de moins");
        Assert.That(campaign.MonthlyFixedCosts - NewCampaign().MonthlyFixedCosts, Is.EqualTo(listing.Rent * 1.04m));

        // Le mois suivant : un loyer pour ce site, aucun pour la chambre
        campaign.AdvanceDays(31);
        Assert.That(campaign.Ledger.Bills.Count(b => b.Category == Category.Rent), Is.EqualTo(1));
    }

    [Test]
    public void TechnicalRoomsAreUnlockedInTheTreeAndHostNoDesks()
    {
        var campaign = Rich();
        campaign.Unlocked.Add("multisite");
        Assert.That(campaign.RentSite("server_room").Message, Does.Contain("Salle serveur"));
        Assert.That(campaign.MoveTo("server_room").Ok, Is.False);
        campaign.Unlocked.Add("server_room");
        Assert.That(campaign.MoveTo("server_room").Message, Does.Contain("bureaux"), "le fondateur travaille dans sa chambre");
        Assert.That(campaign.RentSite("server_room").Ok, Is.True);
        Assert.That(campaign.Premises.Items.Any(i => i.DefId.StartsWith("rack_")), Is.False, "les baies du plan ne sont qu'un aperçu");
        Assert.That(campaign.Premises.Items.Any(i => i.DefId == "crac_unit" && i.Included), Is.True, "la climatisation est livrée");
    }

    [Test]
    public void WhoWorksWhereDoesNotDependOnTheSiteOnScreen()
    {
        Campaign Build()
        {
            var c = Rich(7);
            c.Unlocked.Add("multisite");
            var product = NewProject(c);
            var dev = HireDeveloper(c);
            Assert.That(c.RentSite("small").Ok, Is.True);
            var post = c.InstallWorkstation(buy: true)!;
            c.Seat(dev, post);
            Assert.That(c.SiteOf(dev), Is.EqualTo(c.Premises));
            Assert.That(c.SiteOf(c.Founder), Is.EqualTo(c.Headquarters));
            return c;
        }
        var watchingHome = Build();
        var watchingOffice = Build();
        watchingOffice.Id = watchingHome.Id;
        watchingHome.ViewSite(watchingHome.Headquarters.Id);

        watchingHome.AdvanceDays(12);
        watchingOffice.AdvanceDays(12);

        Assert.That(watchingHome.Products[0].Components.Sum(c => c.Level), Is.GreaterThan(0));
        string Without(Campaign c) => SaveStore.SerializeState(c).Replace($"\"SiteId\":{c.SiteId}", "");
        Assert.That(Without(watchingHome), Is.EqualTo(Without(watchingOffice)), "le site affiché ne change rien");
    }

    [Test]
    public void ClosingASiteStoresEverythingNamesWhoLostTheirDeskAndRefundsTheDeposit()
    {
        var campaign = Rich();
        campaign.Unlocked.Add("multisite");
        var dev = HireDeveloper(campaign);
        Assert.That(campaign.RentSite("small").Ok, Is.True);
        var office = campaign.Premises;
        campaign.Seat(dev, campaign.InstallWorkstation(buy: true)!);
        int owned = office.Items.Count(i => !i.Included);
        int stored = campaign.Storage.Count;
        decimal cash = campaign.Ledger.Cash;

        Assert.That(campaign.CloseSite(office.Id).Ok, Is.True);

        Assert.That(campaign.Sites, Has.Count.EqualTo(1));
        Assert.That(campaign.Storage.Count - stored, Is.EqualTo(owned), "rien n'est perdu");
        Assert.That(campaign.PostOf(dev).Post, Is.Null);
        Assert.That(campaign.Notices.Last().Detail, Does.Contain(dev.Name));
        Assert.That(campaign.Ledger.Cash - cash, Is.EqualTo(office.Deposit));
        Assert.That(campaign.Ledger.DepositHeld, Is.EqualTo(0));
        Assert.That(campaign.SiteId, Is.EqualTo(campaign.Headquarters.Id));
        Assert.That(campaign.CloseSite(campaign.Headquarters.Id).Ok, Is.False, "le dernier site ne se ferme pas");
    }

    [Test]
    public void AFormat3SaveBecomesTheFirstSiteWithoutPayingRentTwice()
    {
        string path = Path.Combine(balance.BalanceReportTest.RootFolder(), "Tests", "fixtures", "campaign_v3.json");
        var old = JsonNode.Parse(File.ReadAllText(path))!["Campaign"]!;
        string name = old["Premises"]!["Name"]!.GetValue<string>();
        int items = old["Premises"]!["Items"]!.AsArray().Count;
        decimal cash = old["Ledger"]!["Cash"]!.GetValue<decimal>();

        var campaign = SaveStore.Deserialize(File.ReadAllText(path), out bool migrated);

        Assert.That(migrated, Is.True);
        Assert.That(campaign.Sites, Has.Count.EqualTo(1));
        Assert.That(campaign.Premises.Name, Is.EqualTo(name));
        Assert.That(campaign.Premises.Place, Is.EqualTo("Flon"));
        Assert.That(campaign.Premises.Items, Has.Count.EqualTo(items));
        Assert.That(campaign.Ledger.Cash, Is.EqualTo(cash));
        Assert.That(campaign.People.All(p => campaign.PostOf(p).Post != null), Is.True, "chacun garde son poste");
        Assert.That(campaign.MigrationNote, Does.Contain("premier site"));

        string month = campaign.Month;
        int rentBefore = campaign.Ledger.Bills.Count(b => b.Category == Category.Rent);
        // Un jour plus tard (même mois) : pas de second loyer
        campaign.AdvanceDays(1);
        if (campaign.Month == month)
            Assert.That(campaign.Ledger.Bills.Count(b => b.Category == Category.Rent), Is.EqualTo(rentBefore));
        var again = SaveStore.Deserialize(SaveStore.Serialize(campaign));
        Assert.That(SaveStore.SerializeState(again), Is.EqualTo(SaveStore.SerializeState(campaign)), "aller-retour fidèle au format 4");
    }
}
