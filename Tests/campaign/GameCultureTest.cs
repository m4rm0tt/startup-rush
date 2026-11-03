using System.Globalization;
using Core;

namespace Tests.campaign;

/// <summary>Le format des nombres ne dépend pas de la langue du système (lot 3 de la version Steam).</summary>
public class GameCultureTest
{
    [TestCase("en-US")]
    [TestCase("de-DE")]
    [TestCase("fr-FR")]
    public void AmountsLookTheSameWhateverTheSystemLanguage(string system)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(system);
            GameCulture.Install();
            Assert.That($"{87760m:N0} CHF", Is.EqualTo("87 760 CHF"));
            Assert.That($"{-1234567.5:N1}", Is.EqualTo("-1 234 567,5"));
            Assert.That($"{0.39:P0}", Is.EqualTo("39 %").Or.EqualTo("39 %").Or.EqualTo("39 %"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            GameCulture.Install();
        }
    }
}
