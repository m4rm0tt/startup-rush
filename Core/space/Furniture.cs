namespace Core.space;

public enum FurnitureKind
{
    Desk, Chair, Laptop, Tower, Monitor, Keyboard, Plant, Coffee, Lounge, Whiteboard, Bookshelf, Bed, Kitchen,

    /// <summary>Objet personnel posé sur un bureau (tasse, figurine) : un peu de moral pour la personne du poste.</summary>
    Personal,

    /// <summary>Ambiance du local (lampes, rangement, corbeille) : petite part du moral commun, plafonnée.</summary>
    Ambiance,

    /// <summary>Objet du pack sans effet de jeu pour l'instant : il se dessine, rien de plus (voir docs/asset_usage.csv).</summary>
    Decor
}

/// <summary>
/// Un meuble ou un équipement jouable : un asset du pack (même identifiant) avec son prix et son effet. L'emprise,
/// les accès et les sockets viennent du manifeste du pack ; seuls le prix et l'effet sont des choix de jeu.
/// </summary>
public record FurnitureDef(string Id, string Name, FurnitureKind Kind, decimal Price, string Effect)
{
    /// <summary>Production d'un ordinateur (1 = poste standard).</summary>
    public double Output { get; init; } = 1;

    /// <summary>Qualité ajoutée au travail fait sur ce poste.</summary>
    public double Quality { get; init; }

    /// <summary>Fatigue d'une chaise : multiplie l'énergie perdue par heure.</summary>
    public double EnergyDrain { get; init; } = 1;

    /// <summary>Moral apporté (à l'équipe pour l'ambiance, à la personne du poste pour un bureau ou un objet personnel).</summary>
    public double Morale { get; init; }

    /// <summary>Énergie récupérée par une pause à cet endroit.</summary>
    public double BreakRecovery { get; init; }

    /// <summary>Personnes de plus qu'une équipe peut coordonner sans perte.</summary>
    public int Coordination { get; init; }

    public double TrainingBonus { get; init; }

    /// <summary>Énergie que le fondateur récupère en plus chaque nuit (son lit).</summary>
    public double NightRecovery { get; init; }

    /// <summary>Gain de production d'un écran de plus (plafonné par poste).</summary>
    public double ScreenBonus { get; init; }

    public PackAsset Pack => space.Pack.Asset(Id);

    public bool Buyable => Price > 0;

    /// <summary>Accroché à un socket d'un autre objet (écran, clavier, chaise, objet de bureau) plutôt que posé au sol.</summary>
    public bool Attached => Pack.Centered && Pack.Provides.Length > 0;

    public bool IsComputer => Kind is FurnitureKind.Laptop or FurnitureKind.Tower;
}

/// <summary>Le catalogue des meubles jouables, et un repli « décor » pour les autres assets du pack livrés avec un local.</summary>
public static class FurnitureCatalog
{
    public static readonly IReadOnlyList<FurnitureDef> All =
    [
        // Postes de travail : un bureau, une chaise dans son socket, un ordinateur (portable, ou tour + écran + clavier)
        new("desk_compact", "Bureau compact", FurnitureKind.Desk, 150m, "Un poste sur une seule case."),
        new("desk_simple", "Bureau simple", FurnitureKind.Desk, 180m, "Un poste, avec de la place pour un second écran."),
        new("desk_tech", "Bureau technique", FurnitureKind.Desk, 460m, "Grand plan et étagère : +3 de moral pour la personne qui y travaille.") { Morale = 3 },
        new("desk_corner", "Bureau d'angle", FurnitureKind.Desk, 520m, "Bureau en L : +2 de moral pour la personne qui y travaille.") { Morale = 2 },
        new("desk_long", "Bench de 3 postes", FurnitureKind.Desk, 600m, "Trois postes côte à côte, pour des portables."),
        new("chair_budget", "Chaise économique", FurnitureKind.Chair, 60m, "Se glisse au bureau."),
        new("chair_office", "Chaise de bureau", FurnitureKind.Chair, 180m, "Fatigue 10 % moins vite.") { EnergyDrain = 0.9 },
        new("chair_ergonomic", "Chaise ergonomique", FurnitureKind.Chair, 390m, "Fatigue 20 % moins vite.") { EnergyDrain = 0.8 },
        new("laptop_old", "Portable ancien", FurnitureKind.Laptop, 120m, "Production ×0,85.") { Output = 0.85 },
        new("laptop_standard", "Portable standard", FurnitureKind.Laptop, 900m, "Production ×0,95.") { Output = 0.95 },
        new("laptop_pro", "Portable professionnel", FurnitureKind.Laptop, 1_800m, "Production ×1,05 et qualité +0,02.") { Output = 1.05, Quality = 0.02 },
        new("tower_used", "Tour d'occasion", FurnitureKind.Tower, 300m, "Production ×0,85. Demande un écran et un clavier.") { Output = 0.85 },
        new("tower_office", "Tour bureautique", FurnitureKind.Tower, 900m, "Production ×1,00. Demande un écran et un clavier.") { Output = 1.0 },
        new("workstation_dev", "Station de développement", FurnitureKind.Tower, 2_600m, "Production ×1,10 et qualité +0,04. Demande un écran et un clavier.") { Output = 1.1, Quality = 0.04 },
        new("monitor_basic", "Écran simple", FurnitureKind.Monitor, 150m, "L'écran d'une tour."),
        new("monitor_large", "Grand écran", FurnitureKind.Monitor, 380m, "Production +3 % (6 % au plus par poste).") { ScreenBonus = 0.03 },
        new("monitor_arm_second", "Second écran sur bras", FurnitureKind.Monitor, 220m, "Production +3 % (6 % au plus par poste).") { ScreenBonus = 0.03 },
        new("keyboard_mouse", "Clavier et souris", FurnitureKind.Keyboard, 40m, "Le clavier d'une tour."),

        // Confort et ambiance (plafonnés : voir Layout)
        new("plant_floor_medium", "Plante", FurnitureKind.Plant, 45m, "+1 de moral pour l'équipe (ambiance plafonnée).") { Morale = 1 },
        new("plant_floor_tall", "Grande plante", FurnitureKind.Plant, 70m, "+1 de moral pour l'équipe (ambiance plafonnée).") { Morale = 1 },
        new("plant_succulent", "Plante grasse", FurnitureKind.Plant, 15m, "Sur un meuble : +0,5 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.5 },
        new("lamp_floor", "Lampadaire", FurnitureKind.Ambiance, 90m, "+0,5 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.5 },
        new("lamp_desk", "Lampe de bureau", FurnitureKind.Ambiance, 35m, "Sur un meuble : +0,5 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.5 },
        new("rug_small", "Tapis", FurnitureKind.Ambiance, 120m, "On marche dessus : +0,5 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.5 },
        new("trash_bin", "Corbeille", FurnitureKind.Ambiance, 15m, "Un local tenu : +0,25 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.25 },
        new("dresser", "Commode", FurnitureKind.Ambiance, 150m, "Du rangement : +0,5 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.5 },
        new("nightstand", "Table de chevet", FurnitureKind.Ambiance, 60m, "Du rangement : +0,25 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.25 },
        new("mug", "Tasse", FurnitureKind.Personal, 10m, "Sur un bureau : +0,5 de moral pour la personne du poste (1 au plus).") { Morale = 0.5 },
        new("side_project", "Projet perso", FurnitureKind.Personal, 30m, "Sur un bureau : +0,5 de moral pour la personne du poste (1 au plus).") { Morale = 0.5 },
        new("rubber_duck_debug", "Canard de débogage", FurnitureKind.Personal, 8m, "Sur un meuble : +0,5 de moral pour la personne du poste (1 au plus).") { Morale = 0.5 },
        new("trophy_project", "Trophée", FurnitureKind.Ambiance, 0m, "La fierté d'un premier projet : +0,5 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.5 },
        new("coffee_machine", "Machine à café", FurnitureKind.Coffee, 650m, "Sur un plan de travail ou un bureau : les pauses rendent +12 d'énergie. Une machine pour 8 personnes.") { BreakRecovery = 12 },
        new("kitchen_counter", "Plan de travail", FurnitureKind.Kitchen, 280m, "Accueille une machine à café ou un micro-ondes."),
        new("sofa", "Canapé", FurnitureKind.Lounge, 900m, "Coin pause : +8 d'énergie par pause et +2 de moral.") { BreakRecovery = 8, Morale = 2 },
        new("armchair", "Fauteuil", FurnitureKind.Lounge, 350m, "Coin pause : +5 d'énergie par pause.") { BreakRecovery = 5 },
        new("whiteboard_mobile", "Tableau blanc", FurnitureKind.Whiteboard, 220m, "Une équipe se coordonne sans perte avec 1 personne de plus (2 tableaux au plus).") { Coordination = 1 },
        new("bookcase", "Bibliothèque", FurnitureKind.Bookshelf, 320m, "Formations 15 % plus efficaces (2 au plus).") { TrainingBonus = 0.15 },
        new("bed_single", "Lit", FurnitureKind.Bed, 0m, "Ton lit : tu récupères +10 d'énergie de plus chaque nuit.") { NightRecovery = 10 },
    ];

    private static readonly Dictionary<string, FurnitureDef> ById = All.ToDictionary(d => d.Id);

    /// <summary>Le meuble jouable, ou un décor sans effet pour un autre asset du pack (livré avec un local).</summary>
    public static FurnitureDef Get(string id) =>
        ById.TryGetValue(id, out var def) ? def : new FurnitureDef(id, space.Pack.Has(id) ? space.Pack.Asset(id).NameFr : id, FurnitureKind.Decor, 0m, "Décor du local.");

    public static bool Playable(string id) => ById.ContainsKey(id);

    public static bool Exists(string id) => ById.ContainsKey(id) || space.Pack.Has(id);

    public static IEnumerable<FurnitureDef> Buyable => All.Where(d => d.Buyable);
}
