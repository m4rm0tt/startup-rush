using Core.campaign;
using Core.persistence;
using Core.space;
using static Tests.campaign.CampaignTestKit;

namespace Tests.space;

/// <summary>
/// Lot 7 : finitions des locaux (sols et habillages muraux du pack). Chaque test vérifie une conséquence : ce que le local
/// porte, le prix payé, le froid distribué par le faux plancher, l'ambiance du lambris (plafonnée), la sauvegarde.
/// </summary>
public class FinishesTest
{
    private static Campaign Rich()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 200_000;
        return campaign;
    }

    [Test]
    public void ThePlanGivesTheFloorsAndWallPatternsOfThePremises()
    {
        var plan = Pack.Plan("room_bedroom_start");
        Assert.That(plan.Floors.Single().Material, Is.EqualTo("floor_wood_planks"));
        Assert.That(plan.LeftPattern, Is.EqualTo("pattern_wallpaper"));
        Assert.That(plan.RightPattern, Is.EqualTo("pattern_paint"));
        Assert.That(plan.LeftSwatches["wall"], Is.EqualTo("sage"));

        var room = NewCampaign().Premises;
        Assert.That(room.FloorMaterial, Is.Null, "un local neuf garde les sols de son plan");
        Assert.That(Finishes.FloorGrid(room).Cast<string?>(), Is.All.EqualTo("floor_wood_planks"));
        Assert.That(Finishes.Pattern(room, "left"), Is.EqualTo("pattern_wallpaper"));

        // Une zone posée après recouvre la précédente (carrelage de la cuisine sur la moquette)
        var office = RealEstate.Create(RealEstate.Get("floor"), () => 1);
        var grid = Finishes.FloorGrid(office);
        Assert.That(grid[0, 5], Is.EqualTo("floor_tile_ceramic"));
        Assert.That(grid[5, 5], Is.EqualTo("floor_carpet_tiles"));
        Assert.That(Finishes.Catalog.Select(f => f.Id), Is.All.Matches<string>(Pack.Has), "des sols et des motifs du pack");
    }

    [Test]
    public void RedoingTheFloorOrTheWallsIsPaidOnceAndAppliesEverywhere()
    {
        var campaign = Rich();
        var room = campaign.Premises;
        decimal cash = campaign.Ledger.Cash;

        var floor = campaign.SetFinish("floor_vinyl");
        Assert.That(floor.Ok, Is.True, floor.Message);
        Assert.That(campaign.Ledger.Cash, Is.EqualTo(cash - 25m * 6 * 5), "25 CHF par case, 6 × 5 cases");
        Assert.That(room.FloorMaterial, Is.EqualTo("floor_vinyl"));
        Assert.That(Finishes.FloorGrid(room).Cast<string?>(), Is.All.EqualTo("floor_vinyl"));
        Assert.That(campaign.SetFinish("floor_vinyl").Ok, Is.False, "rien à refaire");

        cash = campaign.Ledger.Cash;
        Assert.That(campaign.SetFinish("pattern_brick").Ok, Is.True);
        Assert.That(campaign.Ledger.Cash, Is.EqualTo(cash - 110m * (5 + 6)), "110 CHF par segment, 5 + 6 segments");
        Assert.That(Finishes.Pattern(room, "left"), Is.EqualTo("pattern_brick"));
        Assert.That(Finishes.Pattern(room, "right"), Is.EqualTo("pattern_brick"));

        Assert.That(campaign.SetFinish("floor_lava").Ok, Is.False);
        campaign.Ledger.Cash = -1_000_000;
        Assert.That(campaign.SetFinish("floor_parquet").Ok, Is.False, "sans argent, pas de travaux");
        Assert.That(room.FloorMaterial, Is.EqualTo("floor_vinyl"));
    }

    [Test]
    public void EveryFinishOfTheCatalogCanBeAppliedAndIsPaid()
    {
        var campaign = Rich();
        campaign.Unlocked.Add("server_room");
        var room = campaign.Premises;
        foreach (var finish in Finishes.Catalog)
        {
            decimal cash = campaign.Ledger.Cash, cost = Finishes.Cost(room, finish);
            var result = campaign.SetFinish(finish.Id);
            Assert.That(result.Ok, Is.True, $"{finish.Id} : {result.Message}");
            Assert.That(Finishes.IsCurrent(room, finish), Is.True, finish.Id);
            Assert.That(campaign.Ledger.Cash, Is.EqualTo(cash - cost), finish.Id);
            Assert.That(cost, Is.GreaterThan(0), finish.Id);
        }
        Assert.That(campaign.Ledger.Entries.Count(e => e.Label.StartsWith("Travaux")), Is.EqualTo(Finishes.Catalog.Count), "chaque chantier figure dans les comptes");
    }

    [Test]
    public void ARaisedFloorSpreadsTheExistingColdWithoutCreatingAny()
    {
        var campaign = Rich();
        var room = campaign.Premises;
        double cold = Infrastructure.Of(room).Cooling;
        Assert.That(cold, Is.GreaterThan(0), "la chambre a un peu de froid naturel");
        Assert.That(campaign.SetFinish("floor_raised").Message, Does.Contain("débloquer"));

        campaign.Unlocked.Add("server_room");
        Assert.That(campaign.SetFinish("floor_raised").Ok, Is.True);
        Assert.That(campaign.SiteInfra(room).Cooling, Is.EqualTo(cold * 1.05).Within(1e-9), "+5 % du froid existant");
        Assert.That(campaign.SetFinish("floor_raised_vent").Ok, Is.True);
        Assert.That(campaign.SiteInfra(room).Cooling, Is.EqualTo(cold * 1.10).Within(1e-9), "+10 % avec les dalles perforées");

        // Sans climatiseur, une salle serveur n'a pas de froid : le faux plancher n'en crée pas
        var server = RealEstate.Create(RealEstate.Get("server_room"), () => 1);
        Assert.That(Finishes.CoolingBonus(server), Is.EqualTo(0.10).Within(1e-9), "le plan livre un faux plancher perforé");
        double coolers = server.Items.Sum(i => i.Def.Cooling);
        Assert.That(coolers, Is.GreaterThan(0));
        Assert.That(Infrastructure.Of(server).Cooling, Is.EqualTo(coolers * 1.10).Within(1e-9));
        server.Items.RemoveAll(i => i.Def.Cooling > 0);
        Assert.That(Infrastructure.Of(server).Cooling, Is.EqualTo(0));

        // À proportion de la surface : le laboratoire avancé n'en a qu'un quart
        var lab = RealEstate.Create(RealEstate.Get("advanced_lab"), () => 1);
        Assert.That(Finishes.CoolingBonus(lab), Is.EqualTo(35.0 / 140 * 0.05).Within(1e-9));
    }

    [Test]
    public void WoodPanelsAddAmbianceWithinTheCommonCap()
    {
        var campaign = Rich();
        var room = campaign.Premises;
        double before = Layout.Comfort(room, 1).Ambiance;
        Assert.That(before + 1, Is.LessThanOrEqualTo(Layout.MaxAmbiance));

        Assert.That(campaign.SetFinish("pattern_wood_panels").Ok, Is.True);
        Assert.That(campaign.SiteComfort(room).Ambiance, Is.EqualTo(before + 1).Within(1e-9), "deux murs en lambris : +1");
        Assert.That(campaign.SetFinish("pattern_acoustic").Ok, Is.True);
        Assert.That(campaign.SiteComfort(room).Ambiance, Is.EqualTo(before).Within(1e-9), "un autre habillage : rien de plus");

        // Le siège livre un mur en lambris ; le plafond d'ambiance tient toujours
        var hq = RealEstate.Create(RealEstate.Get("hq"), () => 1);
        Assert.That(Finishes.Ambiance(hq), Is.EqualTo(0.5));
        hq.WallPattern = "pattern_wood_panels";
        for (int i = 0; i < 20; i++)
            hq.Items.Add(new PlacedItem { Id = 1000 + i, DefId = "plant_floor_tall", X = 1, Y = 1 });
        Assert.That(Layout.Comfort(hq, 1).Ambiance, Is.EqualTo(Layout.MaxAmbiance));
    }

    [Test]
    public void FinishesAreSavedAndAFormat6SaveStillLoads()
    {
        var campaign = Rich();
        campaign.Unlocked.Add("server_room");
        campaign.SetFinish("floor_raised");
        campaign.SetFinish("pattern_wood_panels");
        var loaded = SaveStore.Deserialize(SaveStore.Serialize(campaign), out bool migrated);
        Assert.That(migrated, Is.False);
        Assert.That(loaded.Premises.FloorMaterial, Is.EqualTo("floor_raised"));
        Assert.That(loaded.Premises.WallPattern, Is.EqualTo("pattern_wood_panels"));
        Assert.That(loaded.SiteInfra(loaded.Premises).Cooling, Is.EqualTo(campaign.SiteInfra(campaign.Premises).Cooling).Within(1e-9));

        var plain = NewCampaign();
        string json = SaveStore.Serialize(plain).Replace($"\"Format\":{SaveStore.CurrentFormat}", "\"Format\":6");
        var old = SaveStore.Deserialize(json, out migrated);
        Assert.That(migrated, Is.True);
        Assert.That(old.MigrationNote, Does.Contain("finitions"));
        Assert.That(old.Premises.FloorMaterial, Is.Null, "un local d'avant garde les sols de son plan");
        old.MigrationNote = "";
        Assert.That(SaveStore.SerializeState(old), Is.EqualTo(SaveStore.SerializeState(plain)), "rien n'est converti ni perdu");
    }
}
