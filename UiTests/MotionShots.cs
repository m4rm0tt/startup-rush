using Avalonia.Headless.NUnit;

namespace UiTests;

/// <summary>Séquence d'images du jeu en marche (journée qui passe, menus) pour une courte vidéo de présentation.</summary>
[Explicit, Category("Shots")]
public class MotionShots
{
    [AvaloniaTest]
    public void ADayAtTheStudio()
    {
        var (window, game, campaign) = Shots.Fresh(tutorial: false);
        campaign.Ledger.Cash = 300_000;
        campaign.CheatUnlockAll();
        var (_, product) = campaign.CreateProject("Planio", "freelances", Core.products.RevenueModel.Subscription);
        campaign.CheatFinishWork();
        campaign.Launch(product!.Id);
        campaign.CheatHire();
        campaign.AdvanceDays(3);
        for (int i = 0; i < 16; i++)
        {
            campaign.Advance(50);
            game.Tick(0.3);
            Shots.Save(window, $"motion_{i:00}");
        }
        window.Close();
    }
}
