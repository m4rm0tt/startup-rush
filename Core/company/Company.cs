using Core.simulation;

namespace Core.company;

public class Company
{
    private Wallet _wallet;
    private double _operatingCost;

    public double WalletBalance => _wallet.Balance;

    public event Action<double> BalanceChanged
    {
        add => _wallet.BalanceChanged += value;
        remove => _wallet.BalanceChanged -= value;
    }

    public Company(GameConfig config)
    {
        _wallet = new Wallet(config.StartingCapital);
        _operatingCost = config.BaseOperatingCost;
    }

    public void Update(double deltaTime)
    {
        var cost = _operatingCost * deltaTime;
        _wallet.Pay(cost);
    }
}
