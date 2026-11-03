using System.Globalization;

namespace Core;

/// <summary>
/// La présentation des nombres du jeu, la même sur tous les PC (sinon « 87,760 CHF » sur un Windows en anglais) : usage
/// suisse romand, milliers séparés par une espace fine insécable (U+202F, présente dans les deux polices du jeu), virgule
/// décimale (« 15 840 CHF », « 2,5 % »). Les dates sont déjà écrites avec des formats explicites (« dd.MM.yyyy »).
/// Installée au démarrage de l'application et des tests ; les sauvegardes (JSON) n'en dépendent pas.
/// </summary>
public static class GameCulture
{
    public const string GroupSeparator = " ";

    public static CultureInfo Culture { get; } = Build();

    private static CultureInfo Build()
    {
        CultureInfo baseCulture;
        try
        {
            baseCulture = CultureInfo.GetCultureInfo("fr-CH");
        }
        catch (CultureNotFoundException)
        {
            baseCulture = CultureInfo.InvariantCulture;
        }
        var culture = (CultureInfo)baseCulture.Clone();
        var n = culture.NumberFormat;
        n.NumberGroupSeparator = n.CurrencyGroupSeparator = n.PercentGroupSeparator = GroupSeparator;
        n.NumberDecimalSeparator = n.CurrencyDecimalSeparator = n.PercentDecimalSeparator = ",";
        n.NumberGroupSizes = n.CurrencyGroupSizes = n.PercentGroupSizes = [3];
        n.NegativeSign = "-";
        n.CurrencySymbol = "CHF";
        n.PercentSymbol = "%";
        n.PercentPositivePattern = 0; // « 39 % »
        n.PercentNegativePattern = 0; // « -39 % »
        return CultureInfo.ReadOnly(culture);
    }

    /// <summary>Pour tout le processus : le fil courant et les fils créés ensuite (tâches parallèles, minuteries).</summary>
    public static void Install()
    {
        CultureInfo.DefaultThreadCurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.CurrentUICulture = Culture;
    }
}
