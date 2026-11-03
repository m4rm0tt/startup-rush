namespace Core.campaign;

/// <summary>
/// Une étape du tutoriel : une consigne courte, la touche ou le menu concerné, ce que l'interface met en évidence, et la
/// condition (lue dans l'état réel de la partie ou dans les gestes signalés par l'interface) qui la valide.
/// </summary>
public record TutorialStep(string Id, string Title, string Text, string? Key, string Target, Func<Campaign, bool> Done);

/// <summary>
/// Le tutoriel : une suite d'actions à faire soi-même, validées par ce qui se passe vraiment dans la partie (un projet créé,
/// des mots tapés, un lancement), jamais par un bouton « Suivant ». Une étape déjà faite avant d'y arriver est validée
/// d'office. Il ne met jamais le jeu en pause et se passe d'un clic. Sauvegardé avec la campagne.
/// </summary>
public partial class Campaign
{
    /// <summary>Le tutoriel est en cours (une nouvelle partie le propose ; les anciennes sauvegardes ne l'ont pas).</summary>
    public bool TutorialOn { get; set; }

    /// <summary>Étape en cours (indice dans <see cref="TutorialSteps"/>).</summary>
    public int TutorialIndex { get; set; }

    /// <summary>Gestes signalés par l'interface (« camera », « select:founder », « speed », « open:finances »...).</summary>
    public HashSet<string> TutorialEvents { get; set; } = new();

    /// <summary>Mots justes tapés au terminal depuis le début de la partie.</summary>
    public int WordsTypedTotal { get; set; }

    /// <summary>Point de développement offert à la fin du tutoriel.</summary>
    public const int TutorialReward = 1;

    public static readonly IReadOnlyList<TutorialStep> TutorialSteps =
    [
        new("view", "Ta chambre, ton bureau", "Déplace la vue : clic droit maintenu ou WASD. La molette zoome.", "WASD", "scene",
            c => c.TutorialEvents.Contains("camera")),
        new("founder", "C'est toi", "Clique sur ton personnage, assis à son bureau : sa fiche montre son énergie et son moral.", null, "founder",
            c => c.TutorialEvents.Contains("select:founder")),
        new("project", "Ton premier projet", "Ouvre Projets et crée ton premier projet : un type, un public, c'est tout.", "P", "tool:Projets",
            c => c.Products.Count > 0),
        new("terminal", "Au travail !", "Appuie sur Entrée : tape les mots qui s'affichent. Chaque mot juste fait avancer le projet.", "Entrée", "work",
            c => c.WordsTypedTotal >= 8 || c.Products.Any(p => p.IsLive)),
        new("streak", "Garde le rythme", "Enchaîne les mots sans faute : la série multiplie ce que rapporte chaque mot. Termine les composants de départ.", "Entrée", "work",
            c => c.ReadyToLaunch.Any() || c.Products.Any(p => p.IsLive)),
        new("launch", "Prêt à sortir", "Ouvre ton projet dans Projets et clique « Lancer ! ».", "P", "tool:Projets",
            c => c.Products.Any(p => p.IsLive)),
        new("time", "Le temps file", "Espace met en pause, 1 à 4 règlent la vitesse. Passe en vitesse 2 pour voir arriver les utilisateurs.", "1-4", "speed",
            c => c.TutorialEvents.Contains("speed")),
        new("reviews", "Écoute tes utilisateurs", "Ouvre ton projet, onglet Avis : ce qui leur plaît, ce qui manque, et le composant qui y répond.", "P", "tool:Projets",
            c => c.TutorialEvents.Contains("tab:reviews")),
        new("update", "Prépare la version 2", "Dans l'onglet Composants, ajoute ou améliore un composant : il entre dans la prochaine version.", "P", "tool:Projets",
            c => c.Products.Any(p => p.IsLive && (p.Queue.Count > 0 || p.Staged.Count > 0 || p.Version >= 2))),
        new("money", "L'argent", $"En haut à droite : ton argent et ce qu'il fait chaque mois. Les charges (salaires, loyer, serveurs) partent chaque mois, " +
            $"les revenus viennent de tes utilisateurs payants. Une facture impayée {Tuning.Finance.OverdueDaysBeforeNotice} jours devient une mise en demeure : " +
            $"sans paiement dans les {Tuning.Finance.NoticeDays} jours, c'est la faillite. Ouvre Finances pour voir le détail.", "F", "tool:Finances",
            c => c.TutorialEvents.Contains("open:finances")),
        new("build", "Chez toi", "Aménagement : une plante ou une lampe remonte le moral. Une corbeille évite les déchets par terre.", "B", "tool:Aménagement",
            c => c.TutorialEvents.Contains("open:build")),
    ];

    /// <summary>L'étape en cours, ou null quand le tutoriel est fini ou désactivé.</summary>
    public TutorialStep? CurrentTutorialStep => TutorialOn && TutorialIndex < TutorialSteps.Count ? TutorialSteps[TutorialIndex] : null;

    /// <summary>L'interface signale un geste du joueur ; le tutoriel avance si c'est ce qu'il attendait.</summary>
    public void TutorialEvent(string name)
    {
        if (!TutorialOn)
            return;
        TutorialEvents.Add(name);
        CheckTutorial();
    }

    /// <summary>Valide l'étape en cours (et les suivantes déjà faites). Renvoie true si le tutoriel a avancé.</summary>
    public bool CheckTutorial()
    {
        bool moved = false;
        while (CurrentTutorialStep is { } step && step.Done(this))
        {
            TutorialIndex++;
            moved = true;
            if (TutorialIndex == TutorialSteps.Count)
                FinishTutorial(rewarded: true);
        }
        return moved;
    }

    /// <summary>Passer le tutoriel : il s'arrête (sans récompense) et ne revient pas.</summary>
    public void SkipTutorial()
    {
        if (TutorialOn)
            FinishTutorial(rewarded: false);
    }

    private void FinishTutorial(bool rewarded)
    {
        TutorialOn = false;
        TutorialIndex = TutorialSteps.Count;
        if (!rewarded)
            return;
        DevPoints += TutorialReward;
        Notify(Severity.Good, "progression", "Tutoriel terminé", $"+{TutorialReward} point de développement : dépense-le dans Développement (D).", "tree");
    }
}
