using System;
using Avalonia.Media;
using Core.requests;
using UI.Pixel;

namespace UI.ViewModels;

/// <summary>Formats et couleurs partagés par les écrans (toutes les couleurs viennent de la palette).</summary>
public static class Display
{
    public static readonly IBrush Normal = Palette.Cloud;
    public static readonly IBrush Muted = Palette.Fog;
    public static readonly IBrush Good = Palette.Leaf;
    public static readonly IBrush Warning = Palette.Gold;
    public static readonly IBrush Danger = Palette.Blood;
    public static readonly IBrush Gold = Palette.Lemon;

    /// <summary>Montant lisible d'un coup d'œil : 1 234 CHF, 456 k CHF, 12,3 M CHF, 1,05 Md CHF.</summary>
    public static string Money(double amount)
    {
        double abs = Math.Abs(amount);
        string sign = amount < 0 ? "−" : "";

        if (abs < 100_000)
            return $"{sign}{abs:N0} CHF";
        if (abs < 1_000_000)
            return $"{sign}{abs / 1_000:0.#} k CHF";
        if (abs < 1_000_000_000)
            return $"{sign}{abs / 1_000_000:0.##} M CHF";
        return $"{sign}{abs / 1_000_000_000:0.##} Md CHF";
    }

    public static string Seconds(double seconds) => $"{Math.Max(0, Math.Ceiling(seconds)):0} s";

    public static string Clock(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }

    public static string Stars(int difficulty) => new string('★', difficulty);

    public static string Multiplier(double value) => $"×{value:0.##}";

    public static string TypeName(RequestType type) => type switch
    {
        RequestType.Website => "Site web",
        RequestType.MobileApp => "App mobile",
        RequestType.Logo => "Logo",
        RequestType.Database => "Base de données",
        RequestType.BugFix => "Correction de bugs",
        _ => type.ToString()
    };
}
