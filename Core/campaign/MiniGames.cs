using Core.products;

namespace Core.campaign;

/// <summary>
/// Les mini-jeux de terminal des grosses actions (lancement, publication d'une version). Chacun évoque un vrai geste de
/// l'informatique, mais se résout avec la seule règle affichée, sans rien savoir d'avance :
/// <list type="bullet">
/// <item>« logs » : lire un journal et repérer les lignes qui bloquent (seulement celles marquées ERREUR, pas « 0 erreur ») ;</item>
/// <item>« firewall » : laisser passer ou bloquer chaque requête selon deux règles ;</item>
/// <item>« merge » : résoudre des conflits en gardant, pour chaque ligne, la version qui respecte la règle ;</item>
/// <item>« command » : taper la commande de mise en ligne, sans faute ;</item>
/// <item>et ceux d'avant : remettre un déploiement dans l'ordre, repérer les tests en anomalie, calibrer une série.</item>
/// </list>
/// Tirés d'un hasard séparé (graine de la partie, projet, version) : ils ne touchent pas au hasard de la simulation. La difficulté
/// monte avec l'expérience (nombre de sorties déjà faites dans la partie) : plus de lignes, des pièges plus fins.
/// </summary>
public static class MiniGames
{
    public static readonly string[] SoftwareKinds = ["deploy", "logs", "firewall", "merge", "command"];

    /// <summary>Niveau de difficulté (0 à 3) selon les sorties déjà faites dans la partie.</summary>
    public static int Difficulty(int releases) => Math.Clamp(releases / 3, 0, 3);

    public static string Name(string kind) => kind switch
    {
        "deploy" => "Déploiement",
        "review" => "Recette",
        "calibrate" => "Calibration",
        "logs" => "Journal",
        "firewall" => "Pare-feu",
        "merge" => "Fusion",
        "command" => "Commande",
        "match" => "Évaluation",
        "signals" => "Signaux",
        _ => kind,
    };

    /// <summary>Le journal d'une mise en ligne : quelques lignes ERREUR à trouver parmi des lignes OK, INFO et ATTENTION.</summary>
    public static LaunchChallenge Logs(SimRandom random, int difficulty)
    {
        string[] ok = ["[OK] code récupéré", "[OK] paquets installés", "[OK] tests unitaires : 214 passés", "[OK] image construite",
            "[OK] base de données joignable", "[OK] certificat valide", "[OK] cache vidé", "[OK] service redémarré"];
        string[] info = ["[INFO] 0 erreur trouvée", "[INFO] erreur 404 corrigée hier", "[INFO] journal des erreurs archivé", "[INFO] 3 avertissements ignorés",
            "[INFO] version précédente gardée", "[INFO] durée : 42 s"];
        string[] warn = ["[ATTENTION] disque à 81 %", "[ATTENTION] dépendance ancienne", "[ATTENTION] réponse lente (900 ms)"];
        string[] errors = ["[ERREUR] connexion à la base refusée", "[ERREUR] fichier de configuration introuvable", "[ERREUR] port 443 déjà utilisé",
            "[ERREUR] mot de passe expiré", "[ERREUR] mémoire épuisée", "[ERREUR] test « paiement » en échec"];
        int count = 7 + difficulty * 2, wanted = 2 + Math.Min(2, difficulty);
        var lines = new List<(string Text, bool Error)>();
        foreach (string e in Shuffle(random, errors).Take(wanted))
            lines.Add((e, true));
        // Les pièges (« 0 erreur trouvée ») arrivent avec l'expérience
        var others = Shuffle(random, ok.Concat(difficulty > 0 ? info : info.Skip(3)).Concat(difficulty > 1 ? warn : [])).Take(count - wanted);
        lines.AddRange(others.Select(o => (o, false)));
        lines = Shuffle(random, lines).ToList();
        return new LaunchChallenge("logs", "Coche les lignes qui bloquent la mise en ligne : celles qui commencent par [ERREUR].",
            lines.Select(l => l.Text).ToList(), lines.Select((l, i) => (l, i)).Where(x => x.l.Error).Select(x => x.i).ToList());
    }

    /// <summary>Un pare-feu : chaque requête passe ou est bloquée selon deux règles simples (une adresse, une page).</summary>
    public static LaunchChallenge Firewall(SimRandom random, int difficulty)
    {
        string blockedNet = random.Pick(new[] { "10.0", "172.16", "203.0" });
        string blockedPath = random.Pick(new[] { "/admin", "/backup", "/config" });
        string[] nets = ["192.168", "10.0", "172.16", "203.0", "85.12"];
        string[] paths = ["/login", "/admin", "/api", "/backup", "/shop", "/config", "/aide"];
        int count = 6 + difficulty * 2;
        var items = new List<string>();
        var answer = new List<int>();
        for (int i = 0; i < count; i++)
        {
            string net = random.Pick(nets), path = random.Pick(paths);
            items.Add($"{net}.{random.Next(1, 255)}.{random.Next(1, 255)}  →  {path}");
            answer.Add(net == blockedNet || path == blockedPath ? 1 : 0);
        }
        return new LaunchChallenge("firewall", $"Bloque ce qui vient de {blockedNet}.x.x ou qui vise {blockedPath}. Laisse passer le reste.", items, answer)
        {
            Options = ["Passe", "Bloqué"],
        };
    }

    /// <summary>Des conflits de fusion : pour chaque réglage, deux versions ; on garde celle qui respecte la règle.</summary>
    public static LaunchChallenge Merge(SimRandom random, int difficulty)
    {
        (string Key, Func<int, string> Value)[] settings =
        [
            ("timeout", v => $"{v} s"), ("workers", v => $"{v}"), ("cache", v => $"{v} Mo"), ("retries", v => $"{v}"),
            ("port", v => $"{8000 + v}"), ("taille_max", v => $"{v} Mo"), ("logs_jours", v => $"{v}"),
        ];
        bool keepHigher = random.Chance(0.5);
        int count = 4 + difficulty;
        var items = new List<string>();
        var answer = new List<int>();
        foreach (var (key, value) in Shuffle(random, settings).Take(count))
        {
            int a = random.Next(2, 60), b = a + random.Next(1, 20) * (random.Chance(0.5) ? 1 : -1);
            if (b < 1)
                b = a + 5;
            items.Add($"{key} :  A = {value(a)}   |   B = {value(b)}");
            bool aWins = keepHigher ? a > b : a < b;
            answer.Add(aWins ? 0 : 1);
        }
        return new LaunchChallenge("merge", keepHigher
                ? "Deux branches modifient les mêmes réglages : pour chaque ligne, garde la plus grande valeur."
                : "Deux branches modifient les mêmes réglages : pour chaque ligne, garde la plus petite valeur.", items, answer)
        {
            Options = ["Garder A", "Garder B"],
        };
    }

    /// <summary>La commande de mise en ligne à taper sans faute (plus longue avec l'expérience).</summary>
    public static LaunchChallenge Command(SimRandom random, Product product, int version, int difficulty)
    {
        // Sans accent (pas de touche morte), lettres et chiffres seulement
        string plain = new string(product.Name.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
        string slug = new string(plain.Where(c => c is >= 'a' and <= 'z' or >= '0' and <= '9').ToArray());
        if (slug.Length == 0)
            slug = "projet";
        string[] env = ["prod", "live", "public"];
        string command = difficulty switch
        {
            0 => $"publier {slug}",
            1 => $"publier {slug} v{version}",
            2 => $"publier {slug} v{version} --{random.Pick(env)}",
            _ => $"publier {slug} v{version} --{random.Pick(env)} --verifier",
        };
        return new LaunchChallenge("command", "Tape la commande de mise en ligne exactement comme elle est écrite, puis valide.", [command], []);
    }

    /// <summary>Note d'une commande tapée : 1 si elle est exacte, sinon la part de caractères justes (distance d'édition).</summary>
    public static double ScoreText(string expected, string typed)
    {
        typed = typed.Trim();
        if (typed == expected)
            return 1;
        int distance = Levenshtein(expected, typed);
        return Math.Clamp(1 - distance / (double)Math.Max(1, expected.Length) * 2, 0, 1);
    }

    private static int Levenshtein(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++)
            d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++)
            d[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
        return d[a.Length, b.Length];
    }

    private static IEnumerable<T> Shuffle<T>(SimRandom random, IEnumerable<T> source)
    {
        var list = source.ToList();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }
}
