using Core.businesses;
using Core.company;
using Core.finance;
using Core.investments;
using Core.simulation;
using Core.staff;
using Core.upgrades;

namespace Tests.empire;

/// <summary>Matériel, employés, boîtes, bourse et factures.</summary>
[TestFixture]
public class EmpireSystemsTest
{
    [Test]
    public void UpgradeIsBoughtInOrderAndStopsAtMax()
    {
        var track = new UpgradeTrackInfo(UpgradeTrackId.Chair, "Chaise", "clic",
            [new("Tabouret", 0, 1), new("Chaise", 100, 2)]);
        var upgrades = new Upgrades([track]);
        var wallet = new Wallet(500);

        Assert.That(upgrades.TryBuy(UpgradeTrackId.Chair, wallet), Is.True);
        Assert.That(upgrades.ClickMultiplier, Is.EqualTo(2));
        Assert.That(upgrades[UpgradeTrackId.Chair].IsMaxed, Is.True);
        Assert.That(upgrades.TryBuy(UpgradeTrackId.Chair, wallet), Is.False);
        Assert.That(wallet.Balance, Is.EqualTo(400));
    }

    [Test]
    public void UpgradeRefusedWhenTooExpensive()
    {
        var upgrades = new Upgrades(new GameConfig().UpgradeTracks);
        var wallet = new Wallet(10);

        Assert.That(upgrades.TryBuy(UpgradeTrackId.Computer, wallet), Is.False);
        Assert.That(upgrades[UpgradeTrackId.Computer].Level, Is.EqualTo(0));
    }

    [Test]
    public void WorkSpeedMultipliesComputerScreensCoffeeAndHousing()
    {
        var upgrades = new Upgrades(new GameConfig().UpgradeTracks);
        var wallet = new Wallet(1_000_000);

        upgrades.TryBuy(UpgradeTrackId.Computer, wallet); // ×1.4
        upgrades.TryBuy(UpgradeTrackId.Coffee, wallet); // ×1.1

        Assert.That(upgrades.WorkSpeedMultiplier, Is.EqualTo(1.4 * 1.1).Within(1e-9));
    }

    [Test]
    public void HousingRentIsPartOfMonthlyCost()
    {
        var upgrades = new Upgrades(new GameConfig().UpgradeTracks);
        double before = upgrades.MonthlyCost;

        upgrades.TryBuy(UpgradeTrackId.Housing, new Wallet(1_000_000));

        Assert.That(upgrades.MonthlyCost, Is.GreaterThan(before));
    }

    [Test]
    public void StaffAddsCapacitySpeedAndSalaries()
    {
        var staff = new Staff(new GameConfig().EmployeeRoles);
        var wallet = new Wallet(100_000);

        staff.Hire(EmployeeRole.Developer, wallet);
        staff.Hire(EmployeeRole.Developer, wallet);
        staff.Hire(EmployeeRole.Salesperson, wallet);

        Assert.That(staff.TotalCount, Is.EqualTo(3));
        Assert.That(staff.ExtraCapacity, Is.EqualTo(2));
        Assert.That(staff.SpeedMultiplier, Is.EqualTo(1.1).Within(1e-9));
        Assert.That(staff.AutoAccept, Is.True);
        Assert.That(staff.MonthlySalaries, Is.EqualTo(2 * 4_000 + 4_500));
    }

    [Test]
    public void FiringRemovesEmployeeAndUnknownFireFails()
    {
        var staff = new Staff(new GameConfig().EmployeeRoles);
        staff.Hire(EmployeeRole.Intern, new Wallet(10_000));

        Assert.That(staff.Fire(EmployeeRole.Intern), Is.True);
        Assert.That(staff.Fire(EmployeeRole.Intern), Is.False);
        Assert.That(staff.TotalCount, Is.EqualTo(0));
    }

    [Test]
    public void BusinessCostGrowsAndIncomeGrowsWithLevel()
    {
        var group = new BusinessGroup([new BusinessInfo("Agence", 1000, 10, "")], 1.5, 100_000, 2);
        var wallet = new Wallet(10_000);

        group.TryInvest(0, wallet);
        group.TryInvest(0, wallet);

        var business = group.Businesses[0];
        Assert.That(business.Level, Is.EqualTo(2));
        Assert.That(business.IncomePerSecond, Is.EqualTo(20));
        Assert.That(business.NextCost, Is.EqualTo(2250));
        Assert.That(wallet.Balance, Is.EqualTo(10_000 - 1000 - 1500));
        Assert.That(group.Valuation, Is.EqualTo(2500));
    }

    [Test]
    public void BusinessesPayPassiveIncomeDoubledByHolding()
    {
        var group = new BusinessGroup([new BusinessInfo("Agence", 1000, 10, "")], 1.5, 5_000, 2);
        var wallet = new Wallet(6_000);
        group.TryInvest(0, wallet);

        group.Update(10, wallet);
        Assert.That(wallet.Balance, Is.EqualTo(5_100));

        group.TryBuyHolding(wallet);
        group.Update(10, wallet);
        Assert.That(wallet.Balance, Is.EqualTo(100 + 200));
    }

    [Test]
    public void MarketIsReproducibleWithSameSeed()
    {
        var config = new GameConfig();
        var a = new Market(config.MarketAssets, new Random(5), 60, 30);
        var b = new Market(config.MarketAssets, new Random(5), 60, 30);

        for (int i = 0; i < 100; i++)
        {
            a.Update(0.5, new Wallet(0));
            b.Update(0.5, new Wallet(0));
        }

        Assert.That(a.Assets.Select(x => x.Price), Is.EqualTo(b.Assets.Select(x => x.Price)));
        Assert.That(a.Assets[3].Price, Is.Not.EqualTo(config.MarketAssets[3].StartPrice));
    }

    [Test]
    public void BuyingAndSellingAtSamePriceGivesMoneyBack()
    {
        var market = new Market(new GameConfig().MarketAssets, new Random(1), 60, 30);
        var wallet = new Wallet(1_000);

        Assert.That(market.Buy(1, 400, wallet), Is.True);
        Assert.That(market.TotalValue, Is.EqualTo(400).Within(1e-9));
        Assert.That(wallet.Balance, Is.EqualTo(600));

        market.SellAll(1, wallet);
        Assert.That(wallet.Balance, Is.EqualTo(1_000).Within(1e-9));
        Assert.That(market.TotalValue, Is.EqualTo(0));
    }

    [Test]
    public void CannotBuyMoreThanCash()
    {
        var market = new Market(new GameConfig().MarketAssets, new Random(1), 60, 30);
        Assert.That(market.Buy(0, 500, new Wallet(100)), Is.False);
    }

    [Test]
    public void YieldAssetsPayIncome()
    {
        var assets = new List<AssetInfo> { new("Obligation", 100, 0, 0, 0.01, "") };
        var market = new Market(assets, new Random(1), 60, 30);
        var wallet = new Wallet(10_000);
        market.Buy(0, 10_000, wallet);

        market.Update(60, wallet); // un mois

        Assert.That(wallet.Balance, Is.EqualTo(100).Within(1e-6));
    }

    [Test]
    public void BillsArePaidMonthlyAndCountUnpaid()
    {
        var bills = new Bills(monthDuration: 10, maxMissedPayments: 2);
        var wallet = new Wallet(500);
        var bill = new BillBreakdown(300, 100, 0);
        var paid = new List<bool>();
        bills.BillPaid += (_, unpaid) => paid.Add(unpaid);

        bills.Update(10, wallet, bill);
        bills.Update(10, wallet, bill);
        bills.Update(10, wallet, bill);

        Assert.That(wallet.Balance, Is.EqualTo(500 - 3 * 400));
        Assert.That(paid, Is.EqualTo(new[] { false, true, true }));
        Assert.That(bills.IsBankrupt, Is.True);
    }
}
