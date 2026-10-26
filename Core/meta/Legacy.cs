using Core.roguelike;
using Core.simulation;

namespace Core.meta;

/// <param name="Costs">Prix en lingots de chaque niveau.</param>
public record LegacyUpgrade(string Id, string Name, string Description, IReadOnlyList<int> Costs);

/// <summary>La boutique entre les runs : bonus permanents et personnages.</summary>
public static class Legacy
{
    public const string StartingCash = "cash";
    public const string Contacts = "contacts";
    public const string Mentor = "mentor";
    public const string Voucher = "voucher";
    public const string Insurance = "insurance";
    public const string Vip = "vip";

    public static IReadOnlyList<LegacyUpgrade> Upgrades { get; } =
    [
        new(StartingCash, "Bas de laine", "+150 / +400 / +1 000 CHF au départ de chaque run.", [10, 25, 50]),
        new(Contacts, "Carnet d'adresses", "Commence avec de la réputation (+15 / +60 points).", [20, 45]),
        new(Voucher, "Bon de relance", "+1 relance gratuite à chaque choix d'atout (2 niveaux).", [25, 50]),
        new(Mentor, "Mentor", "Une carte de plus à chaque choix d'atout.", [40]),
        new(Insurance, "Mutuelle", "Commence chaque run avec une facture impayée pardonnée.", [60]),
        new(Vip, "Contacts VIP", "Ajoute les atouts légendaires secrets au tirage.", [50]),
    ];

    private static readonly double[] CashLevels = [0, 150, 400, 1_000];
    private static readonly double[] ReputationLevels = [0, 15, 60];

    public static int Level(Profile profile, string id) => profile.LegacyLevels.GetValueOrDefault(id);

    public static int? NextCost(Profile profile, LegacyUpgrade upgrade)
    {
        int level = Level(profile, upgrade.Id);
        return level < upgrade.Costs.Count ? upgrade.Costs[level] : null;
    }

    public static bool TryBuy(Profile profile, string id)
    {
        var upgrade = Upgrades.First(u => u.Id == id);
        if (NextCost(profile, upgrade) is not { } cost || profile.Lingots < cost)
            return false;

        profile.Lingots -= cost;
        profile.LegacyLevels[id] = Level(profile, id) + 1;
        return true;
    }

    public static bool IsUnlocked(Profile profile, Character character) =>
        character.UnlockCost == 0 || profile.UnlockedCharacters.Contains(character.Id);

    public static bool TryUnlock(Profile profile, Character character)
    {
        if (IsUnlocked(profile, character) || profile.Lingots < character.UnlockCost)
            return false;

        profile.Lingots -= character.UnlockCost;
        profile.UnlockedCharacters.Add(character.Id);
        return true;
    }

    public static LegacyBonuses BonusesFor(Profile profile) => new(
        ExtraStartingCash: CashLevels[Math.Min(Level(profile, StartingCash), CashLevels.Length - 1)],
        StartingReputation: ReputationLevels[Math.Min(Level(profile, Contacts), ReputationLevels.Length - 1)],
        ExtraDraftChoices: Level(profile, Mentor),
        FreeRerolls: Level(profile, Voucher),
        StartingShields: Level(profile, Insurance),
        LegendaryPerksUnlocked: Level(profile, Vip) > 0);
}

/// <summary>Les lingots gagnés à la fin d'une run, avec le détail affiché au joueur.</summary>
public record RunReward(int Lingots, IReadOnlyList<(string Label, int Amount)> Breakdown, bool PressureUnlocked, bool NewRecord);

public static class RunRewards
{
    public static RunReward Compute(GameResult result, int pressure, Profile profile)
    {
        var breakdown = new List<(string, int)>
        {
            ($"Statut atteint : {result.StageTitle}", result.StageIndex * 5),
            ($"{result.Months} mois tenus", result.Months / 2),
        };
        if (result.IsVictory)
            breakdown.Add(("Victoire", 50));
        if (pressure > 0)
            breakdown.Add(($"Pression {pressure}", (int)(breakdown.Sum(b => b.Item2) * 0.25 * pressure)));

        int total = breakdown.Sum(b => b.Item2);
        bool unlocksPressure = result.IsVictory && pressure == profile.MaxPressureUnlocked &&
                               pressure + 1 < PressureCatalog.All.Count;
        bool record = result.PeakNetWorth > profile.BestNetWorth;

        return new RunReward(total, breakdown, unlocksPressure, record);
    }

    /// <summary>Ajoute la run au profil : lingots, statistiques, records et déblocages.</summary>
    public static void Apply(Profile profile, GameResult result, RunReward reward)
    {
        profile.Lingots += reward.Lingots;
        profile.Runs++;
        profile.BestStage = Math.Max(profile.BestStage, result.StageIndex);
        profile.BestNetWorth = Math.Max(profile.BestNetWorth, result.PeakNetWorth);

        if (result.IsVictory)
        {
            profile.Wins++;
            profile.FastestWin = profile.FastestWin == 0 ? result.SurvivedTime : Math.Min(profile.FastestWin, result.SurvivedTime);
        }

        if (reward.PressureUnlocked)
            profile.MaxPressureUnlocked++;
    }
}
