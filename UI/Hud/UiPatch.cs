using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using UI.Pixel;

namespace UI.Hud;

/// <summary>Un contrôle du jeu que la réconciliation peut mettre à jour sur place (il recopie lui-même ses données).</summary>
public interface IPatchable
{
    /// <summary>Reprend les données d'une version fraîchement construite du même contrôle.</summary>
    void PatchFrom(Control fresh);
}

/// <summary>
/// Mise à jour sur place de l'interface. Les menus se décrivent en reconstruisant leurs contrôles (Build) à chaque
/// rafraîchissement ; remplacer l'arbre visible par le nouveau détruisait le contrôle sous la souris (curseur qui clignote,
/// infobulles qui se ferment et se rouvrent, clic perdu entre l'appui et le relâchement, liste déroulante refermée, barre de
/// défilement lâchée, position de défilement perdue). Ici, on compare le nouvel arbre à celui qui est affiché : un contrôle
/// du même type au même endroit est gardé et reçoit seulement les valeurs qui ont changé ; seul ce qui a vraiment changé
/// de nature est remplacé.
///
/// Règles pour écrire un menu compatible :
/// <list type="bullet">
/// <item>une action de bouton passe par <see cref="Ui.OnClick"/> (l'action est une valeur recopiée : le bouton gardé exécute
/// toujours celle de la dernière construction) ;</item>
/// <item>un état visuel de survol passe par <see cref="PixelPanel.HoverSkin"/> ou <see cref="HoverImage"/>, pas par un
/// gestionnaire PointerEntered qui écrirait une valeur que la réconciliation écraserait ;</item>
/// <item>une valeur partagée entre deux contrôles (texte saisi lu par un bouton) vit dans un champ du menu, pas dans une
/// variable locale de Build.</item>
/// </list>
/// </summary>
public static class UiPatch
{
    /// <summary>
    /// Fusionne <paramref name="fresh"/> dans <paramref name="old"/> si c'est possible et renvoie le contrôle à afficher :
    /// <paramref name="old"/> mis à jour, ou <paramref name="fresh"/> s'il faut remplacer.
    /// </summary>
    public static Control Merge(Control? old, Control fresh)
    {
        if (old == null || ReferenceEquals(old, fresh))
            return fresh;
        return Patch(old, fresh) ? old : fresh;
    }

    /// <summary>Remplace le contenu d'un ContentControl par une nouvelle version, en gardant ce qui peut l'être.</summary>
    public static void Apply(ContentControl host, Control? fresh)
    {
        if (fresh == null)
        {
            host.Content = null;
            return;
        }
        var merged = Merge(host.Content as Control, fresh);
        if (!ReferenceEquals(merged, host.Content))
            host.Content = merged;
    }

    /// <summary>Nombre de contrôles gardés et remplacés à la dernière fusion (tests).</summary>
    public static (int Kept, int Replaced) LastStats => (_kept, _replaced);

    private static int _kept, _replaced;

    public static void ResetStats() => _kept = _replaced = 0;

    private static bool Patchable(Control control) =>
        control is IPatchable or PixelPanel or HoverImage
        || control.GetType().Namespace?.StartsWith("Avalonia", StringComparison.Ordinal) == true;

    private static bool Compatible(Control a, Control b) =>
        a.GetType() == b.GetType() && Equals(a.Tag, b.Tag) && Patchable(a);

    private static bool Patch(Control old, Control fresh)
    {
        if (!Compatible(old, fresh))
        {
            _replaced++;
            return false;
        }
        _kept++;
        CopyProperties(old, fresh);
        SyncClasses(old, fresh);
        if (old is PixelPanel oldPanel && fresh is PixelPanel freshPanel && oldPanel.Scale != freshPanel.Scale)
        {
            oldPanel.Scale = freshPanel.Scale;
            oldPanel.InvalidateVisual();
        }
        if (old is IPatchable patchable)
            patchable.PatchFrom(fresh);
        // Les colonnes et rangées d'une grille sont des propriétés directes (non recopiées plus bas) : une barre de progression
        // en dépend pour sa proportion
        if (old is Grid oldGrid && fresh is Grid freshGrid)
        {
            if (!Same(oldGrid.ColumnDefinitions, freshGrid.ColumnDefinitions))
            {
                var columns = new ColumnDefinitions();
                columns.AddRange(freshGrid.ColumnDefinitions.Select(c => new ColumnDefinition(c.Width) { MinWidth = c.MinWidth, MaxWidth = c.MaxWidth }));
                oldGrid.ColumnDefinitions = columns;
            }
            if (!Same(oldGrid.RowDefinitions, freshGrid.RowDefinitions))
            {
                var rows = new RowDefinitions();
                rows.AddRange(freshGrid.RowDefinitions.Select(r => new RowDefinition(r.Height) { MinHeight = r.MinHeight, MaxHeight = r.MaxHeight }));
                oldGrid.RowDefinitions = rows;
            }
        }

        switch (old)
        {
            case Panel panel:
                PatchChildren(panel.Children, ((Panel)fresh).Children);
                break;
            case ContentControl content:
            {
                var freshContent = ((ContentControl)fresh).Content;
                if (content.Content is Control oldChild && freshContent is Control newChild)
                {
                    ((ContentControl)fresh).Content = null;
                    var merged = Merge(oldChild, newChild);
                    if (!ReferenceEquals(merged, oldChild))
                        content.Content = merged;
                }
                else if (!Equals(content.Content, freshContent))
                {
                    ((ContentControl)fresh).Content = null;
                    content.Content = freshContent;
                }
                break;
            }
            case Decorator decorator:
            {
                var newChild = ((Decorator)fresh).Child;
                if (decorator.Child is { } oldChild && newChild != null)
                {
                    ((Decorator)fresh).Child = null;
                    var merged = Merge(oldChild, newChild);
                    if (!ReferenceEquals(merged, oldChild))
                        decorator.Child = merged;
                }
                else if (!ReferenceEquals(decorator.Child, newChild))
                {
                    ((Decorator)fresh).Child = null;
                    decorator.Child = newChild;
                }
                break;
            }
        }

        if (old is Button oldButton && fresh is Button freshButton)
            PatchFlyout(oldButton, freshButton);
        return true;
    }

    /// <summary>
    /// Une liste déroulante garde son menu (Flyout) : le remplacer le fermerait. On met seulement à jour son contenu.
    /// </summary>
    private static void PatchFlyout(Button old, Button fresh)
    {
        if (old.Flyout is Flyout oldFlyout && fresh.Flyout is Flyout freshFlyout)
        {
            if (oldFlyout.Content is Control oldContent && freshFlyout.Content is Control newContent)
            {
                freshFlyout.Content = null;
                var merged = Merge(oldContent, newContent);
                if (!ReferenceEquals(merged, oldContent))
                    oldFlyout.Content = merged;
            }
            oldFlyout.Placement = freshFlyout.Placement;
        }
        else if (!ReferenceEquals(old.Flyout, fresh.Flyout))
            old.Flyout = fresh.Flyout;
    }

    /// <summary>
    /// Les enfants : on garde l'ordre, on aligne le début et la fin des deux listes (une ligne ajoutée en haut ne décale pas
    /// tout le reste), et on fusionne deux à deux ce qui se trouve au même endroit.
    /// </summary>
    private static void PatchChildren(Controls old, Controls fresh)
    {
        var olds = old.ToList();
        var news = fresh.ToList();
        fresh.Clear();

        int prefix = 0;
        while (prefix < olds.Count && prefix < news.Count && Compatible(olds[prefix], news[prefix]))
            prefix++;
        int suffix = 0;
        while (suffix < olds.Count - prefix && suffix < news.Count - prefix
               && Compatible(olds[olds.Count - 1 - suffix], news[news.Count - 1 - suffix]))
            suffix++;

        var result = new List<Control>(news.Count);
        for (int i = 0; i < news.Count; i++)
        {
            Control? counterpart = null;
            if (i < prefix)
                counterpart = olds[i];
            else if (i >= news.Count - suffix)
                counterpart = olds[olds.Count - (news.Count - i)];
            else if (i - prefix < olds.Count - prefix - suffix)
                counterpart = olds[i];
            result.Add(Merge(counterpart, news[i]));
        }

        if (result.Count == olds.Count && result.Zip(olds).All(p => ReferenceEquals(p.First, p.Second)))
            return;
        // Retirer ce qui ne sert plus, puis insérer les nouveaux à leur place (les gardés restent dans le même ordre)
        var keep = new HashSet<Control>(result, ReferenceEqualityComparer.Instance);
        for (int i = old.Count - 1; i >= 0; i--)
            if (!keep.Contains(old[i]))
                old.RemoveAt(i);
        for (int i = 0; i < result.Count; i++)
            if (i >= old.Count || !ReferenceEquals(old[i], result[i]))
                old.Insert(i, result[i]);
    }

    private static void SyncClasses(Control old, Control fresh)
    {
        var a = old.Classes.Where(c => !c.StartsWith(':')).ToList();
        var b = fresh.Classes.Where(c => !c.StartsWith(':')).ToList();
        if (!a.SequenceEqual(b))
            old.Classes.Replace(b);
    }

    // ------------------------------------------------------------------ Propriétés

    /// <summary>Propriétés jamais recopiées : la structure (traitée à part), et l'état que le joueur fait vivre.</summary>
    private static readonly HashSet<AvaloniaProperty> Skipped =
    [
        ContentControl.ContentProperty,
        Decorator.ChildProperty,
        Button.FlyoutProperty,
        ScrollViewer.OffsetProperty,
        StyledElement.DataContextProperty,
        Visual.ZIndexProperty,
    ];

    /// <summary>Propriétés effacées sur le contrôle gardé quand la nouvelle version ne les donne plus.</summary>
    private static readonly HashSet<AvaloniaProperty> Clearable =
    [
        ToolTip.TipProperty,
        Visual.OpacityProperty,
        TextBlock.ForegroundProperty,
        TemplatedControl.ForegroundProperty,
        TemplatedControl.BackgroundProperty,
        Border.BackgroundProperty,
        Border.BorderBrushProperty,
        PixelPanel.BackgroundProperty,
        PixelPanel.BorderBrushProperty,
        PixelPanel.HoverSkinProperty,
        PixelPanel.FocusSkinProperty,
        Layoutable.MarginProperty,
        Layoutable.WidthProperty,
        Layoutable.HeightProperty,
        Layoutable.MinWidthProperty,
        Layoutable.MaxWidthProperty,
        InputElement.CursorProperty,
        InputElement.IsHitTestVisibleProperty,
        Visual.IsVisibleProperty,
        TextBlock.FontWeightProperty,
        TextBlock.TextDecorationsProperty,
        Grid.ColumnProperty,
        Grid.RowProperty,
        Grid.ColumnSpanProperty,
        Grid.RowSpanProperty,
    ];

    private static readonly ConditionalWeakTable<Type, AvaloniaProperty[]> Properties = new();

    private static AvaloniaProperty[] PropertiesOf(Type type) => Properties.GetValue(type, t =>
        AvaloniaPropertyRegistry.Instance.GetRegistered(t)
            .Concat(AvaloniaPropertyRegistry.Instance.GetRegisteredAttached(t))
            .Where(p => !p.IsReadOnly && !p.IsDirect && !Skipped.Contains(p))
            .Distinct()
            .ToArray());

    private static void CopyProperties(Control old, Control fresh)
    {
        foreach (var property in PropertiesOf(old.GetType()))
        {
            if (!fresh.IsSet(property))
            {
                if (Clearable.Contains(property) && old.IsSet(property) && IsLocal(old, property))
                    old.ClearValue(property);
                continue;
            }
            // Ce que le joueur est en train de saisir ou de faire glisser lui appartient
            if (property == TextBox.TextProperty && old.IsFocused)
                continue;
            if (property == RangeBase.ValueProperty && old.IsPointerOver)
                continue;
            // Une image qui suit le survol choisit elle-même ce qu'elle affiche
            if (property == Image.SourceProperty && old is HoverImage)
                continue;
            var value = fresh.GetValue(property);
            if (!Same(old.GetValue(property), value))
                old.SetValue(property, value);
        }
    }

    private static bool IsLocal(AvaloniaObject target, AvaloniaProperty property) =>
        Avalonia.Diagnostics.AvaloniaObjectExtensions.GetDiagnostic(target, property).Priority == Avalonia.Data.BindingPriority.LocalValue;

    /// <summary>Égalité de valeur, y compris pour les objets que chaque construction recrée (pinceaux, colonnes de grille).</summary>
    private static bool Same(object? a, object? b)
    {
        if (ReferenceEquals(a, b) || Equals(a, b))
            return true;
        return (a, b) switch
        {
            (ISolidColorBrush x, ISolidColorBrush y) => x.Color == y.Color && x.Opacity == y.Opacity && x.Transform == null && y.Transform == null,
            (ColumnDefinitions x, ColumnDefinitions y) => x.Count == y.Count && x.Zip(y).All(p => p.First.Width == p.Second.Width
                && p.First.MinWidth == p.Second.MinWidth && p.First.MaxWidth == p.Second.MaxWidth),
            (RowDefinitions x, RowDefinitions y) => x.Count == y.Count && x.Zip(y).All(p => p.First.Height == p.Second.Height
                && p.First.MinHeight == p.Second.MinHeight && p.First.MaxHeight == p.Second.MaxHeight),
            (FontFamily x, FontFamily y) => x.Name == y.Name,
            _ => false,
        };
    }
}
