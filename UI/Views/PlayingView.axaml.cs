using Avalonia.Controls;
using Core.states;
using Core.upgrades;
using UI.ViewModels;

namespace UI.Views;

public partial class PlayingView : UserControl
{
    private readonly PlayingState _state;
    private readonly PlayingViewModel _viewModel;

    public PlayingView(PlayingState state)
    {
        InitializeComponent();
        _state = state;
        _viewModel = new PlayingViewModel(state.Session);
        DataContext = _viewModel;

        Scene.Clicked += _ => Hustle();
        state.Session.Company.TaskCompleted += task =>
            Scene.ShowFloater($"+{Display.Money(task.EarnedReward)}", Display.Good, size: 22);
    }

    /// <summary>Appelé à chaque image par la fenêtre, après la mise à jour du jeu.</summary>
    public void Refresh()
    {
        _viewModel.Refresh();

        var session = _state.Session;
        var upgrades = session.Upgrades;
        Scene.Render(new SceneLook(
            upgrades[UpgradeTrackId.Computer].Level,
            upgrades[UpgradeTrackId.Screens].Level,
            upgrades[UpgradeTrackId.Chair].Level,
            upgrades[UpgradeTrackId.Coffee].Level,
            upgrades[UpgradeTrackId.Housing].Level,
            session.Career.Stage,
            session.Staff.TotalCount));
        Scene.Animate(_viewModel.IsWorking);
    }

    private void Hustle()
    {
        var result = _state.Session.Hustle();

        if (result.Money > 0)
            Scene.ShowFloater($"+{Display.Money(result.Money)}", Display.Gold);
        else if (result.Work > 0)
            Scene.ShowFloater($"+{result.Work:0.#} s", Display.Normal, size: 13);
    }
}
