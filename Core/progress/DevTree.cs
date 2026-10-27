namespace Core.progress;

/// <summary>Un nœud de l'arbre de développement : un déblocage réel, avec son coût et ses prérequis.</summary>
public record DevNode(string Id, string Branch, string Name, string Icon, string Effect, int Cost, string? Requires = null);

/// <summary>
/// L'arbre de développement de l'entreprise. Une seule monnaie : les points de développement, gagnés par des
/// accomplissements réels (lancements adoptés, paliers d'utilisateurs, premiers bénéfices...). Rien ne se perd,
/// aucune branche n'en exclut une autre. Chaque nœud a un effet réel dans la simulation.
/// </summary>
public static class DevTree
{
    public static readonly string[] Branches = ["Création", "Organisation", "Infrastructure", "Commercial", "Innovation", "Outils du boss"];

    public static readonly IReadOnlyList<DevNode> Nodes =
    [
        new("advanced", "Création", "Composants avancés", "gem", "Les composants peuvent monter au niveau 3.", 1),
        new("community", "Création", "Apps communautaires", "people", "Nouveau type de projet : app communautaire financée par la publicité.", 2),
        new("multiplayer", "Création", "Multijoueur", "dice", "Nouveau composant de jeu : le multijoueur en ligne.", 2, "advanced"),
        new("marketplace", "Création", "Places de marché", "tag", "Nouveau type de projet : place de marché à commission.", 3, "community"),

        new("hiring", "Organisation", "Recrutement élargi", "user", "2 candidats de plus chaque semaine, souvent plus expérimentés.", 1),
        new("leads", "Organisation", "Responsables", "crown", "On peut nommer des responsables : une équipe de 6 travaille sans perte.", 1),
        new("academy", "Organisation", "Formation interne", "calendar", "Formations à moitié prix et 30 % plus efficaces.", 2, "hiring"),
        new("bigteam", "Organisation", "Grandes équipes", "building", "+2 personnes par projet sans perte de coordination.", 2, "leads"),

        new("cloud_m", "Infrastructure", "Cloud M", "wifi", "Offre d'hébergement Cloud M (capacité ×4).", 1),
        new("optimization", "Infrastructure", "Optimisation", "bolt", "−15 % de charge serveur sur tous les projets.", 1),
        new("cloud_l", "Infrastructure", "Cloud L et cluster", "wifi", "Les deux plus grosses offres d'hébergement.", 2, "cloud_m"),

        new("search_ads", "Commercial", "Moteurs de recherche", "link", "Nouveau canal marketing : publicité sur les moteurs de recherche.", 1),
        new("launches", "Commercial", "Lancements événementiels", "rocket", "Avant sa sortie, un jeu attire 50 % d'intérêt en plus.", 1),
        new("press", "Commercial", "Salons et presse", "megaphone", "Nouveau canal marketing : salons et presse.", 2, "search_ads"),
        new("loyalty", "Commercial", "Fidélisation", "heart", "Les utilisateurs partent 15 % moins vite.", 2),

        new("quality_lab", "Innovation", "Laboratoire qualité", "check", "+0,05 de qualité sur tout le travail.", 2),
        new("ai_tools", "Innovation", "Assistants de code", "computer", "+10 % de production pour toute l'équipe.", 3, "quality_lab"),

        // Outils du boss : le terminal, puis la délégation (Campaign.Terminal.cs, Campaign.Launch.cs)
        new("boss_speed", "Outils du boss", "Raccourcis clavier", "bolt", "Chaque mot du terminal rapporte 15 % de travail en plus.", 1),
        new("boss_comfort", "Outils du boss", "Clavier confortable", "heart", "Le terminal fatigue 40 % moins par mot.", 1),
        new("boss_tests", "Outils du boss", "Tests assistés", "check", "Au terminal, les mots de tests rapportent 30 % de plus.", 2, "boss_speed"),
        new("boss_macros", "Outils du boss", "Macros", "screen", "Sessions de 32 mots au lieu de 24 (même rendement par mot).", 2, "boss_comfort"),
        new("delegation", "Outils du boss", "Délégation des lancements", "people", "Un employé peut lancer à ta place, sans mini-jeu ; mode « déléguer par défaut ».", 2),
        new("procedures", "Outils du boss", "Procédures de publication", "rocket", "Un lancement délégué obtient la moitié du meilleur bonus de stabilité.", 2, "delegation"),
    ];

    public static DevNode Node(string id) => Nodes.First(n => n.Id == id);

    /// <summary>Points de départ : de quoi faire un premier choix tout de suite.</summary>
    public const int StartingPoints = 1;
}
