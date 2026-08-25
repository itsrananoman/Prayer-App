using System.Globalization;
using Prayer.Models;

namespace Prayer.Services;

public class PrayerCalculationService : IPrayerCalculationService
{
    private static readonly Dictionary<string, string> UrduPrayerNames = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Fajr", "فجر" },
        { "Sunrise", "طلوع آفتاب" },
        { "Dhuhr", "ظہر" },
        { "Asr", "عصر" },
        { "Sunset", "غروب آفتاب" },
        { "Maghrib", "مغرب" },
        { "Isha", "عشاء" }
    };

    public EffectivePrayerTimes GetEffectiveTimes(PrayerTimeRecord record, IEnumerable<ManualOverride> overrides)
    {
        var result = new EffectivePrayerTimes
        {
            Date = record.Date,
            Fajr = ParseTime(record.Fajr, new TimeSpan(5, 0, 0), "Fajr"),
            Sunrise = ParseTime(record.Sunrise, new TimeSpan(6, 15, 0), "Sunrise"),
            Dhuhr = ParseTime(record.Dhuhr, new TimeSpan(12, 30, 0), "Dhuhr"),
            Asr = ParseTime(record.Asr, new TimeSpan(16, 45, 0), "Asr"),
            Sunset = ParseTime(record.Sunset, new TimeSpan(18, 45, 0), "Sunset"),
            Maghrib = ParseTime(record.Maghrib, new TimeSpan(19, 0, 0), "Maghrib"),
            Isha = ParseTime(record.Isha, new TimeSpan(20, 15, 0), "Isha")
        };

        var activeOverrides = overrides.Where(o => o.IsEnabled).ToList();
        foreach (var ov in activeOverrides)
        {
            if (!string.IsNullOrEmpty(ov.EffectiveDate) && ov.EffectiveDate != record.Date)
            {
                continue;
            }

            var prayerName = ov.PrayerName.Trim();
            var parsedTime = ParseTime(ov.Time, TimeSpan.Zero, prayerName);
            if (parsedTime != TimeSpan.Zero)
            {
                switch (prayerName.ToLowerInvariant())
                {
                    case "fajr": result.Fajr = parsedTime; break;
                    case "dhuhr": result.Dhuhr = parsedTime; break;
                    case "asr": result.Asr = parsedTime; break;
                    case "maghrib": result.Maghrib = parsedTime; break;
                    case "isha": result.Isha = parsedTime; break;
                }
            }
        }

        // Chronological consistency safeguards
        // E.g., if Dhuhr is 1:15 (13:15), ensure Dhuhr is in PM
        if (result.Dhuhr.Hours >= 1 && result.Dhuhr.Hours <= 11)
        {
            result.Dhuhr = result.Dhuhr.Add(TimeSpan.FromHours(12));
        }

        // Ensure Asr is after Dhuhr
        if (result.Asr <= result.Dhuhr && result.Asr.Hours <= 11)
        {
            result.Asr = result.Asr.Add(TimeSpan.FromHours(12));
        }

        // Ensure Maghrib is after Asr
        if (result.Maghrib <= result.Asr && result.Maghrib.Hours <= 11)
        {
            result.Maghrib = result.Maghrib.Add(TimeSpan.FromHours(12));
        }

        // Ensure Isha is after Maghrib
        if (result.Isha <= result.Maghrib && result.Isha.Hours <= 11)
        {
            result.Isha = result.Isha.Add(TimeSpan.FromHours(12));
        }

        return result;
    }

    public NextPrayerCalculationResult CalculateNextPrayer(EffectivePrayerTimes times, DateTime currentTime, int lockDurationMinutes)
    {
        var today = currentTime.Date;
        var nowTime = currentTime.TimeOfDay;

        var prayers = new List<(string Name, TimeSpan Time, DateTime FullDateTime)>
        {
            ("Fajr", times.Fajr, today.Add(times.Fajr)),
            ("Dhuhr", times.Dhuhr, today.Add(times.Dhuhr)),
            ("Asr", times.Asr, today.Add(times.Asr)),
            ("Maghrib", times.Maghrib, today.Add(times.Maghrib)),
            ("Isha", times.Isha, today.Add(times.Isha))
        };

        // Determine current active or past prayer
        string activePrayerName = "Isha";
        if (nowTime >= times.Fajr && nowTime < times.Dhuhr) activePrayerName = "Fajr";
        else if (nowTime >= times.Dhuhr && nowTime < times.Asr) activePrayerName = "Dhuhr";
        else if (nowTime >= times.Asr && nowTime < times.Maghrib) activePrayerName = "Asr";
        else if (nowTime >= times.Maghrib && nowTime < times.Isha) activePrayerName = "Maghrib";

        // Find next upcoming prayer
        (string Name, TimeSpan Time, DateTime FullDateTime)? nextPrayer = null;
        (string Name, TimeSpan Time, DateTime FullDateTime)? previousPrayer = null;

        for (int i = 0; i < prayers.Count; i++)
        {
            if (nowTime < prayers[i].Time)
            {
                nextPrayer = prayers[i];
                previousPrayer = i > 0 ? prayers[i - 1] : ("Isha (Yesterday)", times.Isha, today.AddDays(-1).Add(times.Isha));
                break;
            }
        }

        // If after Isha, next prayer is tomorrow's Fajr
        if (nextPrayer == null)
        {
            var tomorrowFajr = today.AddDays(1).Add(times.Fajr);
            nextPrayer = ("Fajr", times.Fajr, tomorrowFajr);
            previousPrayer = prayers[^1]; // Isha
        }

        var remaining = nextPrayer.Value.FullDateTime - currentTime;
        if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

        // Check if any prayer is due now (within 30 seconds)
        bool isDueNow = prayers.Any(p => Math.Abs((currentTime - p.FullDateTime).TotalSeconds) <= 30);

        // Calculate interval progress
        double progress = 0.0;
        if (previousPrayer.HasValue)
        {
            var totalInterval = (nextPrayer.Value.FullDateTime - previousPrayer.Value.FullDateTime).TotalSeconds;
            var elapsed = (currentTime - previousPrayer.Value.FullDateTime).TotalSeconds;
            if (totalInterval > 0)
            {
                progress = Math.Clamp(elapsed / totalInterval, 0.0, 1.0);
            }
        }

        string urduName = UrduPrayerNames.TryGetValue(nextPrayer.Value.Name, out var urdu) ? urdu : nextPrayer.Value.Name;

        return new NextPrayerCalculationResult
        {
            PrayerName = nextPrayer.Value.Name,
            PrayerUrduName = urduName,
            PrayerDisplayTitle = $"{nextPrayer.Value.Name} ({urduName})",
            TargetTime = nextPrayer.Value.FullDateTime,
            TimeRemaining = remaining,
            ProgressFraction = progress,
            FormattedRemaining = remaining.TotalHours >= 1
                ? $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}"
                : $"{remaining.Minutes:D2}:{remaining.Seconds:D2}",
            IsDueNow = isDueNow,
            CurrentActivePrayerName = activePrayerName
        };
    }

    public static TimeSpan ParseTime(string timeStr, TimeSpan defaultFallback, string? prayerName = null)
    {
        if (string.IsNullOrWhiteSpace(timeStr)) return defaultFallback;

        var cleaned = timeStr.Trim();

        // 1. Try parsing with explicit AM/PM formats
        string[] formatsWithAmPm = { "h:mm tt", "hh:mm tt", "h:m tt", "hh:m tt", "h:mmtt", "hh:mmtt" };
        if (DateTime.TryParseExact(cleaned, formatsWithAmPm, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtAmPm))
        {
            return dtAmPm.TimeOfDay;
        }

        // 2. Try standard 24h formats
        string[] formats24h = { "H:mm", "HH:mm", "H:m", "HH:m", "H:mm:ss", "HH:mm:ss" };
        if (DateTime.TryParseExact(cleaned, formats24h, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt24))
        {
            return dt24.TimeOfDay;
        }

        // 3. Fallback to general DateTime parsing
        if (DateTime.TryParse(cleaned, CultureInfo.InvariantCulture, DateTimeStyles.None, out var generalDt))
        {
            return generalDt.TimeOfDay;
        }

        if (TimeSpan.TryParse(cleaned, out var ts))
        {
            return ts;
        }

        return defaultFallback;
    }
}
