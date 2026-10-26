using Core.upgrades;

namespace Core.roguelike;

/// <summary>Un personnage jouable, avec son trait permanent.</summary>
/// <param name="UnlockCost">Lingots à dépenser dans l'Héritage pour le débloquer (0 = disponible d'office).</param>
public record Character(
    string Id,
    string Name,
    string Title,
    string Description,
    string TraitText,
    double StartingCash,
    int UnlockCost,
    Action<Modifiers> Trait,
    IReadOnlyDictionary<UpgradeTrackId, int>? StartingUpgrades = null,
    double StartingReputation = 0);

/// <summary>Un niveau de difficulté. Chaque victoire débloque le suivant.</summary>
/// <param name="CrisisEvery">Mois entre deux crises, si différent de la règle normale.</param>
public record PressureLevel(int Level, string Name, string Description, Action<Modifiers> Apply, bool StartsCursed = false,
    int? CrisisEvery = null);

/// <summary>Les bonus permanents achetés dans l'Héritage, appliqués au début de chaque run.</summary>
public record LegacyBonuses(
    double ExtraStartingCash = 0,
    double StartingReputation = 0,
    int ExtraDraftChoices = 0,
    int FreeRerolls = 0,
    int StartingShields = 0,
    bool LegendaryPerksUnlocked = false);

/// <summary>Tout ce qu'il faut pour lancer une run.</summary>
public record RunSetup(Character Character, int Pressure, int Seed, LegacyBonuses Bonuses);

public static class CharacterCatalog
{
    public static IReadOnlyList<Character> All { get; } =
    [
        new("dev", "Alex", "Le dev fauché",
            "150 CHF, un portable qui rame et un loyer en retard. Il code bien, c'est déjà ça.",
            "Aucun bonus, aucun malus.", 150, 0, _ => { }),
        new("hacker", "Inès", "La hackeuse",
            "Elle tape plus vite que son ombre, mais ne sait pas vendre.",
            "Vitesse +30 %, clics +50 %, valeur des contrats −20 %.", 150, 30,
            m => { m.WorkSpeed *= 1.3; m.ClickPower *= 1.5; m.RequestValue *= 0.8; },
            new Dictionary<UpgradeTrackId, int> { [UpgradeTrackId.Computer] = 1 }),
        new("business", "Hugo", "L'école de commerce",
            "Il ne sait pas coder, mais il sait vendre n'importe quoi à n'importe qui.",
            "Demandes +40 %, valeur +20 %, vitesse −25 %.", 400, 60,
            m => { m.RequestRate *= 1.4; m.RequestValue *= 1.2; m.WorkSpeed *= 0.75; },
            StartingReputation: 15),
        new("heir", "Charles-Henri", "Le fils à papa",
            "Papa a payé l'appart et le premier PC. Mais le train de vie coûte cher.",
            "Commence avec 15 000 CHF et un bel appart. Factures +50 %, réputation gagnée −30 %.", 15_000, 120,
            m => { m.Bills *= 1.5; m.ReputationGain *= 0.7; },
            new Dictionary<UpgradeTrackId, int> { [UpgradeTrackId.Housing] = 1, [UpgradeTrackId.Computer] = 2 }),
    ];
}

public static class PressureCatalog
{
    public static IReadOnlyList<PressureLevel> All { get; } =
    [
        new(0, "Stagiaire", "La run normale.", _ => { }),
        new(1, "Junior", "Factures +25 %, impôt sur la fortune ×2, coût de la vie +1 % par mois.",
            m => { m.Bills *= 1.25; m.ExtraWealthTax += 0.01; m.CostInflation += 0.01; }),
        new(2, "Senior", "Comme Junior, impôt ×3, vie +2 % par mois, offres et échéances −20 %.",
            m => { m.Bills *= 1.25; m.ExtraWealthTax += 0.02; m.CostInflation += 0.02; m.OfferDuration *= 0.8; m.DeadlineMargin *= 0.8; }),
        new(3, "CTO", "Comme Senior, impôt ×4, vie +3 % par mois, une crise tous les deux mois, et tu commences maudit.",
            m => { m.Bills *= 1.25; m.ExtraWealthTax += 0.03; m.CostInflation += 0.03; m.OfferDuration *= 0.8; m.DeadlineMargin *= 0.8; },
            StartsCursed: true, CrisisEvery: 2),
    ];
}
