using Avalonia.Controls;
using Avalonia.Input;
using Core.progression;
using Core.states;
using UI.ViewModels;

namespace UI.Views;

public partial class MenuView : UserControl, IGameScreen
{
    private readonly MenuViewModel _viewModel;

    public MenuView(MenuState state)
    {
        InitializeComponent();
        _viewModel = new MenuViewModel(state);
        DataContext = _viewModel;

        QuitButton.Click += (_, _) => (TopLevel.GetTopLevel(this) as Window)?.Close();

        // Le décor du menu : le studio miteux, la nuit, sous la pluie
        Backdrop.FillArea();
        Backdrop.Render(new SceneLook(0, 0, 0, 0, 0, CareerStage.Broke, 0, false));
    }

    public void Tick(double deltaTime)
    {
        Backdrop.Animate(new SceneMood(true, 0.9, false, false));
    }

    public bool HandleKey(Key key)
    {
        switch (key)
        {
            case Key.Escape when !_viewModel.IsTitle:
                _viewModel.Show("title");
                return true;
            case Key.Enter when _viewModel.IsTitle:
                _viewModel.Show("select");
                return true;
            case Key.Enter when _viewModel.IsSelect:
                _viewModel.Start();
                return true;
            default:
                return false;
        }
    }
}
