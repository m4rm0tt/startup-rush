using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using UI.Pixel;

namespace UiTests;

/// <summary>La police pixel n'est nette qu'en multiples de 12 px, à une échelle entière de pixels physiques (guide du pack).</summary>
public class PixelTextTest
{
    /// <summary>Décision du 4 novembre 2026 (BUG-010) : le texte courant fait 18 px ; 12 px reste pour les minuscules étiquettes, puis la grille de 12.</summary>
    [AvaloniaTest]
    public void BodyTextIs18PixelsAndLargerSizesLandOnTheTwelvePixelGrid()
    {
        Assert.That(new TextBlock { FontSize = 15 }.FontSize, Is.EqualTo(18));
        Assert.That(new TextBlock { FontSize = 12 }.FontSize, Is.EqualTo(18), "le texte demandé à 12 px est du texte courant");
        Assert.That(new TextBlock { FontSize = 18 }.FontSize, Is.EqualTo(18));
        Assert.That(new TextBlock { FontSize = 22 }.FontSize, Is.EqualTo(24));
        Assert.That(new TextBlock { FontSize = 44 }.FontSize, Is.EqualTo(48));
        Assert.That(new TextBlock { FontSize = 8 }.FontSize, Is.EqualTo(12), "jamais sous la taille de dessin");
        Assert.That(new TextBlock { FontSize = 11 }.FontSize, Is.EqualTo(12), "touches et indices minuscules");
        foreach (double size in new[] { 11.0, 22, 30, 40, 64 })
            Assert.That(PixelText.Snap(size) % 12, Is.EqualTo(0), $"{size}");
        foreach (double size in new[] { 13.0, 14, 16, 20 })
            Assert.That(PixelText.Snap(size), Is.EqualTo(18), $"{size}");
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
