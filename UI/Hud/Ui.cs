using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using UI.Audio;
using UI.Pixel;

namespace UI.Hud;

/// <summary>
/// Petites fabriques de contrôles pour écrire les menus en C# de façon compacte et cohérente
/// (mêmes tailles, mêmes couleurs, mêmes espacements partout).
/// </summary>
public static class Ui
{
    public const double Gap = 8;

    public static TextBlock Text(string text, double size = 16, IBrush? color = null, bool bold = false, bool wrap = true) => new()
    {
        Text = text,
        FontSize = size,
        Foreground = color ?? Palette.Cloud,
        FontWeight = bold ? FontWeight.Bold : FontWeight.Normal,
        TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
    };

    public static TextBlock Title(string text, double size = 22) => Text(text, size, Palette.Snow, bold: true);

    public static TextBlock Muted(string text, double size = 14) => Text(text, size, Palette.Fog);

    /// <summary>Chiffre en police terminal (lisible, à chasse fixe).</summary>
    public static TextBlock Number(string text, double size = 22, IBrush? color = null)
    {
        var block = Text(text, size, color ?? Palette.Snow, wrap: false);
        block.FontFamily = (FontFamily)Application.Current!.FindResource("TermFont")!;
        return block;
    }

    public static StackPanel Column(params Control?[] children) => Column(Gap, children);

    public static StackPanel Column(double spacing, params Control?[] children)
    {
        var panel = new StackPanel { Spacing = spacing };
        foreach (var child in children)
            if (child != null)
                panel.Children.Add(child);
        return panel;
    }

    public static StackPanel Row(params Control?[] children) => Row(Gap, children);

    public static StackPanel Row(double spacing, params Control?[] children)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = spacing };
        foreach (var child in children)
            if (child != null)
                panel.Children.Add(child);
        return panel;
    }

    /// <summary>Une rangée « libellé ... valeur » sur toute la largeur.</summary>
    public static Grid Pair(string label, string value, IBrush? valueColor = null)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        grid.Children.Add(Text(label, 15, Palette.Fog));
        var right = Text(value, 15, valueColor ?? Palette.Snow, wrap: false);
        right.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(right, 1);
        grid.Children.Add(right);
        return grid;
    }

    public static Button Button(string label, Action onClick, string style = "", bool enabled = true, string? tip = null)
    {
        var button = new Button { Content = label, IsEnabled = enabled, FontSize = 15, Padding = new Thickness(12, 4, 12, 8) };
        foreach (var cls in style.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            button.Classes.Add(cls);
        button.Click += (_, _) =>
        {
            Sfx.Play(Sound.Select);
            onClick();
        };
        if (tip != null)
            ToolTip.SetTip(button, tip);
        return button;
    }

    /// <summary>
    /// Bouton à icône du pack (hud_button_icon, icône 16 px affichée en 24 comme partout), avec un libellé facultatif ; « selected » le
    /// montre enfoncé (bascule allumée).
    /// </summary>
    public static Button IconButton(string icon, Action onClick, string tip, string? label = null, bool selected = false, bool enabled = true, string style = "icon")
    {
        // Le libellé hérite de la couleur du bouton (texte clair sur un bouton rouge ou doré)
        var caption = label == null ? null : new TextBlock { Text = label, FontSize = 15 };
        bool onAccent = style.Contains("red") || style.Contains("gold");
        var image = Image(Icons.Get(icon, onAccent ? "#fbf5e6" : "#2b2128"), 24, 24);
        Control content = caption == null ? image : Row(6, image, caption);
        if (content is StackPanel row)
            foreach (var child in row.Children)
                child.VerticalAlignment = VerticalAlignment.Center;
        var button = new Button { Content = content, IsEnabled = enabled };
        foreach (var cls in style.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            button.Classes.Add(cls);
        if (selected)
            button.Classes.Add("selected");
        button.Click += (_, _) =>
        {
            Sfx.Play(Sound.Select);
            onClick();
        };
        ToolTip.SetTip(button, tip);
        return button;
    }

    /// <summary>Bloc encadré (cadre 9-slice en creux).</summary>
    public static PixelPanel Card(Control content, IBrush? border = null) => new()
    {
        Skin = "inset",
        Padding = new Thickness(10, 8),
        Child = content,
        BorderBrush = border,
        BorderThickness = new Thickness(border != null ? 2 : 0),
    };

    /// <summary>Section titrée : en-tête du pack (hud_section_header), puis son contenu.</summary>
    public static Control Section(string title, params Control?[] children)
    {
        var column = Column(6, children);
        column.Children.Insert(0, new PixelPanel
        {
            Skin = "pack:hud_section_header",
            Padding = new Thickness(14, 3, 8, 7),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = Text(title.ToUpperInvariant(), 13, Palette.Ink, bold: true, wrap: false),
        });
        column.Margin = new Thickness(0, 6, 0, 2);
        return column;
    }

    /// <summary>
    /// Sens d'une couleur du thème pour les composants du pack qui ont des variantes (badges, remplissages, notifications) :
    /// success, warning, danger, info, accent ou neutral.
    /// </summary>
    public static string Semantic(IBrush? color) =>
        color == Palette.Leaf || color == Palette.Forest ? "success"
        : color == Palette.Gold ? "warning"
        : color == Palette.Blood || color == Palette.Plum || color == Palette.Wine ? "danger"
        : color == Palette.Ember || color == Palette.Rust ? "accent"
        : color == Palette.Sky || color == Palette.Lemon || color == Palette.Ice || color == Palette.Navy ? "info"
        : "neutral";

    /// <summary>Barre de progression du pack : piste et remplissage de la couleur de sens (sarcelle par défaut).</summary>
    public static Control Bar(double fraction, IBrush? color = null, double height = 8, double width = double.NaN)
    {
        string fill = Semantic(color ?? Palette.Leaf) switch
        {
            "success" => "success",
            "warning" => "warning",
            "danger" or "accent" => "danger",
            _ => "default",
        };
        var track = new PixelPanel { Skin = "pack:hud_progress_track", Scale = 1, Height = Math.Max(8, height), Width = width, MinWidth = 40, Padding = new Thickness(1) };
        var bar = new PixelPanel { Skin = $"pack:hud_progress_fill_{fill}", Scale = 1, HorizontalAlignment = HorizontalAlignment.Left };
        track.Child = bar;
        track.SizeChanged += (_, e) => bar.Width = Math.Clamp(fraction, 0, 1) * Math.Max(0, e.NewSize.Width - 2);
        return track;
    }

    /// <summary>Une icône : identifiant du pack (« icon_save ») ou nom du jeu (voir Icons).</summary>
    public static Image Icon(string key, double size = 24) => Image(Icons.Get(key), size, size);

    public static Image Image(IImage bitmap, double width, double height)
    {
        var image = new Image { Source = bitmap, Width = width, Height = height, Stretch = Stretch.Uniform };
        RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.None);
        return image;
    }

    /// <summary>Étiquette d'état : badge du pack de la couleur de sens (neutre sur fond clair).</summary>
    public static PixelPanel Tag(string text, IBrush background, IBrush? foreground = null)
    {
        string kind = Semantic(background);
        bool light = kind is "neutral" or "warning";
        return new PixelPanel
        {
            Skin = $"pack:hud_badge_{kind}",
            Padding = new Thickness(7, 2, 7, 4),
            Child = Text(text, 13, foreground ?? (light ? Palette.Ink : Palette.Paper), bold: true, wrap: false),
            VerticalAlignment = VerticalAlignment.Center,
        };
    }

    /// <summary>
    /// Champ de saisie dans un cadre du pack : texte (hud_field_text) ou recherche avec sa loupe (hud_field_search) ; le cadre
    /// suit le survol et le focus.
    /// </summary>
    public static Control Input(string value, Action<string> changed, string? watermark = null, double width = double.NaN, bool search = false)
    {
        string component = search ? "hud_field_search" : "hud_field_text";
        var box = new TextBox
        {
            Text = value, PlaceholderText = watermark, MinWidth = 120, Background = Brushes.Transparent, BorderThickness = new Thickness(0),
            Padding = new Thickness(search ? 28 : 8, 4, 8, 6), VerticalContentAlignment = VerticalAlignment.Center,
        };
        var frame = new PixelPanel { Skin = $"pack:{component}:normal", Width = width, Child = box };
        if (!double.IsNaN(width))
            frame.HorizontalAlignment = HorizontalAlignment.Left;
        void State() => frame.Skin = $"pack:{component}:{(box.IsFocused ? "focus" : box.IsPointerOver ? "hover" : "normal")}";
        box.GotFocus += (_, _) => State();
        box.LostFocus += (_, _) => State();
        box.PointerEntered += (_, _) => State();
        box.PointerExited += (_, _) => State();
        box.TextChanged += (_, _) => changed(box.Text ?? "");
        if (!search)
            return frame;
        var magnifier = Icon("icon_search", 16);
        magnifier.HorizontalAlignment = HorizontalAlignment.Left;
        magnifier.Margin = new Thickness(8, 0, 0, 0);
        magnifier.IsHitTestVisible = false;
        return new Panel { Children = { frame, magnifier }, HorizontalAlignment = frame.HorizontalAlignment };
    }

    /// <summary>Case à cocher du pack (hud_checkbox) et son libellé ; un clic bascule.</summary>
    public static Button CheckBox(string label, bool on, Action<bool> changed, bool enabled = true) => StateToggle("hud_checkbox", label, on, changed, enabled);

    /// <summary>Interrupteur du pack (hud_switch) et son libellé.</summary>
    public static Button Switch(string label, bool on, Action<bool> changed, bool enabled = true) => StateToggle("hud_switch", label, on, changed, enabled);

    /// <summary>Bouton radio du pack (hud_radio) : un choix parmi plusieurs, chacun avec son libellé.</summary>
    public static Button Radio(string label, bool on, Action pick, bool enabled = true) => StateToggle("hud_radio", label, on, _ => pick(), enabled);

    /// <summary>Un contrôle à états du pack (« on_normal », « off_hover »…) affiché au double, suivi de son libellé.</summary>
    private static Button StateToggle(string id, string label, bool on, Action<bool> changed, bool enabled)
    {
        string prefix = on ? "on" : "off";
        var normal = PackSkins.Sprite(id, $"{prefix}_{(enabled ? "normal" : "disabled")}");
        var image = Image(normal, normal.PixelSize.Width * 2, normal.PixelSize.Height * 2);
        image.VerticalAlignment = VerticalAlignment.Center;
        var caption = Text(label, 15, enabled ? Palette.Ink : Palette.Fog);
        caption.VerticalAlignment = VerticalAlignment.Center;
        var button = new Button { Content = Row(8, image, caption), IsEnabled = enabled, Padding = new Thickness(2, 2, 6, 2), HorizontalAlignment = HorizontalAlignment.Left };
        button.Classes.Add("plain");
        if (enabled)
        {
            button.PointerEntered += (_, _) => image.Source = PackSkins.Sprite(id, $"{prefix}_hover");
            button.PointerExited += (_, _) => image.Source = normal;
        }
        button.Click += (_, _) =>
        {
            Sfx.Play(Sound.Select);
            changed(!on);
        };
        return button;
    }

    /// <summary>
    /// Glissière du pack (piste, remplissage, bouton) avec sa valeur dans une bulle (hud_popover). La valeur s'applique pendant
    /// qu'on glisse, sans reconstruire le menu (sinon la glissière disparaîtrait sous le pointeur).
    /// </summary>
    public static Control Slider(double value, double min, double max, double step, Action<double> changed, Func<double, string> format, double width = 260)
    {
        var bubble = Text(format(value), 14, Palette.Ink, wrap: false);
        var slider = new Avalonia.Controls.Slider
        {
            Minimum = min, Maximum = max, Value = value, TickFrequency = step, IsSnapToTickEnabled = true, Width = width,
            VerticalAlignment = VerticalAlignment.Center,
        };
        // Piste et remplissage du pack sous le bouton : le remplissage va du début de la piste au centre du bouton (20 de large)
        var track = new PixelPanel { Skin = "pack:hud_slider_track", Height = 12, Width = width - 20, Margin = new Thickness(10, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
        var fill = new PixelPanel { Skin = "pack:hud_slider_fill", Height = 12, Margin = new Thickness(10, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
        void Fill() => fill.Width = (slider.Value - min) / Math.Max(1e-9, max - min) * (width - 20);
        Fill();
        slider.PropertyChanged += (_, e) =>
        {
            if (e.Property != Avalonia.Controls.Primitives.RangeBase.ValueProperty)
                return;
            bubble.Text = format(slider.Value);
            Fill();
            changed(slider.Value);
        };
        var layered = new Panel { Width = width, Children = { track, fill, slider } };
        // La bulle et sa pointe tournée vers la glissière (la pointe recouvre le bord de 1 px)
        var popover = new PixelPanel { Skin = "pack:hud_popover", Padding = new Thickness(12, 5, 12, 9), Child = bubble, VerticalAlignment = VerticalAlignment.Center };
        var arrowBitmap = PackSkins.Extra("hud_popover", "popover_arrow_left");
        var arrow = Image(arrowBitmap, arrowBitmap.PixelSize.Width * 2, arrowBitmap.PixelSize.Height * 2);
        arrow.VerticalAlignment = VerticalAlignment.Center;
        arrow.Margin = new Thickness(0, 0, -2, 0);
        return Row(4, layered, Row(0, arrow, popover));
    }

    /// <summary>Case de couleur du pack (hud_swatch_cell) : la couleur peinte sous le cadre, survol et sélection du cadre.</summary>
    public static Button Swatch(Color color, bool selected, Action pick, string tip)
    {
        var frame = Image(PackSkins.Sprite("hud_swatch_cell", selected ? "selected" : "normal"), 36, 36);
        var fill = new Border { Background = new SolidColorBrush(color), Width = 20, Height = 20, Margin = new Thickness(8), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var button = new Button { Content = new Panel { Width = 36, Height = 36, Children = { fill, frame } }, Padding = new Thickness(0), Margin = new Thickness(0, 0, 4, 0) };
        button.Classes.Add("plain");
        if (!selected)
        {
            button.PointerEntered += (_, _) => frame.Source = PackSkins.Sprite("hud_swatch_cell", "hover");
            button.PointerExited += (_, _) => frame.Source = PackSkins.Sprite("hud_swatch_cell", "normal");
        }
        button.Click += (_, _) =>
        {
            Sfx.Play(Sound.Select);
            pick();
        };
        ToolTip.SetTip(button, tip);
        return button;
    }

    /// <summary>Un portrait dans son cadre du pack (hud_portrait_frame) : le visage dessous, la fenêtre de 32 px par-dessus.</summary>
    public static Control Portrait(IImage portrait, bool selected = false) => new Panel
    {
        Width = 76, Height = 76,
        Children =
        {
            new Border { Margin = new Thickness(6), Child = Image(portrait, 64, 64) },
            Image(PackSkins.Sprite("hud_portrait_frame", selected ? "selected" : "normal"), 76, 76),
        },
    };

    /// <summary>Onglets du pack (hud_tabs) : l'actif, clair, déborde sur le contenu ; les autres restent en retrait.</summary>
    public static Control Tabs<T>(IEnumerable<(T Value, string Label)> options, T current, Action<T> pick)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, Margin = new Thickness(0, 0, 0, -4) };
        foreach (var (value, label) in options)
        {
            bool selected = EqualityComparer<T>.Default.Equals(value, current);
            row.Children.Add(Button(label, () => pick(value), selected ? "tabs selected" : "tabs"));
        }
        // Le filet sous les onglets : l'onglet actif le recouvre
        return new StackPanel { Children = { row, new Border { Height = 2, Background = Palette.Steel } } };
    }

    /// <summary>Choix exclusif parmi quelques valeurs : contrôle segmenté du pack, le choisi en sarcelle.</summary>
    public static WrapPanel Choice<T>(IEnumerable<(T Value, string Label)> options, T current, Action<T> pick, bool enabled = true)
    {
        var panel = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        var list = new List<(T Value, string Label)>(options);
        for (int i = 0; i < list.Count; i++)
        {
            var (value, label) = list[i];
            bool selected = EqualityComparer<T>.Default.Equals(value, current);
            string classes = "seg" + (selected ? " selected" : "") + (i == 0 ? " first" : "") + (i == list.Count - 1 ? " last" : "");
            panel.Children.Add(Button(label, () => pick(value), classes, enabled));
        }
        return panel;
    }

    public static string Chf(decimal amount) => $"{amount:N0} CHF";

    /// <summary>Montant signé lisible : « +1 200 CHF », « −300 CHF ».</summary>
    public static string SignedChf(decimal amount) => amount >= 0 ? $"+{amount:N0} CHF" : $"−{-amount:N0} CHF";

    /// <summary>Sections dépliées par le joueur (par titre) : un menu se reconstruit chaque seconde, elles doivent rester ouvertes.</summary>
    private static readonly HashSet<string> OpenDetails = new();

    /// <summary>Ouvre ou ferme une section dépliable par son titre (parcours de captures).</summary>
    public static void SetDetailsOpen(string title, bool open)
    {
        if (open)
            OpenDetails.Add(title);
        else
            OpenDetails.Remove(title);
    }

    /// <summary>
    /// Section repliée par défaut : les détails pour qui veut comprendre, jamais nécessaires pour jouer. Son état (ouverte ou
    /// fermée) est retenu : sans cela, le rafraîchissement automatique du menu la refermait aussitôt.
    /// </summary>
    public static Control Details(string title, Control content, bool open = false)
    {
        bool expanded = open || OpenDetails.Contains(title);
        var chevron = Icon(expanded ? "icon_chevron_down" : "icon_chevron_right", 16);
        var header = new Button { Content = Row(6, chevron, Text(title, 14, Palette.Fog, wrap: false)), Padding = new Thickness(4, 3, 10, 5) };
        header.Classes.Add("plain");
        var body = new Border { Child = content, IsVisible = expanded, Margin = new Thickness(22, 4, 0, 0) };
        header.Click += (_, _) =>
        {
            body.IsVisible = !body.IsVisible;
            chevron.Source = Icons.Get(body.IsVisible ? "icon_chevron_down" : "icon_chevron_right");
            SetDetailsOpen(title, body.IsVisible);
        };
        return new StackPanel { Margin = new Thickness(0, 6, 0, 0), Children = { header, body } };
    }

    /// <summary>Un chiffre clé : petite étiquette au-dessus, gros nombre dessous.</summary>
    public static PixelPanel Stat(string label, string value, IBrush? color = null, double width = 150, string? tip = null)
    {
        var panel = new PixelPanel { Skin = "inset", Padding = new Thickness(10, 6), Margin = new Thickness(0, 0, 6, 6), Width = width, Child = Column(0, Text(label, 12, Palette.Fog, wrap: false), Number(value, 22, color)) };
        if (tip != null)
            ToolTip.SetTip(panel, tip);
        return panel;
    }

    /// <summary>Pastilles de niveau (●●○) : se lisent sans couleur.</summary>
    public static TextBlock Pips(int level, int max = 3, IBrush? color = null) =>
        Text(new string('●', Math.Max(0, level)) + new string('○', Math.Max(0, max - level)), 14, color ?? Palette.Gold, wrap: false);

    /// <summary>Carré de couleur avec une icône : l'identité visuelle d'un projet.</summary>
    public static Border IconTile(string icon, Color color, double size = 44) => new()
    {
        Width = size,
        Height = size,
        Background = new SolidColorBrush(color),
        BorderBrush = Palette.Ink,
        BorderThickness = new Thickness(2),
        Child = Icon(icon, size * 0.6),
    };

    public static IBrush Signed(decimal amount) => amount < 0 ? Palette.Blood : amount > 0 ? Palette.Leaf : Palette.Cloud;

    public static ScrollViewer Scroll(Control content) => new()
    {
        Content = content,
        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
        VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
    };
}
