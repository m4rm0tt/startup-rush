namespace Core.space;

public enum FurnitureKind
{
    Desk, Chair, Computer, Plant, Coffee, Sofa, Whiteboard, Bookshelf, Decor,

    /// <summary>Équipement fixe du logement ou du local (lit, cuisine) : ni déplaçable ni revendable.</summary>
    Fixture
}

/// <summary>
/// Un modèle de meuble ou de matériel. L'emprise au sol (Width × Height cases) est ce qui compte pour le placement ;
/// la taille dessinée du sprite est une affaire d'affichage (voir FURNITURE_MANIFEST.md).
/// </summary>
public record FurnitureDef(
    string Id,
    string Name,
    FurnitureKind Kind,
    decimal Price,
    int Width,
    int Height,
    string Effect)
{
    /// <summary>Production d'un ordinateur (1 = poste standard).</summary>
    public double Output { get; init; } = 1;

    /// <summary>Qualité ajoutée au travail fait sur ce poste.</summary>
    public double Quality { get; init; }

    /// <summary>Fatigue d'une chaise : multiplie l'énergie perdue par heure.</summary>
    public double EnergyDrain { get; init; } = 1;

    /// <summary>Moral apporté à l'équipe (rendement décroissant au-delà de quelques objets).</summary>
    public double Morale { get; init; }

    /// <summary>Énergie récupérée par une pause à cet endroit.</summary>
    public double BreakRecovery { get; init; }

    /// <summary>Personnes de plus qu'une équipe peut coordonner sans perte.</summary>
    public int Coordination { get; init; }

    public double TrainingBonus { get; init; }

    /// <summary>Peut-on se tenir devant (case juste au sud) pour l'utiliser ?</summary>
    public bool HasFrontAccess =>
        Kind is FurnitureKind.Coffee or FurnitureKind.Sofa or FurnitureKind.Whiteboard or FurnitureKind.Bookshelf || BreakRecovery > 0;

    public bool Movable => Kind != FurnitureKind.Fixture;

    /// <summary>Objet posé sur un bureau plutôt qu'au sol.</summary>
    public bool OnDesk => Kind == FurnitureKind.Computer;
}

/// <summary>Le catalogue des meubles achetables et des équipements fixes.</summary>
public static class FurnitureCatalog
{
    public static readonly IReadOnlyList<FurnitureDef> All =
    [
        new("desk", "Bureau simple", FurnitureKind.Desk, 180m, 2, 1,
            "Plan de travail. Un poste complet demande aussi une chaise derrière et un ordinateur dessus."),
        new("desk_pro", "Bureau large", FurnitureKind.Desk, 460m, 2, 1,
            "Plus de place : +3 de moral pour la personne qui y travaille.") { Morale = 3 },
        new("chair", "Chaise", FurnitureKind.Chair, 60m, 1, 1,
            "Se place derrière un bureau.") { EnergyDrain = 1 },
        new("chair_ergo", "Chaise ergonomique", FurnitureKind.Chair, 390m, 1, 1,
            "Fatigue 20 % moins vite.") { EnergyDrain = 0.8 },
        new("laptop", "Vieux portable", FurnitureKind.Computer, 0m, 1, 1,
            "Production ×0,85. Le tien, déjà payé.") { Output = 0.85 },
        new("pc", "PC fixe", FurnitureKind.Computer, 1_250m, 1, 1,
            "Production ×1,00.") { Output = 1.0 },
        new("workstation", "Station de travail", FurnitureKind.Computer, 3_100m, 1, 1,
            "Production ×1,10 et qualité +0,04. Gain plus petit que le saut depuis le portable.") { Output = 1.1, Quality = 0.04 },
        new("plant", "Plante", FurnitureKind.Plant, 45m, 1, 1,
            "+1 de moral pour l'équipe. Au-delà de 6 plantes, plus aucun effet.") { Morale = 1 },
        new("coffee", "Machine à café", FurnitureKind.Coffee, 650m, 1, 1,
            "Les pauses rendent +12 d'énergie. Une machine suffit pour 8 personnes.") { BreakRecovery = 12 },
        new("sofa", "Canapé", FurnitureKind.Sofa, 900m, 2, 1,
            "Coin pause : +8 d'énergie par pause et +2 de moral.") { BreakRecovery = 8, Morale = 2 },
        new("whiteboard", "Tableau blanc", FurnitureKind.Whiteboard, 220m, 2, 1,
            "Une équipe se coordonne sans perte avec 1 personne de plus (2 tableaux au plus).") { Coordination = 1 },
        new("bookshelf", "Bibliothèque technique", FurnitureKind.Bookshelf, 320m, 1, 1,
            "Formations 15 % plus efficaces (2 au plus).") { TrainingBonus = 0.15 },
        new("rug", "Tapis", FurnitureKind.Decor, 120m, 2, 2,
            "Décoration : aucun effet sur la simulation."),

        // Équipements fixes
        new("bed", "Lit", FurnitureKind.Fixture, 0m, 2, 1, "Ton lit."),
        new("kitchen", "Cuisine", FurnitureKind.Fixture, 0m, 3, 1, "Cuisine équipée."),
        new("fridge", "Frigo", FurnitureKind.Fixture, 0m, 1, 1, "Frigo."),
        new("home_sofa", "Canapé du salon", FurnitureKind.Fixture, 0m, 2, 1, "Le canapé du salon.") { BreakRecovery = 6 },
        new("dining", "Table à manger", FurnitureKind.Fixture, 0m, 2, 2, "Table à manger."),
        new("kitchenette", "Kitchenette", FurnitureKind.Fixture, 0m, 2, 1, "Évier et micro-ondes du local."),
    ];

    private static readonly Dictionary<string, FurnitureDef> ById = All.ToDictionary(d => d.Id);

    public static FurnitureDef Get(string id) => ById[id];

    public static bool Exists(string id) => ById.ContainsKey(id);

    public static IEnumerable<FurnitureDef> Buyable => All.Where(d => d.Movable && d.Price > 0);
}
