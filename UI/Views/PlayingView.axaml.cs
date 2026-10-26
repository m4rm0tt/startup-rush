using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Core.roguelike;
using Core.states;
using Core.upgrades;
using UI.Audio;
using UI.Pixel;
using UI.ViewModels;

namespace UI.Views;

public partial class PlayingView : UserControl, IGameScreen
{
    private readonly PlayingState _state;
    private readonly PlayingViewModel _viewModel;

    public PlayingView(PlayingState state)
    {
        InitializeComponent();
        _state = state;
        _viewModel = new PlayingViewModel(state);
        DataContext = _viewModel;

        Scene.Clicked += point => Hustle(point);
        state.Session.Company.TaskCompleted += task =>
        {
            Scene.ShowFloater($"+{Display.Money(task.EarnedReward)}", Palette.Leaf, size: 24);
            Scene.BurstCoins(12);
        };
    }

    public void Tick(double deltaTime)
    {
        _viewModel.Refresh(deltaTime);

        var session = _state.Session;
        var upgrades = session.Upgrades;
        Scene.Render(new SceneLook(
            upgrades[UpgradeTrackId.Computer].Level,
            upgrades[UpgradeTrackId.Screens].Level,
            upgrades[UpgradeTrackId.Chair].Level,
            upgrades[UpgradeTrackId.Coffee].Level,
            upgrades[UpgradeTrackId.Housing].Level,
            session.Career.Stage,
            session.Staff.TotalCount,
            session.Perks.Any(p => p.Id == "cat")));
        Scene.Animate(new SceneMood(_viewModel.IsWorking, _viewModel.DayPhase, _viewModel.InCrisis, _viewModel.InDebt));
    }

    public bool HandleKey(Key key)
    {
        switch (key)
        {
            case Key.Escape:
                _viewModel.TogglePause();
                return true;
            case Key.Space:
                Hustle(null);
                return true;
            case Key.Enter:
                _viewModel.Confirm();
                return true;
            case Key.R:
                _viewModel.Reroll();
                return true;
            case >= Key.D1 and <= Key.D4:
                _viewModel.PickChoice(key - Key.D1);
                return true;
            case >= Key.NumPad1 and <= Key.NumPad4:
                _viewModel.PickChoice(key - Key.NumPad1);
                return true;
            default:
                return false;
        }
    }

    private void Hustle(Point? at)
    {
        if (_state.IsPaused || _state.Session.IsPaused)
            return;

        var result = _state.Session.Hustle();
        var position = at.HasValue ? new Point(at.Value.X - 12, at.Value.Y - 20) : (Point?)null;

        if (result.IsCritical)
        {
            Sfx.Play(Sound.Crit);
            Scene.ShowFloater("COUP DE GÉNIE !", Palette.Ice, position, 20);
            Juice.Shake(4);
        }
        else
        {
            Sfx.Play(Sound.Click, 0.7f);
        }

        if (result.Money > 0)
            Scene.ShowFloater($"+{Display.Money(result.Money)}", Palette.Gold, position, 16);
        else if (result.Work > 0)
            Scene.ShowFloater($"+{result.Work:0.#}s", Palette.Cloud, position, 14);
    }
}
