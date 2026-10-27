using System.Text.Json.Serialization;
using Core.products;
using Core.team;

namespace Core.campaign;

/// <summary>
/// Une session au terminal du fondateur. Rien n'est exécuté sur l'ordinateur du joueur : c'est un jeu de saisie
/// dont chaque mot validé apporte un peu de travail à la vraie tâche du projet. Elle n'est pas sauvegardée : le travail
/// déjà validé est dans les tâches, et une session interrompue ne laisse aucun minuteur derrière elle.
/// </summary>
public class TerminalSession
{
    public int ProductId { get; init; }

    /// <summary>Le mot à taper.</summary>
    public string Word { get; internal set; } = "";

    public int WordsTyped { get; internal set; }
    public int Errors { get; internal set; }
    public double Points { get; internal set; }

    /// <summary>Ce sur quoi le dernier mot a travaillé (« Comptes · design »).</summary>
    public string Stage { get; internal set; } = "";

    /// <summary>Rendement du dernier mot (1 = vitesse normale), pour l'afficher.</summary>
    public double LastYield { get; internal set; } = 1;

    public bool Finished { get; internal set; }

    /// <summary>Pourquoi la session s'est arrêtée toute seule (plus rien à faire, session complète).</summary>
    public string EndReason { get; internal set; } = "";

    /// <summary>Mots de la session (plus avec les macros).</summary>
    public int Length { get; init; } = Tuning.Terminal.WordsPerSession;

    public int WordsLeft => Math.Max(0, Length - WordsTyped);

    internal SimRandom Words { get; init; } = new(1);
}

/// <summary>Le résultat d'un mot validé : juste ou non, les points apportés, et la tâche concernée.</summary>
public record WordResult(bool Correct, double Points, string Stage);

public partial class Campaign
{
    /// <summary>La session ouverte, s'il y en a une (au plus une à la fois).</summary>
    [JsonIgnore]
    public TerminalSession? Terminal { get; private set; }

    /// <summary>Nombre de sessions ouvertes depuis le début : sert à tirer les mots sans toucher au hasard de la simulation.</summary>
    public int TerminalSessions { get; set; }

    /// <summary>Le fondateur peut-il travailler au terminal sur ce projet maintenant ? Sinon, pourquoi.</summary>
    public (bool Ok, string Reason) CanOpenTerminal(int productId)
    {
        if (Status != CampaignStatus.Running)
            return (false, "La partie est terminée.");
        if (Terminal != null)
            return (false, "Le terminal est déjà ouvert.");
        var founder = Founder;
        if (OnVacation(founder))
            return (false, "Tu es en congé.");
        if (founder.TrainingDiscipline != null)
            return (false, "Tu es en formation.");
        int hour = GameTime.Hour(Minute);
        if (hour < Tuning.Terminal.FirstHour || hour >= Tuning.Terminal.LastHour)
            return (false, $"Il est tard : le terminal rouvre à {Tuning.Terminal.FirstHour} h.");
        var post = Premises.SeatOf(founder.Id);
        if (post == null || space.Layout.Check(Premises, post) != space.WorkstationIssue.None)
            return (false, "Il te faut un poste complet (bureau, chaise, ordinateur).");
        var product = FindProduct(productId);
        if (product == null)
            return (false, "Projet inconnu.");
        if (PickWork(founder, product) == null && MaintenanceLeft(product) <= 0)
            return (false, product.Queue.Count == 0
                ? "Rien en file sur ce projet : ajoute ou améliore un composant."
                : "Les tâches de ce projet attendent la fin d'une autre étape.");
        return (true, "");
    }

    /// <summary>
    /// Ce qu'il reste à corriger sur un projet en ligne (points de maintenance) : quand rien n'est en file, le terminal
    /// sert à corriger les bugs. Borné : une fois tout corrigé pour la journée, les mots ne rapportent plus rien.
    /// </summary>
    public static double MaintenanceLeft(Product product) =>
        product.IsLive ? Math.Max(0, product.Bugs * 3 + product.MaintenanceDebt - product.MaintenanceDone) : 0;

    public CommandResult OpenTerminal(int productId)
    {
        var (ok, reason) = CanOpenTerminal(productId);
        if (!ok)
            return CommandResult.Fail(reason);
        TerminalSessions++;
        Terminal = new TerminalSession
        {
            ProductId = productId,
            Words = new SimRandom(Seed ^ (ulong)TerminalSessions * 0x9E3779B97F4A7C15UL),
            Length = IsUnlocked("boss_macros") ? Tuning.Terminal.WordsPerSessionWithMacros : Tuning.Terminal.WordsPerSession,
        };
        Founder.CurrentWorkItemId = null;
        NextWord(Founder);
        return CommandResult.Success();
    }

    public void CloseTerminal()
    {
        if (Terminal == null)
            return;
        Founder.CurrentWorkItemId = null;
        Terminal = null;
    }

    /// <summary>
    /// Valide un mot. « seconds » : temps passé depuis que le mot est apparu (mesuré par l'interface en temps réel).
    /// Juste : une part bornée de travail va à la prochaine tâche prête du projet ; une faute ne coûte rien.
    /// </summary>
    public WordResult SubmitWord(string typed, double seconds)
    {
        var session = Terminal;
        if (session == null || session.Finished)
            return new WordResult(false, 0, "");
        if (!string.Equals(typed.Trim(), session.Word, StringComparison.Ordinal))
        {
            session.Errors++;
            return new WordResult(false, 0, session.Stage);
        }

        var founder = Founder;
        var product = FindProduct(session.ProductId);
        var item = product == null ? null : PickWork(founder, product);
        if (product == null || item == null && MaintenanceLeft(product) <= 0)
        {
            Finish(session, product == null ? "Le projet n'existe plus." : "Plus rien de prêt sur ce projet pour l'instant.");
            return new WordResult(true, 0, session.Stage);
        }
        var discipline = item?.Discipline ?? Discipline.Code;

        double chars = session.Word.Length / Math.Max(0.2, seconds);
        double speed = chars <= Tuning.Terminal.SlowCharsPerSecond ? Tuning.Terminal.SlowFactor
            : chars >= Tuning.Terminal.FastCharsPerSecond ? Tuning.Terminal.FastFactor
            : Tuning.Terminal.SlowFactor + (Tuning.Terminal.FastFactor - Tuning.Terminal.SlowFactor)
              * (chars - Tuning.Terminal.SlowCharsPerSecond) / (Tuning.Terminal.FastCharsPerSecond - Tuning.Terminal.SlowCharsPerSecond);
        double skill = 0.4 + founder.Skill(discipline) * 0.012;
        double energy = founder.Energy >= 40 ? 1 : 0.6 + founder.Energy / 100;
        double tools = (IsUnlocked("ai_tools") ? 1.1 : 1) * (IsUnlocked("boss_speed") ? 1.15 : 1)
                       * (discipline == Discipline.Test && IsUnlocked("boss_tests") ? 1.3 : 1);
        double points = Tuning.Terminal.PointsPerWord * skill * StationOutput(founder) * speed * energy * tools;

        if (item != null)
        {
            points = Math.Min(item.Remaining, points);
            item.Progress += points;
            item.QualityPoints += points * WorkQuality(founder, item.Discipline);
            session.Stage = StageName(item);
            if (item.Done)
            {
                founder.CurrentWorkItemId = null;
                OnWorkItemDone(product, item);
            }
        }
        else
        {
            // Maintenance : comme celle des employés (Campaign.Time.cs, DoUpkeep), appliquée à minuit
            double effective = Math.Min(MaintenanceLeft(product), points * WorkQuality(founder, discipline) / 0.6);
            product.MaintenanceDone += effective;
            points = effective * 0.6 / Math.Max(0.1, WorkQuality(founder, discipline));
            session.Stage = "Maintenance · corrections";
        }
        founder.PointsThisWeek += points;
        founder.Energy = Math.Max(0, founder.Energy - Tuning.Terminal.EnergyPerWord * (IsUnlocked("boss_comfort") ? 0.6 : 1));
        founder.Status = $"Au terminal : {product.Name}";
        session.Points += points;
        session.WordsTyped++;
        session.LastYield = speed * energy;

        if (session.WordsLeft == 0)
            Finish(session, "Session complète.");
        else if (PickWork(founder, product) == null && MaintenanceLeft(product) <= 0)
            Finish(session, product.Queue.Count == 0 ? "Plus rien à faire sur ce projet pour l'instant." : "La suite attend la fin d'une autre étape.");
        else
            NextWord(founder);
        return new WordResult(true, points, session.Stage);
    }

    private static string StageName(work.WorkItem item) => $"{item.Title} · {Person.DisciplineName(item.Discipline).ToLowerInvariant()}";

    private static void Finish(TerminalSession session, string reason)
    {
        session.Finished = true;
        session.EndReason = reason;
    }

    /// <summary>Le mot suivant, dans le vocabulaire de l'étape en cours (design, code, tests, support).</summary>
    private void NextWord(Person founder)
    {
        var session = Terminal!;
        var product = FindProduct(session.ProductId);
        var item = product == null ? null : PickWork(founder, product);
        session.Stage = item != null ? StageName(item) : "Maintenance · corrections";
        var words = TerminalWords.For(item?.Discipline ?? Discipline.Test);
        string next;
        do
            next = session.Words.Pick(words);
        while (next == session.Word && words.Count > 1);
        session.Word = next;
    }
}

/// <summary>Le vocabulaire du terminal : des mots courts, sans accent (pas de touche morte), lisibles sans savoir programmer.</summary>
public static class TerminalWords
{
    public static readonly IReadOnlyList<string> Design =
    [
        "maquette", "couleur", "bouton", "grille", "icone", "palette", "police", "marge", "contraste", "esquisse",
        "ecran", "menu", "onglet", "logo", "pixel", "calque", "titre", "image", "cadre", "style",
    ];

    public static readonly IReadOnlyList<string> Code =
    [
        "fonction", "boucle", "variable", "classe", "module", "tableau", "requete", "serveur", "branche", "compiler",
        "objet", "valeur", "index", "liste", "client", "route", "cache", "format", "donnees", "script",
    ];

    public static readonly IReadOnlyList<string> Test =
    [
        "verifier", "scenario", "erreur", "rapport", "valider", "limite", "essai", "mesure", "controle", "relancer",
        "trace", "resultat", "cas", "stable", "corriger", "comparer", "test", "lecture", "journal", "alerte",
    ];

    public static readonly IReadOnlyList<string> Support =
    [
        "reponse", "merci", "question", "aide", "bonjour", "guide", "astuce", "lien", "compte", "suivi",
    ];

    public static IReadOnlyList<string> For(Discipline discipline) => discipline switch
    {
        Discipline.Design => Design,
        Discipline.Test => Test,
        Discipline.Support => Support,
        _ => Code
    };
}
