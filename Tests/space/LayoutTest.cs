using Core.campaign;
using Core.space;

namespace Tests.space;

public class LayoutTest
{
    private static Premises EmptyOffice() =>
        RealEstate.Create(RealEstate.Get("small"), new Counter().Next);

    private class Counter
    {
        private int _value = 100;
        public int Next() => _value++;
    }

    [Test]
    public void ADeskNeedsAChairBehindAndAComputerToBeAWorkstation()
    {
        var office = EmptyOffice();
        var desk = new PlacedItem { Id = 1, DefId = "desk", X = 5, Y = 4 };
        office.Items.Add(desk);

        Assert.That(Layout.Check(office, desk), Is.EqualTo(WorkstationIssue.NoChair));

        office.Items.Add(new PlacedItem { Id = 2, DefId = "chair", X = 5, Y = 3 });
        Assert.That(Layout.Check(office, desk), Is.EqualTo(WorkstationIssue.NoComputer));

        office.Items.Add(new PlacedItem { Id = 3, DefId = "pc", X = 5, Y = 4, HostId = 1 });
        Assert.That(Layout.Check(office, desk), Is.EqualTo(WorkstationIssue.None));
    }

    [Test]
    public void AChairAloneIsNotAWorkstation()
    {
        var office = EmptyOffice();
        office.Items.Add(new PlacedItem { Id = 2, DefId = "chair", X = 5, Y = 3 });

        Assert.That(Layout.CompleteWorkstations(office), Is.Empty);
    }

    [Test]
    public void FurnitureCannotOverlapLeaveTheRoomOrBlockTheEntrance()
    {
        var office = EmptyOffice();
        office.Items.Add(new PlacedItem { Id = 1, DefId = "desk", X = 5, Y = 4 });

        Assert.That(Layout.CanPlace(office, FurnitureCatalog.Get("plant"), 6, 4).Ok, Is.False, "chevauchement");
        Assert.That(Layout.CanPlace(office, FurnitureCatalog.Get("desk"), 15, 4).Ok, Is.False, "hors du local");
        Assert.That(Layout.CanPlace(office, FurnitureCatalog.Get("plant"), office.Entrance.X, office.Entrance.Y).Ok, Is.False, "entrée");
        Assert.That(Layout.CanPlace(office, FurnitureCatalog.Get("desk"), 3, 0).Ok, Is.False, "pas de place pour la chaise contre le mur");
        Assert.That(Layout.CanPlace(office, FurnitureCatalog.Get("plant"), 5, 3).Ok, Is.False, "place de la chaise réservée");
        Assert.That(Layout.CanPlace(office, FurnitureCatalog.Get("plant"), 10, 6).Ok, Is.True);
    }

    [Test]
    public void APlacementThatWouldCutOffAWorkstationIsRefused()
    {
        // Couloir d'une case : un bureau au fond, on ne peut pas murer son accès
        var office = EmptyOffice();
        office.Items.Add(new PlacedItem { Id = 1, DefId = "desk", X = 14, Y = 1 });
        office.Items.Add(new PlacedItem { Id = 2, DefId = "chair", X = 14, Y = 0 });
        // Mur de plantes sur la colonne 13, sauf une ouverture en (13, 0)
        int id = 10;
        for (int y = 1; y < office.Height; y++)
            office.Items.Add(new PlacedItem { Id = id++, DefId = "plant", X = 13, Y = y });
        // On remplit (15, 0..) pour que le seul accès soit par (13, 0)
        for (int y = 0; y < office.Height; y++)
            if (y != 1)
                office.Items.Add(new PlacedItem { Id = id++, DefId = "plant", X = 15, Y = y });
        Assert.That(Layout.CanPlace(office, FurnitureCatalog.Get("plant"), 12, 0).Ok, Is.False);
    }

    [Test]
    public void PathsNeverGoThroughFurnitureOrWalls()
    {
        var office = EmptyOffice();
        for (int y = 0; y < office.Height - 1; y++)
            office.Items.Add(new PlacedItem { Id = 10 + y, DefId = "plant", X = 8, Y = y });
        var blocked = office.Occupancy();

        var path = Pathfinder.FindPath(office, blocked, office.Outside, new Cell(12, 0));

        Assert.That(path, Is.Not.Null);
        Assert.That(path!.First(), Is.EqualTo(office.Outside));
        Assert.That(path.Skip(1).All(c => office.InBounds(c)), Is.True, "aucune case dans un mur");
        Assert.That(path.Skip(1).Take(path.Count - 2).All(c => !blocked[c.X, c.Y]), Is.True, "aucun meuble traversé");
        for (int i = 1; i < path.Count; i++)
            Assert.That(Math.Abs(path[i].X - path[i - 1].X) + Math.Abs(path[i].Y - path[i - 1].Y), Is.EqualTo(1), "pas de saut");
        Assert.That(path.Any(c => c == new Cell(8, office.Height - 1)), Is.True, "passe par la seule ouverture");
    }

    [Test]
    public void BusinessFurnitureStaysOutOfTheLivingPartOfTheApartment()
    {
        var campaign = Campaign.New("Test", "Alex", "Studio", FounderProfile.Developer, 1);

        var check = Layout.CanPlace(campaign.Premises, FurnitureCatalog.Get("desk"), 1, 7);

        Assert.That(check.Ok, Is.False);
        Assert.That(check.Reason, Does.Contain("privée"));
    }

    [Test]
    public void PlantsHaveDiminishingReturns()
    {
        var office = EmptyOffice();
        for (int i = 0; i < 12; i++)
            office.Items.Add(new PlacedItem { Id = 10 + i, DefId = "plant", X = 4 + i, Y = 8 });

        Assert.That(Layout.SharedMorale(office), Is.EqualTo(Layout.MaxUsefulPlants * 1.0));
    }

    [Test]
    public void MovingKeepsTheEquipmentAndChargesTheDepositOnce()
    {
        var campaign = Campaign.New("Test", "Alex", "Studio", FounderProfile.Developer, 1);
        campaign.Ledger.Cash = 50_000;
        int movable = campaign.Premises.Items.Count(i => i.Def.Movable);
        var listing = RealEstate.Get("small");

        var result = campaign.MoveTo("small");

        Assert.That(result.Ok, Is.True, result.Message);
        Assert.That(campaign.Storage, Has.Count.EqualTo(movable));
        Assert.That(campaign.Premises.Rent, Is.EqualTo(listing.Rent));
        Assert.That(campaign.Ledger.DepositHeld, Is.EqualTo(listing.Deposit));
        Assert.That(campaign.MoveTo("small").Ok, Is.False);
    }
}
