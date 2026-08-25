using Prayer.Models;

namespace Prayer.Services;

public class NextPrayerCalculationResult
{
    public string PrayerName { get; set; } = string.Empty;
    public string PrayerUrduName { get; set; } = string.Empty;
    public string PrayerDisplayTitle { get; set; } = string.Empty;
    public DateTime TargetTime { get; set; }
    public TimeSpan TimeRemaining { get; set; }
    public double ProgressFraction { get; set; } // 0.0 to 1.0 between previous prayer and next prayer
    public string FormattedRemaining { get; set; } = string.Empty; // "02:15:30" or "15m 30s"
    public bool IsDueNow { get; set; } // Current time matches the exact scheduled minute
    public string CurrentActivePrayerName { get; set; } = string.Empty;
}

public class EffectivePrayerTimes
{
    public string Date { get; set; } = string.Empty;
    public TimeSpan Fajr { get; set; }
    public TimeSpan Sunrise { get; set; }
    public TimeSpan Dhuhr { get; set; }
    public TimeSpan Asr { get; set; }
    public TimeSpan Sunset { get; set; }
    public TimeSpan Maghrib { get; set; }
    public TimeSpan Isha { get; set; }

    public string FajrFormatted => $"{Fajr.Hours:D2}:{Fajr.Minutes:D2}";
    public string SunriseFormatted => $"{Sunrise.Hours:D2}:{Sunrise.Minutes:D2}";
    public string DhuhrFormatted => $"{Dhuhr.Hours:D2}:{Dhuhr.Minutes:D2}";
    public string AsrFormatted => $"{Asr.Hours:D2}:{Asr.Minutes:D2}";
    public string MaghribFormatted => $"{Maghrib.Hours:D2}:{Maghrib.Minutes:D2}";
    public string IshaFormatted => $"{Isha.Hours:D2}:{Isha.Minutes:D2}";
}

public interface IPrayerCalculationService
{
    EffectivePrayerTimes GetEffectiveTimes(PrayerTimeRecord record, IEnumerable<ManualOverride> overrides);
    NextPrayerCalculationResult CalculateNextPrayer(EffectivePrayerTimes times, DateTime currentTime, int lockDurationMinutes);
}
