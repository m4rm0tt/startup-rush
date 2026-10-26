using Avalonia.Controls;
using Avalonia.Input;
using Core.states;

namespace UI.Views;

public partial class MenuView : UserControl
{
    private readonly MenuState _state;

    public MenuView(MenuState state)
    {
        InitializeComponent();
        _state = state;

        NewGameButton.Click += (_, _) => StartNewGame();
        QuitButton.Click += (_, _) => (TopLevel.GetTopLevel(this) as Window)?.Close();
        PlayerNameBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
                StartNewGame();
        };
    }

    private void StartNewGame() => _state.StartNewGame(PlayerNameBox.Text ?? "");
}
