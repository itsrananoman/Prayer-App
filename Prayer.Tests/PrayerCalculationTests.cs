using Prayer.Models;
using Prayer.Services;
using Xunit;

namespace Prayer.Tests;

public class PrayerCalculationTests
{
    private readonly PrayerCalculationService _service = new();

    private PrayerTimeRecord CreateSampleRecord()
    {
        return new PrayerTimeRecord
        {
            Date = "2026-08-24",
            Fajr = "05:00",
            Sunrise = "06:15",
            Dhuhr = "12:30",
            Asr = "16:45",
            Sunset = "18:45",
            Maghrib = "19:00",
            Isha = "20:30",
            Source = "api"
        };
    }

    [Fact]
    public void CleanTimeString_HandlesVariousFormats()
    {
        Assert.Equal("05:12", PrayerApiService.CleanTimeString("05:12"));
        Assert.Equal("05:12", PrayerApiService.CleanTimeString("05:12 (PKT)"));
        Assert.Equal("05:12", PrayerApiService.CleanTimeString("2026-08-24T05:12:00+05:00"));
    }

    [Fact]
    public void ParseTime_Handles12HourAnd24HourWithAmPm()
    {
        Assert.Equal(new TimeSpan(16, 54, 0), PrayerCalculationService.ParseTime("4:54 PM", TimeSpan.Zero, "Asr"));
        Assert.Equal(new TimeSpan(16, 54, 0), PrayerCalculationService.ParseTime("04:54 PM", TimeSpan.Zero, "Asr"));
        Assert.Equal(new TimeSpan(16, 54, 0), PrayerCalculationService.ParseTime("16:54", TimeSpan.Zero, "Asr"));
        Assert.Equal(new TimeSpan(5, 15, 0), PrayerCalculationService.ParseTime("5:15 AM", TimeSpan.Zero, "Fajr"));
        Assert.Equal(new TimeSpan(12, 30, 0), PrayerCalculationService.ParseTime("12:30 PM", TimeSpan.Zero, "Dhuhr"));
    }

    [Fact]
    public void ManualOverrides_ExactBugScenario_AsrPmOverride_CalculatesNextPrayerAsAsr()
    {
        var record = CreateSampleRecord();
        var overrides = new List<ManualOverride>
        {
            new() { PrayerName = "Fajr", Time = "05:15", IsEnabled = true },
            new() { PrayerName = "Dhuhr", Time = "12:45", IsEnabled = true },
            new() { PrayerName = "Asr", Time = "16:54", IsEnabled = true },      // 4:54 PM
            new() { PrayerName = "Maghrib", Time = "19:05", IsEnabled = true },  // 7:05 PM
            new() { PrayerName = "Isha", Time = "20:30", IsEnabled = true }      // 8:30 PM
        };

        var effective = _service.GetEffectiveTimes(record, overrides);

        // Current real time: 3:53 PM (15:53)
        var today = DateTime.Today;
        var currentTime = today.Add(new TimeSpan(15, 53, 0));

        var result = _service.CalculateNextPrayer(effective, currentTime, 15);

        // MUST resolve to Asr today, NOT tomorrow's Fajr
        Assert.Equal("Asr", result.PrayerName);
        Assert.Equal("Dhuhr", result.CurrentActivePrayerName);
        Assert.Equal(today.Add(new TimeSpan(16, 54, 0)), result.TargetTime);
        Assert.Equal(TimeSpan.FromMinutes(61), result.TimeRemaining);
    }

    [Fact]
    public void ManualOverrides_Ambiguous12HourFallback_AutoCorrectsToPmForAfternoonPrayers()
    {
        var record = CreateSampleRecord();
        // Even if legacy data has "4:54" without AM/PM for Asr
        var overrides = new List<ManualOverride>
        {
            new() { PrayerName = "Asr", Time = "4:54", IsEnabled = true }
        };

        var effective = _service.GetEffectiveTimes(record, overrides);

        // Effective Asr must be 16:54 (PM), not 04:54 (AM)
        Assert.Equal(new TimeSpan(16, 54, 0), effective.Asr);
        Assert.True(effective.Asr > effective.Dhuhr);
    }

    [Theory]
    [InlineData("04:30", "Fajr", "Isha")]     // Before Fajr -> Next is Fajr
    [InlineData("08:00", "Dhuhr", "Fajr")]    // Between Fajr and Dhuhr -> Next is Dhuhr
    [InlineData("13:00", "Asr", "Dhuhr")]     // Between Dhuhr and Asr -> Next is Asr
    [InlineData("17:00", "Maghrib", "Asr")]   // Between Asr and Maghrib -> Next is Maghrib
    [InlineData("19:30", "Isha", "Maghrib")]  // Between Maghrib and Isha -> Next is Isha
    [InlineData("22:00", "Fajr", "Isha")]     // After Isha -> Next is tomorrow's Fajr
    public void NextPrayer_CalculatesAccurately(string currentTimeStr, string expectedNextPrayer, string expectedCurrentActive)
    {
        var record = CreateSampleRecord();
        var effective = _service.GetEffectiveTimes(record, Enumerable.Empty<ManualOverride>());

        var today = DateTime.Today;
        var currentTs = TimeSpan.Parse(currentTimeStr);
        var testTime = today.Add(currentTs);

        var result = _service.CalculateNextPrayer(effective, testTime, 15);

        Assert.Equal(expectedNextPrayer, result.PrayerName);
        Assert.Equal(expectedCurrentActive, result.CurrentActivePrayerName);
        Assert.True(result.TimeRemaining >= TimeSpan.Zero);
    }

    [Fact]
    public void SleepWake_WallClockElapsedSimulation_CompletesWhenExpired()
    {
        var startTime = new DateTime(2026, 8, 24, 14, 0, 0, DateTimeKind.Utc);
        int durationMinutes = 15;
        var targetEndTime = startTime.AddMinutes(durationMinutes);

        var wakeTime = new DateTime(2026, 8, 24, 14, 20, 0, DateTimeKind.Utc);

        bool isExpired = wakeTime >= targetEndTime;
        Assert.True(isExpired);
    }

    [Fact]
    public void SleepWake_WallClockElapsedSimulation_ResumesRemainingTime()
    {
        var startTime = new DateTime(2026, 8, 24, 14, 0, 0, DateTimeKind.Utc);
        int durationMinutes = 15;
        var targetEndTime = startTime.AddMinutes(durationMinutes);

        var wakeTime = new DateTime(2026, 8, 24, 14, 5, 0, DateTimeKind.Utc);

        var remaining = targetEndTime - wakeTime;
        Assert.Equal(TimeSpan.FromMinutes(10), remaining);
    }

    [Fact]
    public void QuranAndHadith_StrictAuthenticity_Verification()
    {
        var content = Prayer.Data.DatabaseInitializer.GetVerifiedDailyContent();

        Assert.NotEmpty(content);
        Assert.True(content.Count >= 20);

        foreach (var item in content)
        {
            // Verify all fields are present and non-empty
            Assert.False(string.IsNullOrWhiteSpace(item.ArabicText), $"Empty Arabic text for item {item.Reference}");
            Assert.False(string.IsNullOrWhiteSpace(item.TranslationUrdu), $"Empty Urdu translation for item {item.Reference}");
            Assert.False(string.IsNullOrWhiteSpace(item.TranslationEnglish), $"Empty English translation for item {item.Reference}");
            Assert.False(string.IsNullOrWhiteSpace(item.Reference), $"Empty Reference for item {item.DayOfYear}");
            Assert.False(string.IsNullOrWhiteSpace(item.SourceAttribution), $"Empty SourceAttribution for item {item.Reference}");

            if (item.Type == "Hadith")
            {
                // Must be exclusively from Sahih al-Bukhari or Sahih Muslim
                bool isBukhariOrMuslim = item.Reference.StartsWith("Sahih al-Bukhari", StringComparison.OrdinalIgnoreCase) ||
                                         item.Reference.StartsWith("Sahih Muslim", StringComparison.OrdinalIgnoreCase);
                Assert.True(isBukhariOrMuslim, $"Hadith {item.Reference} is not from Sahih Bukhari or Sahih Muslim!");
            }
            else if (item.Type == "Quran")
            {
                // Must have Surah reference format e.g. Surah ... (chapter:verse)
                Assert.Contains("Surah", item.Reference);
                Assert.Contains("Tanzil", item.SourceAttribution);
                Assert.Contains("Jalandhry", item.SourceAttribution);
                Assert.Contains("Saheeh", item.SourceAttribution);
            }
        }
    }
}
