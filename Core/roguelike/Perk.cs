namespace Core.roguelike;

public enum Rarity
{
    Common,
    Rare,
    Epic,
    Legendary,
    Curse
}

/// <summary>
/// Un atout (ou une malédiction) gardé jusqu'à la fin de la run.
/// </summary>
/// <param name="Icon">Clé de l'icône pixel affichée par l'interface.</param>
/// <param name="Apply">Modifie les bonus du joueur tant qu'il possède l'atout.</param>
/// <param name="Shields">Factures impayées pardonnées, données une fois au moment du choix.</param>
/// <param name="RequiresUnlock">Absent du tirage tant que l'Héritage ne l'a pas débloqué.</param>
public record Perk(
    string Id,
    string Name,
    string Description,
    Rarity Rarity,
    string Icon,
    Action<Modifiers> Apply,
    int Shields = 0,
    bool RequiresUnlock = false);

public static class PerkCatalog
{
    public static IReadOnlyList<Perk> All { get; } =
    [
        // Communs
        new("coffee", "Café serré", "+15 % de vitesse de travail.", Rarity.Common, "coffee", m => m.WorkSpeed *= 1.15),
        new("fingers", "Doigts de fée", "+60 % de puissance des clics.", Rarity.Common, "hand", m => m.ClickPower *= 1.6),
        new("linkedin", "Profil LinkedIn", "+20 % de demandes.", Rarity.Common, "link", m => m.RequestRate *= 1.2),
        new("haggler", "Négociateur", "+15 % de valeur des contrats.", Rarity.Common, "money", m => m.RequestValue *= 1.15),
        new("roommate", "Coloc sympa", "−25 % de loyer.", Rarity.Common, "home", m => m.Rent *= 0.75),
        new("gigs", "Fiverr addict", "Petits boulots ×3.", Rarity.Common, "coin", m => m.GigMoney *= 3),
        new("patience", "Clients patients", "Les offres restent 50 % plus longtemps.", Rarity.Common, "hourglass", m => m.OfferDuration *= 1.5),
        new("margins", "Planning large", "Échéances 30 % plus longues.", Rarity.Common, "clock", m => m.DeadlineMargin *= 1.3),
        new("wordofmouth", "Bouche-à-oreille", "+40 % de réputation gagnée.", Rarity.Common, "star", m => m.ReputationGain *= 1.4),

        // Rares
        new("savings", "Livret d'épargne", "Fin de mois : +5 % d'intérêts sur ton cash.", Rarity.Rare, "piggy", m => m.MonthlyInterest += 0.05),
        new("punctual", "Toujours à l'heure", "Contrats livrés à temps : +25 % de bonus.", Rarity.Rare, "check", m => m.OnTimeBonus += 0.25),
        new("multitask", "Multitâche", "+1 tâche en même temps.", Rarity.Rare, "plus", m => m.ExtraCapacity += 1),
        new("genius", "Coup de génie", "10 % de chance qu'un clic vaille ×10.", Rarity.Rare, "bolt", m => m.ClickCritChance += 0.1),
        new("premium", "Clientèle premium", "+40 % de valeur, mais −15 % de demandes.", Rarity.Rare, "gem",
            m => { m.RequestValue *= 1.4; m.RequestRate *= 0.85; }),
        new("ascetic", "Ascète", "−35 % de factures, mais −10 % de vitesse.", Rarity.Rare, "leaf",
            m => { m.Bills *= 0.65; m.WorkSpeed *= 0.9; }),
        new("overclock", "Overclocking", "+35 % de vitesse, mais +10 % de factures.", Rarity.Rare, "fire",
            m => { m.WorkSpeed *= 1.35; m.Bills *= 1.1; }),
        new("sales", "Chasseur de soldes", "−25 % sur le matériel, le logement et les locaux.", Rarity.Rare, "tag", m => m.PurchaseCost *= 0.75),

        // Épiques
        new("insurance", "Assurance tous risques", "Une facture impayée est pardonnée.", Rarity.Epic, "shield", _ => { }, Shields: 1),
        new("startupnation", "Startup Nation", "+50 % de revenus des boîtes.", Rarity.Epic, "rocket", m => m.BusinessIncome *= 1.5),
        new("recruiter", "Chasseur de têtes", "Embauches −50 % et +2 places dans les locaux.", Rarity.Epic, "people",
            m => { m.HiringCost *= 0.5; m.ExtraSeats += 2; }),
        new("crunch", "Crunch", "+60 % de vitesse, mais −30 % de réputation gagnée.", Rarity.Epic, "bolt",
            m => { m.WorkSpeed *= 1.6; m.ReputationGain *= 0.7; }),
        new("cat", "Chat de bureau", "+10 % de vitesse, de valeur et de demandes. Il ronronne.", Rarity.Epic, "cat",
            m => { m.WorkSpeed *= 1.1; m.RequestValue *= 1.1; m.RequestRate *= 1.1; }),
        new("leverage", "Effet de levier", "Fin de mois : +8 % d'intérêts sur ton cash.", Rarity.Epic, "chart", m => m.MonthlyInterest += 0.08),

        // Légendaires
        new("unicorn", "Licorne", "Revenus des boîtes ×2.", Rarity.Legendary, "unicorn", m => m.BusinessIncome *= 2),
        new("midas", "Main de Midas", "Chaque clic rapporte, même en travaillant. Clics ×2.", Rarity.Legendary, "crown",
            m => { m.ClicksAlwaysPay = true; m.ClickPower *= 2; }),
        new("monopoly", "Monopole", "Valeur des contrats ×2, mais −30 % de demandes.", Rarity.Legendary, "gem",
            m => { m.RequestValue *= 2; m.RequestRate *= 0.7; }, RequiresUnlock: true),
        new("insider", "Délit d'initié", "Fin de mois : +4 % sur ton portefeuille boursier.", Rarity.Legendary, "chart",
            m => m.MarketMonthlyBonus += 0.04, RequiresUnlock: true),
        new("immortal", "Increvable", "Deux factures impayées pardonnées.", Rarity.Legendary, "shield", _ => { }, Shields: 2,
            RequiresUnlock: true),

        // Malédictions (jamais tirées dans un choix, données par les événements et la pression)
        new("dilution", "Dilution", "Un investisseur prend 15 % des revenus de tes boîtes.", Rarity.Curse, "skull",
            m => m.BusinessIncome *= 0.85),
        new("debt", "Dette étudiante", "+15 % de factures.", Rarity.Curse, "skull", m => m.Bills *= 1.15),
        new("insomnia", "Insomnie", "−10 % de vitesse de travail.", Rarity.Curse, "skull", m => m.WorkSpeed *= 0.9),
        new("badreview", "Mauvais avis Google", "−15 % de demandes.", Rarity.Curse, "skull", m => m.RequestRate *= 0.85),
    ];
}
