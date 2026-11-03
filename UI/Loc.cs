using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

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

    /// <summary>Langue affichée (« fr » ou « en ») ; une valeur inconnue revient au français.</summary>
    public static string Language
    {
        get => _language;
        set => _language = Languages.Contains(value) ? value : Reference;
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
}
