using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Core.persistence;
using UI;

namespace UiTests;

/// <summary>Lot 6 : textes de l'interface en français et en anglais (UI/Lang), repli sur le français, changement de langue.</summary>
public class LocalizationTest
{
    private static List<string> Texts(Window window) =>
        window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").Where(t => t != "").ToList();

    private static readonly Regex Placeholder = new(@"\{(\d+)(:[^}]*)?\}");

    [Test]
    public void EveryFrenchKeyHasAnEnglishTextWithTheSameArguments()
    {
        var fr = Loc.Table("fr");
        var en = Loc.Table("en");
        Assert.That(fr, Has.Count.GreaterThan(100), "la table française est embarquée");
        Assert.That(fr.Keys.Except(en.Keys), Is.Empty, "clés sans traduction anglaise");
        Assert.That(en.Keys.Except(fr.Keys), Is.Empty, "clés anglaises sans texte de référence");
        foreach (var (key, french) in fr)
        {
            Assert.That(en[key], Is.Not.Empty, key);
            var a = Placeholder.Matches(french).Select(m => m.Groups[1].Value).Distinct().OrderBy(x => x);
            var b = Placeholder.Matches(en[key]).Select(m => m.Groups[1].Value).Distinct().OrderBy(x => x);
            Assert.That(b, Is.EqualTo(a), $"arguments de {key}");
        }
    }

    [Test]
    public void AMissingKeyOrLanguageFallsBackToFrenchThenToTheKey()
    {
        string previous = Loc.Language;
        try
        {
            Loc.Language = "de";
            Assert.That(Loc.Language, Is.EqualTo("fr"), "langue inconnue : français");
            Loc.Language = "en";
            Assert.That(Loc.T("title.new"), Is.EqualTo("New game"));
            Assert.That(Loc.T("cle.inconnue"), Is.EqualTo("cle.inconnue"), "une clé manquante reste visible");
            Assert.That(Loc.T("load.deleteText", "Partie A", 3), Is.EqualTo("“Partie A” and its 3 save(s) will disappear from the list."));
            Assert.That(Loc.CompanyNameError(" "), Is.EqualTo("Give your company a name."));
            Assert.That(Loc.CompanyNameError("Studio"), Is.Null);
        }
        finally
        {
            Loc.Language = previous;
        }
        Assert.That(Loc.T("title.new"), Is.EqualTo("Nouvelle partie"));
    }

    [AvaloniaTest]
    public void SwitchingToEnglishRebuildsTheMenusAndTheToolbarAndIsRemembered()
    {
        var (window, game, _) = Shots.Fresh(tutorial: false);
        try
        {
            window.SetLanguage("en");
            Dispatcher.UIThread.RunJobs();
            game.Tick(0.3);
            Assert.That(Texts(window), Has.Some.EqualTo("Projects").And.Some.EqualTo("Finances"), "barre d'outils traduite");
            Assert.That(Settings.Load(window.SettingsPath, Path.Combine(Path.GetTempPath(), "none.json")).Language, Is.EqualTo("en"));
            window.ShowTitle();
            Dispatcher.UIThread.RunJobs();
            Assert.That(Texts(window), Has.Some.EqualTo("New game").And.Some.EqualTo("Settings"));
            window.ShowNewCampaign();
            Dispatcher.UIThread.RunJobs();
            Assert.That(Texts(window), Has.Some.EqualTo("Company name (required)"));
        }
        finally
        {
            window.SetLanguage("fr");
            window.Close();
        }
        Assert.That(new Settings().Language, Is.EqualTo("fr"), "français par défaut");
    }
}
