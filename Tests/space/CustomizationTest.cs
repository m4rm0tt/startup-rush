using Core.campaign;
using Core.persistence;
using Core.space;
using static Tests.campaign.CampaignTestKit;

namespace Tests.space;

/// <summary>
/// Demandes des testeurs, lot 2 : capacité réelle des locaux, objets livrés qu'on retire, couleurs des meubles, finitions par
/// mur, par zone ou par case (avec aperçu sans frais), déchets et corbeilles, problèmes signalés sur les équipements.
/// </summary>
public class CustomizationTest
{
    private static Campaign Rich()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 2_000_000;
        campaign.CheatUnlockAll();
        return campaign;
    }

    [Test]
    public void TheBedroomCapacityFollowsItsLease()
    {
        var campaign = NewCampaign();
        var capacity = campaign.Capacity(campaign.Premises);
        Assert.That(capacity.LimitedByLease, Is.True);
        Assert.That(capacity.Installed, Is.EqualTo(1), "le bureau de départ");
        Assert.That(capacity.Complete, Is.EqualTo(1));
        Assert.That(capacity.Max, Is.EqualTo(campaign.Premises.MaxDesks), "chambre : un bureau par place du bail");
        Assert.That(capacity.People, Is.EqualTo(1));
    }

    [Test]
    public void AnOfficeCapacityCountsTheDesksThatReallyFit()
    {
        var campaign = Rich();
        Assert.That(campaign.MoveTo("small").Ok, Is.True);
        var before = campaign.Capacity(campaign.Premises);
        Assert.That(before.LimitedByLease, Is.False);
        Assert.That(before.Spare, Is.GreaterThan(0));
        var spot = campaign.FindSpot(FurnitureCatalog.Get("desk_compact"));
        Assert.That(spot, Is.Not.Null);
        var (x, y, f) = spot!.Value;
        Assert.That(campaign.BuyAndPlace("desk_compact", x, y, f).Ok, Is.True);
        var after = campaign.Capacity(campaign.Premises);
        Assert.That(after.Installed, Is.EqualTo(before.Installed + 1));
        Assert.That(after.Spare, Is.LessThan(before.Spare), "la place prise par le bureau n'est plus libre");
        Assert.That(after.Max, Is.LessThanOrEqualTo(before.Max + 1));
    }

    [Test]
    public void IncludedDecorCanBeRemovedButNotTheBuilding()
    {
        var campaign = Rich();
        var bed = campaign.Premises.Floor.First(i => i.Def.Kind == FurnitureKind.Bed);
        Assert.That(campaign.Store(bed.Id).Ok, Is.False, "le lit de la chambre est indispensable");
        var plant = campaign.Premises.Floor.First(i => i.Included && i.Def.Kind == FurnitureKind.Plant);
        Assert.That(campaign.Store(plant.Id).Ok, Is.True, "le décor livré se retire");
        Assert.That(campaign.Storage.Any(s => s.Id == plant.Id), Is.True, "il part dans les cartons");

        Assert.That(campaign.MoveTo("floor").Ok, Is.True);
        var structural = campaign.Premises.Items.First(i => i.Included && i.Def.Structural);
        Assert.That(campaign.Store(structural.Id).Ok, Is.False);
        Assert.That(campaign.Sell(structural.Id).Ok, Is.False);
        Assert.That(campaign.Premises.Find(structural.Id), Is.Not.Null, "il reste en place");
        var decor = campaign.Premises.Floor.FirstOrDefault(i => i.Included && !i.Def.Structural && i.Def.Kind is FurnitureKind.Plant or FurnitureKind.Lounge or FurnitureKind.Ambiance);
        Assert.That(decor, Is.Not.Null, "le bureau d'équipe livre du décor");
        Assert.That(campaign.Sell(decor!.Id).Ok, Is.True, "le décor livré se jette (sans valeur)");
    }

    [Test]
    public void FurnitureColorsAreValidatedSavedAndFollowTheItemIntoTheCartons()
    {
        var campaign = Rich();
        Assert.That(campaign.MoveTo("small").Ok, Is.True);
        var (x, y, f) = campaign.FindSpot(FurnitureCatalog.Get("sofa"))!.Value;
        Assert.That(campaign.BuyAndPlace("sofa", x, y, f).Ok, Is.True);
        var sofa = campaign.Premises.Items.Last();
        var channels = Campaign.ColorChannels(sofa);
        Assert.That(channels.Select(c => c.Channel), Does.Contain("fabric"));
        Assert.That(campaign.SetItemColor(sofa.Id, "fabric", "rose").Ok, Is.True);
        Assert.That(campaign.SetItemColor(sofa.Id, "fabric", "nope").Ok, Is.False, "teinte inconnue");
        Assert.That(campaign.SetItemColor(sofa.Id, "screen", "sky").Ok, Is.False, "partie non recolorable");
        Assert.That(Campaign.ColorChannels(sofa).First(c => c.Channel == "fabric").Current, Is.EqualTo("rose"));

        var loaded = SaveStore.Deserialize(SaveStore.Serialize(campaign));
        Assert.That(loaded.Premises.Find(sofa.Id)!.Swatches["fabric"], Is.EqualTo("rose"), "sauvegardé");

        Assert.That(campaign.Store(sofa.Id).Ok, Is.True);
        Assert.That(campaign.PlaceFromStorage(sofa.Id, x, y, f).Ok, Is.True);
        Assert.That(campaign.Premises.Find(sofa.Id)!.Swatches["fabric"], Is.EqualTo("rose"), "la couleur suit l'objet dans les cartons");
        Assert.That(campaign.ResetItemColors(sofa.Id).Ok, Is.True);
        Assert.That(campaign.Premises.Find(sofa.Id)!.Swatches, Is.Empty);
    }

    [Test]
    public void AFinishCanTargetOneWallOneZoneOrOneTileAndCostsWhatItCovers()
    {
        var campaign = Rich();
        Assert.That(campaign.MoveTo("small").Ok, Is.True);
        var site = campaign.Premises;
        var paint = Finishes.Get("pattern_brick");

        string? rightBefore = Finishes.Pattern(site, "right");
        decimal cash = campaign.Ledger.Cash;
        Assert.That(campaign.SetFinish("pattern_brick", new FinishTarget("left"), "sage").Ok, Is.True);
        Assert.That(cash - campaign.Ledger.Cash, Is.EqualTo(paint.Price * Finishes.WallSegments(site, "left")));
        Assert.That(Finishes.Pattern(site, "left"), Is.EqualTo("pattern_brick"));
        Assert.That(Finishes.Pattern(site, "right"), Is.EqualTo(rightBefore), "l'autre mur ne change pas");
        Assert.That(site.RightWallColor, Is.Null);
        Assert.That(Finishes.WallColor(site, "left"), Is.EqualTo("sage"));

        cash = campaign.Ledger.Cash;
        Assert.That(campaign.SetFinish("floor_parquet", FinishTarget.Rect(2, 2, 1, 1), "oak").Ok, Is.True);
        Assert.That(cash - campaign.Ledger.Cash, Is.EqualTo(Finishes.Get("floor_parquet").Price), "une case se paie une case");
        Assert.That(Finishes.FloorGrid(site)[2, 2], Is.EqualTo("floor_parquet"));
        Assert.That(campaign.SetFinish("floor_parquet", FinishTarget.Rect(2, 2, 1, 1), "nope").Ok, Is.False, "couleur inconnue");

        var zone = Finishes.ZoneAt(site, new Cell(2, 2));
        Assert.That(zone.Width * zone.Depth, Is.EqualTo(1), "la case refaite est devenue sa propre zone");
        Assert.That(campaign.SetFinish("floor_carpet", FinishTarget.Rect(0, 0, site.Width + 1, 1)).Ok, Is.False, "hors du local");

        var loaded = SaveStore.Deserialize(SaveStore.Serialize(campaign));
        Assert.That(Finishes.Pattern(loaded.Premises, "left"), Is.EqualTo("pattern_brick"));
        Assert.That(Finishes.FloorGrid(loaded.Premises)[2, 2], Is.EqualTo("floor_parquet"));

        Assert.That(campaign.ResetFinish(FinishKind.Wall).Ok, Is.True);
        Assert.That(site.LeftPattern, Is.Null);
        Assert.That(site.LeftWallColor, Is.Null);
    }

    [Test]
    public void APreviewCopyNeverTouchesThePremises()
    {
        var campaign = Rich();
        var site = campaign.Premises;
        decimal cash = campaign.Ledger.Cash;
        var copy = site.PreviewCopy();
        Finishes.Apply(copy, Finishes.Get("floor_carpet"), FinishTarget.Everywhere, "navy");
        Finishes.Apply(copy, Finishes.Get("pattern_brick"), new FinishTarget("right"), "sand");
        Assert.That(site.FloorMaterial, Is.Null);
        Assert.That(site.FloorPatches, Is.Empty);
        Assert.That(site.RightPattern, Is.Null);
        Assert.That(campaign.Ledger.Cash, Is.EqualTo(cash));
        Assert.That(Finishes.FloorGrid(copy)[0, 0], Is.EqualTo("floor_carpet"));
        Assert.That(Finishes.Pattern(copy, "right"), Is.EqualTo("pattern_brick"));
    }

    [Test]
    public void WithoutBinsLitterPilesUpCostsMoraleAndCanBePickedUp()
    {
        var campaign = Rich();
        Assert.That(campaign.MoveTo("small").Ok, Is.True);
        foreach (var bin in campaign.Premises.Items.Where(i => i.DefId is "trash_bin" or "recycling_station").ToList())
            campaign.Premises.Items.Remove(bin);
        campaign.InvalidateLayout();
        HireDeveloper(campaign);
        HireDeveloper(campaign);
        campaign.AdvanceDays(14);
        var site = campaign.Premises;
        Assert.That(site.Litter, Is.Not.Empty, "sans corbeille, les déchets finissent par terre");
        Assert.That(Campaign.MessPenalty(site), Is.GreaterThan(0));
        Assert.That(campaign.Notices.Any(n => n.Key == $"litter:{site.Id}" && !n.Resolved), Is.True);

        var first = site.Litter[0];
        Assert.That(campaign.PickUpLitter(first.X, first.Y).Ok, Is.True);
        Assert.That(campaign.PickUpLitter(-5, -5).Ok, Is.False);
        Assert.That(campaign.CleanUp().Ok, Is.True);
        Assert.That(site.Litter, Is.Empty);

        // Avec assez de corbeilles, plus rien ne traîne
        var (x, y, f) = campaign.FindSpot(FurnitureCatalog.Get("recycling_station"))!.Value;
        Assert.That(campaign.BuyAndPlace("recycling_station", x, y, f).Ok, Is.True);
        campaign.AdvanceDays(14);
        Assert.That(site.Litter, Is.Empty);
        Assert.That(SaveStore.Deserialize(SaveStore.Serialize(campaign)).Premises.Litter, Is.Empty);
    }

    [Test]
    public void TheBedroomBinKeepsTheFounderTidy()
    {
        var campaign = NewCampaign();
        Assert.That(Campaign.BinCapacity(campaign.Premises), Is.GreaterThanOrEqualTo(1));
        campaign.AdvanceDays(20);
        Assert.That(campaign.Premises.Litter, Is.Empty);
    }

    [Test]
    public void ARackWithoutSwitchIsFlaggedUntilTheSwitchIsInstalled()
    {
        var campaign = Rich();
        Assert.That(campaign.RentSite("server_room").Ok, Is.True);
        var (x, y, f) = campaign.FindSpot(FurnitureCatalog.Get("rack_standard"))!.Value;
        Assert.That(campaign.BuyAndPlace("rack_standard", x, y, f).Ok, Is.True);
        var rack = campaign.Premises.Items.Last();
        Assert.That(campaign.BuyAndAttach("module_server_1u", rack.Id).Ok, Is.True);
        var issues = campaign.ItemIssues(campaign.Premises);
        Assert.That(issues.ContainsKey(rack.Id), Is.True);
        Assert.That(issues[rack.Id].Select(i => i.Fix), Does.Contain("module_network_switch_1u"));

        Assert.That(campaign.BuyAndAttach("module_network_switch_1u", rack.Id).Ok, Is.True);
        Assert.That(campaign.ItemIssues(campaign.Premises).TryGetValue(rack.Id, out var left) && left.Any(i => i.Fix == "module_network_switch_1u"), Is.False,
            "l'indicateur disparaît une fois le switch posé");
    }
}
