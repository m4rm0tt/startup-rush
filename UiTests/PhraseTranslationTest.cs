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

    private static string PhrasesFolder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "StartupRush.sln")))
            dir = dir.Parent;
        return Path.Combine(dir!.FullName, "UI", "Lang", "phrases", "en");
    }

    /// <summary>Un même texte français a une seule traduction (pas deux fichiers qui se contredisent), avec les mêmes trous, et rien n'est vide.</summary>
    [Test]
    public void PhraseFilesDoNotContradictEachOtherAndKeepTheirHoles()
    {
        var holes = new System.Text.RegularExpressions.Regex(@"\{(\d+)\}");
        var seen = new Dictionary<string, (string File, string Value)>();
        var problems = new List<string>();
        foreach (string file in Directory.GetFiles(PhrasesFolder(), "*.json"))
        {
            var table = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file))!;
            foreach (var (french, english) in table)
            {
                if (string.IsNullOrWhiteSpace(english))
                    problems.Add($"{Path.GetFileName(file)} : « {french} » n'a pas de traduction");
                var a = holes.Matches(french).Select(m => m.Value).Distinct().OrderBy(x => x);
                var b = holes.Matches(english).Select(m => m.Value).Distinct().OrderBy(x => x);
                if (!a.SequenceEqual(b))
                    problems.Add($"{Path.GetFileName(file)} : trous différents dans « {french} »");
                if (seen.TryGetValue(french, out var other) && other.Value != english)
                    problems.Add($"« {french} » traduit deux fois : {other.File} « {other.Value} » et {Path.GetFileName(file)} « {english} »");
                seen[french] = (Path.GetFileName(file), english);
            }
        }
        Assert.That(problems, Is.Empty, string.Join("\n", problems.Take(20)));
    }
}
