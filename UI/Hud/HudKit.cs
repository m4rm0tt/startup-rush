using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using UI.Iso;
using UI.Pixel;

namespace UI.Hud;

/// <summary>
/// Les composants du HUD « Papier » du pack branchés pour la version complète : liste déroulante, champ numérique à flèches,
/// champ multiligne, menu contextuel, jauge à segments, bandeau, puce sombre, séparateurs, panneau secondaire, carte,
/// barres de graphique et barre « en cours » rayée. Chacun est un vrai contrôle relié aux données, jamais une image figée.
/// </summary>
public static class HudKit
{
    // ------------------------------------------------------------------ Liste déroulante (hud_dropdown, hud_dropdown_list)

    /// <summary>Une liste déroulante : le choix courant et son chevron ; un clic ouvre la liste du pack, un choix la ferme.</summary>
    public static Control Dropdown<T>(IEnumerable<(T Value, string Label)> options, T current, Action<T> pick, bool enabled = true, double width = 200)
    {
        var list = options.ToList();
        string label = list.FirstOrDefault(o => EqualityComparer<T>.Default.Equals(o.Value, current)).Label ?? "—";
        var caret = Ui.Image(Icons.Get("icon_caret_down", "#2b2128"), 16, 16);
        caret.HorizontalAlignment = HorizontalAlignment.Right;
        var text = Ui.Text(label, 12, Palette.Ink, wrap: false);
        var frame = new PixelPanel
        {
            Skin = enabled ? "pack:hud_dropdown:normal" : "pack:hud_dropdown:disabled", HoverSkin = enabled ? "pack:hud_dropdown:hover" : null,
            Width = width, Padding = new Thickness(8, 4, 6, 6),
            Child = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { text, caret } },
        };
        Grid.SetColumn(caret, 1);
        var button = new Button { Content = frame, Padding = new Thickness(0), IsEnabled = enabled, Tag = "dropdown" };
        button.Classes.Add("plain");
        var rows = new StackPanel();
        var popup = new Flyout { Placement = PlacementMode.BottomEdgeAlignedLeft, Content = new PixelPanel { Skin = "pack:hud_dropdown_list", Padding = new Thickness(4, 4, 4, 8), MinWidth = width, Child = rows } };
        foreach (var (value, name) in list)
        {
            // Le menu affiché est celui du bouton gardé à l'écran (UiPatch) : on ferme celui qui est ouvert, pas celui de cette construction
            var row = Ui.Button(name, () =>
            {
                CloseOpenFlyout();
                pick(value);
            }, EqualityComparer<T>.Default.Equals(value, current) ? "tab selected" : "tab");
            row.HorizontalAlignment = HorizontalAlignment.Stretch;
            row.HorizontalContentAlignment = HorizontalAlignment.Left;
            rows.Children.Add(row);
        }
        button.Flyout = popup;
        Track(popup, open => frame.Skin = open ? "pack:hud_dropdown:open" : enabled ? "pack:hud_dropdown:normal" : "pack:hud_dropdown:disabled");
        return button;
    }

    /// <summary>Le menu ou la liste déroulante ouvert en ce moment (un seul à la fois), ou null.</summary>
    public static FlyoutBase? OpenFlyout { get; private set; }

    /// <summary>Ferme le menu ouvert, s'il y en a un.</summary>
    public static void CloseOpenFlyout() => OpenFlyout?.Hide();

    /// <summary>Retient le menu ouvert : tant qu'il l'est, les menus ne se mettent pas à jour sous lui (il resterait ouvert sur d'anciennes valeurs).</summary>
    private static void Track(FlyoutBase flyout, Action<bool>? changed = null)
    {
        flyout.Opened += (_, _) =>
        {
            OpenFlyout = flyout;
            changed?.Invoke(true);
        };
        flyout.Closed += (_, _) =>
        {
            if (ReferenceEquals(OpenFlyout, flyout))
                OpenFlyout = null;
            changed?.Invoke(false);
        };
    }

    // ------------------------------------------------------------------ Champ numérique (hud_field_numeric, hud_stepper_up/down)

    /// <summary>Un nombre à flèches : chaque flèche ajoute ou retire « step » dans [min, max] ; la valeur est alignée à droite.</summary>
    public static Control Numeric(int value, int min, int max, int step, Action<int> changed, string unit = "", double width = 120)
    {
        var text = Ui.Text($"{value:N0}{unit}", 12, Palette.Ink, wrap: false);
        text.HorizontalAlignment = HorizontalAlignment.Right;
        text.VerticalAlignment = VerticalAlignment.Center;
        text.Margin = new Thickness(0, 0, 26, 0);
        Control Stepper(string id, int delta)
        {
            bool enabled = delta > 0 ? value < max : value > min;
            var image = new HoverImage
            {
                Normal = PackSkins.Sprite(id, enabled ? "normal" : "disabled"), Hover = enabled ? PackSkins.Sprite(id, "hover") : null,
                Pressed = enabled ? PackSkins.Sprite(id, "pressed") : null, Width = 20, Height = 14,
            };
            var button = new Button { Content = image, Padding = new Thickness(0), IsEnabled = enabled, Tag = id };
            button.Classes.Add("plain");
            return Ui.OnClick(button, () => changed(Math.Clamp(value + delta, min, max)));
        }
        var arrows = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 3, 0),
            Children = { Stepper("hud_stepper_up", step), Stepper("hud_stepper_down", -step) } };
        return new PixelPanel { Skin = "pack:hud_field_numeric:normal", Width = width, Height = 32, Padding = new Thickness(6, 2, 2, 4), Child = new Panel { Children = { text, arrows } } };
    }

    // ------------------------------------------------------------------ Champ multiligne (hud_field_multiline)

    /// <summary>Un texte sur plusieurs lignes (notes), dans le cadre multiligne du pack ; chaque modification est rendue.</summary>
    public static Control Multiline(string value, Action<string> changed, string? watermark = null, double height = 96)
    {
        var box = new TextBox
        {
            Text = value, PlaceholderText = watermark, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = height,
            Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(8, 6, 14, 14),
        };
        box.Classes.Add("bare");
        var frame = new PixelPanel { Skin = "pack:hud_field_multiline:normal", HoverSkin = "pack:hud_field_multiline:hover", FocusSkin = "pack:hud_field_multiline:focus", Child = box };
        box.TextChanged += (_, _) => changed(box.Text ?? "");
        return frame;
    }

    // ------------------------------------------------------------------ Menu (hud_menu_panel, hud_menu_item, hud_menu_separator)

    /// <summary>Une entrée de menu : libellé, action, possible ou non ; null : un séparateur.</summary>
    public record MenuEntry(string Label, Action Run, bool Enabled = true, string? Icon = null);

    /// <summary>Un menu du pack (clic droit, ou bouton « … »), qui se ferme après un choix.</summary>
    public static Flyout Menu(IEnumerable<MenuEntry?> entries)
    {
        var column = new StackPanel { MinWidth = 200 };
        var flyout = new Flyout { Placement = PlacementMode.Pointer, Content = new PixelPanel { Skin = "pack:hud_menu_panel", Padding = new Thickness(4, 4, 4, 8), Child = column } };
        Track(flyout);
        foreach (var entry in entries)
        {
            if (entry == null)
            {
                column.Children.Add(new PixelPanel { Skin = "pack:hud_menu_separator", Height = 10, Margin = new Thickness(4, 2) });
                continue;
            }
            var frame = new PixelPanel { Skin = entry.Enabled ? "pack:hud_menu_item:normal" : "pack:hud_menu_item:disabled", HoverSkin = entry.Enabled ? "pack:hud_menu_item:hover" : null, Padding = new Thickness(24, 3, 10, 5),
                Child = Ui.Row(6, entry.Icon != null ? Ui.Image(Icons.Get(entry.Icon, "#2b2128"), 16, 16) : null, Ui.Text(entry.Label, 12, entry.Enabled ? Palette.Ink : Palette.Fog, wrap: false)) };
            var item = new Button { Content = frame, Padding = new Thickness(0), IsEnabled = entry.Enabled, HorizontalAlignment = HorizontalAlignment.Stretch, Tag = entry.Label };
            item.Classes.Add("plain");
            item.Click += (_, _) =>
            {
                flyout.Hide();
                entry.Run();
            };
            column.Children.Add(item);
        }
        return flyout;
    }

    /// <summary>Le menu s'ouvre au clic droit sur ce contrôle.</summary>
    public static T WithContextMenu<T>(T control, Func<IEnumerable<MenuEntry?>> entries) where T : Control
    {
        control.PointerPressed += (_, e) =>
        {
            var point = e.GetCurrentPoint(control).Properties;
            if (point.IsRightButtonPressed || point.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
            {
                Menu(entries()).ShowAt(control, true);
                e.Handled = true;
            }
        };
        return control;
    }

    /// <summary>Le bouton « … » (icon_more) qui ouvre le même menu au clic gauche.</summary>
    public static Button MoreButton(Func<IEnumerable<MenuEntry?>> entries)
    {
        Button? button = null;
        button = Ui.IconButton("icon_more", () => Menu(entries()).ShowAt(button!), "Plus d'actions");
        return button;
    }

    // ------------------------------------------------------------------ Jauge (hud_gauge_base, hud_gauge_fill, hud_gauge_segment_map)

    private static readonly Dictionary<int, Bitmap> Gauges = new();

    /// <summary>
    /// Une jauge à dix segments (règle du pack : gauge_fill là où le rouge de segment_map ≤ N, N = valeur × 10, gauge_base
    /// ailleurs), composée une fois par niveau ; affichée à l'échelle entière ×2.
    /// </summary>
    public static Image Gauge(double fraction, string? tip = null)
    {
        int n = (int)Math.Round(Math.Clamp(fraction, 0, 1) * 10);
        if (!Gauges.TryGetValue(n, out var bitmap))
        {
            var basis = PackLibrary.Png(PackLibrary.StateFile(PackLibrary.Asset("hud_gauge_base"), null));
            var fill = PackLibrary.Png(PackLibrary.StateFile(PackLibrary.Asset("hud_gauge_fill"), null));
            var map = PackLibrary.Png(PackLibrary.StateFile(PackLibrary.Asset("hud_gauge_segment_map"), null));
            var img = basis.Clone();
            for (int i = 0; i < img.P.Length; i += 4)
                if (map.P[i + 3] > 0 && map.P[i] <= n && map.P[i] > 0)
                    Array.Copy(fill.P, i, img.P, i, 4);
            Gauges[n] = bitmap = img.ToBitmap();
        }
        var image = Ui.Image(bitmap, 68, 40);
        image.Tag = $"gauge:{n}";
        if (tip != null)
            ToolTip.SetTip(image, tip);
        return image;
    }

    // ------------------------------------------------------------------ Bandeau, puce, séparateurs, panneau secondaire, carte

    /// <summary>Un bandeau d'alerte du pack (corps 9-slice et ses deux pointes) : un message important, en clair.</summary>
    public static Control Banner(string text)
    {
        var left = Ui.Image(PackSkins.Extra("hud_banner", "banner_end_left"), 12, 40);
        var right = Ui.Image(PackSkins.Extra("hud_banner", "banner_end_right"), 12, 40);
        var body = new PixelPanel { Skin = "pack:hud_banner", Height = 40, Padding = new Thickness(10, 6, 10, 10), Child = Ui.Text(text, 12, PixelPanel.CenterColor("pack:hud_banner") is { } c ? Palette.ReadableOn(c) : Palette.Paper, bold: true, wrap: false) };
        return new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Children = { left, body, right }, Tag = "banner" };
    }

    /// <summary>Une puce sombre (type, site, métier, version) au texte clair.</summary>
    public static Control Chip(string text) =>
        new PixelPanel { Skin = "pack:hud_chip_dark", Padding = new Thickness(8, 2, 8, 4), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center,
            Child = Ui.Text(text, 12, PixelPanel.CenterColor("pack:hud_chip_dark") is { } c ? Palette.ReadableOn(c) : Palette.Paper, wrap: false), Tag = "chip" };

    public static Control SeparatorH() => new PixelPanel { Skin = "pack:hud_separator_h", Height = 6, Margin = new Thickness(0, 4), Tag = "separator_h" };

    public static Control SeparatorV() => new PixelPanel { Skin = "pack:hud_separator_v", Width = 6, Margin = new Thickness(6, 0), Tag = "separator_v" };

    /// <summary>Un panneau secondaire (fond un peu plus sombre) pour un contenu de second niveau.</summary>
    public static Control Secondary(Control content) => new PixelPanel { Skin = "pack:hud_panel_secondary", Padding = new Thickness(10, 8, 10, 10), Child = content, Tag = "secondary" };

    /// <summary>Une carte du pack (en-tête de 16 px, survol, sélection, désactivée).</summary>
    public static PixelPanel Card(Control content, bool enabled = true, bool selected = false)
    {
        string state = !enabled ? "disabled" : selected ? "selected" : "normal";
        return new PixelPanel
        {
            Skin = $"pack:hud_card:{state}", HoverSkin = enabled && !selected ? "pack:hud_card:hover" : null,
            Padding = new Thickness(10, 10, 10, 14), Child = content, Tag = "card",
        };
    }

    // ------------------------------------------------------------------ Barres de graphique (hud_chart_bar)

    private static readonly Dictionary<string, Bitmap> BarTints = new();

    /// <summary>
    /// Un graphique en barres : une barre par valeur, hauteur = valeur réelle (positive vers le haut, négative vers le bas),
    /// teintée par multiplication selon le sens (succès, danger), libellé dessous.
    /// </summary>
    public static Control Bars(IReadOnlyList<(string Label, decimal Value)> values, double height = 80)
    {
        decimal max = Math.Max(1, values.Count == 0 ? 1 : values.Max(v => Math.Abs(v.Value)));
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Tag = "bars" };
        // Pas de moitié basse vide quand rien n'est négatif : l'étiquette restait loin sous les barres
        bool anyNegative = values.Any(v => v.Value < 0);
        foreach (var (label, value) in values)
        {
            string hex = value >= 0 ? "#62a04b" : "#c8453d";
            if (!BarTints.TryGetValue(hex, out var tinted))
            {
                var img = PackLibrary.Png(PackLibrary.StateFile(PackLibrary.Asset("hud_chart_bar"), null)).Clone();
                var c = Color.Parse(hex);
                for (int i = 0; i < img.P.Length; i += 4)
                {
                    img.P[i] = (byte)(img.P[i] * c.R / 255);
                    img.P[i + 1] = (byte)(img.P[i + 1] * c.G / 255);
                    img.P[i + 2] = (byte)(img.P[i + 2] * c.B / 255);
                }
                BarTints[hex] = tinted = img.ToBitmap();
            }
            double h = Math.Max(4, (double)(Math.Abs(value) / max) * height / 2);
            var bar = new Image { Source = tinted, Width = 24, Height = h, Stretch = Stretch.Fill, Tag = $"bar:{value}" };
            RenderOptions.SetBitmapInterpolationMode(bar, BitmapInterpolationMode.None);
            var top = new Border { Height = height / 2, Child = value >= 0 ? bar : null, VerticalAlignment = VerticalAlignment.Bottom };
            var bottom = new Border { Height = anyNegative ? height / 2 : 0, Child = value < 0 ? bar : null };
            if (value >= 0)
                bar.VerticalAlignment = VerticalAlignment.Bottom;
            else
                bar.VerticalAlignment = VerticalAlignment.Top;
            var caption = Ui.Text(label, 12, Palette.Fog, wrap: false);
            caption.HorizontalAlignment = HorizontalAlignment.Center;
            var amount = Ui.Text($"{value:N0}", 12, value >= 0 ? Palette.Leaf : Palette.Blood, wrap: false);
            amount.HorizontalAlignment = HorizontalAlignment.Center;
            var column = new StackPanel { Width = 92, Spacing = 2, Children = { top, new PixelPanel { Skin = "pack:hud_separator_h", Height = 2 }, bottom, caption, amount } };
            ToolTip.SetTip(column, $"{label} : {value:N0} CHF");
            row.Children.Add(column);
        }
        return row;
    }

    /// <summary>Une barre « en cours » : le remplissage rayé du pack, pour un travail qui avance en ce moment.</summary>
    public static Control Striped(double fraction, double height = 8)
    {
        return new PixelPanel
        {
            Skin = "pack:hud_progress_track", Scale = 1, Height = Math.Max(8, height), MinWidth = 40, Padding = new Thickness(1), Tag = "striped",
            Child = Ui.Fill("pack:hud_progress_fill_striped", fraction),
        };
    }

    /// <summary>Un contenu qui défile à l'horizontale (barre du pack hud_scroll_*_h, flèches gauche et droite).</summary>
    public static ScrollViewer Horizontal(Control content) => new()
    {
        Content = content,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
        Tag = "horizontal",
    };
}
