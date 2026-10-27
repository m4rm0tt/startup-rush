using Core.products;

namespace Core.campaign;

/// <summary>
/// Un mini-jeu de lancement : une règle en une phrase, une solution, et un score de 0 à 1. Il est tiré d'un hasard séparé
/// (graine de la campagne et projet) : il ne touche pas au hasard de la simulation et se rejoue à l'identique.
/// </summary>
public record LaunchChallenge(string Kind, string Rule, IReadOnlyList<string> Items, IReadOnlyList<int> Answer)
{
    /// <summary>« deploy » : remettre les étapes dans l'ordre. « review » : repérer les tests en anomalie. « calibrate » : régler.</summary>
    public bool Ordering => Kind == "deploy";

    public bool Calibration => Kind == "calibrate";

    /// <summary>
    /// Score d'une réponse. Déploiement : part des étapes à la bonne place. Recette : anomalies trouvées moins fausses alertes,
    /// rapporté au nombre d'anomalies. Toujours entre 0 et 1.
    /// </summary>
    public double Score(IReadOnlyList<int> answer)
    {
        if (Ordering)
        {
            int right = Enumerable.Range(0, Math.Min(answer.Count, Answer.Count)).Count(i => answer[i] == Answer[i]);
            return (double)right / Answer.Count;
        }
        if (Calibration)
        {
            // Pile sur la cible : plein score ; à un cran : la moitié ; au-delà : rien
            double sum = Enumerable.Range(0, Math.Min(answer.Count, Answer.Count))
                .Sum(i => Math.Abs(answer[i] - Answer[i]) switch { 0 => 1.0, 1 => 0.5, _ => 0 });
            return sum / Answer.Count;
        }
        var picked = answer.ToHashSet();
        int found = picked.Count(Answer.Contains), wrong = picked.Count - found;
        return Math.Clamp((found - wrong) / (double)Answer.Count, 0, 1);
    }
}

/// <summary>
/// Le lancement, grosse action du projet : un mini-jeu facultatif (bonus plafonné de stabilité à la sortie), ou le résultat
/// de base sans jouer. Avec la délégation (arbre « Outils du boss »), un employé lance à ta place, et le mode « déléguer par
/// défaut » sort tout seul les projets prêts. Les mises à jour ne demandent jamais de mini-jeu.
/// </summary>
public partial class Campaign
{
    /// <summary>Part des bugs évités au lancement pour un score de 1 (le bonus le plus fort possible).</summary>
    public const double LaunchBugReduction = 0.5;

    /// <summary>Les projets prêts sortent tout seuls, lancés par un employé (si la délégation est débloquée).</summary>
    public bool DelegateLaunches { get; set; }

    /// <summary>
    /// « Me proposer les grosses actions » : un projet prêt est annoncé par une bulle qui ouvre le lancement. Sinon, il attend
    /// discrètement dans le portefeuille (le journal le note). Dans les deux cas, plusieurs projets prêts font une seule annonce.
    /// </summary>
    public bool ProposeBigActions { get; set; } = true;

    /// <summary>Clé de l'annonce regroupée des projets prêts à sortir.</summary>
    public const string ReadyKey = "ready";

    /// <summary>Les projets prêts à sortir : file vide et assez de composants terminés.</summary>
    public IEnumerable<Product> ReadyToLaunch => Products.Where(p => !p.IsLive && p.Queue.Count == 0 && CanLaunch(p));

    /// <summary>
    /// Annonce les projets prêts, en une seule sollicitation : la même annonce se met à jour au lieu de s'empiler. Rien
    /// n'est annoncé quand un employé va les lancer demain matin (déléguer par défaut, quelqu'un de présent). « quiet » :
    /// seulement remettre à jour l'annonce existante (après un lancement), sans en créer ni la marquer non lue.
    /// </summary>
    private void ProposeLaunches(bool quiet = false)
    {
        var ready = ReadyToLaunch.ToList();
        if (ready.Count == 0)
        {
            Resolve(ReadyKey);
            return;
        }
        if (DelegateLaunches && IsUnlocked("delegation") && ready.All(p => CanDelegateLaunch(p).Ok))
            return;
        string title = ready.Count == 1 ? $"{ready[0].Name} est prêt à sortir" : $"{ready.Count} projets prêts à sortir";
        string detail = ready.Count == 1 ? "Lance-le quand tu veux : un court mini-jeu peut réduire les bugs de la sortie."
            : string.Join(", ", ready.Select(p => p.Name)) + " : lance-les quand tu veux.";
        var existing = Notices.LastOrDefault(n => n.Key == ReadyKey && !n.Resolved);
        if (existing != null)
        {
            // Une seule annonce : on la met à jour sans relancer de bulle
            existing.Title = title;
            existing.Detail = detail;
            existing.Target = ready.Count == 1 ? $"launch:{ready[0].Id}" : "projects";
            if (!quiet)
            {
                existing.Minute = Minute;
                existing.Read = false;
            }
            return;
        }
        if (quiet)
            return;
        Notify(ProposeBigActions ? Severity.Good : Severity.Info, "projet", title, detail,
            ready.Count == 1 ? $"launch:{ready[0].Id}" : "projects", key: ReadyKey);
    }

    private static readonly string[] DeploySteps =
        ["Sauvegarder les données", "Migrer la base", "Mettre en ligne", "Vérifier le service", "Annoncer la sortie"];

    public LaunchChallenge Challenge(int productId)
    {
        var product = FindProduct(productId) ?? throw new ArgumentException("Projet inconnu.");
        var random = new SimRandom(Seed ^ ((ulong)productId * 0xD1B54A32D192ED03UL) ^ 0x5EED);
        if (product.IsHardware)
        {
            // Calibration de la première série : trois réglages à amener dans leur zone (0 à 10)
            string[] knobs = ["Couple de vissage", "Température de soudure", "Tension de test"];
            var targets = knobs.Select(_ => random.Next(2, 9)).ToList();
            return new LaunchChallenge("calibrate", "Amène chaque réglage sur sa valeur cible (zone verte).", knobs, targets);
        }
        if (product.ArchetypeId == "game")
        {
            // Huit comptes rendus de test ; trois ne donnent pas le résultat attendu
            string[] checks = ["Sauvegarde", "Niveau 1", "Son", "Menu pause", "Score", "Chargement", "Succès", "Options"];
            var anomalies = new HashSet<int>();
            while (anomalies.Count < 3)
                anomalies.Add(random.Next(0, checks.Length));
            var items = checks.Select((c, i) =>
            {
                int expected = random.Next(2, 9);
                int got = anomalies.Contains(i) ? expected + random.Next(1, 4) : expected;
                return $"{c} : attendu {expected}, obtenu {got}";
            }).ToList();
            return new LaunchChallenge("review", "Repère les tests dont le résultat ne correspond pas à l'attendu.", items, anomalies.OrderBy(i => i).ToList());
        }
        var order = Enumerable.Range(0, DeploySteps.Length).ToList();
        var shuffled = order.ToList();
        do
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
        while (shuffled.SequenceEqual(order));
        // Les étapes sont présentées mélangées ; la réponse attendue est l'ordre des indices présentés
        var presented = shuffled.Select(i => DeploySteps[i]).ToList();
        var answer = order.Select(step => shuffled.IndexOf(step)).ToList();
        return new LaunchChallenge("deploy", "Remets les étapes du déploiement dans l'ordre.", presented, answer);
    }

    /// <summary>Lance après le mini-jeu (ou sans, score 0 : résultat de base).</summary>
    public CommandResult LaunchWith(int productId, double score)
    {
        var product = FindProduct(productId);
        if (product == null || !CanLaunch(product))
            return Launch(productId);
        double bonus = Math.Clamp(score, 0, 1);
        double bugs = product.Bugs;
        var result = Launch(productId);
        if (result.Ok && product.IsHardware)
        {
            // Matériel : le réglage profite à la première série (moins de défauts), pas aux bugs du logiciel embarqué
            product.FirstSeriesLeft = Tuning.Hardware.FirstSeries;
            product.FirstSeriesScore = bonus;
        }
        else if (result.Ok && bonus > 0)
            product.Bugs = Math.Round(bugs * (1 - LaunchBugReduction * bonus), 1);
        return result;
    }

    /// <summary>Qui peut lancer à ta place : un employé présent aujourd'hui (pas en congé ni en formation).</summary>
    private team.Person? Delegate() =>
        Staff.Where(p => !OnVacation(p) && p.TrainingDiscipline == null && Minute >= p.StartsAt).OrderByDescending(p => p.Skill(team.Discipline.Test)).FirstOrDefault();

    public (bool Ok, string Reason) CanDelegateLaunch(Product product)
    {
        if (!IsUnlocked("delegation"))
            return (false, "À débloquer dans Développement : Délégation des lancements.");
        if (!CanLaunch(product))
            return (false, "Le projet n'est pas prêt.");
        if (Delegate() == null)
            return (false, "Il faut un employé présent pour lancer à ta place.");
        return (true, "");
    }

    /// <summary>Un employé lance sans mini-jeu : résultat de base, ou la moitié du meilleur bonus avec les procédures de publication.</summary>
    public CommandResult DelegateLaunch(int productId)
    {
        var product = FindProduct(productId);
        if (product == null)
            return CommandResult.Fail("Projet inconnu.");
        var (ok, reason) = CanDelegateLaunch(product);
        if (!ok)
            return CommandResult.Fail(reason);
        var who = Delegate()!;
        var result = LaunchWith(productId, IsUnlocked("procedures") ? 0.5 : 0);
        if (result.Ok)
            Notify(Severity.Info, "projet", $"{who.Name} a lancé {product.Name}", IsUnlocked("procedures") ? "Avec les procédures de publication." : "Lancement standard.", $"product:{product.Id}");
        return result;
    }

    /// <summary>Automatisation avancée : l'équipe prépare seule les petites mises à jour (réglable).</summary>
    public bool AutoImprove { get; set; } = true;

    /// <summary>
    /// Chaque lundi, avec l'Automatisation avancée : sur chaque projet en ligne dont la file est vide, l'équipe met en file
    /// la petite amélioration que demandent les avis (sinon le besoin le plus mal couvert). Il faut au moins un employé :
    /// c'est du travail payé de l'équipe, pas du fondateur. Une seule annonce regroupée.
    /// </summary>
    private void AutoImproveProjects()
    {
        if (!AutoImprove || !IsUnlocked("automation") || !Staff.Any())
            return;
        var queued = new List<string>();
        foreach (var product in Products.Where(p => p.IsLive && p.Queue.Count == 0).ToList())
        {
            string? component = Reviews.Trends(product, Minute, IsUnlocked).FirstOrDefault(t => !t.Positive && t.Link.StartsWith("component:"))?.Link["component:".Length..]
                                ?? Design.WeakestNeed(product, IsUnlocked).Fix?.Id;
            if (component != null && QueueComponent(product.Id, component).Ok)
                queued.Add($"{product.Name} : {ProductCatalog.Component(component).Name}");
        }
        if (queued.Count > 0)
            Notify(Severity.Info, "projet", queued.Count == 1 ? "Une mise à jour préparée par l'équipe" : $"{queued.Count} mises à jour préparées par l'équipe",
                string.Join(" · ", queued), "projects", key: "autoimprove");
    }

    /// <summary>Chaque matin : en mode « déléguer par défaut », les projets prêts (file vide) sortent tout seuls.</summary>
    private void AutoLaunch()
    {
        if (!DelegateLaunches || !IsUnlocked("delegation"))
            return;
        foreach (var product in ReadyToLaunch.ToList())
            DelegateLaunch(product.Id);
        // Personne de présent pour lancer : le joueur en est prévenu (une seule annonce)
        ProposeLaunches();
    }
}
