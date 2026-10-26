namespace Core.roguelike;

/// <summary>Ce qui met la partie en pause en attendant une décision du joueur.</summary>
public interface IPendingChoice
{
    string Title { get; }
}

/// <summary>Un choix d'atout : le joueur en prend un parmi plusieurs, relance ou passe.</summary>
public class Draft : IPendingChoice
{
    private readonly PerkDeck _deck;
    private readonly int _count;
    private readonly Rarity _minRarity;
    private readonly double _rerollBaseCost;
    private readonly double _rerollGrowth;

    public string Title { get; }
    public IReadOnlyList<Perk> Options { get; private set; }
    public int FreeRerollsLeft { get; private set; }
    public int Rerolls { get; private set; }
    public double SkipReward { get; }

    public double RerollCost => FreeRerollsLeft > 0 ? 0 : _rerollBaseCost * Math.Pow(_rerollGrowth, Rerolls);

    public Draft(string title, PerkDeck deck, int count, Rarity minRarity, int freeRerolls,
        double rerollBaseCost, double rerollGrowth, double skipReward)
    {
        Title = title;
        _deck = deck;
        _count = count;
        _minRarity = minRarity;
        _rerollBaseCost = rerollBaseCost;
        _rerollGrowth = rerollGrowth;
        FreeRerollsLeft = freeRerolls;
        SkipReward = skipReward;
        Options = deck.Draw(count, minRarity);
    }

    /// <summary>Tire de nouvelles cartes. Le coût est payé par l'appelant.</summary>
    internal void Reroll()
    {
        if (FreeRerollsLeft > 0)
            FreeRerollsLeft--;
        else
            Rerolls++;

        Options = _deck.Draw(_count, _minRarity);
    }
}

/// <summary>
/// La pioche d'atouts de la run : jamais deux fois le même atout,
/// et les plus rares sortent moins souvent.
/// </summary>
public class PerkDeck
{
    private static readonly Dictionary<Rarity, double> Weights = new()
    {
        [Rarity.Common] = 60,
        [Rarity.Rare] = 28,
        [Rarity.Epic] = 10,
        [Rarity.Legendary] = 2
    };

    private readonly List<Perk> _available;
    private readonly Random _random;

    public PerkDeck(IEnumerable<Perk> perks, Random random, bool legendaryUnlocked)
    {
        _available = perks.Where(p => p.Rarity != Rarity.Curse && (!p.RequiresUnlock || legendaryUnlocked)).ToList();
        _random = random;
    }

    public int Remaining => _available.Count;

    public void Remove(Perk perk) => _available.Remove(perk);

    public IReadOnlyList<Perk> Draw(int count, Rarity minRarity)
    {
        var pool = _available.Where(p => p.Rarity >= minRarity).ToList();
        if (pool.Count == 0)
            pool = _available.ToList();

        var options = new List<Perk>();
        while (options.Count < count && pool.Count > 0)
        {
            double total = pool.Sum(p => Weights[p.Rarity]);
            double roll = _random.NextDouble() * total;
            var picked = pool[^1];
            foreach (var perk in pool)
            {
                roll -= Weights[perk.Rarity];
                if (roll <= 0)
                {
                    picked = perk;
                    break;
                }
            }

            options.Add(picked);
            pool.Remove(picked);
        }

        return options;
    }
}
