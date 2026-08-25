using Prayer.Models;
using System.Windows.Input;
using System.Windows.Threading;

namespace Prayer.ViewModels;

public class LockOverlayViewModel : ViewModelBase, IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly Action _onUnlockRequested;
    private int _secondsRemaining;
    private readonly int _totalSeconds;
    private bool _canUnlock = false;
    private string _formattedTime = "15:00";
    private double _progress = 1.0;

    public string PrayerName { get; }
    public string PrayerTitle { get; }
    public DailyVerse DailyVerse { get; }
    public bool IsTestMode { get; }

    public string FormattedTime
    {
        get => _formattedTime;
        private set => SetProperty(ref _formattedTime, value);
    }

    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public bool CanUnlock
    {
        get => _canUnlock;
        private set
        {
            if (SetProperty(ref _canUnlock, value))
            {
                OnPropertyChanged(nameof(StatusMessage));
            }
        }
    }

    public string StatusMessage => CanUnlock
        ? "الحمد للہ — نماز مکمل کرنے کے بعد نیچے دیے گئے بٹن پر کلک کریں"
        : "براہِ کرم نماز ادا فرمائیں — سکرین مقررہ وقت تک مقفل رہے گی";

    public ICommand UnlockCommand { get; }
    public ICommand DismissTestCommand { get; }

    public LockOverlayViewModel(
        string prayerName,
        int durationMinutes,
        DailyVerse dailyVerse,
        bool isTestMode,
        Action onUnlockRequested)
    {
        PrayerName = prayerName;
        PrayerTitle = GetPrayerTitle(prayerName);
        DailyVerse = dailyVerse;
        IsTestMode = isTestMode;
        _onUnlockRequested = onUnlockRequested;

        // If test mode with short preview (e.g. 10 seconds), set seconds accordingly
        _totalSeconds = isTestMode ? 10 : Math.Max(1, durationMinutes * 60);
        _secondsRemaining = _totalSeconds;

        UpdateFormattedTime();

        UnlockCommand = new RelayCommand(ExecuteUnlock, () => CanUnlock);
        DismissTestCommand = new RelayCommand(() => _onUnlockRequested.Invoke());

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_secondsRemaining > 0)
        {
            _secondsRemaining--;
            UpdateFormattedTime();
        }

        if (_secondsRemaining <= 0)
        {
            _timer.Stop();
            CanUnlock = true;
            FormattedTime = "00:00";
            Progress = 0.0;
        }
    }

    private void UpdateFormattedTime()
    {
        int minutes = _secondsRemaining / 60;
        int seconds = _secondsRemaining % 60;
        FormattedTime = $"{minutes:D2}:{seconds:D2}";
        Progress = _totalSeconds > 0 ? (double)_secondsRemaining / _totalSeconds : 0.0;
    }

    public void SynchronizeRemainingTime(TimeSpan remaining)
    {
        if (remaining <= TimeSpan.Zero)
        {
            ForceCountdownComplete();
        }
        else
        {
            _secondsRemaining = (int)remaining.TotalSeconds;
            UpdateFormattedTime();
            CanUnlock = false;
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
        }
    }

    public void ForceCountdownComplete()
    {
        _timer.Stop();
        _secondsRemaining = 0;
        FormattedTime = "00:00";
        Progress = 0.0;
        CanUnlock = true;
    }

    private void ExecuteUnlock()
    {
        if (CanUnlock)
        {
            _onUnlockRequested.Invoke();
        }
    }

    private static string GetPrayerTitle(string prayer)
    {
        return prayer.Trim().ToLowerInvariant() switch
        {
            "fajr" => "فجر کا وقت (Fajr Prayer Time)",
            "dhuhr" => "ظہر کا وقت (Dhuhr Prayer Time)",
            "asr" => "عصر کا وقت (Asr Prayer Time)",
            "maghrib" => "مغرب کا وقت (Maghrib Prayer Time)",
            "isha" => "عشاء کا وقت (Isha Prayer Time)",
            _ => $"{prayer} کا وقت (Prayer Time)"
        };
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        GC.SuppressFinalize(this);
    }
}
