using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Core.campaign;
using UI.Pixel;

namespace UI.Hud;

/// <summary>
/// Les plateaux des mini-jeux de terminal (Core/campaign/MiniGames.cs) : un écran sombre en police de terminal, une règle, et
/// un geste différent à chaque fois : cocher des lignes d'un journal, passer ou bloquer des requêtes, garder A ou B dans un
/// conflit, taper une commande. Les autres genres (déploiement, recette, calibration) restent dans LaunchView.
/// </summary>
public static class MiniGames
{
    private static readonly IBrush Screen = Palette.Brush("#2b2128");
    private static readonly IBrush Ink = Palette.Brush("#fbf5e6");
    private static readonly IBrush Dim = Palette.Brush("#9c8878");
    private static readonly IBrush Good = Palette.Brush("#8fc062");
    private static readonly IBrush Bad = Palette.Brush("#f29483");
    private static readonly IBrush Warn = Palette.Brush("#fbd769");

    private static FontFamily Mono => (FontFamily)Application.Current!.FindResource("TermFont")!;

    /// <summary>Le plateau d'un genre de terminal, ou null si ce genre se joue ailleurs.</summary>
    public static Control? Board(LaunchChallenge challenge, out Func<double> score)
    {
        switch (challenge.Kind)
        {
            case "logs":
                return Logs(challenge, out score);
            case "firewall" or "merge":
                return Choices(challenge, out score);
            case "command":
                return Command(challenge, out score);
            default:
                score = () => 0;
                return null;
        }
    }

    private static Control Surface(Control content, string title) => new Border
    {
        Background = Screen,
        BorderBrush = Palette.Brush("#6f5f5d"),
        BorderThickness = new Thickness(2),
        Padding = new Thickness(12, 8),
        Child = new StackPanel
        {
            Spacing = 6,
            Children = { new TextBlock { Text = $"$ {title}", FontFamily = Mono, FontSize = 18, Foreground = Dim }, content },
        },
    };

    private static TextBlock Line(string text, IBrush color) =>
        new() { Text = text, FontFamily = Mono, FontSize = 18, Foreground = color, TextWrapping = TextWrapping.Wrap };

    /// <summary>Un journal : chaque ligne se coche (« [x] ») d'un clic.</summary>
    private static Control Logs(LaunchChallenge challenge, out Func<double> score)
    {
        var picked = new HashSet<int>();
        var list = new StackPanel { Spacing = 2 };
        void Paint()
        {
            list.Children.Clear();
            for (int i = 0; i < challenge.Items.Count; i++)
            {
                int index = i;
                string text = challenge.Items[i];
                IBrush color = text.StartsWith("[ERREUR]") ? Bad : text.StartsWith("[ATTENTION]") ? Warn : text.StartsWith("[OK]") ? Good : Ink;
                var row = new Button
                {
                    Content = Line($"{(picked.Contains(i) ? "[x]" : "[ ]")} {text}", color),
                    Padding = new Thickness(4, 1), Background = picked.Contains(i) ? Palette.Brush("#4a3b45") : Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left,
                };
                row.Classes.Add("plain");
                Ui.OnClick(row, () =>
                {
                    if (!picked.Add(index))
                        picked.Remove(index);
                    Paint();
                });
                list.Children.Add(row);
            }
        }
        Paint();
        score = () => challenge.Score(picked.ToList());
        return Surface(list, "tail -f deploiement.log");
    }

    /// <summary>Une ligne par cas, et un bouton qui passe d'une option à l'autre (Passe / Bloqué, Garder A / Garder B).</summary>
    private static Control Choices(LaunchChallenge challenge, out Func<double> score)
    {
        var picked = Enumerable.Repeat(0, challenge.Items.Count).ToList();
        var list = new StackPanel { Spacing = 4 };
        void Paint()
        {
            list.Children.Clear();
            for (int i = 0; i < challenge.Items.Count; i++)
            {
                int index = i;
                var button = Ui.Button(challenge.Options[picked[i]], () =>
                {
                    picked[index] = (picked[index] + 1) % challenge.Options.Count;
                    Paint();
                }, challenge.Kind == "firewall" ? (picked[i] == 0 ? "green" : "red") : picked[i] == 0 ? "" : "gold");
                button.MinWidth = 120;
                var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 10 };
                grid.Children.Add(button);
                var text = Line(challenge.Items[i], Ink);
                text.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(text, 1);
                grid.Children.Add(text);
                list.Children.Add(grid);
            }
        }
        Paint();
        score = () => challenge.Score(picked);
        return Surface(list, challenge.Kind == "firewall" ? "pare-feu --regles" : "git merge");
    }

    /// <summary>La commande à recopier, et le champ où la taper (Entrée n'est pas nécessaire : « Valider » la note).</summary>
    private static Control Command(LaunchChallenge challenge, out Func<double> score)
    {
        var input = new TextBox { FontFamily = Mono, FontSize = 18, Watermark = "tape la commande ici", MinWidth = 420 };
        var column = new StackPanel
        {
            Spacing = 8,
            Children = { Line(challenge.Items[0], Warn), input },
        };
        score = () => challenge.Score(input.Text ?? "");
        Avalonia.Threading.Dispatcher.UIThread.Post(() => input.Focus());
        return Surface(column, "deploiement");
    }
}
