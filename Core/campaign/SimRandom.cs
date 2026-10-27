namespace Core.campaign;

/// <summary>
/// Générateur pseudo-aléatoire de la campagne (xorshift64*). Son état tient dans un seul nombre sauvegardé :
/// recharger une partie reproduit exactement les mêmes tirages, et une graine fixe rend les tests reproductibles.
/// </summary>
public class SimRandom
{
    public ulong State { get; set; }

    public SimRandom()
    {
    }

    public SimRandom(ulong seed)
    {
        State = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
    }

    public ulong NextULong()
    {
        ulong x = State;
        x ^= x >> 12;
        x ^= x << 25;
        x ^= x >> 27;
        State = x;
        return x * 0x2545F4914F6CDD1DUL;
    }

    /// <summary>Nombre dans [0, 1).</summary>
    public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

    /// <summary>Entier dans [min, max).</summary>
    public int Next(int min, int max) => max <= min ? min : min + (int)(NextULong() % (ulong)(max - min));

    public double Range(double min, double max) => min + NextDouble() * (max - min);

    public bool Chance(double probability) => NextDouble() < probability;

    public T Pick<T>(IReadOnlyList<T> items) => items[Next(0, items.Count)];
}
