using Core.company;

namespace Core.investments;

/// <param name="MonthlyDrift">Hausse moyenne du cours par mois (0.02 = +2 %).</param>
/// <param name="MonthlyVolatility">Ampleur des variations aléatoires par mois.</param>
/// <param name="MonthlyYield">Revenu versé chaque mois en proportion de la valeur détenue (loyers, intérêts).</param>
public record AssetInfo(
    string Name,
    double StartPrice,
    double MonthlyDrift,
    double MonthlyVolatility,
    double MonthlyYield,
    string Description);

public class MarketAsset
{
    public const int HistoryLength = 60;

    private readonly List<double> _history = new();

    public AssetInfo Info { get; }
    public double Price { get; private set; }
    public double Units { get; private set; }
    public double Value => Price * Units;
    public IReadOnlyList<double> History => _history;

    /// <summary>Variation du cours sur l'historique affiché (0.1 = +10 %).</summary>
    public double Change => _history.Count < 2 ? 0 : Price / _history[0] - 1;

    public MarketAsset(AssetInfo info)
    {
        Info = info;
        Price = info.StartPrice;
        _history.Add(Price);
    }

    internal void SetPrice(double price)
    {
        Price = price;
        _history.Add(price);
        if (_history.Count > HistoryLength)
            _history.RemoveAt(0);
    }

    internal void AddUnits(double units) => Units += units;
}

/// <summary>
/// La bourse : chaque jour de jeu, les cours bougent au hasard autour d'une tendance.
/// Le Random est injecté pour des tests reproductibles.
/// </summary>
public class Market
{
    private readonly Random _random;
    private readonly double _dayDuration;
    private readonly double _monthDuration;
    private readonly int _daysPerMonth;
    private double _timeUntilNextDay;

    public IReadOnlyList<MarketAsset> Assets { get; }
    public double TotalValue => Assets.Sum(a => a.Value);

    public Market(IEnumerable<AssetInfo> assets, Random random, double monthDuration, int daysPerMonth)
    {
        Assets = assets.Select(info => new MarketAsset(info)).ToList();
        _random = random;
        _monthDuration = monthDuration;
        _daysPerMonth = daysPerMonth;
        _dayDuration = monthDuration / daysPerMonth;
        _timeUntilNextDay = _dayDuration;
    }

    /// <summary>Fait bouger les cours et verse les revenus des actifs détenus. Renvoie ces revenus.</summary>
    public double Update(double deltaTime, Wallet wallet)
    {
        _timeUntilNextDay -= deltaTime;
        while (_timeUntilNextDay <= 0)
        {
            foreach (var asset in Assets)
                asset.SetPrice(NextPrice(asset));
            _timeUntilNextDay += _dayDuration;
        }

        double income = Assets.Sum(a => a.Value * a.Info.MonthlyYield) * deltaTime / _monthDuration;
        if (income > 0)
            wallet.Earn(income);

        return income;
    }

    /// <summary>Fait chuter (ou monter) tous les cours d'un coup : -0.3 = −30 %.</summary>
    public void Shock(double change)
    {
        foreach (var asset in Assets)
            asset.SetPrice(asset.Price * (1 + change));
    }

    public bool Buy(int index, double amount, Wallet wallet)
    {
        if (index < 0 || index >= Assets.Count || amount <= 0 || !wallet.TrySpend(amount))
            return false;

        var asset = Assets[index];
        asset.AddUnits(amount / asset.Price);
        return true;
    }

    /// <summary>Vend tout l'actif au cours actuel et renvoie le montant encaissé.</summary>
    public double SellAll(int index, Wallet wallet)
    {
        if (index < 0 || index >= Assets.Count)
            return 0;

        var asset = Assets[index];
        double amount = asset.Value;
        asset.AddUnits(-asset.Units);
        if (amount > 0)
            wallet.Earn(amount);

        return amount;
    }

    // Marche aléatoire log-normale : tendance + bruit gaussien, sur un jour
    private double NextPrice(MarketAsset asset)
    {
        double dt = 1.0 / _daysPerMonth;
        double drift = asset.Info.MonthlyDrift;
        double volatility = asset.Info.MonthlyVolatility;
        double logReturn = (drift - volatility * volatility / 2) * dt + volatility * Math.Sqrt(dt) * NextGaussian();

        return asset.Price * Math.Exp(logReturn);
    }

    private double NextGaussian()
    {
        double u1 = 1 - _random.NextDouble();
        double u2 = _random.NextDouble();
        return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
    }
}
