using Microsoft.Win32;
using Prayer.Interop;
using Prayer.Models;
using Prayer.ViewModels;
using Prayer.Views;
using System.Windows;

namespace Prayer.Services;

public class LockManager : IDisposable
{
    private readonly KeyboardHookService _keyboardHookService;
    private readonly IAudioService _audioService;
    private readonly IVerseService _verseService;

    private readonly List<Window> _activeOverlayWindows = new();
    private LockOverlayWindow? _primaryWindow;
    private LockOverlayViewModel? _currentViewModel;

    private bool _isLocked = false;
    private DateTime _lockStartTime;
    private DateTime _lockTargetEndTime;
    private int _durationMinutes;
    private string _currentPrayerName = string.Empty;
    private bool _isTestMode = false;

    public bool IsLocked => _isLocked;
    public event Action? LockStarted;
    public event Action? LockDismissed;

    public LockManager(
        KeyboardHookService keyboardHookService,
        IAudioService audioService,
        IVerseService verseService)
    {
        _keyboardHookService = keyboardHookService;
        _audioService = audioService;
        _verseService = verseService;

        // Power mode and display topology subscriptions
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    public async Task StartLockAsync(string prayerName, int durationMinutes, UserSetting settings, bool isTestMode = false)
    {
        if (_isLocked) return;

        _isLocked = true;
        _currentPrayerName = prayerName;
        _durationMinutes = durationMinutes;
        _isTestMode = isTestMode;
        _lockStartTime = DateTime.UtcNow;
        _lockTargetEndTime = _lockStartTime.AddMinutes(durationMinutes);

        // 1. Install low-level keyboard hook
        _keyboardHookService.AllowTestEscape = isTestMode;
        _keyboardHookService.InstallHook();

        // 2. Play Azaan or default chime
        _audioService.PlayAzaan(settings.AzaanFilePath, settings.PlayDefaultChime);

        // 3. Retrieve today's Quranic verse / Hadith
        var verse = await _verseService.GetTodayVerseAsync(DateTime.Now);

        // 4. Create and show overlay windows across all monitors
        await App.Current.Dispatcher.InvokeAsync(() =>
        {
            ShowOverlayWindows(prayerName, durationMinutes, verse, isTestMode);
        });

        LockStarted?.Invoke();
    }

    private void ShowOverlayWindows(string prayerName, int durationMinutes, DailyVerse verse, bool isTestMode)
    {
        CloseAllOverlayWindows();

        var screens = System.Windows.Forms.Screen.AllScreens;
        var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen ?? screens.First();

        _currentViewModel = new LockOverlayViewModel(
            prayerName,
            durationMinutes,
            verse,
            isTestMode,
            onUnlockRequested: () => DismissLock());

        // Spawn Primary Overlay Window
        _primaryWindow = new LockOverlayWindow
        {
            DataContext = _currentViewModel
        };

        _activeOverlayWindows.Add(_primaryWindow);
        _primaryWindow.Show();
        PositionWindowOnScreen(_primaryWindow, primaryScreen);

        // Spawn Secondary Overlay Windows for other connected monitors
        foreach (var screen in screens.Where(s => !s.Primary))
        {
            var secWindow = new LockOverlaySecondary();
            _activeOverlayWindows.Add(secWindow);
            secWindow.Show();
            PositionWindowOnScreen(secWindow, screen);
        }
    }

    private static void PositionWindowOnScreen(Window window, System.Windows.Forms.Screen screen)
    {
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.WindowState = WindowState.Normal;
        window.ResizeMode = ResizeMode.NoResize;
        window.Topmost = true;

        var helper = new System.Windows.Interop.WindowInteropHelper(window);
        if (helper.Handle != IntPtr.Zero)
        {
            Win32Native.SetWindowPos(
                helper.Handle,
                Win32Native.HWND_TOPMOST,
                screen.Bounds.X,
                screen.Bounds.Y,
                screen.Bounds.Width,
                screen.Bounds.Height,
                Win32Native.SWP_SHOWWINDOW);
        }
        else
        {
            window.SourceInitialized += (s, e) =>
            {
                var h = new System.Windows.Interop.WindowInteropHelper(window);
                Win32Native.SetWindowPos(
                    h.Handle,
                    Win32Native.HWND_TOPMOST,
                    screen.Bounds.X,
                    screen.Bounds.Y,
                    screen.Bounds.Width,
                    screen.Bounds.Height,
                    Win32Native.SWP_SHOWWINDOW);
            };
        }
    }

    public void DismissLock()
    {
        if (!_isLocked) return;

        _isLocked = false;

        // 1. Release keyboard hook
        _keyboardHookService.UninstallHook();

        // 2. Stop audio playback
        _audioService.Stop();

        // 3. Close all overlay windows
        App.Current?.Dispatcher.Invoke(() =>
        {
            CloseAllOverlayWindows();
        });

        LockDismissed?.Invoke();
    }

    private void CloseAllOverlayWindows()
    {
        foreach (var win in _activeOverlayWindows)
        {
            try
            {
                win.Close();
            }
            catch { }
        }
        _activeOverlayWindows.Clear();
        _primaryWindow = null;
        _currentViewModel?.Dispose();
        _currentViewModel = null;
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume && _isLocked)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                var now = DateTime.UtcNow;
                if (now >= _lockTargetEndTime)
                {
                    // Duration has already elapsed during sleep
                    _currentViewModel?.ForceCountdownComplete();
                }
                else
                {
                    // Resume with exact remaining wall-clock delta
                    var remaining = _lockTargetEndTime - now;
                    _currentViewModel?.SynchronizeRemainingTime(remaining);
                }

                // Re-verify keyboard hook and multi-monitor window topmost state
                _keyboardHookService.InstallHook();
                foreach (var win in _activeOverlayWindows)
                {
                    win.Topmost = true;
                    win.Activate();
                }
            });
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (_isLocked && _currentViewModel != null)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                // Re-span overlays across current screens
                var verse = _currentViewModel.DailyVerse;
                ShowOverlayWindows(_currentPrayerName, _durationMinutes, verse, _isTestMode);
            });
        }
    }

    public void Dispose()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        DismissLock();
        GC.SuppressFinalize(this);
    }
}
