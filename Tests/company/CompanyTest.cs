using Core.company;
using Core.simulation;

namespace Tests.company;

[TestFixture]
public class CompanyTest
{
    [Test]
    public void StartsWithStartingCapital()
    {
        var company = new Company(new GameConfig { StartingCapital = 500 });
        Assert.That(company.WalletBalance, Is.EqualTo(500));
    }

    [Test]
    public void TenSecondsOfOperatingCostsAreDeducted()
    {
        var company = new Company(new GameConfig { StartingCapital = 1000, BaseOperatingCost = 10 });

        company.Update(10);

        Assert.That(company.WalletBalance, Is.EqualTo(900).Within(1e-9));
    }

    [Test]
    public void OperatingCostsCanMakeBalanceNegative()
    {
        var company = new Company(new GameConfig { StartingCapital = 50, BaseOperatingCost = 10 });

        company.Update(10);

        Assert.That(company.WalletBalance, Is.EqualTo(-50).Within(1e-9));
    }
}
