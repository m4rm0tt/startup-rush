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

    /// <summary>
    /// Taille du texte courant : 18 px (la police du pack reste nette en multiples de 12, mais 12 px était trop petit à lire :
    /// BUG-010). Seules les minuscules étiquettes demandées à 11 px ou moins (touches, indices) gardent 12 px.
    /// </summary>
    public const double Body = 18;

    /// <summary>Taille du texte courant avec « Texte agrandi » : 24 px, deux fois la grille.</summary>
    public const double BodyLarge = 24;

    /// <summary>Réglage « Texte agrandi » : le texte courant passe de 18 à 24 px, les titres d'un cran de plus.</summary>
    public static bool Large { get; set; }

    private static int _hudDepth;

    /// <summary>
    /// Pendant sa durée de vie, le texte créé garde sa taille normale même si « Texte agrandi » est actif : barre du haut et
    /// barre d'outils, qui n'ont pas la place d'un texte de 24 px en 1280 × 720 (elles restent à 18 px, lisibles).
    /// </summary>
    public static IDisposable Hud()
    {
        _hudDepth++;
        return new HudScope();
    }

    private sealed class HudScope : IDisposable
    {
        private bool _done;

        public void Dispose()
        {
            if (_done)
                return;
            _done = true;
            _hudDepth--;
        }
    }

    /// <summary>Marque la racine d'une barre qui garde le texte normal (voir <see cref="Hud"/>) : elle est ignorée quand on agrandit le texte en jeu.</summary>
    public static readonly AttachedProperty<bool> HudProperty = AvaloniaProperty.RegisterAttached<Control, Control, bool>("Hud");

    public static double Snap(double size)
    {
        bool large = Large && _hudDepth == 0;
        if (size < 11.5)
            return Grid;                                  // étiquettes minuscules (touches, indices)
        if (size < 21)
            return large ? BodyLarge : Body;              // texte courant (demandé de 13 à 20 px)
        double snapped = Math.Max(Grid, Math.Round(size / Grid, MidpointRounding.AwayFromZero) * Grid);
        return large && snapped < 36 ? snapped + Grid : snapped;
    }

    private static bool _installed;

    /// <summary>À appeler une fois, avant la création du premier contrôle.</summary>
    public static void Install()
    {
        if (_installed)
            return;
        _installed = true;
        TextBlock.FontSizeProperty.OverrideMetadata<TextBlock>(new StyledPropertyMetadata<double>(Body, coerce: (_, v) => Snap(v)));
        // Langue : tout texte affiché passe par la traduction des phrases (français : rien ne change)
        TextBlock.TextProperty.Changed.AddClassHandler<TextBlock>((block, e) =>
        {
            if (e.NewValue is string text && Loc.Language != Loc.Reference)
            {
                string translated = Loc.Translate(text);
                if (translated != text)
                    block.SetCurrentValue(TextBlock.TextProperty, translated);
            }
        });
        Avalonia.Controls.Documents.TextElement.FontSizeProperty.OverrideMetadata<TextPresenter>(new StyledPropertyMetadata<double>(Body, coerce: (_, v) => Snap(v)));
        // Les libellés des boutons et onglets héritent la taille de leur contrôle : elle suit la même règle (texte agrandi compris)
        Avalonia.Controls.Primitives.TemplatedControl.FontSizeProperty.OverrideMetadata<Avalonia.Controls.Primitives.TemplatedControl>(new StyledPropertyMetadata<double>(Body, coerce: (_, v) => Snap(v)));
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
        new(Loc.Translate(text), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, Snap(size), brush);
}
