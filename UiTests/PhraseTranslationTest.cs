using UI;

namespace UiTests;

/// <summary>Le moteur de traduction des phrases (Loc.Translate) : phrases exactes, à trous, plusieurs lignes ou phrases.</summary>
public class PhraseTranslationTest
{
    private string _previous = "fr";

    [SetUp]
    public void SetUp() { _previous = Loc.Language; Loc.Language = "en"; }

    [TearDown]
    public void TearDown() { Loc.Language = _previous; Loc.RecordMisses = false; Loc.Misses.Clear(); }

    [Test]
    public void FrenchIsLeftAlone()
    {
        Loc.Language = "fr";
        Assert.That(Loc.Translate("__essai exact"), Is.EqualTo("__essai exact"));
    }

    [Test]
    public void ExactAndHolePhrasesAreTranslatedAndHolesKeepTheirValue()
    {
        Assert.That(Loc.Translate("__essai exact"), Is.EqualTo("__test exact"));
        Assert.That(Loc.Translate("__essai Léa puis Site vitrine."), Is.EqualTo("__test Site vitrine then Léa."));
        Assert.That(Loc.Translate("__essai nombre 15 840 CHF"), Is.EqualTo("__test number 15 840 CHF"));
    }

    [Test]
    public void SeveralLinesAndSentencesAreTranslatedOneByOne()
    {
        Assert.That(Loc.Translate("__essai exact\n__essai exact"), Is.EqualTo("__test exact\n__test exact"));
        Assert.That(Loc.Translate("Texte libre. __essai exact"), Is.EqualTo("Texte libre. __test exact"));
    }

    [Test]
    public void AnUntranslatedFrenchTextIsRecordedWhenAskedTo()
    {
        Loc.RecordMisses = true;
        Loc.Translate("Une phrase déjà française que personne n'a traduite");
        Loc.Translate("Plain english text");
        Assert.That(Loc.Misses.Keys, Is.EquivalentTo(new[] { "Une phrase déjà française que personne n'a traduite" }));
    }

    [Test]
    public void TheDefaultBodyTextIs18AndTheLargeOptionIs24()
    {
        bool large = UI.Pixel.PixelText.Large;
        try
        {
            UI.Pixel.PixelText.Large = false;
            Assert.That(new[] { 10d, 11, 12, 13, 15, 16, 20 }.Select(UI.Pixel.PixelText.Snap), Is.EqualTo(new[] { 12d, 12, 18, 18, 18, 18, 18 }));
            Assert.That(UI.Pixel.PixelText.Snap(22), Is.EqualTo(24));
            UI.Pixel.PixelText.Large = true;
            Assert.That(UI.Pixel.PixelText.Snap(14), Is.EqualTo(24));
            Assert.That(UI.Pixel.PixelText.Snap(22), Is.EqualTo(36));
        }
        finally { UI.Pixel.PixelText.Large = large; }
    }
}
