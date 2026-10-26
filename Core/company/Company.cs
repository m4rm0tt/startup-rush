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

    public Company()
    {
        _wallet = new Wallet(GameConfig.StartingCapital);
        _operatingCost = GameConfig.BaseOperatingCost;
    }
    
    public void Update(double deltaTime)
    {
        var cost = _operatingCost * deltaTime;
        _wallet.TrySpend(cost);
    }
}