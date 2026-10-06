namespace Core.company;

public class Wallet
{
    public int Balance { get; private set; } = 0;
    
    public event Action<int> BalanceChanged;

    public Wallet(int startingCapital)
    {
        Balance = startingCapital;
    }

    public void Earn(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        Balance += amount;
        BalanceChanged?.Invoke(Balance);
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
        BalanceChanged?.Invoke(Balance);
        return true;
    }
}