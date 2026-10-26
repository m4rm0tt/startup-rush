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
        if (result.IsVictory)
        {
            TitleText.Text = "Roi du monde !";
            TitleText.Foreground = Display.Gold;
            SubtitleText.Text = $"{state.Result.CharacterName} est parti de 150 CHF et d'un portable qui rame. " +
                                "Aujourd'hui, le monde lui appartient.";
        }
        else
        {
            TitleText.Text = "Faillite !";
            TitleText.Foreground = Display.Danger;
            SubtitleText.Text = $"Les huissiers ont tout saisi. {state.Result.CharacterName} retourne vivre chez ses parents.";
        }

        StageText.Text = result.StageTitle;
        SurvivedText.Text = Display.Clock(result.SurvivedTime);
        PeakText.Text = Display.Money(result.PeakNetWorth);
        EarnedText.Text = Display.Money(result.TotalEarned);
        CompletedText.Text = result.CompletedTasks.ToString();
        FailedText.Text = result.FailedTasks.ToString();

        ReplayButton.Click += (_, _) => state.Replay();
        MenuButton.Click += (_, _) => state.BackToMenu();
    }
}
