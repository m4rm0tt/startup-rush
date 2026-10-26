using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Core.roguelike;
using Core.states;
using UI.Audio;
using UI.Pixel;
using UI.ViewModels;

namespace UI.Views;

/// <summary>Le résumé de fin de run : ce qui s'est passé, et ce que la run rapporte pour la suite.</summary>
public partial class GameOverView : UserControl, IGameScreen
{
    private readonly GameOverState _state;
    private double _shownLingots;

    public GameOverView(GameOverState state)
    {
        InitializeComponent();
        _state = state;
        var result = state.Result;

        PortraitImage.Source = Portraits.Get(state.Setup.Character.Id);
        GoldImage.Source = Icons.Get("gold");

        string title = result.IsVictory ? "ROI DU MONDE !" : "FAILLITE";
        TitleText.Text = title;
        TitleShadow.Text = title;
        TitleText.Foreground = result.IsVictory ? Palette.Gold : Palette.Blood;
        SubtitleText.Text = result.IsVictory
            ? $"{result.CharacterName} est parti de rien. Aujourd'hui, le monde lui appartient."
            : $"{result.CauseOfDeath}. {result.CharacterName} retourne vivre chez ses parents.";

        StageValue.Text = result.StageTitle;
        TimeValue.Text = Display.Clock(result.SurvivedTime);
        PeakValue.Text = Display.Money(result.PeakNetWorth);
        EarnedValue.Text = Display.Money(result.TotalEarned);
        TasksValue.Text = result.CompletedTasks.ToString();
        CrisesValue.Text = result.CrisesSurvived.ToString();
        PressureValue.Text = result.Pressure.ToString();
        SeedValue.Text = result.Seed.ToString();

        var perks = result.PerkIds
            .Select(id => PerkCatalog.All.FirstOrDefault(p => p.Id == id))
            .Where(p => p != null)
            .Select(p => new PerkCardViewModel(p!))
            .ToList();
        PerksList.ItemsSource = perks;
        NoPerksText.IsVisible = perks.Count == 0;

        foreach (var (label, amount) in state.Reward.Breakdown)
        {
            var line = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            line.Children.Add(new TextBlock { Text = label, FontSize = 17 });
            var value = new TextBlock { Text = $"+{amount}", FontSize = 22, Foreground = Palette.Gold, Classes = { "num" } };
            Grid.SetColumn(value, 1);
            line.Children.Add(value);
            RewardLines.Children.Add(line);
        }

        var badges = new[]
        {
            state.Reward.NewRecord ? "★ NOUVEAU RECORD DE PATRIMOINE" : null,
            state.Reward.PressureUnlocked ? $"★ PRESSION {state.Setup.Pressure + 1} DÉBLOQUÉE" : null,
        };
        BadgesText.Text = string.Join("\n", badges.Where(b => b != null));

        DrawChart(result.NetWorthHistory.ToArray());

        ReplayButton.Click += (_, _) => state.Replay();
        MenuButton.Click += (_, _) => state.BackToMenu();

        Sfx.Play(result.IsVictory ? Sound.Victory : Sound.Defeat);
    }

    public void Tick(double deltaTime)
    {
        // Les lingots défilent comme un compteur de machine à sous
        int target = _state.Reward.Lingots;
        if (_shownLingots < target)
        {
            _shownLingots = Math.Min(target, _shownLingots + Math.Max(1, target * deltaTime * 0.8));
            if ((int)_shownLingots % 3 == 0)
                Sfx.Play(Sound.Hover, 0.6f);
        }
        LingotsText.Text = $"{(int)_shownLingots}";
    }

    public bool HandleKey(Key key)
    {
        switch (key)
        {
            case Key.Enter:
                _state.Replay();
                return true;
            case Key.Escape:
                _state.BackToMenu();
                return true;
            default:
                return false;
        }
    }

    /// <summary>Courbe du patrimoine en échelle logarithmique : on voit aussi bien les 500 CHF du début que le milliard.</summary>
    private void DrawChart(double[] history)
    {
        if (history.Length < 2)
            return;

        double width = Chart.Width;
        double height = Chart.Height;
        double[] logs = history.Select(v => Math.Log10(Math.Max(1, v))).ToArray();
        double min = logs.Min();
        double max = Math.Max(logs.Max(), min + 1);

        // Repères horizontaux pour chaque puissance de 10
        for (int power = (int)Math.Ceiling(min); power <= max; power++)
        {
            double y = height - (power - min) / (max - min) * height;
            Chart.Children.Add(new Rectangle { Width = width, Height = 1, Fill = Palette.Slate, [Canvas.TopProperty] = y });
            var label = new TextBlock
            {
                Text = Display.Money(Math.Pow(10, power)),
                FontSize = 12,
                Foreground = Palette.Fog,
                [Canvas.TopProperty] = y - 15,
            };
            Chart.Children.Add(label);
        }

        var points = logs
            .Select((v, i) => new Point(i * width / (logs.Length - 1), height - (v - min) / (max - min) * height))
            .ToList();
        Chart.Children.Add(new Polyline
        {
            Points = points,
            Stroke = _state.Result.IsVictory ? Palette.Gold : Palette.Leaf,
            StrokeThickness = 3,
        });
    }
}
