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

    /// <summary>Ouvre le journal sur les bonnes nouvelles (au lieu de ce qui est à traiter).</summary>
    public NoticesPanel ShowGoodNews()
    {
        _filter = Filter.Good;
        return this;
    }

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
            // Le sens se lit à l'icône, pas seulement à la couleur
            string icon = notice.Severity switch
            {
                Severity.Critical => "icon_error",
                Severity.Warning => "icon_warning",
                Severity.Good => "icon_success",
                _ => "icon_info"
            };
            var text = Ui.Column(2,
                Ui.Row(6, Ui.Text(notice.Title + (notice.Count > 1 ? $" (×{notice.Count})" : ""), 14, notice.Resolved ? Palette.Fog : Palette.Snow, bold: true),
                    notice.Resolved ? Ui.Tag("réglé", Palette.Leaf) : null),
                string.IsNullOrEmpty(notice.Detail) ? null : Ui.Muted(notice.Detail, 12),
                Ui.Muted(GameTime.Format(notice.Minute), 11));
            var mark = Ui.Icon(icon, 24);
            mark.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            mark.Margin = new Thickness(0, 0, 10, 0);
            var content = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
            content.Children.Add(mark);
            Grid.SetColumn(text, 1);
            content.Children.Add(text);
            var button = new Button
            {
                Content = content,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                Padding = new Thickness(10, 6, 10, 9),
            };
            button.Classes.Add("row");
            var n = notice;
            button.Click += (_, _) => Screen.Navigate(n);
            column.Children.Add(button);
        }
        if (column.Children.Count == 1)
            column.Children.Add(Ui.Muted(_filter == Filter.Problems ? "Rien à traiter. Tout roule." : "Rien pour l'instant.", 14));
        return column;
    }
}
