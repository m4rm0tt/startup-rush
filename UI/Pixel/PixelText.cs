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

    /// <summary>Réglage « Texte agrandi » : le texte courant (12 px) passe à 18, les titres gardent la grille de 12.</summary>
    public static bool Large { get; set; }

    public static double Snap(double size)
    {
        double snapped = Math.Max(Grid, Math.Round(size / Grid, MidpointRounding.AwayFromZero) * Grid);
        return Large && snapped < 24 ? 18 : snapped;
    }

    private static bool _installed;

    /// <summary>À appeler une fois, avant la création du premier contrôle.</summary>
    public static void Install()
    {
        if (_installed)
            return;
        _installed = true;
        TextBlock.FontSizeProperty.OverrideMetadata<TextBlock>(new StyledPropertyMetadata<double>(Grid, coerce: (_, v) => Snap(v)));
        Avalonia.Controls.Documents.TextElement.FontSizeProperty.OverrideMetadata<TextPresenter>(new StyledPropertyMetadata<double>(Grid, coerce: (_, v) => Snap(v)));
        // Les libellés des boutons et onglets héritent la taille de leur contrôle : elle suit la même règle (texte agrandi compris)
        Avalonia.Controls.Primitives.TemplatedControl.FontSizeProperty.OverrideMetadata<Avalonia.Controls.Primitives.TemplatedControl>(new StyledPropertyMetadata<double>(Grid, coerce: (_, v) => Snap(v)));
        // Les polices du pack n'existent qu'en graisse normale : un « gras » était épaissi par le moteur de texte, d'un pixel
        // irrégulier, et collait les lettres (« mise » se lisait « nise »). La hiérarchie passe par la taille et la couleur.
        TextBlock.FontWeightProperty.OverrideMetadata<TextBlock>(new StyledPropertyMetadata<FontWeight>(FontWeight.Normal, coerce: (_, _) => FontWeight.Normal));
        Avalonia.Controls.Documents.TextElement.FontWeightProperty.OverrideMetadata<TextPresenter>(new StyledPropertyMetadata<FontWeight>(FontWeight.Normal, coerce: (_, _) => FontWeight.Normal));
        Avalonia.Controls.Primitives.TemplatedControl.FontWeightProperty.OverrideMetadata<Avalonia.Controls.Primitives.TemplatedControl>(new StyledPropertyMetadata<FontWeight>(FontWeight.Normal, coerce: (_, _) => FontWeight.Normal));
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

    /// <summary>
    /// Échelle automatique de l'écran de jeu pour une fenêtre de w × h (pixels logiques), conçue pour 1500 × 860 :
    /// nette (nombre entier de pixels physiques par pixel du pack) et jamais sous 1 dès que la fenêtre fait au moins
    /// 1240 × 700 pixels physiques (1280 × 720 passait à 0,84 : texte minuscule et flou, alors que les menus défilent).
    /// Plus de plafond à 1,5 : un écran 4K à 100 % passe à 2 au lieu de 1.
    /// </summary>
    public static double AutoScale(double w, double h, double renderScaling, double designW = 1500, double designH = 860)
    {
        double fit = Math.Min(w / designW, h / designH);
        bool roomy = w * renderScaling >= 1240 && h * renderScaling >= 700;
        if (roomy)
            fit = Math.Max(fit, 1 / renderScaling);
        return CrispScale(Math.Clamp(fit, 0.6, 4), renderScaling);
    }

    /// <summary>Texte dessiné à la main (graphiques) : même règle.</summary>
    public static FormattedText Format(string text, Typeface typeface, double size, IBrush brush) =>
        new(text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, Snap(size), brush);
}
