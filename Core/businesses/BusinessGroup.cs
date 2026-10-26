using Core.company;

namespace Core.businesses;

/// <param name="BaseIncomePerSecond">Revenu passif par seconde et par niveau.</param>
public record BusinessInfo(string Name, double BaseCost, double BaseIncomePerSecond, string Description);

/// <summary>Une boîte créée par le joueur, qui rapporte un revenu passif.</summary>
public class Business
{
    private readonly double _costGrowth;

    public BusinessInfo Info { get; }
    public int Level { get; private set; }
    public double TotalInvested { get; private set; }

    public bool IsOwned => Level > 0;
    public double NextCost => Info.BaseCost * Math.Pow(_costGrowth, Level);
    public double IncomePerSecond => Info.BaseIncomePerSecond * Level;

    public Business(BusinessInfo info, double costGrowth)
    {
        Info = info;
        _costGrowth = costGrowth;
    }

    internal void LevelUp(double paid)
    {
        Level++;
        TotalInvested += paid;
    }

    internal void Reset()
    {
        Level = 0;
        TotalInvested = 0;
    }
}

/// <summary>Toutes les boîtes du joueur, et sa holding qui les regroupe.</summary>
public class BusinessGroup
{
    private readonly double _holdingCost;
    private readonly double _holdingMultiplier;

    public IReadOnlyList<Business> Businesses { get; }
    public bool HasHolding { get; private set; }
    public double HoldingCost => _holdingCost;
    public double HoldingMultiplier => _holdingMultiplier;

    public double IncomePerSecond =>
        Businesses.Sum(b => b.IncomePerSecond) * (HasHolding ? _holdingMultiplier : 1);

    /// <summary>Revenu nominal des boîtes, sans holding ni crise : sert de base aux charges mensuelles.</summary>
    public double BaseIncomePerSecond => Businesses.Sum(b => b.IncomePerSecond);

    /// <summary>Valeur des boîtes pour le patrimoine : ce qui y a été investi.</summary>
    public double Valuation => Businesses.Sum(b => b.TotalInvested) + (HasHolding ? _holdingCost : 0);

    public event Action<Business>? BusinessGrew;

    public BusinessGroup(IEnumerable<BusinessInfo> businesses, double costGrowth, double holdingCost, double holdingMultiplier)
    {
        Businesses = businesses.Select(info => new Business(info, costGrowth)).ToList();
        _holdingCost = holdingCost;
        _holdingMultiplier = holdingMultiplier;
    }

    /// <summary>Crée la boîte si elle n'existe pas encore, sinon la fait grandir d'un niveau.</summary>
    public bool TryInvest(int index, Wallet wallet, double costMultiplier = 1)
    {
        if (index < 0 || index >= Businesses.Count)
            return false;

        var business = Businesses[index];
        double cost = business.NextCost * costMultiplier;
        if (!wallet.TrySpend(cost))
            return false;

        business.LevelUp(cost);
        BusinessGrew?.Invoke(business);
        return true;
    }

    public bool TryBuyHolding(Wallet wallet)
    {
        if (HasHolding || !wallet.TrySpend(_holdingCost))
            return false;

        HasHolding = true;
        return true;
    }

    /// <summary>La boîte disparaît (vendue) : elle repart de zéro.</summary>
    public void Sell(Business business) => business.Reset();

    /// <summary>Verse le revenu passif du delta dans le portefeuille et renvoie le montant.</summary>
    public double Update(double deltaTime, Wallet wallet, double incomeMultiplier = 1)
    {
        double income = IncomePerSecond * incomeMultiplier * deltaTime;
        if (income > 0)
            wallet.Earn(income);

        return income;
    }
}
