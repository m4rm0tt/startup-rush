using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Core.campaign;
using UI.Pixel;

namespace UI.Hud.Panels;

/// <summary>
/// Le journal en trois catégories : problèmes à traiter (ceux qui sont réglés disparaissent), bonnes nouvelles, informations.
/// Chaque ligne ouvre l'endroit où agir.
/// </summary>
public class NoticesPanel : GamePanel
{
    public override string Title => "Journal";

    private enum Filter { Problems, Good, Info }

    private Filter _filter = Filter.Problems;

    public override Control Build()
    {
        int problems = Campaign.Notices.Count(n => n.IsProblem && !n.Resolved);
        var column = Ui.Column(6, Ui.Choice([(Filter.Problems, $"À traiter ({problems})"), (Filter.Good, "Bonnes nouvelles"), (Filter.Info, "Infos")], _filter, f =>
        {
            _filter = f;
            Refresh();
        }));
        var notices = _filter switch
        {
            Filter.Problems => Campaign.Notices.Where(n => n.IsProblem && !n.Resolved),
            Filter.Good => Campaign.Notices.Where(n => n.Severity == Severity.Good),
            _ => Campaign.Notices.Where(n => n.Severity == Severity.Info || n.IsProblem && n.Resolved),
        };
        foreach (var notice in notices.Reverse().Take(40))
        {
            IBrush color = notice.Severity switch
            {
                Severity.Critical => Palette.Blood,
                Severity.Warning => Palette.Ember,
                Severity.Good => Palette.Leaf,
                _ => Palette.Sky
            };
            var content = Ui.Column(2,
                Ui.Row(6, Ui.Text(notice.Title + (notice.Count > 1 ? $" (×{notice.Count})" : ""), 14, notice.Resolved ? Palette.Fog : Palette.Snow, bold: true),
                    notice.Resolved ? Ui.Tag("réglé", Palette.Leaf) : null),
                string.IsNullOrEmpty(notice.Detail) ? null : Ui.Muted(notice.Detail, 12),
                Ui.Muted(GameTime.Format(notice.Minute), 11));
            var button = new Button
            {
                Content = new Border { BorderBrush = color, BorderThickness = new Thickness(3, 0, 0, 0), Padding = new Thickness(8, 0, 0, 0), Child = content },
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Padding = new Thickness(6, 4, 6, 8),
            };
            button.Classes.Add("tab");
            var n = notice;
            button.Click += (_, _) => Screen.Navigate(n);
            column.Children.Add(button);
        }
        if (column.Children.Count == 1)
            column.Children.Add(Ui.Muted(_filter == Filter.Problems ? "Rien à traiter. Tout roule." : "Rien pour l'instant.", 14));
        return column;
    }
}
