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

    /// <summary>Bloc encadré (cadre 9-slice en creux).</summary>
    public static PixelPanel Card(Control content, IBrush? border = null) => new()
    {
        Skin = "inset",
        Padding = new Thickness(10, 8),
        Child = content,
        BorderBrush = border,
        BorderThickness = new Thickness(border != null ? 2 : 0),
    };

    public static Control Section(string title, params Control?[] children)
    {
        var column = Column(6, children);
        column.Children.Insert(0, Text(title.ToUpperInvariant(), 13, Palette.Gold, bold: true));
        column.Margin = new Thickness(0, 6, 0, 2);
        return column;
    }

    public static Control Bar(double fraction, IBrush? color = null, double height = 8, double width = double.NaN)
    {
        var track = new Border { Background = Palette.Slate, Height = height, Width = width, MinWidth = 40 };
        var fill = new Border
        {
            Background = color ?? Palette.Leaf,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        track.Child = fill;
        track.SizeChanged += (_, e) => fill.Width = Math.Clamp(fraction, 0, 1) * e.NewSize.Width;
        return track;
    }

    public static Image Icon(string key, double size = 24) => Image(Icons.Get(key), size, size);

    public static Image Image(IImage bitmap, double width, double height)
    {
        var image = new Image { Source = bitmap, Width = width, Height = height, Stretch = Stretch.Uniform };
        RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.None);
        return image;
    }

    public static Border Tag(string text, IBrush background, IBrush? foreground = null) => new()
    {
        Background = background,
        Padding = new Thickness(6, 1),
        Child = Text(text, 13, foreground ?? Palette.Ink, bold: true, wrap: false),
        VerticalAlignment = VerticalAlignment.Center,
    };

    public static TextBox Input(string value, Action<string> changed, string? watermark = null, double width = double.NaN)
    {
        var box = new TextBox { Text = value, PlaceholderText = watermark, Width = width, MinWidth = 120 };
        if (!double.IsNaN(width))
            box.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        box.TextChanged += (_, _) => changed(box.Text ?? "");
        return box;
    }

    /// <summary>Choix parmi quelques valeurs, affichées comme des boutons (le choisi en doré).</summary>
    public static WrapPanel Choice<T>(IEnumerable<(T Value, string Label)> options, T current, Action<T> pick, bool enabled = true)
    {
        var panel = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var (value, label) in options)
        {
            bool selected = EqualityComparer<T>.Default.Equals(value, current);
            var button = Button(label, () => pick(value), selected ? "tab selected" : "tab", enabled);
            button.Margin = new Thickness(0, 0, 4, 4);
            panel.Children.Add(button);
        }
        return panel;
    }

    public static string Chf(decimal amount) => $"{amount:N0} CHF";

    /// <summary>Montant signé lisible : « +1 200 CHF », « −300 CHF ».</summary>
    public static string SignedChf(decimal amount) => amount >= 0 ? $"+{amount:N0} CHF" : $"−{-amount:N0} CHF";

    /// <summary>Section repliée par défaut : les détails pour qui veut comprendre, jamais nécessaires pour jouer.</summary>
    public static Expander Details(string title, Control content, bool open = false) => new()
    {
        Header = Text(title, 14, Palette.Fog, wrap: false),
        Content = content,
        IsExpanded = open,
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
        Margin = new Thickness(0, 6, 0, 0),
    };

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
