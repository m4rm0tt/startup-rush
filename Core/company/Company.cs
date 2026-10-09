using Core.simulation;

namespace Core.company;

public class Company
{
    private Wallet _wallet;

    public Company()
    {
        _wallet = new Wallet(GameConfig.StartingCapital);
    }
    
    public void Update(double deltaTime)
    {
        var cost = GameConfig.OperatingCost * deltaTime;
        _wallet.TrySpend(cost);
    }
}