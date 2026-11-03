using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using UI.Iso;

namespace UiTests;

/// <summary>BUG-013 : quatre vitesses, quatre icônes distinctes dans la barre du haut.</summary>
public class SpeedIconTest
{
    [AvaloniaTest]
    public void EachSpeedHasItsOwnIcon()
    {
        var (window, _, _) = Shots.Fresh(tutorial: false);
        var icons = new[] { "icon_speed_1", "icon_speed_2", "icon_speed_3", PackLibrary.SpeedFour }
            .Select(id => PackLibrary.Icon(id, "#2b2128")).ToList();
        Assert.That(icons.Distinct().Count(), Is.EqualTo(4));
        Assert.That(icons[3].PixelSize, Is.EqualTo(icons[2].PixelSize), "même taille que les icônes du pack");
        // Les quatre boutons de vitesse de la barre du haut ont chacun une image différente
        var images = window.GetVisualDescendants().OfType<Button>()
            .Where(b => (ToolTip.GetTip(b) as string)?.StartsWith("Vitesse ") == true)
            .Select(b => b.GetVisualDescendants().OfType<Image>().First().Source).ToList();
        Assert.That(images, Has.Count.EqualTo(4));
        Assert.That(images.Distinct().Count(), Is.EqualTo(4));
        window.Close();
    }
}
