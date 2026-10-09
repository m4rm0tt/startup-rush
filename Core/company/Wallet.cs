namespace Core.company;

public class Wallet
{
    public double Balance { get; private set; } = 0;
    
    public event Action<double>? BalanceChanged;

    public Wallet(double startingCapital)
    {
        Balance = startingCapital;
    }

    public void Earn(double amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        Balance += amount;
        BalanceChanged?.Invoke(Balance);
    }

    public bool CanAfford(double amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        return Balance >= amount;
    }

    public bool TrySpend(double amount)
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