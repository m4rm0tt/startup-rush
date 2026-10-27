namespace Core.space;

public enum FurnitureKind
{
    Desk, Chair, Laptop, Tower, Monitor, Keyboard, Plant, Coffee, Lounge, Whiteboard, Bookshelf, Bed, Kitchen,

    /// <summary>Objet personnel posé sur un bureau (tasse, figurine) : un peu de moral pour la personne du poste.</summary>
    Personal,

    /// <summary>Ambiance du local (lampes, tapis, affiches, trophées) : petite part du moral commun, plafonnée.</summary>
    Ambiance,

    /// <summary>Boisson ou collation en pause (fontaine, distributeurs, frigo) : de l'énergie, pour un nombre de personnes donné.</summary>
    Refreshment,

    /// <summary>Rangement et propreté (armoires, casiers, poubelles) : confort de base, selon les personnes couvertes.</summary>
    Storage,

    /// <summary>Sanitaires (WC, cabine, lavabo) : confort de base, selon les personnes couvertes.</summary>
    Sanitary,

    /// <summary>Cloisons : moins de bruit, un peu plus de concentration (plafonnée).</summary>
    Partition,

    /// <summary>Réunion et coordination (tables de réunion, planning, écran de présentation, notes).</summary>
    Meeting,

    /// <summary>Banque d'accueil : on reçoit mieux les candidats.</summary>
    Reception,

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

    /// <summary>Énergie récupérée par une pause grâce à ce service (boisson, collation ou siège).</summary>
    public double BreakRecovery { get; init; }

    /// <summary>Personnes de plus qu'une équipe peut coordonner sans perte (les petits supports comptent pour un quart).</summary>
    public double Coordination { get; init; }

    public double TrainingBonus { get; init; }

    /// <summary>Énergie que le fondateur récupère en plus chaque nuit (son lit).</summary>
    public double NightRecovery { get; init; }

    /// <summary>Gain de production d'un écran de plus (plafonné par poste).</summary>
    public double ScreenBonus { get; init; }

    /// <summary>Personnes servies : une pause, un rangement, un sanitaire ou une cloison ne profite qu'à ce nombre de personnes.</summary>
    public int Covers { get; init; }

    /// <summary>Concentration apportée par une cloison (part de production en plus, plafonnée pour le local).</summary>
    public double Focus { get; init; }

    /// <summary>Candidats de plus chaque semaine (banque d'accueil).</summary>
    public int Recruits { get; init; }

    /// <summary>Récompense d'un jalon (trophée, plaque) : elle ne s'achète pas, elle arrive dans les cartons.</summary>
    public bool Reward { get; init; }

    public PackAsset Pack => space.Pack.Asset(Id);

    public bool Buyable => Price > 0;

    /// <summary>
    /// Accroché à un socket d'un autre objet (écran, clavier, chaise, objet de bureau) plutôt que posé au sol. Un objet centré
    /// dont aucun meuble du pack n'accepte le type (le caisson « sous le bureau ») se pose au sol.
    /// </summary>
    public bool Attached => Pack.Centered && Pack.Provides.Any(space.Pack.Accepted);

    /// <summary>Se fixe sur un mur du fond (affiche, horloge, stores, planning).</summary>
    public bool OnWall => Pack.WallMount;

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

        // Ambiance : moral commun plafonné (voir Layout.Comfort)
        new("plant_floor_medium", "Plante", FurnitureKind.Plant, 45m, "+1 de moral pour l'équipe (ambiance plafonnée).") { Morale = 1 },
        new("plant_floor_tall", "Grande plante", FurnitureKind.Plant, 70m, "+1 de moral pour l'équipe (ambiance plafonnée).") { Morale = 1 },
        new("plant_succulent", "Plante grasse", FurnitureKind.Plant, 15m, "Sur un meuble : +0,5 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.5 },
        new("plant_trailing", "Plante retombante", FurnitureKind.Plant, 25m, "Sur un meuble ou une étagère : +0,5 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.5 },
        new("lamp_floor", "Lampadaire", FurnitureKind.Ambiance, 90m, "+0,5 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.5 },
        new("lamp_desk", "Lampe de bureau", FurnitureKind.Ambiance, 35m, "Sur un meuble : +0,5 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.5 },
        new("lamp_ceiling", "Luminaire suspendu", FurnitureKind.Ambiance, 140m, "Au plafond, sans prendre de place au sol : +0,75 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.75 },
        new("rug_small", "Tapis", FurnitureKind.Ambiance, 120m, "On marche dessus : +0,5 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.5 },
        new("rug_large", "Grand tapis", FurnitureKind.Ambiance, 260m, "On marche dessus, il étouffe le bruit : +1 de moral pour l'équipe (ambiance plafonnée).") { Morale = 1 },
        new("poster_project", "Affiche de projet", FurnitureKind.Ambiance, 40m, "Au mur : +0,5 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.5 },
        new("photo_abstract", "Photo encadrée", FurnitureKind.Ambiance, 60m, "Au mur : +0,5 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.5 },
        new("clock_wall", "Horloge murale", FurnitureKind.Ambiance, 35m, "Au mur : +0,25 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.25 },
        new("window_blinds", "Stores", FurnitureKind.Ambiance, 80m, "Sur une fenêtre : moins d'éblouissement, +0,25 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.25 },
        new("display_case", "Présentoir vitré", FurnitureKind.Ambiance, 480m, "On y expose trophées et prototypes : +1 de moral pour l'équipe (ambiance plafonnée).") { Morale = 1 },
        new("trophy_project", "Trophée", FurnitureKind.Ambiance, 0m, "Ton premier lancement : +0,5 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.5, Reward = true },
        new("award_plaque", "Plaque de réussite", FurnitureKind.Ambiance, 0m, "Au mur, tes 10 000 CHF par mois : +0,5 de moral pour l'équipe (ambiance plafonnée).") { Morale = 0.5, Reward = true },

        // Objets personnels : moral de la personne du poste (1 au plus)
        new("mug", "Tasse", FurnitureKind.Personal, 10m, "Sur un bureau : +0,5 de moral pour la personne du poste (1 au plus).") { Morale = 0.5 },
        new("side_project", "Projet perso", FurnitureKind.Personal, 30m, "Sur un bureau : +0,5 de moral pour la personne du poste (1 au plus).") { Morale = 0.5 },
        new("rubber_duck_debug", "Canard de débogage", FurnitureKind.Personal, 8m, "Sur un meuble : +0,5 de moral pour la personne du poste (1 au plus).") { Morale = 0.5 },
        new("figurine_robot", "Figurine robot", FurnitureKind.Personal, 20m, "Sur un bureau : +0,5 de moral pour la personne du poste (1 au plus).") { Morale = 0.5 },
        new("retro_message", "Petit écran rétro", FurnitureKind.Personal, 45m, "Sur un bureau : +0,5 de moral pour la personne du poste (1 au plus).") { Morale = 0.5 },

        // Pauses : une boisson ou une collation, et un siège ; chaque service a un nombre de places (voir Layout.BreakRecovery)
        new("coffee_machine", "Machine à café", FurnitureKind.Coffee, 650m, "Sur un plan de travail ou un meuble bas : +9 d'énergie par pause, pour 8 personnes.") { BreakRecovery = 9, Covers = 8 },
        new("water_cooler", "Fontaine à eau", FurnitureKind.Refreshment, 220m, "+4 d'énergie par pause, pour 10 personnes.") { BreakRecovery = 4, Covers = 10 },
        new("vending_drinks", "Distributeur de boissons", FurnitureKind.Refreshment, 1_200m, "Réapprovisionné par le fournisseur : +6 d'énergie par pause, pour 15 personnes.") { BreakRecovery = 6, Covers = 15 },
        new("bottle", "Bouteille d'eau", FurnitureKind.Refreshment, 5m, "Sur un bureau : +2 d'énergie par pause, pour 1 personne.") { BreakRecovery = 2, Covers = 1 },
        new("vending_snacks", "Distributeur de snacks", FurnitureKind.Refreshment, 1_100m, "Réapprovisionné par le fournisseur : +3 d'énergie par pause, pour 15 personnes.") { BreakRecovery = 3, Covers = 15 },
        new("fridge", "Réfrigérateur", FurnitureKind.Refreshment, 600m, "+3 d'énergie par pause, pour 10 personnes.") { BreakRecovery = 3, Covers = 10 },
        new("microwave", "Micro-ondes", FurnitureKind.Refreshment, 180m, "Sur un plan de travail ou un meuble bas : +3 d'énergie par pause, pour 8 personnes.") { BreakRecovery = 3, Covers = 8 },
        new("snack_bowl", "Bol de snacks", FurnitureKind.Refreshment, 12m, "Sur un meuble : +1,5 d'énergie par pause, pour 3 personnes.") { BreakRecovery = 1.5, Covers = 3 },
        new("kitchen_sink", "Évier", FurnitureKind.Refreshment, 400m, "Complète une cuisine (frigo, micro-ondes ou café) : +1 d'énergie par pause, pour 10 personnes.") { BreakRecovery = 1, Covers = 10 },
        new("kitchen_counter", "Plan de travail", FurnitureKind.Kitchen, 280m, "Accueille une machine à café ou un micro-ondes."),
        new("sofa", "Canapé", FurnitureKind.Lounge, 900m, "Coin pause : +5 d'énergie par pause pour 3 personnes, et +2 de moral.") { BreakRecovery = 5, Covers = 3, Morale = 2 },
        new("armchair", "Fauteuil", FurnitureKind.Lounge, 350m, "Coin pause : +2 d'énergie par pause pour 1 personne, et +1 de moral.") { BreakRecovery = 2, Covers = 1, Morale = 1 },
        new("table_coffee", "Table basse", FurnitureKind.Lounge, 150m, "Complète un coin pause (avec un canapé ou un fauteuil) : +1 d'énergie par pause pour 4 personnes.") { BreakRecovery = 1, Covers = 4 },

        // Confort de base : rangement et sanitaires, selon les personnes couvertes
        new("dresser", "Commode", FurnitureKind.Storage, 150m, "Du rangement pour 2 personnes (confort de base).") { Covers = 2 },
        new("nightstand", "Table de chevet", FurnitureKind.Storage, 60m, "Du rangement pour 1 personne (confort de base).") { Covers = 1 },
        new("trash_bin", "Corbeille", FurnitureKind.Storage, 15m, "Un local propre, pour 3 personnes (confort de base).") { Covers = 3 },
        new("recycling_station", "Bacs de tri", FurnitureKind.Storage, 200m, "Un local propre, pour 8 personnes (confort de base).") { Covers = 8 },
        new("coat_rack", "Porte-manteau", FurnitureKind.Storage, 60m, "Les affaires de 4 personnes (confort de base).") { Covers = 4 },
        new("filing_cabinet", "Classeur à tiroirs", FurnitureKind.Storage, 260m, "Les papiers de 3 personnes (confort de base).") { Covers = 3 },
        new("locker", "Casiers", FurnitureKind.Storage, 380m, "Les affaires de 4 personnes (confort de base).") { Covers = 4 },
        new("wardrobe_office", "Armoire", FurnitureKind.Storage, 420m, "Du rangement pour 3 personnes (confort de base).") { Covers = 3 },
        new("shelf_metal", "Étagère métallique", FurnitureKind.Storage, 190m, "Du rangement pour 4 personnes (confort de base).") { Covers = 4 },
        new("sideboard_low", "Meuble bas", FurnitureKind.Storage, 340m, "Du rangement pour 2 personnes ; accueille une machine à café ou un micro-ondes.") { Covers = 2 },
        new("pedestal_drawer", "Caisson à roulettes", FurnitureKind.Storage, 140m, "Du rangement pour 1 personne (confort de base).") { Covers = 1 },
        new("storage_boxes", "Boîtes de rangement", FurnitureKind.Storage, 25m, "Sur un meuble : du rangement pour 1 personne (confort de base).") { Covers = 1 },
        new("cable_organizer", "Range-câbles", FurnitureKind.Storage, 15m, "Sur un bureau : un poste en ordre, pour 1 personne (confort de base).") { Covers = 1 },
        new("kitchen_wall_cabinet", "Placard haut", FurnitureKind.Storage, 220m, "Au mur : du rangement pour 2 personnes (confort de base).") { Covers = 2 },
        new("toilet", "WC", FurnitureKind.Sanitary, 900m, "Les sanitaires de 10 personnes (confort de base).") { Covers = 10 },
        new("toilet_stall", "Cabine sanitaire", FurnitureKind.Sanitary, 700m, "Avec un WC : plus d'intimité (confort de base).") { Covers = 10 },
        new("washbasin", "Lavabo", FurnitureKind.Sanitary, 450m, "Avec un WC : de quoi se laver les mains (confort de base).") { Covers = 10 },

        // Concentration : cloisons (plafonnée)
        new("partition_acoustic", "Séparation acoustique", FurnitureKind.Partition, 420m, "Moins de bruit : production +2 % pour 4 personnes (4 % au plus).") { Focus = 0.02, Covers = 4 },
        new("partition_low", "Cloison basse", FurnitureKind.Partition, 180m, "Délimite une zone : production +1 % pour 4 personnes (4 % au plus).") { Focus = 0.01, Covers = 4 },
        new("partition_glass", "Vitrage intérieur", FurnitureKind.Partition, 520m, "Sépare sans fermer : production +1 % pour 4 personnes (4 % au plus).") { Focus = 0.01, Covers = 4 },
        new("partition_glass_door", "Vitrage avec porte", FurnitureKind.Partition, 640m, "Sépare avec un passage : production +1 % pour 4 personnes (4 % au plus).") { Focus = 0.01, Covers = 4 },

        // Coordination, formation, accueil
        new("whiteboard_mobile", "Tableau blanc", FurnitureKind.Whiteboard, 220m, "Une équipe se coordonne sans perte avec 1 personne de plus (3 au plus pour le local).") { Coordination = 1 },
        new("planning_board", "Planning mural", FurnitureKind.Meeting, 260m, "Au mur, sur deux cases : 1 personne de plus par équipe sans perte (3 au plus pour le local).") { Coordination = 1 },
        new("table_meeting_small", "Petite table de réunion", FurnitureKind.Meeting, 600m, "Avec 2 chaises : 1 personne de plus par équipe sans perte, formations +5 %.") { Coordination = 1, TrainingBonus = 0.05 },
        new("table_meeting_large", "Grande table de réunion", FurnitureKind.Meeting, 1_400m, "Avec 4 chaises : 2 personnes de plus par équipe sans perte, formations +5 %.") { Coordination = 2, TrainingBonus = 0.05 },
        new("table_collab", "Table de travail collective", FurnitureKind.Meeting, 900m, "1 personne de plus par équipe sans perte (3 au plus pour le local).") { Coordination = 1 },
        new("chair_meeting", "Chaise de réunion", FurnitureKind.Chair, 120m, "Autour d'une table de réunion (ou à un bureau, sans confort particulier)."),
        new("presentation_screen", "Écran de présentation", FurnitureKind.Meeting, 1_100m, "Formations +10 %, et une demi-personne de plus par équipe sans perte.") { Coordination = 0.5, TrainingBonus = 0.1 },
        new("sticky_notes", "Notes adhésives", FurnitureKind.Meeting, 10m, "Au mur : un quart de personne de plus par équipe sans perte (1 au plus pour les petits supports).") { Coordination = 0.25 },
        new("idea_sketch", "Croquis d'idée", FurnitureKind.Meeting, 10m, "Au mur : un quart de personne de plus par équipe sans perte (1 au plus pour les petits supports).") { Coordination = 0.25 },
        new("notebook", "Carnet et stylo", FurnitureKind.Meeting, 8m, "Sur un meuble : un quart de personne de plus par équipe sans perte (1 au plus pour les petits supports).") { Coordination = 0.25 },
        new("paper_stack", "Pile de papiers", FurnitureKind.Meeting, 5m, "Sur un meuble : un quart de personne de plus par équipe sans perte (1 au plus pour les petits supports).") { Coordination = 0.25 },
        new("bookcase", "Bibliothèque", FurnitureKind.Bookshelf, 320m, "Formations 15 % plus efficaces (2 au plus).") { TrainingBonus = 0.15 },
        new("books_stack", "Pile de livres", FurnitureKind.Bookshelf, 25m, "Sur un meuble : formations +5 % (40 % au plus pour le local).") { TrainingBonus = 0.05 },
        new("poster_schematic", "Schéma technique", FurnitureKind.Bookshelf, 50m, "Au mur : formations +5 % (40 % au plus pour le local).") { TrainingBonus = 0.05 },
        new("reception_desk", "Banque d'accueil", FurnitureKind.Reception, 1_500m, "On reçoit les candidats : 1 candidat de plus chaque semaine.") { Recruits = 1 },
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
