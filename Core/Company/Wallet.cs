namespace Core.Company;

public class Wallet
{
    public int Balance { get; private set; } = 0;

    public Wallet(int startingCapital)
    {
        Balance = startingCapital;
    }

    public void Earn(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        Balance += amount;
    }

    public bool CanAfford(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        return Balance >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (!CanAfford(amount))
            return false;

        Balance -= amount;
        return true;
    }
}