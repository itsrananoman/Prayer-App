using Prayer.Models;
using Prayer.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Prayer.ViewModels;

public class PrayerDisplayCard : ViewModelBase
{
    private bool _isNext;
    private bool _isCurrent;
    private string _status = "Upcoming";

    public string Name { get; set; } = string.Empty;
    public string UrduName { get; set; } = string.Empty;
    public string FormattedTime { get; set; } = string.Empty;
    public string IconKind { get; set; } = "Moon";

    public bool IsNext
    {
        get => _isNext;
        set => SetProperty(ref _isNext, value);
    }

    public bool IsCurrent
    {
        get => _isCurrent;
        set => SetProperty(ref _isCurrent, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
}

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IPrayerApiService _apiService;
    private readonly IPrayerCalculationService _calcService;
    private readonly ISettingsService _settingsService;
    private readonly IVerseService _verseService;
    private readonly LockManager _lockManager;
    private readonly Action _openSettingsWindow;
    private Action<string, string>? _showNotification;

    private readonly DispatcherTimer _clockTimer;
    private UserSetting _currentSettings = new();
    private EffectivePrayerTimes? _effectiveTimes;
    private DateTime _lastFetchedDate = DateTime.MinValue;
    private string _lastFiredReminderKey = string.Empty;

    private string _currentDateDisplay = string.Empty;
    private string _currentTimeDisplay = string.Empty;
    private string _locationDisplay = "Karachi, PK";
    private string _nextPrayerName = "Zuhr";
    private string _nextPrayerUrduName = "ظہر";
    private string _nextPrayerCountdown = "00:00:00";
    private string _nextPrayerTargetTime = "12:30 PM";
    private double _countdownProgress = 0.0;
    private string _activePrayerBanner = string.Empty;
    private bool _isLoading = false;
    private string _networkStatus = "Connected";

    private DailyVerse _todayVerse = new();

    public ObservableCollection<PrayerDisplayCard> PrayerCards { get; } = new();

    public string CurrentDateDisplay
    {
        get => _currentDateDisplay;
        set => SetProperty(ref _currentDateDisplay, value);
    }

    public string CurrentTimeDisplay
    {
        get => _currentTimeDisplay;
        set => SetProperty(ref _currentTimeDisplay, value);
    }

    public string LocationDisplay
    {
        get => _locationDisplay;
        set => SetProperty(ref _locationDisplay, value);
    }

    public string NextPrayerName
    {
        get => _nextPrayerName;
        set => SetProperty(ref _nextPrayerName, value);
    }

    public string NextPrayerUrduName
    {
        get => _nextPrayerUrduName;
        set => SetProperty(ref _nextPrayerUrduName, value);
    }

    public string NextPrayerCountdown
    {
        get => _nextPrayerCountdown;
        set => SetProperty(ref _nextPrayerCountdown, value);
    }

    public string NextPrayerTargetTime
    {
        get => _nextPrayerTargetTime;
        set => SetProperty(ref _nextPrayerTargetTime, value);
    }

    public double CountdownProgress
    {
        get => _countdownProgress;
        set => SetProperty(ref _countdownProgress, value);
    }

    public string ActivePrayerBanner
    {
        get => _activePrayerBanner;
        set => SetProperty(ref _activePrayerBanner, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string NetworkStatus
    {
        get => _networkStatus;
        set => SetProperty(ref _networkStatus, value);
    }

    public DailyVerse TodayVerse
    {
        get => _todayVerse;
        set => SetProperty(ref _todayVerse, value);
    }

    private string _dailyQuoteText = "Indeed, Prayer restrains from immorality and wrongdoing.";
    private string _dailyQuoteSource = "— Qur'an 29:45";
    private string _dailyQuoteArabic = "إِنَّ الصَّلَاةَ تَنْهَىٰ عَنِ الْفَحْشَاءِ وَالْمُنكَرِ";

    public string DailyQuoteText
    {
        get => _dailyQuoteText;
        set => SetProperty(ref _dailyQuoteText, value);
    }

    public string DailyQuoteSource
    {
        get => _dailyQuoteSource;
        set => SetProperty(ref _dailyQuoteSource, value);
    }

    public string DailyQuoteArabic
    {
        get => _dailyQuoteArabic;
        set => SetProperty(ref _dailyQuoteArabic, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand TestLockPreviewCommand { get; }
    public ICommand TestLockFullCommand { get; }

    public MainViewModel(
        IPrayerApiService apiService,
        IPrayerCalculationService calcService,
        ISettingsService settingsService,
        IVerseService verseService,
        LockManager lockManager,
        Action openSettingsWindow)
    {
        _apiService = apiService;
        _calcService = calcService;
        _settingsService = settingsService;
        _verseService = verseService;
        _lockManager = lockManager;
        _openSettingsWindow = openSettingsWindow;

        RefreshCommand = new RelayCommand(async () => await LoadDataAsync(forceApi: true));
        OpenSettingsCommand = new RelayCommand(openSettingsWindow);
        TestLockPreviewCommand = new RelayCommand(async () => await TriggerLockAsync(isTestPreview: true));
        TestLockFullCommand = new RelayCommand(async () => await TriggerLockAsync(isTestPreview: false));

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += OnClockTick;
        _clockTimer.Start();
    }

    public void SetNotificationHandler(Action<string, string> showNotification)
    {
        _showNotification = showNotification;
    }

    public async Task InitializeAsync()
    {
        await LoadDataAsync(forceApi: false);
    }

    public async Task LoadDataAsync(bool forceApi = false)
    {
        IsLoading = true;
        try
        {
            _currentSettings = await _settingsService.GetSettingsAsync();
            LocationDisplay = $"{_currentSettings.City}, {_currentSettings.Country}";

            var today = DateTime.Today;
            _lastFetchedDate = today;
            CurrentDateDisplay = today.ToString("dddd, dd MMMM yyyy");

            // 1. Fetch Daily Verse
            TodayVerse = await _verseService.GetTodayVerseAsync(today);

            // 2. Fetch or load cached prayer times
            PrayerTimeRecord? record = null;
            if (forceApi)
            {
                record = await _apiService.FetchTimingsByCityAsync(
                    today,
                    _currentSettings.City,
                    _currentSettings.Country,
                    _currentSettings.CalculationMethod);
            }

            if (record == null)
            {
                // Try from local DB context or fetch
                record = await _apiService.FetchTimingsByCityAsync(
                    today,
                    _currentSettings.City,
                    _currentSettings.Country,
                    _currentSettings.CalculationMethod);

                NetworkStatus = record != null ? "Online" : "Offline (Cached)";
            }
            else
            {
                NetworkStatus = "Online";
            }

            if (record == null)
            {
                // Fallback default times if completely offline on first launch
                record = new PrayerTimeRecord
                {
                    Date = today.ToString("yyyy-MM-dd"),
                    Fajr = "05:15",
                    Sunrise = "06:10",
                    Dhuhr = "12:35",
                    Asr = "16:50",
                    Sunset = "18:55",
                    Maghrib = "19:00",
                    Isha = "20:20",
                    Source = "default",
                    City = _currentSettings.City,
                    Country = _currentSettings.Country
                };
            }

            // 3. Apply manual overrides
            var overrides = await _settingsService.GetOverridesAsync();
            _effectiveTimes = _calcService.GetEffectiveTimes(record, overrides);

            // 4. Populate 5 prayer cards
            PopulatePrayerCards(_effectiveTimes);

            // 5. Update countdown & status
            UpdateCountdown();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void PopulatePrayerCards(EffectivePrayerTimes times)
    {
        PrayerCards.Clear();
        PrayerCards.Add(new() { Name = "Fajr", UrduName = "فجر", FormattedTime = FormatTimeSpan(times.Fajr), IconKind = "Sunrise" });
        PrayerCards.Add(new() { Name = "Dhuhr", UrduName = "ظہر", FormattedTime = FormatTimeSpan(times.Dhuhr), IconKind = "Sun" });
        PrayerCards.Add(new() { Name = "Asr", UrduName = "عصر", FormattedTime = FormatTimeSpan(times.Asr), IconKind = "Sunset" });
        PrayerCards.Add(new() { Name = "Maghrib", UrduName = "مغرب", FormattedTime = FormatTimeSpan(times.Maghrib), IconKind = "Moonrise" });
        PrayerCards.Add(new() { Name = "Isha", UrduName = "عشاء", FormattedTime = FormatTimeSpan(times.Isha), IconKind = "Moon" });
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        var dt = DateTime.Today.Add(ts);
        return dt.ToString("hh:mm tt");
    }

    private void OnClockTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        CurrentTimeDisplay = now.ToString("hh:mm:ss tt");

        // Check for midnight rollover
        if (now.Date != _lastFetchedDate)
        {
            _ = LoadDataAsync(forceApi: true);
            return;
        }

        if (_effectiveTimes != null)
        {
            UpdateCountdown();
        }
    }

    private void UpdateCountdown()
    {
        if (_effectiveTimes == null) return;

        var now = DateTime.Now;
        var result = _calcService.CalculateNextPrayer(_effectiveTimes, now, _currentSettings.LockDurationMinutes);

        NextPrayerName = result.PrayerName;
        NextPrayerUrduName = result.PrayerUrduName;
        NextPrayerCountdown = result.FormattedRemaining;
        NextPrayerTargetTime = result.TargetTime.ToString("hh:mm tt");
        CountdownProgress = result.ProgressFraction;
        ActivePrayerBanner = $"موجودہ وقت: {result.CurrentActivePrayerName}";

        // Update card highlight states
        foreach (var card in PrayerCards)
        {
            card.IsNext = string.Equals(card.Name, result.PrayerName, StringComparison.OrdinalIgnoreCase);
            card.IsCurrent = string.Equals(card.Name, result.CurrentActivePrayerName, StringComparison.OrdinalIgnoreCase);
            card.Status = card.IsNext ? "Next Prayer" : (card.IsCurrent ? "Current Time" : "Scheduled");
        }

        // 5-Minute Pre-Lock Reminder Notification
        int reminderLead = _currentSettings.ReminderLeadMinutes;
        if (reminderLead > 0)
        {
            double remainingSeconds = result.TimeRemaining.TotalSeconds;
            double targetSeconds = reminderLead * 60.0;

            string reminderKey = $"{now:yyyy-MM-dd}_{result.PrayerName}_{reminderLead}";
            if (remainingSeconds <= targetSeconds && remainingSeconds > 0 && _lastFiredReminderKey != reminderKey)
            {
                _lastFiredReminderKey = reminderKey;
                string title = $"🕌 {result.PrayerName} ({result.PrayerUrduName}) in {reminderLead} Minutes";
                string message = $"{result.PrayerName} prayer time is in {reminderLead} minutes. Your screen will focus lock shortly for Salah.";
                _showNotification?.Invoke(title, message);
            }
        }

        // Automatic lock trigger when prayer time is reached
        if (result.IsDueNow && !_lockManager.IsLocked)
        {
            _ = _lockManager.StartLockAsync(result.PrayerName, _currentSettings.LockDurationMinutes, _currentSettings, isTestMode: false);
        }
    }

    private async Task TriggerLockAsync(bool isTestPreview)
    {
        if (_lockManager.IsLocked) return;

        int duration = isTestPreview ? 0 : _currentSettings.LockDurationMinutes;
        string prayerName = string.IsNullOrEmpty(NextPrayerName) ? "Zuhr" : NextPrayerName;

        await _lockManager.StartLockAsync(prayerName, duration, _currentSettings, isTestMode: isTestPreview);
    }

    public void Dispose()
    {
        _clockTimer.Stop();
        _clockTimer.Tick -= OnClockTick;
        GC.SuppressFinalize(this);
    }
}
