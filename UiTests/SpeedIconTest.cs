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

/// <summary>Lot 3 : échelle automatique selon la résolution.</summary>
public class AutoScaleTest
{
    [TestCase(1280, 720, 1.0, 1.0)]
    [TestCase(1366, 768, 1.0, 1.0)]
    [TestCase(1920, 1080, 1.0, 1.0)]
    [TestCase(2560, 1080, 1.0, 1.0)]
    [TestCase(3840, 2160, 1.0, 2.0)]
    [TestCase(1920, 1080, 2.0, 1.0)] // 4K avec la mise à l'échelle Windows à 200 %
    [TestCase(1000, 600, 1.0, 0.6667)] // petite fenêtre : l'interface rétrécit pour tout montrer
    public void TheAutomaticScaleFitsCommonScreens(double w, double h, double renderScaling, double expected)
    {
        Assert.That(UI.Pixel.PixelText.AutoScale(w, h, renderScaling), Is.EqualTo(expected).Within(0.001));
    }
}
