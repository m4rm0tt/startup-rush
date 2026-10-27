using Core.campaign;
using Core.persistence;
using Core.space;
using static Tests.campaign.CampaignTestKit;

namespace Tests.space;

/// <summary>
/// Lot 3 : le mobilier de confort du pack. Chaque groupe agit sur un seul indicateur, avec un plafond et une couverture ;
/// les objets muraux se posent sur les murs du fond ; le luminaire au plafond. Chaque test vérifie un avant/après.
/// </summary>
public class ComfortTest
{
    /// <summary>Une pièce vide de 10 × 8 cases (sans plan : ni murs ni sanitaires « de chez soi »).</summary>
    private static Premises EmptyRoom() => new() { Name = "Pièce de test", Width = 10, Height = 8, EntranceX = 0, EntranceY = 7, EntranceWall = "left" };

    private static int _id = 5000;

    private static PlacedItem Put(Premises room, string defId, int x = 1, int y = 1)
    {
        var item = new PlacedItem { Id = _id++, DefId = defId, X = x, Y = y };
        room.Items.Add(item);
        return item;
    }

    private static PlacedItem Child(Premises room, PlacedItem host, string defId, string socket)
    {
        var item = new PlacedItem { Id = _id++, DefId = defId, HostId = host.Id, Socket = socket, Facing = host.Facing };
        room.Items.Add(item);
        return item;
    }

    [Test]
    public void WallItemsHangOnPlainWallsBlindsOnWindowsNeverOnTheDoor()
    {
        // La chambre : mur de gauche plain, plain, fenêtre, plain, porte ; mur de droite plain, plain, fenêtre, plain, plain, plain
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 10_000;
        var room = campaign.Premises;
        var photo = FurnitureCatalog.Get("photo_abstract");

        Assert.That(Layout.CanPlace(room, photo, 0, 4, "se").Reason, Does.Contain("porte"));
        Assert.That(Layout.CanPlace(room, photo, 0, 2, "se").Reason, Does.Contain("fenêtre"));
        Assert.That(Layout.CanPlace(room, photo, 2, 2, "se").Reason, Does.Contain("mur"), "loin des murs");
        Assert.That(Layout.CanPlace(room, photo, 4, 0, "se").Ok, Is.False, "l'affiche du plan est déjà là");
        Assert.That(Layout.CanPlace(room, FurnitureCatalog.Get("window_blinds"), 0, 0, "se").Reason, Does.Contain("fenêtre"));
        Assert.That(Layout.CanPlace(room, FurnitureCatalog.Get("planning_board"), 5, 0, "se").Reason, Does.Contain("s'arrête"), "un planning prend deux segments");

        var blocked = room.Occupancy();
        double ambiance = campaign.SiteComfort().Ambiance;
        var result = campaign.BuyAndPlace("photo_abstract", 3, 0);
        Assert.That(result.Ok, Is.True, result.Message);
        var hung = room.Items.Last();
        Assert.That((hung.Wall, hung.Facing), Is.EqualTo(("right", "sw")));
        Assert.That(room.Occupancy(), Is.EqualTo(blocked), "un objet mural ne prend aucune case au sol");

        // Dans le coin, l'orientation choisit le mur
        Assert.That(campaign.BuyAndPlace("clock_wall", 0, 0, "se").Ok, Is.True);
        Assert.That(room.Items.Last().Wall, Is.EqualTo("left"));
        Assert.That(campaign.SiteComfort().Ambiance, Is.EqualTo(ambiance + 0.5 + 0.25).Within(1e-9), "photo +0,5, horloge +0,25");
        Assert.That(campaign.Rotate(room.Items.Last().Id).Ok, Is.True);
        Assert.That((room.Items.Last().Wall, room.Items.Last().Facing), Is.EqualTo(("right", "sw")));
        Assert.That(campaign.Rotate(hung.Id).Ok, Is.False, "hors du coin, un objet mural suit son mur");
    }

    [Test]
    public void ThePlanWallDecorBecomesRealItemsAndOlderSavesGetItOnce()
    {
        var campaign = NewCampaign();
        int planned = Pack.Plan("room_bedroom_start").WallItems.Count;
        Assert.That(campaign.Premises.WallItems.Count(), Is.EqualTo(planned));
        Assert.That(campaign.Premises.WallItems.All(i => i.Included), Is.True, "livrés avec le local");

        // Une sauvegarde d'avant : ni objets muraux ni marqueur
        campaign.Premises.Items.RemoveAll(i => i.Wall != null);
        campaign.WallItemsImported = false;
        var loaded = SaveStore.Deserialize(SaveStore.Serialize(campaign));
        Assert.That(loaded.Premises.WallItems.Count(), Is.EqualTo(planned));
        var again = SaveStore.Deserialize(SaveStore.Serialize(loaded));
        Assert.That(again.Premises.WallItems.Count(), Is.EqualTo(planned), "pas de doublon au rechargement");
    }

    [Test]
    public void ACeilingLampTakesNoFloorSpace()
    {
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 10_000;
        var desk = campaign.Premises.Desks.First();
        var lamp = FurnitureCatalog.Get("lamp_ceiling");
        var blocked = campaign.Premises.Occupancy();
        double before = campaign.SiteComfort().Ambiance;

        Assert.That(campaign.BuyAndPlace("lamp_ceiling", desk.X, desk.Y).Ok, Is.True, "au-dessus du bureau");
        Assert.That(campaign.Premises.Occupancy(), Is.EqualTo(blocked));
        Assert.That(Layout.CanPlace(campaign.Premises, lamp, desk.X, desk.Y, "se").Ok, Is.False, "un seul luminaire par case");
        Assert.That(campaign.SiteComfort().Ambiance, Is.GreaterThan(before).Or.EqualTo(Layout.MaxAmbiance));
    }

    [Test]
    public void StorageAndSanitariesGiveBaseComfortForThePeopleTheyCover()
    {
        var room = EmptyRoom();
        Assert.That(Layout.Comfort(room, 4).Basics, Is.EqualTo(0), "rien n'est obligatoire : un manque ne pénalise pas");

        Put(room, "locker");
        Assert.That(Layout.Comfort(room, 4).StorageCoverage, Is.EqualTo(1));
        Assert.That(Layout.Comfort(room, 8).StorageCoverage, Is.EqualTo(0.5), "des casiers pour 4 personnes");
        Assert.That(Layout.Comfort(room, 4).Basics, Is.EqualTo(1.5));

        Put(room, "washbasin");
        Assert.That(Layout.Comfort(room, 4).HygieneCoverage, Is.EqualTo(0), "un lavabo seul ne remplace pas un WC");
        Put(room, "toilet");
        Assert.That(Layout.Comfort(room, 4).HygieneCoverage, Is.EqualTo(0.9).Within(1e-9));
        Put(room, "toilet_stall");
        Assert.That(Layout.Comfort(room, 4).Basics, Is.EqualTo(Layout.MaxBasics).Within(1e-9));
        Assert.That(Layout.Comfort(room, 20).HygieneCoverage, Is.EqualTo(0.5).Within(1e-9), "un WC pour 10 personnes");

        // Ta chambre : la salle de bains de l'appartement compte
        Assert.That(NewCampaign().SiteComfort().HygieneCoverage, Is.EqualTo(1));
    }

    [Test]
    public void BreakServicesServeALimitedNumberOfPeople()
    {
        var room = EmptyRoom();
        Put(room, "kitchen_sink");
        Assert.That(Layout.Comfort(room, 1).Break, Is.EqualTo(Layout.BaseBreak), "un évier seul ne nourrit personne");

        var counter = Put(room, "kitchen_counter", 3, 1);
        Child(room, counter, "coffee_machine", "counter");
        Assert.That(Layout.Comfort(room, 8).Break, Is.EqualTo(3 + 9 + 1).Within(1e-9), "café pour 8, et l'évier complète la cuisine");
        Assert.That(Layout.Comfort(room, 16).Break, Is.EqualTo(3 + 8 * 9 / 16.0 + 10 / 16.0).Within(1e-9), "seize personnes pour une machine");

        Put(room, "vending_drinks", 5, 1);
        Assert.That(Layout.Comfort(room, 16).Break, Is.EqualTo(3 + (8 * 9 + 8 * 6) / 16.0 + 10 / 16.0).Within(1e-9), "chacun prend le meilleur service libre");

        for (int i = 0; i < 10; i++)
            Put(room, "sofa", 1 + i % 8, 4 + i / 8 * 2);
        Assert.That(Layout.Comfort(room, 2).Break, Is.EqualTo(Layout.MaxBreak), "plafonnée");
    }

    [Test]
    public void ACoffeeTableOnlyCompletesASeatingArea()
    {
        var room = EmptyRoom();
        Put(room, "table_coffee");
        Assert.That(Layout.Comfort(room, 4).Break, Is.EqualTo(Layout.BaseBreak));

        Put(room, "sofa", 3, 3);
        double sofa = 3 + 3 * 5 / 4.0;
        Assert.That(Layout.Comfort(room, 4).Break, Is.EqualTo(sofa + 1).Within(1e-9), "la table basse complète le coin pour 4 personnes");
        Assert.That(Layout.Comfort(room, 4).Lounge, Is.EqualTo(2));
    }

    [Test]
    public void PartitionsHelpFocusUpToACapForThePeopleTheyCover()
    {
        var room = EmptyRoom();
        Put(room, "partition_acoustic");
        Assert.That(Layout.Comfort(room, 4).Focus, Is.EqualTo(0.02).Within(1e-9));
        Assert.That(Layout.Comfort(room, 8).Focus, Is.EqualTo(0.01).Within(1e-9), "une cloison pour 4 personnes");
        for (int i = 0; i < 5; i++)
            Put(room, "partition_acoustic", 2 + i, 1);
        Assert.That(Layout.Comfort(room, 8).Focus, Is.EqualTo(Layout.MaxFocus).Within(1e-9));

        // Dans le jeu : la production du poste suit
        var campaign = NewCampaign();
        campaign.Ledger.Cash = 10_000;
        double before = campaign.StationOutput(campaign.Founder);
        var spot = campaign.FindSpot(FurnitureCatalog.Get("partition_acoustic"))!.Value;
        Assert.That(campaign.BuyAndPlace("partition_acoustic", spot.X, spot.Y, spot.Facing).Ok, Is.True);
        Assert.That(campaign.StationOutput(campaign.Founder), Is.EqualTo(before * 1.02).Within(1e-9));
    }

    [Test]
    public void MeetingTablesCountWithTheirChairsAndSmallNotesAreCapped()
    {
        var room = EmptyRoom();
        var table = Put(room, "table_meeting_small", 3, 3);
        Assert.That(Layout.Comfort(room, 1).Coordination, Is.EqualTo(0), "une table sans chaises");
        Child(room, table, "chair_meeting", "chair_0");
        Child(room, table, "chair_meeting", "chair_1");
        Assert.That(Layout.Comfort(room, 1).Coordination, Is.EqualTo(1));
        Assert.That(Layout.Comfort(room, 1).Training, Is.EqualTo(0.05).Within(1e-9));

        for (int i = 0; i < 8; i++)
            Put(room, "notebook", 6, 1);
        Assert.That(Layout.Comfort(room, 1).Coordination, Is.EqualTo(1 + Layout.MaxSmallCoordination), "les petits supports comptent pour 1 au plus");
        Put(room, "whiteboard_mobile", 7, 6);
        Put(room, "whiteboard_mobile", 8, 6);
        Assert.That(Layout.CoordinationBonus(room), Is.EqualTo((int)Layout.MaxCoordination));
    }

    [Test]
    public void TrainingSupportsAreCapped()
    {
        var room = EmptyRoom();
        for (int i = 0; i < 3; i++)
            Put(room, "bookcase", 1 + 2 * i, 1);
        Assert.That(Layout.TrainingBonus(room), Is.EqualTo(0.3).Within(1e-9), "deux bibliothèques utiles");
        Put(room, "presentation_screen", 1, 4);
        Put(room, "poster_schematic", 0, 2);
        Assert.That(Layout.TrainingBonus(room), Is.EqualTo(Layout.MaxTraining).Within(1e-9));
    }

    [Test]
    public void AReceptionDeskBringsOneMoreCandidateEachWeek()
    {
        var plain = NewCampaign(7);
        var welcoming = NewCampaign(7);
        welcoming.Premises.Items.Add(new PlacedItem { Id = welcoming.NewId(), DefId = "reception_desk", X = 2, Y = 1 });
        welcoming.InvalidateLayout();
        plain.AdvanceDays(8);
        welcoming.AdvanceDays(8);
        Assert.That(welcoming.Candidates.Count, Is.EqualTo(plain.Candidates.Count + 1));
    }

    [Test]
    public void TheFirstLaunchLeavesATrophyToPlace()
    {
        var campaign = NewCampaign();
        var product = NewProject(campaign);
        FinishQueue(campaign, product);
        Assert.That(campaign.Launch(product.Id).Ok, Is.True);
        campaign.AdvanceDays(1);

        var trophy = campaign.Storage.Single(s => s.DefId == "trophy_project");
        double before = campaign.SiteComfort().Ambiance;
        var def = FurnitureCatalog.Get("trophy_project");
        var shelf = campaign.Premises.Floor.First(h => Layout.FreeSocket(campaign.Premises, def, h) != null);
        Assert.That(campaign.AttachFromStorage(trophy.Id, shelf.Id).Ok, Is.True, "un trophée se pose sur un meuble");
        Assert.That(campaign.SiteComfort().Ambiance, Is.EqualTo(Math.Min(Layout.MaxAmbiance, before + 0.5)).Within(1e-9));
        Assert.That(campaign.Sell(campaign.Premises.Items.Last().Id).Message, Does.Contain("sans valeur"), "une récompense ne se revend pas");
    }
}
