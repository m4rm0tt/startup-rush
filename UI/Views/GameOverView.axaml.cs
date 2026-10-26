using Avalonia.Controls;
using Core.states;
using UI.ViewModels;

namespace UI.Views;

public partial class GameOverView : UserControl
{
    public GameOverView(GameOverState state)
    {
        InitializeComponent();

        var result = state.Result;
        CompanyText.Text = state.CompanyName;
        ScoreText.Text = $"Score : {result.Score:N0}";
        SurvivedText.Text = Display.Clock(result.SurvivedTime);
        EarnedText.Text = Display.Money(result.TotalEarned);
        CompletedText.Text = result.CompletedTasks.ToString();
        FailedText.Text = result.FailedTasks.ToString();

        ReplayButton.Click += (_, _) => state.Replay();
        MenuButton.Click += (_, _) => state.BackToMenu();
    }
}
