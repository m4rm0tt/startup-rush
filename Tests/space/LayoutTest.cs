using Core.campaign;
using Core.space;

namespace Tests.space;

public class LayoutTest
{
    /// <summary>Une pièce vide de 10 × 8 cases, porte dans le mur gauche (U = 0) en V = 7.</summary>
    private static Premises EmptyRoom() => new()
    {
        Name = "Pièce de test", Width = 10, Height = 8, EntranceX = 0, EntranceY = 7, EntranceWall = "left",
    };

    private static int _id = 1000;

    private static PlacedItem Put(Premises room, string defId, int x, int y, string facing = "se")
    {
        var item = new PlacedItem { Id = _id++, DefId = defId, X = x, Y = y, Facing = facing };
        room.Items.Add(item);
        return item;
    }

    private static PlacedItem Attach(Premises room, PlacedItem host, string defId, string socket)
    {
        Assert.That(Layout.CanAttach(room, FurnitureCatalog.Get(defId), host, socket).Ok, Is.True, $"{defId} → {socket}");
        var item = new PlacedItem { Id = _id++, DefId = defId, HostId = host.Id, Socket = socket, Facing = host.Facing };
        room.Items.Add(item);
        return item;
    }

    [Test]
    public void ADeskNeedsAChairAComputerAndScreensToBeAWorkstation()
    {
        var room = EmptyRoom();
        var desk = Put(room, "desk_simple", 4, 3);
        var post = room.Workstations.Single();

        Assert.That(Layout.Check(room, post), Is.EqualTo(WorkstationIssue.NoChair));
        Attach(room, desk, "chair_budget", "chair");
        Assert.That(Layout.Check(room, post), Is.EqualTo(WorkstationIssue.NoComputer));
        Attach(room, desk, "tower_office", "tower_floor");
        Assert.That(Layout.Check(room, post), Is.EqualTo(WorkstationIssue.NoScreen), "une tour sans écran ni clavier");
        Attach(room, desk, "monitor_basic", "monitor_main");
        Attach(room, desk, "keyboard_mouse", "keyboard");
        Assert.That(Layout.Check(room, post), Is.EqualTo(WorkstationIssue.None));
    }

    [Test]
    public void ALaptopReplacesTheScreenAndKeyboard()
    {
        var room = EmptyRoom();
        var desk = Put(room, "desk_simple", 4, 3);
        Attach(room, desk, "chair_office", "chair");
        Attach(room, desk, "laptop_standard", "laptop");

        Assert.That(Layout.Check(room, room.Workstations.Single()), Is.EqualTo(WorkstationIssue.None));
        Assert.That(Layout.CanAttach(room, FurnitureCatalog.Get("monitor_basic"), desk, "monitor_main").Ok, Is.False);
        Assert.That(Layout.CanAttach(room, FurnitureCatalog.Get("keyboard_mouse"), desk, "keyboard").Ok, Is.False);
    }

    [Test]
    public void AttachedItemsOnlyGoWhereTheyFit()
    {
        var room = EmptyRoom();
        var desk = Put(room, "desk_simple", 4, 3);
        var counter = Put(room, "kitchen_counter", 8, 0);

        Assert.That(Layout.CanAttach(room, FurnitureCatalog.Get("chair_budget"), desk, "monitor_main").Ok, Is.False);
        Assert.That(Layout.CanAttach(room, FurnitureCatalog.Get("mug"), desk, "desk_right").Ok, Is.True);
        Assert.That(Layout.CanAttach(room, FurnitureCatalog.Get("coffee_machine"), counter, "counter").Ok, Is.True);
        Assert.That(Layout.CanPlace(room, FurnitureCatalog.Get("monitor_basic"), 1, 1, "se").Ok, Is.False, "un écran ne se pose pas au sol");
        Attach(room, desk, "chair_budget", "chair");
        Assert.That(Layout.CanAttach(room, FurnitureCatalog.Get("chair_office"), desk, "chair").Ok, Is.False, "socket déjà pris");
    }

    [Test]
    public void FurnitureCannotOverlapLeaveTheRoomOrBlockAccess()
    {
        var room = EmptyRoom();
        var desk = Put(room, "desk_compact", 4, 3);
        var plant = FurnitureCatalog.Get("plant_floor_medium");
        var access = room.AccessCells(desk).Single();

        Assert.That(Layout.CanPlace(room, plant, 4, 3, "se").Ok, Is.False, "chevauchement");
        Assert.That(Layout.CanPlace(room, FurnitureCatalog.Get("desk_simple"), 9, 3, "sw").Ok, Is.False, "hors du local");
        Assert.That(Layout.CanPlace(room, plant, room.Entrance.X, room.Entrance.Y, "se").Ok, Is.False, "entrée");
        Assert.That(Layout.CanPlace(room, plant, access.X, access.Y, "se").Ok, Is.False, "place devant le bureau réservée");
        Assert.That(Layout.CanPlace(room, plant, 8, 6, "se").Ok, Is.True);
    }

    [Test]
    public void APlacementThatWouldCutOffAWorkstationIsRefused()
    {
        // Un bureau au fond à droite ; une rangée de plantes en U = 7 ne laisse qu'une ouverture en (7, 1)
        var room = EmptyRoom();
        Put(room, "desk_compact", 8, 0);
        for (int v = 0; v < room.Height; v++)
            if (v != 1)
                Put(room, "plant_floor_medium", 7, v);

        Assert.That(Layout.Check(room, room.Workstations.Single()), Is.Not.EqualTo(WorkstationIssue.Unreachable));
        Assert.That(Layout.CanPlace(room, FurnitureCatalog.Get("plant_floor_medium"), 7, 1, "se").Ok, Is.False);
    }

    [Test]
    public void PathsNeverGoThroughFurnitureOrWalls()
    {
        var room = EmptyRoom();
        for (int v = 0; v < room.Height - 1; v++)
            Put(room, "plant_floor_medium", 5, v);
        var blocked = room.Occupancy();

        var path = Pathfinder.FindPath(room, blocked, room.Outside, new Cell(8, 0));

        Assert.That(path, Is.Not.Null);
        Assert.That(path!.First(), Is.EqualTo(room.Outside));
        Assert.That(path.Skip(1).All(room.InBounds), Is.True, "aucune case dans un mur");
        Assert.That(path.Skip(1).Take(path.Count - 2).All(c => !blocked[c.X, c.Y]), Is.True, "aucun meuble traversé");
        for (int i = 1; i < path.Count; i++)
            Assert.That(Math.Abs(path[i].X - path[i - 1].X) + Math.Abs(path[i].Y - path[i - 1].Y), Is.EqualTo(1), "pas de saut");
        Assert.That(path, Does.Contain(new Cell(5, room.Height - 1)), "passe par la seule ouverture");
    }

    [Test]
    public void RotationUsesTheFootprintOfEachOrientation()
    {
        var desk = FurnitureCatalog.Get("desk_simple");
        var se = Premises.Footprint(desk, 3, 3, "se").ToList();
        var sw = Premises.Footprint(desk, 3, 3, "sw").ToList();

        Assert.That(se, Is.EquivalentTo(new[] { new Cell(3, 3), new Cell(3, 4) }));
        Assert.That(sw, Is.EquivalentTo(new[] { new Cell(3, 3), new Cell(4, 3) }));

        var campaign = Campaign.New("Test", "Alex", "Studio", FounderProfile.Developer, 1);
        campaign.Ledger.Cash = 10_000;
        Assert.That(campaign.MoveTo("small").Ok, Is.True);
        var spot = campaign.FindSpot(desk)!.Value;
        Assert.That(campaign.BuyAndPlace("desk_simple", spot.X, spot.Y, spot.Facing).Ok, Is.True);
        var placed = campaign.Premises.Items.Last();
        string before = placed.Facing;
        Assert.That(campaign.Rotate(placed.Id).Ok, Is.True);
        Assert.That(placed.Facing, Is.Not.EqualTo(before));
    }

    [Test]
    public void TheBedroomHoldsAtMostThreeDesks()
    {
        var campaign = Campaign.New("Test", "Alex", "Studio", FounderProfile.Developer, 1);
        campaign.Ledger.Cash = 50_000;
        int placed = 0;
        while (campaign.FindSpot(FurnitureCatalog.Get("desk_compact")) is var (x, y, f) && placed < 5)
        {
            if (!campaign.BuyAndPlace("desk_compact", x, y, f).Ok)
                break;
            placed++;
        }

        Assert.That(campaign.Premises.Desks.Count(), Is.EqualTo(3));
        Assert.That(Layout.CanPlace(campaign.Premises, FurnitureCatalog.Get("desk_compact"), 2, 2, "se").Reason, Does.Contain("bureaux"));
    }

    [Test]
    public void AmbianceHasDiminishingReturns()
    {
        var room = EmptyRoom();
        for (int i = 0; i < 12; i++)
            Put(room, "plant_floor_medium", 1 + i % 8, 1 + i / 8 * 2);

        Assert.That(Layout.SharedMorale(room), Is.EqualTo(Layout.MaxAmbiance));
    }

    [Test]
    public void TheFounderBedHelpsRecoveryOvernight()
    {
        var campaign = Campaign.New("Test", "Alex", "Studio", FounderProfile.Developer, 1);
        Assert.That(Layout.NightRecovery(campaign.Premises), Is.EqualTo(10));
        campaign.Founder.Energy = 50;
        campaign.AdvanceDays(1);
        double withBed = campaign.Founder.Energy;

        var other = Campaign.New("Test", "Alex", "Studio", FounderProfile.Developer, 1);
        other.Premises.Items.RemoveAll(i => i.DefId == "bed_single");
        other.Founder.Energy = 50;
        other.AdvanceDays(1);

        Assert.That(withBed - other.Founder.Energy, Is.EqualTo(10).Within(0.5));
    }

    [Test]
    public void StartingGearHasNoResaleValueButBoughtItemsReturnHalf()
    {
        var campaign = Campaign.New("Test", "Alex", "Studio", FounderProfile.Developer, 1);
        campaign.Ledger.Cash = 10_000;
        var tower = campaign.Premises.Items.First(i => i.DefId == "tower_used");
        decimal cash = campaign.Ledger.Cash;
        Assert.That(campaign.Sell(tower.Id).Ok, Is.True);
        Assert.That(campaign.Ledger.Cash, Is.EqualTo(cash), "la vieille tour était déjà là");

        var spot = campaign.FindSpot(FurnitureCatalog.Get("plant_floor_tall"))!.Value;
        campaign.BuyAndPlace("plant_floor_tall", spot.X, spot.Y, spot.Facing);
        cash = campaign.Ledger.Cash;
        campaign.Sell(campaign.Premises.Items.Last().Id);
        Assert.That(campaign.Ledger.Cash - cash, Is.EqualTo(35m));

        var bed = campaign.Premises.Items.First(i => i.DefId == "bed_single");
        Assert.That(campaign.Sell(bed.Id).Ok, Is.False, "le lit fait partie de la chambre");
    }

    [Test]
    public void MovingKeepsOwnedEquipmentLeavesWhatCameWithThePlaceAndChargesTheDepositOnce()
    {
        var campaign = Campaign.New("Test", "Alex", "Studio", FounderProfile.Developer, 1);
        campaign.Ledger.Cash = 50_000;
        int owned = campaign.Premises.Items.Count(i => !i.Included);
        var listing = RealEstate.Get("small");

        var result = campaign.MoveTo("small");

        Assert.That(result.Ok, Is.True, result.Message);
        Assert.That(campaign.Storage, Has.Count.EqualTo(owned));
        Assert.That(campaign.Storage.Any(s => s.DefId == "bed_single"), Is.False, "le lit reste dans la chambre");
        Assert.That(campaign.Premises.Rent, Is.EqualTo(listing.Rent));
        Assert.That(campaign.Ledger.DepositHeld, Is.EqualTo(listing.Deposit));
        Assert.That(campaign.Premises.Workstations, Is.Empty, "les postes du plan ne sont qu'un aperçu");
        Assert.That(campaign.MoveTo("small").Ok, Is.False);

        campaign.SeatEveryone(buy: false);
        Assert.That(Layout.Check(campaign.Premises, campaign.Premises.SeatOf(campaign.Founder.Id)!), Is.EqualTo(WorkstationIssue.None),
            "le poste se réinstalle avec les cartons, sans achat");
    }
}
