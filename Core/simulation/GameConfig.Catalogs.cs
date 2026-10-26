using Core.businesses;
using Core.investments;
using Core.progression;
using Core.roguelike;
using Core.staff;
using Core.upgrades;

namespace Core.simulation;

/// <summary>Les listes de contenu du jeu, réglables comme le reste de GameConfig.</summary>
public partial record GameConfig
{
    public IReadOnlyList<Perk> Perks { get; init; } = PerkCatalog.All;
    public IReadOnlyList<CrisisInfo> Crises { get; init; } = CrisisCatalog.All;
    public IReadOnlyList<GameEventInfo> Events { get; init; } = EventCatalog.All;

    public IReadOnlyList<CareerStageInfo> CareerStages { get; init; } =
    [
        new(CareerStage.Broke, "Fauché", 0,
            "",
            "150 CHF en poche, un portable qui rame et un loyer à payer à la fin du mois. " +
            "Personne ne croit en toi. Clique pour charbonner et accepte tes premiers contrats."),
        new(CareerStage.Freelancer, "Freelance", 3_000,
            "Atteindre 3 000 CHF de patrimoine",
            "Les clients commencent à revenir. Tu peux maintenant fonder ta propre boîte."),
        new(CareerStage.Founder, "Fondateur", 0,
            "Fonder ta boîte",
            "Ta boîte existe enfin ! Embauche du monde pour prendre plus de contrats en même temps."),
        new(CareerStage.Boss, "Patron", 100_000,
            "100 000 CHF de patrimoine et 3 employés",
            "Ta boîte tourne. Il est temps de créer d'autres entreprises qui rapportent sans toi."),
        new(CareerStage.Investor, "Investisseur", 5_000_000,
            "Atteindre 5 M CHF de patrimoine",
            "Les banquiers t'appellent par ton prénom. La bourse t'est ouverte."),
        new(CareerStage.Tycoon, "Magnat", 100_000_000,
            "Atteindre 100 M CHF de patrimoine",
            "Tu es dans les journaux économiques. Regroupe tes boîtes dans une holding."),
        new(CareerStage.KingOfTheWorld, "Roi du monde", 1_000_000_000,
            "1 milliard de patrimoine et une holding",
            "Milliardaire. Tes boîtes sont partout, les présidents te demandent conseil. Tu es le roi du monde."),
    ];

    public IReadOnlyList<ReputationLevel> ReputationLevels { get; init; } =
    [
        new("Inconnu", 0, 2, 1),
        new("Débutant", 15, 3, 1.6),
        new("Freelance fiable", 60, 4, 2.5),
        new("Pro reconnu", 180, 5, 4.5),
        new("Référence régionale", 450, 6, 8),
        new("Agence nationale", 1_000, 7, 15),
        new("Leader européen", 2_200, 8, 30),
        new("Géant mondial", 5_000, 10, 60),
    ];

    public IReadOnlyList<UpgradeTrackInfo> UpgradeTracks { get; init; } =
    [
        new(UpgradeTrackId.Computer, "Ordinateur", "vitesse de travail",
        [
            new("Vieux portable qui rame", 0, 1.0),
            new("PC d'occasion", 400, 1.4),
            new("PC gamer", 2_500, 2.0),
            new("Station de travail", 15_000, 3.0),
            new("Setup de développeur pro", 90_000, 4.5),
            new("Baie de serveurs", 600_000, 7),
            new("Supercalculateur", 5_000_000, 11),
            new("Ordinateur quantique", 60_000_000, 18),
        ]),
        new(UpgradeTrackId.Screens, "Écrans", "vitesse de travail",
        [
            new("Un écran fêlé", 0, 1.0),
            new("Deux écrans", 300, 1.15),
            new("Trois écrans", 3_000, 1.3),
            new("Mur d'écrans", 40_000, 1.5),
        ]),
        new(UpgradeTrackId.Chair, "Chaise", "puissance des clics",
        [
            new("Tabouret en plastique", 0, 1),
            new("Chaise de bureau", 150, 2),
            new("Fauteuil gamer", 1_200, 4),
            new("Fauteuil ergonomique", 15_000, 8),
        ]),
        new(UpgradeTrackId.Coffee, "Café", "vitesse de travail",
        [
            new("Eau du robinet", 0, 1.0),
            new("Cafetière filtre", 60, 1.1),
            new("Machine à expresso", 1_500, 1.25),
            new("Barista personnel", 250_000, 1.5, 6_000),
        ]),
        new(UpgradeTrackId.Internet, "Internet", "nombre de demandes",
        [
            new("Wifi du voisin", 0, 1.0),
            new("ADSL", 200, 1.3, 40),
            new("Fibre", 2_000, 1.7, 80),
            new("Fibre pro", 30_000, 2.2, 300),
            new("Satellite privé", 3_000_000, 3, 5_000),
        ]),
        new(UpgradeTrackId.Housing, "Logement", "vitesse (repos)",
        [
            new("Studio miteux", 0, 1.0, 450),
            new("Appartement 2 pièces", 2_000, 1.1, 1_100),
            new("Loft en ville", 20_000, 1.2, 3_000),
            new("Maison avec jardin", 200_000, 1.35, 8_000),
            new("Villa avec piscine", 2_000_000, 1.5, 30_000),
            new("Penthouse à Monaco", 25_000_000, 1.7, 150_000),
            new("Île privée", 300_000_000, 2.0, 800_000),
        ]),
        new(UpgradeTrackId.Office, "Locaux", "places pour les employés",
        [
            new("Garage", 0, 3, 200),
            new("Petit bureau", 15_000, 6, 1_500),
            new("Open space", 120_000, 15, 6_000),
            new("Étage d'une tour", 1_200_000, 35, 30_000),
            new("Campus", 15_000_000, 80, 150_000),
        ]),
    ];

    public IReadOnlyList<EmployeeRoleInfo> EmployeeRoles { get; init; } =
    [
        new(EmployeeRole.Intern, "Stagiaire", 500, 900, 1, 0, 0, false,
            "+1 tâche en même temps. Pas cher, pas rapide."),
        new(EmployeeRole.Developer, "Développeur", 3_000, 4_000, 1, 0.05, 0, false,
            "+1 tâche en même temps et +5 % de vitesse."),
        new(EmployeeRole.Salesperson, "Commercial", 3_000, 4_500, 0, 0, 0.3, true,
            "+30 % de demandes et accepte tout seul les contrats faisables à temps."),
        new(EmployeeRole.ProjectManager, "Chef de projet", 10_000, 9_000, 0, 0.2, 0, false,
            "+20 % de vitesse pour toute l'équipe."),
    ];

    public IReadOnlyList<BusinessInfo> BusinessCatalog { get; init; } =
    [
        new("Agence web", 30_000, 150, "Des sites pour les PME du coin."),
        new("Appli mobile", 250_000, 1_100, "Une appli de livraison qui cartonne."),
        new("Studio de jeux vidéo", 2_000_000, 8_000, "Ton premier jeu fait un million de ventes."),
        new("Plateforme SaaS", 15_000_000, 55_000, "Des abonnements qui tombent tous les mois."),
        new("Fintech", 120_000_000, 400_000, "Une banque en ligne pour toute l'Europe."),
        new("Conquête spatiale", 800_000_000, 2_500_000, "Des fusées. Parce que tu peux."),
    ];

    public IReadOnlyList<AssetInfo> MarketAssets { get; init; } =
    [
        new("Obligations d'État", 100, 0.004, 0.01, 0.003, "Sûr mais lent. Verse des intérêts."),
        new("ETF Monde", 100, 0.02, 0.05, 0, "Toute l'économie mondiale en un clic."),
        new("Actions tech", 250, 0.035, 0.12, 0, "Ça monte fort... la plupart du temps."),
        new("Crypto", 30_000, 0.05, 0.35, 0, "Montagnes russes garanties."),
        new("Immobilier", 500_000, 0.008, 0.02, 0.006, "Des immeubles qui versent des loyers."),
    ];
}
