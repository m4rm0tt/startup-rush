using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using UI.Pixel;

namespace UiTests;

/// <summary>La police pixel n'est nette qu'en multiples de 12 px, à une échelle entière de pixels physiques (guide du pack).</summary>
public class PixelTextTest
{
    [AvaloniaTest]
    public void EveryTextSizeLandsOnTheTwelvePixelGrid()
    {
        Assert.That(new TextBlock { FontSize = 15 }.FontSize, Is.EqualTo(12));
        Assert.That(new TextBlock { FontSize = 18 }.FontSize, Is.EqualTo(24));
        Assert.That(new TextBlock { FontSize = 44 }.FontSize, Is.EqualTo(48));
        Assert.That(new TextBlock { FontSize = 8 }.FontSize, Is.EqualTo(12), "jamais sous la taille de dessin");
        foreach (double size in new[] { 11.0, 13, 14, 16, 20, 22, 30, 40, 64 })
            Assert.That(PixelText.Snap(size) % 12, Is.EqualTo(0), $"{size}");
    }

    [Test]
    public void TheAutomaticScaleKeepsWholePhysicalPixels()
    {
        Assert.That(PixelText.CrispScale(1.047, 1), Is.EqualTo(1), "1600 × 900 : échelle 1");
        Assert.That(PixelText.CrispScale(1.256, 1), Is.EqualTo(1), "1920 × 1080 : échelle 1");
        Assert.That(PixelText.CrispScale(1.5, 1.5) * 1.5, Is.EqualTo(2), "Windows 150 % : 2 pixels physiques par pixel du pack");
        Assert.That(PixelText.CrispScale(1.0, 1.25) * 1.25, Is.EqualTo(1), "Windows 125 % : 1 pixel physique");
        Assert.That(PixelText.CrispScale(0.8, 1), Is.EqualTo(0.8), "fenêtre trop petite : on rétrécit pour que tout tienne");
    }
}
