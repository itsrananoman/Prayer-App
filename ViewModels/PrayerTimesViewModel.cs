using Prayer.Models;
using Prayer.Services;
using System.Collections.ObjectModel;

namespace Prayer.ViewModels;

public class PrayerTimesViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IPrayerApiService _apiService;
    private readonly IPrayerCalculationService _calcService;

    private string _locationDisplay = string.Empty;
    private string _calculationMethodName = string.Empty;
    private string _currentDateDisplay = string.Empty;
    private string _sunriseTime = "--:--";
    private string _sunsetTime = "--:--";
    private bool _isLoading = false;

    public ObservableCollection<PrayerDisplayCard> PrayerCards { get; } = new();

    public string LocationDisplay
    {
        get => _locationDisplay;
        set => SetProperty(ref _locationDisplay, value);
    }

    public string CalculationMethodName
    {
        get => _calculationMethodName;
        set => SetProperty(ref _calculationMethodName, value);
    }

    public string CurrentDateDisplay
    {
        get => _currentDateDisplay;
        set => SetProperty(ref _currentDateDisplay, value);
    }

    public string SunriseTime
    {
        get => _sunriseTime;
        set => SetProperty(ref _sunriseTime, value);
    }

    public string SunsetTime
    {
        get => _sunsetTime;
        set => SetProperty(ref _sunsetTime, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public PrayerTimesViewModel(
        ISettingsService settingsService,
        IPrayerApiService apiService,
        IPrayerCalculationService calcService)
    {
        _settingsService = settingsService;
        _apiService = apiService;
        _calcService = calcService;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            LocationDisplay = $"{settings.City}, {settings.Country}";
            CurrentDateDisplay = DateTime.Today.ToString("dddd, dd MMMM yyyy");

            CalculationMethodName = settings.CalculationMethod switch
            {
                1 => "University of Islamic Sciences, Karachi",
                2 => "Islamic Society of North America (ISNA)",
                3 => "Muslim World League (MWL)",
                4 => "Umm Al-Qura University, Makkah",
                5 => "Egyptian General Authority of Survey",
                7 => "Institute of Geophysics, University of Tehran",
                13 => "Diyanet İşleri Başkanlığı, Turkey",
                15 => "Moonsighting Committee Worldwide",
                _ => "Standard Method"
            };

            var record = await _apiService.FetchTimingsByCityAsync(
                DateTime.Today, settings.City, settings.Country, settings.CalculationMethod);

            if (record != null)
            {
                SunriseTime = record.Sunrise;
                SunsetTime = record.Sunset;

                var overrides = await _settingsService.GetOverridesAsync();
                var effective = _calcService.GetEffectiveTimes(record, overrides);

                PrayerCards.Clear();
                PrayerCards.Add(new() { Name = "Fajr", UrduName = "فجر", FormattedTime = FormatTimeSpan(effective.Fajr), IconKind = "Sunrise" });
                PrayerCards.Add(new() { Name = "Dhuhr", UrduName = "ظہر", FormattedTime = FormatTimeSpan(effective.Dhuhr), IconKind = "Sun" });
                PrayerCards.Add(new() { Name = "Asr", UrduName = "عصر", FormattedTime = FormatTimeSpan(effective.Asr), IconKind = "Sunset" });
                PrayerCards.Add(new() { Name = "Maghrib", UrduName = "مغرب", FormattedTime = FormatTimeSpan(effective.Maghrib), IconKind = "Moonrise" });
                PrayerCards.Add(new() { Name = "Isha", UrduName = "عشاء", FormattedTime = FormatTimeSpan(effective.Isha), IconKind = "Moon" });
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        var dt = DateTime.Today.Add(ts);
        return dt.ToString("hh:mm tt");
    }
}
