using Core.company;

namespace Tests.company;

[TestFixture]
public class WalletTest
{
    [Test]
    public void EarnAddsAmountToBalance()
    {
        var wallet = new Wallet(0);
        wallet.Earn(100);
        Assert.That(wallet.Balance, Is.EqualTo(100));
    }
    
    [Test]
    public void TrySpendSubtractsAmountFromBalance()
    {
        var wallet = new Wallet(100);
        wallet.TrySpend(50);
        Assert.That(wallet.Balance, Is.EqualTo(50));
    }
    
    [Test]
    public void TrySpendReturnsFalseWhenAmountIsGreaterThanBalance()
    {
        var wallet = new Wallet(100);
        var result = wallet.TrySpend(150);
        Assert.That(result, Is.False);
        Assert.That(wallet.Balance, Is.EqualTo(100));
    }
    
    [Test]
    public void TrySpendReturnsTrueWhenAmountIsLessThanBalance()
    {
        var wallet = new Wallet(100);
        var result = wallet.TrySpend(50);
        Assert.That(result, Is.True);
        Assert.That(wallet.Balance, Is.EqualTo(50));
    }

    [Test]
    public void TrySpendReturnsTrueWhenAmountIsEqualToBalance()
    {
        var wallet = new Wallet(100);
        var result = wallet.TrySpend(100);
        Assert.That(result, Is.True);
        Assert.That(wallet.Balance, Is.EqualTo(0));
    }
    
    [Test]
    public void CanAffordReturnsTrueWhenAmountIsLessThanBalance()
    {
        var wallet = new Wallet(100);
        var result = wallet.CanAfford(50);
        Assert.That(result, Is.True);
    }

    [Test]
    public void CanAffordReturnsFalseWhenAmountIsGreaterThanBalance()
    {
        var wallet = new Wallet(100);
        var result = wallet.CanAfford(150);
        Assert.That(result, Is.False);
    }
    
    [Test]
    public void CanAffordReturnsTrueWhenAmountIsEqualToBalance()
    {
        var wallet = new Wallet(100);
        var result = wallet.CanAfford(100);
        Assert.That(result, Is.True);
    }
}