using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UI;

/// <summary>
/// Textes de l'interface par langue (UI/Lang/&lt;langue&gt;.json, embarqués) : une clé → un texte, avec arguments
/// (string.Format, format des nombres du jeu). Une clé absente de la langue choisie retombe sur le français, puis sur la clé
/// elle-même (visible, donc repérable). Le français reste la langue de référence et par défaut.
/// Ce qui est déjà écrit dans une sauvegarde (noms choisis par le joueur, journal et notifications déjà générés, résumé
/// d'une sauvegarde) reste dans la langue où il a été écrit : seul ce qui est affiché ensuite change de langue.
/// </summary>
public static class Loc
{
    public static readonly string[] Languages = ["fr", "en"];
    public const string Reference = "fr";

    private static readonly Dictionary<string, Dictionary<string, string>> Tables = new();

    private static string _language = Reference;

    /// <summary>
    /// Active le relevé des textes d'apparence française que la langue choisie n'a pas su traduire (tests de couverture,
    /// <see cref="Misses"/>). Désactivé en jeu : aucun coût.
    /// </summary>
    public static bool RecordMisses { get; set; }

    /// <summary>Textes français affichés sans traduction depuis <see cref="RecordMisses"/> (texte → nombre d'affichages).</summary>
    public static readonly ConcurrentDictionary<string, int> Misses = new();

    /// <summary>Langue affichée (« fr » ou « en ») ; une valeur inconnue revient au français.</summary>
    public static string Language
    {
        get => _language;
        set
        {
            string next = Languages.Contains(value) ? value : Reference;
            if (next != _language)
                lock (Phrases)
                    TranslationCache.Clear();
            _language = next;
        }
    }

    /// <summary>La table d'une langue (chargée une fois depuis les ressources embarquées).</summary>
    public static IReadOnlyDictionary<string, string> Table(string language)
    {
        if (Tables.TryGetValue(language, out var table))
            return table;
        table = new Dictionary<string, string>();
        using var stream = typeof(Loc).Assembly.GetManifestResourceStream($"UI.Lang.{language}.json");
        if (stream != null)
            table = JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? table;
        Tables[language] = table;
        return table;
    }

    /// <summary>Le texte de la clé dans la langue choisie (repli : français, puis la clé), formaté avec ses arguments.</summary>
    public static string T(string key, params object?[] args)
    {
        string text = Table(Language).TryGetValue(key, out var found) ? found
            : Table(Reference).TryGetValue(key, out var french) ? french
            : key;
        return args.Length == 0 ? text : string.Format(Core.GameCulture.Culture, text, args);
    }

    /// <summary>Le texte français de la clé (les icônes des boutons sont choisies d'après le libellé français).</summary>
    public static string French(string key) => Table(Reference).TryGetValue(key, out var french) ? french : key;

    /// <summary>Le message d'erreur du nom d'entreprise dans la langue choisie (le moteur le donne en français).</summary>
    public static string? CompanyNameError(string? name)
    {
        if (Core.campaign.Campaign.CompanyNameError(name) == null)
            return null;
        string trimmed = (name ?? "").Trim();
        return trimmed.Length == 0 ? T("error.company.empty")
            : trimmed.Length < 2 ? T("error.company.short")
            : T("error.company.long", Core.campaign.Campaign.CompanyNameMax);
    }

    // ---- Traduction des phrases (panneaux, terminal, tutoriel, messages du moteur) ----------------------------------------
    //
    // Le moteur et les panneaux écrivent leurs phrases en français, avec des nombres et des noms insérés au milieu
    // (« Léa a terminé Site vitrine »). Plutôt que de remplacer chaque phrase du code par une clé, la phrase française
    // *telle qu'elle s'affiche* sert de clé : UI/Lang/phrases/<langue>/*.json associe « phrase française » → « phrase
    // traduite », avec {0}, {1}… pour les morceaux variables (« {0} a terminé {1} » → « {0} finished {1} »). Les textes déjà
    // écrits dans une sauvegarde (journal, notifications) sont donc traduits eux aussi à l'affichage. La traduction se fait
    // à un seul endroit (PixelText : toute zone de texte, toute infobulle, tout texte dessiné).

    private sealed record Pattern(Regex Regex, string Target, int Holes, int Literal);

    private static readonly object Phrases = new();
    private static readonly Dictionary<string, Dictionary<string, string>> Exact = new();
    private static readonly Dictionary<string, List<Pattern>> Patterns = new();
    private static readonly Dictionary<string, string> TranslationCache = new();
    private static readonly Regex Hole = new(@"\{(\d+)\}", RegexOptions.Compiled);

    private static readonly Regex FrenchLook = new(
        @"[àâçéèêëîïôùûüœÀÉÈÊÂÎÔÛÇ]|\b(le|la|les|des|du|un|une|et|pour|avec|sans|dans|sur|vous|votre|vos|est|sont|pas|cette|chez|aucun|aucune|déjà|bientôt)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Vrai si le texte ressemble à du français (lettres accentuées ou mots courants) : sert à repérer l'oubli d'une traduction.</summary>
    public static bool LooksFrench(string text) => FrenchLook.IsMatch(text);

    private static void LoadPhrases(string language)
    {
        if (Exact.ContainsKey(language))
            return;
        var exact = new Dictionary<string, string>();
        var patterns = new List<Pattern>();
        string prefix = $"UI.Lang.phrases.{language}.";
        var assembly = typeof(Loc).Assembly;
        foreach (string name in assembly.GetManifestResourceNames().Where(n => n.Replace('/', '.').Replace('\\', '.').StartsWith(prefix, StringComparison.Ordinal)).OrderBy(n => n, StringComparer.Ordinal))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            var table = JsonSerializer.Deserialize<Dictionary<string, string>>(stream, new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip }) ?? new();
            foreach (var (french, target) in table)
            {
                if (!Hole.IsMatch(french))
                {
                    exact[french] = target;
                    continue;
                }
                int last = 0, literal = 0;
                var pattern = new System.Text.StringBuilder("^");
                var seen = new HashSet<int>();
                foreach (Match m in Hole.Matches(french))
                {
                    string between = french[last..m.Index];
                    literal += between.Length;
                    pattern.Append(Regex.Escape(between));
                    int n = int.Parse(m.Groups[1].Value);
                    pattern.Append(seen.Add(n) ? $"(?<h{n}>.+?)" : $"\\k<h{n}>");
                    last = m.Index + m.Length;
                }
                literal += french.Length - last;
                pattern.Append(Regex.Escape(french[last..])).Append('$');
                patterns.Add(new Pattern(new Regex(pattern.ToString(), RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline), target, seen.Count, literal));
            }
        }
        // Les gabarits les plus précis (le plus de texte fixe) passent en premier
        patterns.Sort((a, b) => b.Literal.CompareTo(a.Literal));
        Patterns[language] = patterns;
        Exact[language] = exact;
    }

    /// <summary>Nombre de phrases (exactes et à trous) connues pour une langue.</summary>
    public static int PhraseCount(string language)
    {
        lock (Phrases)
        {
            LoadPhrases(language);
            return Exact[language].Count + Patterns[language].Count;
        }
    }

    /// <summary>
    /// Traduit un texte français affiché dans la langue choisie (français : rendu tel quel). Un texte inconnu est rendu tel quel
    /// et, si <see cref="RecordMisses"/> est actif, relevé quand il ressemble à du français.
    /// </summary>
    public static string Translate(string? text)
    {
        if (string.IsNullOrEmpty(text) || _language == Reference)
            return text ?? "";
        lock (Phrases)
        {
            if (TranslationCache.TryGetValue(text, out var cached))
                return cached;
            LoadPhrases(_language);
            string result = TranslateCore(text, 0);
            if (result == text && RecordMisses && LooksFrench(text))
                Misses.AddOrUpdate(text, 1, (_, n) => n + 1);
            if (TranslationCache.Count > 20_000)
                TranslationCache.Clear();
            TranslationCache[text] = result;
            return result;
        }
    }

    private static readonly Regex SentenceBreak = new(@"(?<=[.!?…])\s+|\s+(?=[•·▪])", RegexOptions.Compiled);

    private static string TranslateCore(string text, int depth)
    {
        if (depth > 4 || text.Length == 0)
            return text;
        if (Exact[_language].TryGetValue(text, out var exact))
            return exact;
        foreach (var pattern in Patterns[_language])
        {
            var match = pattern.Regex.Match(text);
            if (!match.Success)
                continue;
            return Hole.Replace(pattern.Target, m =>
            {
                var group = match.Groups[$"h{m.Groups[1].Value}"];
                return group.Success ? TranslateCore(group.Value, depth + 1) : m.Value;
            });
        }
        // Plusieurs lignes ou plusieurs phrases : chacune est traduite à part
        if (text.Contains('\n'))
        {
            var lines = text.Split('\n');
            var translated = lines.Select(l => TranslateCore(l, depth + 1)).ToArray();
            return translated.SequenceEqual(lines) ? text : string.Join('\n', translated);
        }
        var parts = SentenceBreak.Split(text);
        if (parts.Length > 1)
        {
            var separators = SentenceBreak.Matches(text).Select(m => m.Value).ToArray();
            var translated = parts.Select(p => TranslateCore(p, depth + 1)).ToArray();
            if (translated.SequenceEqual(parts))
                return text;
            var rebuilt = new System.Text.StringBuilder();
            for (int i = 0; i < translated.Length; i++)
            {
                rebuilt.Append(translated[i]);
                if (i < separators.Length)
                    rebuilt.Append(separators[i]);
            }
            return rebuilt.ToString();
        }
        return text;
    }
}
