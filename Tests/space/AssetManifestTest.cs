using Core.space;

namespace Tests.space;

/// <summary>Le moteur lit le pack embarqué : chaque meuble jouable y existe, et chaque plan de location donne un local valide.</summary>
public class AssetManifestTest
{
    [Test]
    public void EveryPlayableFurnitureIsAPackAssetWithItsGeometry()
    {
        foreach (var def in FurnitureCatalog.All)
        {
            Assert.That(Pack.Has(def.Id), Is.True, $"absent du pack : {def.Id}");
            Assert.That(def.Pack.Facings, Is.Not.Empty, def.Id);
            if (def.Kind == FurnitureKind.Desk)
                Assert.That(def.Pack.Facing(def.Pack.DefaultFacing).Sockets.Keys.Any(s => s.StartsWith("chair")), Is.True, $"{def.Id} : pas de chaise");
            if (def.Kind is FurnitureKind.Chair or FurnitureKind.Laptop or FurnitureKind.Tower or FurnitureKind.Monitor or FurnitureKind.Keyboard)
                Assert.That(def.Attached, Is.True, $"{def.Id} devrait s'accrocher à un bureau");
        }
    }

    [Test]
    public void EveryRentalPlanBecomesAValidPremises()
    {
        int id = 1;
        foreach (var listing in RealEstate.Listings)
        {
            var premises = RealEstate.Create(listing, () => id++);
            var occupied = new HashSet<Cell>();
            foreach (var item in premises.Floor)
                foreach (var cell in premises.Footprint(item))
                {
                    Assert.That(premises.InBounds(cell), Is.True, $"{listing.Id} : {item.DefId} hors du local");
                    Assert.That(occupied.Add(cell), Is.True, $"{listing.Id} : chevauchement en {cell}");
                }
            Assert.That(occupied, Does.Not.Contain(premises.Entrance), $"{listing.Id} : entrée bloquée");
            foreach (var child in premises.Items.Where(i => i.HostId != null))
            {
                var host = premises.Find(child.HostId!.Value)!;
                Assert.That(Layout.CanAttach(premises, child.Def, host, child.Socket!, child.Id).Ok, Is.True, $"{listing.Id} : {child.DefId} → {child.Socket}");
            }
        }
    }

    [Test]
    public void TheStartingBedroomGivesTheFounderACompleteWorkstation()
    {
        var campaign = Core.campaign.Campaign.New("Test", "Alex", "Studio", Core.campaign.FounderProfile.Developer, 1);
        var post = campaign.Premises.SeatOf(campaign.Founder.Id);

        Assert.That(campaign.Premises.PlanId, Is.EqualTo("room_bedroom_start"));
        Assert.That(post, Is.Not.Null);
        Assert.That(Layout.Check(campaign.Premises, post!), Is.EqualTo(WorkstationIssue.None));
        Assert.That(campaign.Premises.Items.Any(i => i.DefId == "bed_single" && i.Included), Is.True);
    }
}
