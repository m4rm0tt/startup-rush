using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;

namespace UI.Pixel;

/// <summary>
/// Les polices du pack sont dessinées sur une grille de 12 px (ascendante 10, descendante 2) : elles ne sont nettes qu'en
/// 12, 24, 36 ou 48 px, sans anticrénelage (INTEGRATION_GUIDE.md §11). Toute taille demandée est ramenée au multiple de 12
/// le plus proche (12 au moins), pour le texte des contrôles comme pour celui des champs de saisie.
/// </summary>
public static class PixelText
{
    public const double Grid = 12;

    public static double Snap(double size) => Math.Max(Grid, Math.Round(size / Grid, MidpointRounding.AwayFromZero) * Grid);

    private static bool _installed;

    /// <summary>À appeler une fois, avant la création du premier contrôle.</summary>
    public static void Install()
    {
        if (_installed)
            return;
        _installed = true;
        TextBlock.FontSizeProperty.OverrideMetadata<TextBlock>(new StyledPropertyMetadata<double>(Grid, coerce: (_, v) => Snap(v)));
        Avalonia.Controls.Documents.TextElement.FontSizeProperty.OverrideMetadata<TextPresenter>(new StyledPropertyMetadata<double>(Grid, coerce: (_, v) => Snap(v)));
    }

    /// <summary>
    /// Échelle automatique de l'interface : la plus grande qui tient dans la fenêtre (« fit »), ramenée à un nombre entier de
    /// pixels physiques par pixel du pack (mise à l'échelle Windows comprise) pour que texte et sprites restent nets. Une
    /// fenêtre trop petite pour l'échelle 1 rétrécit l'interface (moins nette, mais tout tient).
    /// </summary>
    public static double CrispScale(double fit, double renderScaling)
    {
        double physical = fit * renderScaling;
        if (physical < 1)
            return fit;
        return Math.Max(1, Math.Floor(physical + 0.25)) / renderScaling;
    }

    /// <summary>Texte dessiné à la main (graphiques) : même règle.</summary>
    public static FormattedText Format(string text, Typeface typeface, double size, IBrush brush) =>
        new(text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, Snap(size), brush);
}
