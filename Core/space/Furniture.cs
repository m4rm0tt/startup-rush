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

    /// <summary>Baie : des emplacements en U pour les modules (Core/space/Infrastructure.cs).</summary>
    Rack,

    /// <summary>Module de baie (serveur, calcul, GPU, stockage, réseau, onduleur, obturateur...).</summary>
    RackModule,

    /// <summary>Serveur ou stockage hors baie (serveur tour, NAS de bureau, station GPU).</summary>
    Server,

    /// <summary>Énergie, froid et réseau du site (armoire électrique, climatiseur, routeur, fibre).</summary>
    Utility,

    /// <summary>Onduleur : marge de continuité électrique.</summary>
    Backup,

    /// <summary>Câblage et sécurité (chemins de câbles, cage) : organisation et confiance des services.</summary>
    Cabling,

    /// <summary>Console de supervision (NOC).</summary>
    Supervision,

    /// <summary>Équipement de laboratoire : un poste de recherche (établi, banc) ou un instrument qui rend la recherche possible.</summary>
    Lab,

    /// <summary>Station de production (Core/industry) : une étape d'une recette.</summary>
    Station,

    /// <summary>Convoyeur, jonction, table de transfert ou fin de ligne : relie les stations port à port.</summary>
    Conveyor,

    /// <summary>Rangement de stock (rack à palettes, palette, bac).</summary>
    Warehouse,

    /// <summary>Zone et utilités d'usine (salle propre, aspiration, armoire de contrôle, rodage).</summary>
    Facility,

    /// <summary>Studio audiovisuel (caméra, fond vert, éclairage, micro, enceintes) : qualité du contenu et de la présentation.</summary>
    Studio,

    /// <summary>
    /// Périphérique posé sur un bureau (tablette graphique, casque, station d'accueil, bancs de test) : un petit plus pour le
    /// poste (plafonné), ou un banc de test qui retire des défauts.
    /// </summary>
    Peripheral,

    /// <summary>Zone de test XR (casque, balises, tapis) : tests immersifs simulés.</summary>
    Xr,

    /// <summary>Installation quantique (cryostat, baie de contrôle, support cryogénique), à la charge du site en froid et en énergie.</summary>
    Quantum,

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

    // Infrastructure (Core/space/Infrastructure.cs) : capacités fournies, énergie consommée, services du site

    /// <summary>Unités d'hébergement d'applications (même unité que la charge des projets).</summary>
    public double Hosting { get; init; }

    /// <summary>Unités de calcul CPU (travaux par lots, offres de calcul).</summary>
    public double Compute { get; init; }

    /// <summary>Unités de calcul GPU.</summary>
    public double Gpu { get; init; }

    /// <summary>Stockage, en To.</summary>
    public double Storage { get; init; }

    /// <summary>Débit réseau apporté au site (unités de trafic).</summary>
    public double Network { get; init; }

    /// <summary>Puissance consommée, en kW (elle devient aussi de la chaleur).</summary>
    public double Power { get; init; }

    /// <summary>Puissance électrique distribuable apportée au site, en kW.</summary>
    public double PowerSupply { get; init; }

    /// <summary>Chaleur évacuée, en kW.</summary>
    public double Cooling { get; init; }

    /// <summary>Puissance couverte par un onduleur, en kW.</summary>
    public double Backup { get; init; }

    /// <summary>Points d'organisation du câblage (brassage, passe-câbles, chemins de câbles).</summary>
    public double Organization { get; init; }

    /// <summary>Points de sécurité d'une zone technique (cage).</summary>
    public double Security { get; init; }

    /// <summary>Abonnement mensuel lié à l'objet (fibre), en CHF.</summary>
    public decimal MonthlyCost { get; init; }

    /// <summary>Nœud de l'arbre qu'il faut pour l'acheter.</summary>
    public string? UnlockNode { get; init; }

    public PackAsset Pack => space.Pack.Asset(Id);

    public bool Buyable => Price > 0;

    /// <summary>
    /// Accroché à un socket d'un autre objet (écran, clavier, chaise, objet de bureau) plutôt que posé au sol. Un objet centré
    /// dont aucun meuble du pack n'accepte le type (le caisson « sous le bureau ») se pose au sol.
    /// </summary>
    public bool Attached => Pack.RackModule || Pack.Centered && Pack.Provides.Any(space.Pack.Accepted);

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

        // Infrastructure : baies et modules (en U), serveurs, énergie, froid, réseau, câblage, sécurité (Core/space/Infrastructure.cs)
        new("rack_compact", "Baie compacte (8U)", FurnitureKind.Rack, 900m, "8 emplacements pour des modules. Il faut un switch dans la baie.") { UnlockNode = "server_room" },
        new("rack_standard", "Baie standard (20U)", FurnitureKind.Rack, 1_800m, "20 emplacements pour des modules. Il faut un switch dans la baie.") { UnlockNode = "server_room" },
        new("rack_high_density", "Baie haute densité (20U)", FurnitureKind.Rack, 3_200m, "20 emplacements ; porte arrière refroidie : ses modules chauffent 20 % moins.") { UnlockNode = "high_density" },
        new("module_server_1u", "Serveur 1U", FurnitureKind.RackModule, 2_400m, "Hébergement : 16 unités. 0,3 kW.") { Hosting = 16, Power = 0.3, UnlockNode = "server_room" },
        new("module_compute_2u", "Serveur de calcul 2U", FurnitureKind.RackModule, 3_800m, "Calcul : 40 unités, et 8 d'hébergement. 0,6 kW.") { Compute = 40, Hosting = 8, Power = 0.6, UnlockNode = "server_room" },
        new("module_compute_node_2u", "Nœuds de calcul 2U", FurnitureKind.RackModule, 6_500m, "Calcul parallèle : 64 unités (aucun hébergement). 0,9 kW.") { Compute = 64, Power = 0.9, UnlockNode = "high_density" },
        new("module_gpu_4u", "Serveur GPU 4U", FurnitureKind.RackModule, 14_000m, "GPU : 10 unités. 1,6 kW.") { Gpu = 10, Power = 1.6, UnlockNode = "high_density" },
        new("module_storage_2u", "Stockage 2U (12 baies)", FurnitureKind.RackModule, 4_800m, "Stockage : 48 To ; contrôle les tiroirs de disques de la baie. 0,25 kW.") { Storage = 48, Power = 0.25, UnlockNode = "server_room" },
        new("module_storage_jbod_4u", "Tiroir de disques 4U", FurnitureKind.RackModule, 6_000m, "Stockage : +96 To, avec un stockage 2U dans la même baie. 0,3 kW.") { Storage = 96, Power = 0.3, UnlockNode = "server_room" },
        new("module_network_switch_1u", "Switch réseau 1U", FurnitureKind.RackModule, 900m, "Relie les modules de sa baie au réseau du site. 0,1 kW.") { Power = 0.1, UnlockNode = "server_room" },
        new("module_patch_panel_1u", "Panneau de brassage 1U", FurnitureKind.RackModule, 300m, "Organisation du câblage : +1.") { Organization = 1, UnlockNode = "server_room" },
        new("module_cable_manager_1u", "Passe-câbles 1U", FurnitureKind.RackModule, 150m, "Organisation du câblage : +1.") { Organization = 1, UnlockNode = "server_room" },
        new("module_ups_2u", "Onduleur de baie 2U", FurnitureKind.RackModule, 2_200m, "Couvre 2 kW en cas de coupure.") { Backup = 2, UnlockNode = "server_room" },
        new("module_blank_1u", "Obturateur 1U", FurnitureKind.RackModule, 30m, "Canalise l'air : la baie chauffe 3 % moins (9 % au plus).") { UnlockNode = "server_room" },
        new("server_tower_classic", "Serveur tour", FurnitureKind.Server, 2_900m, "Hébergement : 8 unités, stockage 8 To. 0,35 kW. Sans baie.") { Hosting = 8, Storage = 8, Power = 0.35 },
        new("nas_desktop", "NAS de bureau", FurnitureKind.Server, 900m, "Sur un meuble : stockage 16 To. 0,05 kW.") { Storage = 16, Power = 0.05 },
        new("power_distribution_cabinet", "Armoire électrique", FurnitureKind.Utility, 2_500m, "+30 kW de puissance distribuable sur le site.") { PowerSupply = 30, UnlockNode = "server_room" },
        new("crac_unit", "Climatiseur de salle", FurnitureKind.Utility, 8_500m, "Évacue 24 kW de chaleur ; consomme 4 kW.") { Cooling = 24, Power = 4, UnlockNode = "server_room" },
        new("rack_cooler_inrow", "Refroidissement de rangée", FurnitureKind.Utility, 4_800m, "Évacue 12 kW de chaleur ; consomme 1,5 kW.") { Cooling = 12, Power = 1.5, UnlockNode = "high_density" },
        new("router", "Routeur", FurnitureKind.Utility, 250m, "Sur un meuble : +100 de débit réseau pour le site.") { Network = 100 },
        new("fiber_box", "Boîtier fibre", FurnitureKind.Utility, 600m, "Au mur : +1 000 de débit réseau ; abonnement 120 CHF par mois.") { Network = 1_000, MonthlyCost = 120 },
        new("network_switch_desk", "Switch de bureau", FurnitureKind.Utility, 120m, "Sur un meuble : +50 de débit réseau local.") { Network = 50 },
        new("access_point", "Point d'accès Wi-Fi", FurnitureKind.Utility, 180m, "Au mur : +50 de débit réseau pour les appareils sans fil.") { Network = 50 },
        new("network_cabinet", "Baie de rangement réseau", FurnitureKind.Cabling, 700m, "Organisation du câblage : +2.") { Organization = 2 },
        new("cable_tray_straight", "Chemin de câbles", FurnitureKind.Cabling, 80m, "En hauteur, sans gêner : organisation du câblage +0,5.") { Organization = 0.5, UnlockNode = "server_room" },
        new("cable_tray_corner", "Chemin de câbles (angle)", FurnitureKind.Cabling, 80m, "En hauteur, sans gêner : organisation du câblage +0,5.") { Organization = 0.5, UnlockNode = "server_room" },
        new("cable_conduit", "Passe-câble au sol", FurnitureKind.Cabling, 40m, "Au sol, on marche dessus : organisation du câblage +0,5.") { Organization = 0.5, UnlockNode = "server_room" },
        new("security_cage_panel", "Grille de sécurité", FurnitureKind.Cabling, 450m, "Cage de la zone technique : sécurité +1 (les offres cloud inspirent confiance).") { Security = 1, UnlockNode = "server_room" },
        new("security_cage_door", "Porte de cage", FurnitureKind.Cabling, 900m, "Accès à la cage : sécurité +2.") { Security = 2, UnlockNode = "server_room" },
        new("ups_floor", "Onduleur au sol", FurnitureKind.Backup, 6_000m, "Couvre 10 kW en cas de coupure.") { Backup = 10, UnlockNode = "server_room" },
        new("supervision_console", "Console de supervision", FurnitureKind.Supervision, 3_500m, "Surveille le site : incidents deux fois moins fréquents, organisation +1.") { Organization = 1, UnlockNode = "server_room" },
        new("workstation_gpu", "Station GPU", FurnitureKind.Tower, 4_800m, "Production ×1,05 ; apporte 2 unités GPU au site. 0,5 kW. Demande un écran et un clavier.") { Output = 1.05, Gpu = 2, Power = 0.5 },
        // Laboratoires (lot 5) : postes de recherche et instruments ; les recherches demandent certains équipements sur le site
        new("workbench_electronics", "Établi électronique", FurnitureKind.Lab, 2_400m, "Un poste de recherche (prototypes, cartes) ; accueille instruments et outils.") { Output = 0.9, Power = 0.3, UnlockNode = "hardware" },
        new("research_station", "Paillasse de recherche", FurnitureKind.Lab, 3_200m, "Un poste de chercheur : +10 % sur les recherches du site (2 au plus).") { Output = 1.0, Power = 0.3, UnlockNode = "hardware" },
        new("test_bench_pc", "Banc de test PC", FurnitureKind.Lab, 2_800m, "Valide les PC et les serveurs ; un poste de recherche.") { Output = 0.9, Power = 0.6, UnlockNode = "hardware" },
        new("test_bench_embedded", "Banc de test embarqué", FurnitureKind.Lab, 2_600m, "Valide les cartes et les appareils mobiles ; un poste de recherche.") { Output = 0.9, Power = 0.3, UnlockNode = "hardware" },
        new("calibration_bench", "Établi de calibration", FurnitureKind.Lab, 2_200m, "La précision des prototypes et des premières séries.") { Power = 0.1, UnlockNode = "hardware" },
        new("instrument_rack", "Chariot d'instruments", FurnitureKind.Lab, 3_500m, "Des mesures avancées sur les bancs de test.") { Power = 0.4, UnlockNode = "hardware" },
        new("printer_3d", "Imprimante 3D", FurnitureKind.Lab, 1_800m, "Des pièces et boîtiers de prototype.") { Power = 0.3, UnlockNode = "hardware" },
        new("laser_cutter", "Découpeuse laser", FurnitureKind.Lab, 6_500m, "Des pièces plates et des gabarits.") { Power = 1.5, UnlockNode = "hardware" },
        new("cobot_cell", "Cellule cobot", FurnitureKind.Lab, 22_000m, "Un robot collaboratif pour mettre au point l'assemblage automatisé.") { Power = 1.2, UnlockNode = "industry" },
        new("oscilloscope", "Oscilloscope", FurnitureKind.Lab, 1_200m, "Sur un établi : le diagnostic des signaux.") { Power = 0.1, UnlockNode = "hardware" },
        new("soldering_station", "Station de soudure", FurnitureKind.Lab, 450m, "Sur un établi : souder les prototypes.") { Power = 0.1, UnlockNode = "hardware" },
        new("lab_power_supply", "Alimentation de laboratoire", FurnitureKind.Lab, 400m, "Sur un établi : alimenter les prototypes.") { Power = 0.1, UnlockNode = "hardware" },
        new("microscope", "Microscope", FurnitureKind.Lab, 1_500m, "Sur un établi : inspecter les soudures et les puces.") { UnlockNode = "hardware" },
        new("cnc_mill_desktop", "Fraiseuse CNC", FurnitureKind.Lab, 4_800m, "Sur un établi : usiner des pièces précises.") { Power = 0.6, UnlockNode = "hardware" },

        // Production (lot 5) : stations reliées par des convoyeurs, port à port (Core/industry)
        new("assembly_station_manual", "Poste d'assemblage", FurnitureKind.Station, 2_500m, "Assemblage manuel : 30 min par unité, un opérateur.") { Power = 0.2, UnlockNode = "hardware" },
        new("flash_station", "Station de flash", FurnitureKind.Station, 3_000m, "Installe le logiciel : 10 min par unité, un opérateur.") { Power = 0.5, UnlockNode = "hardware" },
        new("packing_station", "Poste d'emballage", FurnitureKind.Station, 2_200m, "Emballe et étiquette : 8 min par unité, un opérateur.") { Power = 0.3, UnlockNode = "hardware" },
        new("assembly_cell_robotic", "Cellule d'assemblage robotisée", FurnitureKind.Station, 45_000m, "Assemblage automatique : 12 min par unité, supervisée.") { Power = 4, UnlockNode = "industry" },
        new("pick_and_place_machine", "Machine de placement", FurnitureKind.Station, 38_000m, "Place les composants sur les cartes : 6 min par carte.") { Power = 3, UnlockNode = "industry" },
        new("reflow_oven", "Four de refusion", FurnitureKind.Station, 24_000m, "Soude les cartes : 6 min par carte.") { Power = 6, UnlockNode = "industry" },
        new("aoi_inspection", "Inspection optique", FurnitureKind.Station, 16_000m, "Inspecte : 4 min par unité.") { Power = 1, UnlockNode = "industry" },
        new("pcb_tester", "Testeur de cartes", FurnitureKind.Station, 18_000m, "Teste : 8 min par unité.") { Power = 1, UnlockNode = "industry" },
        new("chip_fab_tool", "Outil de traitement des plaquettes", FurnitureKind.Station, 180_000m, "En salle propre : 4 h par plaquette (20 processeurs).") { Power = 25, UnlockNode = "cleanroom" },
        new("precision_station", "Station de précision", FurnitureKind.Facility, 60_000m, "Contrôle les plaquettes : sans elle, la moitié des puces d'une plaquette est rebutée.") { Power = 5, UnlockNode = "cleanroom" },
        new("conveyor_straight", "Convoyeur droit", FurnitureKind.Conveyor, 400m, "Transporte dans un sens (2 unités en attente).") { UnlockNode = "hardware" },
        new("conveyor_curve", "Convoyeur courbe", FurnitureKind.Conveyor, 450m, "Tourne à angle droit.") { UnlockNode = "hardware" },
        new("conveyor_junction_t", "Jonction en T", FurnitureKind.Conveyor, 700m, "Réunit deux convoyeurs en un.") { UnlockNode = "hardware" },
        new("conveyor_transfer_table", "Table de transfert", FurnitureKind.Conveyor, 1_400m, "Renvoie vers n'importe quel côté relié.") { UnlockNode = "hardware" },
        new("conveyor_end", "Fin de ligne", FurnitureKind.Conveyor, 300m, "Les unités terminées y entrent au stock du site.") { UnlockNode = "hardware" },
        new("storage_rack_industrial", "Rack à palettes", FurnitureKind.Warehouse, 1_200m, "Stock du site : +150 unités.") { UnlockNode = "hardware" },
        new("pallet", "Palette", FurnitureKind.Warehouse, 40m, "Stock du site : +40 unités.") { UnlockNode = "hardware" },
        new("tote_bin", "Bac de manutention", FurnitureKind.Warehouse, 25m, "Stock du site : +10 unités.") { UnlockNode = "hardware" },
        new("stress_test_rack", "Banc de rodage", FurnitureKind.Facility, 6_000m, "Rode 30 unités par jour sur le site : −40 % de défauts sur celles-là.") { Power = 2, UnlockNode = "hardware" },
        new("control_cabinet", "Armoire de contrôle", FurnitureKind.Facility, 5_000m, "Supervise 4 machines automatiques sans opérateur.") { Power = 0.3, UnlockNode = "industry" },
        new("cleanroom_partition", "Cloison de salle propre", FurnitureKind.Facility, 2_500m, "Avec le sas, l'aspiration et le refroidissement : une salle propre (4 cloisons au moins).") { UnlockNode = "cleanroom" },
        new("cleanroom_airlock", "Sas de salle propre", FurnitureKind.Facility, 12_000m, "L'entrée de la salle propre.") { Power = 1, UnlockNode = "cleanroom" },
        new("dust_extraction", "Centrale d'aspiration", FurnitureKind.Facility, 6_000m, "Filtration de l'air de la salle propre.") { Power = 2, UnlockNode = "cleanroom" },
        new("industrial_chiller", "Groupe de refroidissement", FurnitureKind.Utility, 15_000m, "Évacue 30 kW de chaleur ; consomme 6 kW. Demandé par la salle propre.") { Cooling = 30, Power = 6, UnlockNode = "industry" },

        // Lot 6 : studio audiovisuel, zone XR, calcul avancé et quantique
        new("camera_tripod", "Caméra sur trépied", FurnitureKind.Studio, 900m, "Studio : la capture vidéo (jeux, campagnes, présentation). 0,05 kW.") { Power = 0.05, UnlockNode = "studio" },
        new("greenscreen_backdrop", "Fond vert sur portique", FurnitureKind.Studio, 700m, "Studio : le fond des vidéos et des trailers.") { UnlockNode = "studio" },
        new("softbox_light", "Éclairage softbox", FurnitureKind.Studio, 350m, "Studio : la lumière (deux au plus comptent). 0,2 kW.") { Power = 0.2, UnlockNode = "studio" },
        new("mic_stand", "Pied de micro avec perche", FurnitureKind.Studio, 280m, "Studio : la capture du son (voix, effets).") { UnlockNode = "studio" },
        new("studio_monitors", "Enceintes de studio (paire)", FurnitureKind.Studio, 650m, "Sur un bureau : le contrôle audio du studio. 0,1 kW.") { Power = 0.1, UnlockNode = "studio" },
        new("xr_headset_stand", "Casque XR sur support", FurnitureKind.Xr, 1_200m, "Sur un bureau : de quoi tester seul un prototype XR. 0,05 kW.") { Power = 0.05, UnlockNode = "xr" },
        new("xr_base_station", "Balise de suivi XR sur pied", FurnitureKind.Xr, 450m, "Zone XR : le suivi de la zone (deux au plus comptent). 0,05 kW.") { Power = 0.05, UnlockNode = "xr" },
        new("xr_play_mat", "Tapis de zone de jeu XR", FurnitureKind.Xr, 600m, "Zone XR : l'emprise de la zone de test, avec le casque et les balises.") { UnlockNode = "xr" },
        new("compute_cabinet", "Calculateur avancé (armoire)", FurnitureKind.Server, 18_000m, "Calcul : 120 unités, pour l'entraînement des gros modèles et la recherche. 2,5 kW.") { Compute = 120, Power = 2.5, UnlockNode = "advanced_lab" },
        new("supervision_terminal", "Terminal de supervision (pupitre)", FurnitureKind.Supervision, 1_800m, "Surveille le site : incidents deux fois moins fréquents, organisation +0,5.") { Organization = 0.5, UnlockNode = "server_room" },
        new("quantum_control_rack", "Baie de contrôle micro-ondes", FurnitureKind.Quantum, 35_000m, "Pilote un cryostat (une par cryostat). 1,5 kW.") { Power = 1.5, UnlockNode = "advanced_lab" },
        new("cryo_support_unit", "Unité de support cryogénique", FurnitureKind.Quantum, 60_000m, "Refroidit un cryostat (une par cryostat). 5 kW.") { Power = 5, UnlockNode = "advanced_lab" },
        new("quantum_cryostat_open", "Ordinateur quantique (cryostat ouvert)", FurnitureKind.Quantum, 120_000m,
            "Installation expérimentale : calibration et recherches quantiques (rendement de recherche). Avec une baie de contrôle et un support cryogénique. 6 kW.") { Power = 6, UnlockNode = "advanced_lab" },
        new("quantum_cryostat_closed", "Ordinateur quantique (cryostat fermé)", FurnitureKind.Quantum, 380_000m,
            "Installation stabilisée : 20 unités de calcul quantique à vendre, après 14 jours de mise en route. Avec une baie de contrôle et un support cryogénique. 8 kW.") { Power = 8, UnlockNode = "res_cryo" },

        // Lot 7 : l'informatique du pack qui n'avait pas encore d'usage
        new("laptop_gaming", "Portable de jeu", FurnitureKind.Laptop, 2_400m, "Production ×1,05 et qualité +0,03. 0,15 kW.") { Output = 1.05, Quality = 0.03, Power = 0.15 },
        new("mini_pc", "Mini-PC", FurnitureKind.Tower, 600m, "Sur le bureau : production ×0,95, très sobre (0,02 kW). Demande un écran et un clavier.") { Output = 0.95, Power = 0.02 },
        new("workstation_creative", "Station créative", FurnitureKind.Tower, 3_200m, "Production ×1,05 et qualité +0,05. 0,3 kW. Demande un écran et un clavier.") { Output = 1.05, Quality = 0.05, Power = 0.3 },
        new("monitor_ultrawide", "Écran ultralarge", FurnitureKind.Monitor, 520m, "Production +5 % (6 % au plus par poste).") { ScreenBonus = 0.05 },
        new("graphics_tablet", "Tablette graphique", FurnitureKind.Peripheral, 320m, "Sur un bureau : qualité du travail +0,02 (0,03 au plus par poste).") { Quality = 0.02 },
        new("headset_stand", "Casque audio", FurnitureKind.Peripheral, 140m, "Sur un bureau : production +2 % (3 % au plus par poste).") { Focus = 0.02 },
        new("docking_station", "Station d'accueil", FurnitureKind.Peripheral, 180m, "Sur un bureau avec un portable : production +2 % (3 % au plus par poste).") { Focus = 0.02 },
        new("console_test_kit", "Kit de test console", FurnitureKind.Peripheral, 1_400m, "Sur un bureau : 15 % de défauts en moins à chaque amélioration d'un jeu."),
        new("device_test_rack", "Banc d'appareils mobiles", FurnitureKind.Peripheral, 1_600m, "Sur un bureau : 15 % de défauts en moins à chaque amélioration d'une application."),
        new("webcam", "Webcam", FurnitureKind.Meeting, 90m, "Sur un écran : réunions à distance, coordination +0,25 (petits supports, 1 au plus).") { Coordination = 0.25 },
        new("speakerphone", "Pieuvre de conférence", FurnitureKind.Meeting, 260m, "Sur une table de réunion : coordination +0,25 (petits supports, 1 au plus).") { Coordination = 0.25 },
        new("videoconf_bar", "Barre de visioconférence", FurnitureKind.Meeting, 900m, "Sur l'écran de présentation : une salle de visio, coordination +0,5.") { Coordination = 0.5 },
        new("printer_mfp", "Imprimante multifonction", FurnitureKind.Meeting, 650m, "Documents et maquettes papier : coordination +0,25 (petits supports, 1 au plus). 0,05 kW.") { Coordination = 0.25, Power = 0.05 },
        new("capture_device", "Boîtier de capture", FurnitureKind.Studio, 240m, "Sur un bureau : capture de démos et de jeux, compte comme la caméra du studio.") { UnlockNode = "studio" },
        new("workstation_compute", "Poste de calcul avancé", FurnitureKind.Tower, 3_900m, "Production ×1,05 ; apporte 12 unités de calcul au site. 0,4 kW. Demande un écran et un clavier.") { Output = 1.05, Compute = 12, Power = 0.4 },
    ];

    private static readonly Dictionary<string, FurnitureDef> ById = All.ToDictionary(d => d.Id);

    /// <summary>Le meuble jouable, ou un décor sans effet pour un autre asset du pack (livré avec un local).</summary>
    public static FurnitureDef Get(string id) =>
        ById.TryGetValue(id, out var def) ? def : new FurnitureDef(id, space.Pack.Has(id) ? space.Pack.Asset(id).NameFr : id, FurnitureKind.Decor, 0m, "Décor du local.");

    public static bool Playable(string id) => ById.ContainsKey(id);

    public static bool Exists(string id) => ById.ContainsKey(id) || space.Pack.Has(id);

    public static IEnumerable<FurnitureDef> Buyable => All.Where(d => d.Buyable);
}
