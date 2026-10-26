using System;
using Avalonia.Media;
using Core.requests;

namespace UI.ViewModels;

/// <summary>Formats et couleurs partagés par les écrans.</summary>
public static class Display
{
    public static readonly IBrush Normal = new SolidColorBrush(Color.Parse("#CFD8DC"));
    public static readonly IBrush Muted = new SolidColorBrush(Color.Parse("#90A4AE"));
    public static readonly IBrush Good = new SolidColorBrush(Color.Parse("#66BB6A"));
    public static readonly IBrush Warning = new SolidColorBrush(Color.Parse("#FFA726"));
    public static readonly IBrush Danger = new SolidColorBrush(Color.Parse("#EF5350"));

    public static string Money(double amount) => $"{amount:N0} CHF";

    public static string Seconds(double seconds) => $"{Math.Max(0, Math.Ceiling(seconds)):0} s";

    public static string Clock(double seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);
        return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }

    public static string Stars(int difficulty) => new string('★', difficulty);

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
