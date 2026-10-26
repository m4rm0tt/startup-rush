using Avalonia.Controls;
using Core.states;
using UI.ViewModels;

namespace UI.Views;

public partial class PlayingView : UserControl
{
    private readonly PlayingViewModel _viewModel;

    public PlayingView(PlayingState state)
    {
        InitializeComponent();
        _viewModel = new PlayingViewModel(state.Session);
        DataContext = _viewModel;
    }

    /// <summary>Appelé à chaque image par la fenêtre, après la mise à jour du jeu.</summary>
    public void Refresh() => _viewModel.Refresh();
}
