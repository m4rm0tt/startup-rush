namespace Core.campaign;

/// <summary>
/// Le calendrier de la campagne. Le temps avance par minutes de jeu entières depuis le début de la campagne :
/// c'est la seule horloge, sérialisée telle quelle, donc un rechargement reprend exactement au même instant.
/// </summary>
public static class GameTime
{
    public const int MinutesPerHour = 60;
    public const int MinutesPerDay = 24 * MinutesPerHour;

    /// <summary>Lundi 4 janvier 2027, 8 h : le matin du premier jour.</summary>
    public static readonly DateTime Origin = new(2027, 1, 4, 0, 0, 0);
    public const long StartMinute = 8 * MinutesPerHour;

    public static DateTime ToDate(long minute) => Origin.AddMinutes(minute);

    public static long ToMinute(DateTime date) => (long)(date - Origin).TotalMinutes;

    public static long Day(long minute) => minute / MinutesPerDay;

    public static int MinuteOfDay(long minute) => (int)(minute % MinutesPerDay);

    public static int Hour(long minute) => MinuteOfDay(minute) / MinutesPerHour;

    public static bool IsWorkday(long minute)
    {
        var day = ToDate(minute).DayOfWeek;
        return day != DayOfWeek.Saturday && day != DayOfWeek.Sunday;
    }

    /// <summary>Minute de jeu au début (minuit) du jour donné.</summary>
    public static long StartOfDay(long day) => day * MinutesPerDay;

    /// <summary>Ajoute un nombre de jours ouvrés (les week-ends ne comptent pas), en gardant l'heure.</summary>
    public static long AddWorkdays(long minute, int workdays)
    {
        long result = minute;
        int added = 0;
        while (added < workdays)
        {
            result += MinutesPerDay;
            if (IsWorkday(result))
                added++;
        }
        return result;
    }

    /// <summary>Nombre de jours ouvrés entiers entre deux instants (négatif si « to » est avant « from »).</summary>
    public static int WorkdaysBetween(long from, long to)
    {
        if (to < from)
            return -WorkdaysBetween(to, from);
        int count = 0;
        for (long day = Day(from) + 1; day <= Day(to); day++)
            if (IsWorkday(StartOfDay(day)))
                count++;
        return count;
    }

    /// <summary>Clé d'un mois comptable, par exemple « 2027-03 ».</summary>
    public static string MonthKey(long minute) => ToDate(minute).ToString("yyyy-MM");

    public static string Format(long minute)
    {
        var date = ToDate(minute);
        return $"{DayName(date.DayOfWeek)} {date:dd.MM.yyyy} {date:HH:mm}";
    }

    public static string FormatDate(long minute) => ToDate(minute).ToString("dd.MM.yyyy");

    public static string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Lun",
        DayOfWeek.Tuesday => "Mar",
        DayOfWeek.Wednesday => "Mer",
        DayOfWeek.Thursday => "Jeu",
        DayOfWeek.Friday => "Ven",
        DayOfWeek.Saturday => "Sam",
        _ => "Dim"
    };
}
