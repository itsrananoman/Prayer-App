using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Prayer.Interop;
using Prayer.ViewModels;
using Prayer.Views;

namespace Prayer.Services;

public class TrayIconManager : IDisposable
{
    public static readonly uint WmTaskbarCreated = Win32Native.RegisterWindowMessage("TaskbarCreated");

    private readonly NotifyIcon _notifyIcon;
    private readonly MainWindow _mainWindow;
    private readonly MainViewModel _mainViewModel;
    private readonly Action _openSettings;
    private readonly TrayFlyoutWindow _flyoutWindow;

    public TrayIconManager(MainWindow mainWindow, MainViewModel mainViewModel, Action openSettings)
    {
        _mainWindow = mainWindow;
        _mainViewModel = mainViewModel;
        _openSettings = openSettings;

        // Custom themed WPF tray flyout menu
        _flyoutWindow = new TrayFlyoutWindow(
            openDashboard: ShowMainWindow,
            openSettings: _openSettings,
            testLock: () => _mainViewModel.TestLockPreviewCommand?.Execute(null),
            exitApp: ExitApplication
        );

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Text = "Prayer — Salah Focus Lock",
            Visible = true
        };

        _notifyIcon.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                string nextInfo = string.IsNullOrWhiteSpace(_mainViewModel.NextPrayerName)
                    ? "Salah Focus Lock"
                    : $"Next: {_mainViewModel.NextPrayerName} ({_mainViewModel.NextPrayerUrduName}) • {_mainViewModel.NextPrayerTargetTime}";

                _flyoutWindow.UpdateInfo(nextInfo);
                _flyoutWindow.ShowAtCursor(System.Windows.Forms.Cursor.Position);
            }
            else if (e.Button == MouseButtons.Left)
            {
                ShowMainWindow();
            }
        };

        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
    }

    public void OnTaskbarCreated()
    {
        try
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Icon = LoadTrayIcon();
            _notifyIcon.Text = "Prayer — Salah Focus Lock";
            _notifyIcon.Visible = true;
        }
        catch { }
    }

    public void ShowNotification(string title, string message)
    {
        try
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            _notifyIcon.ShowBalloonTip(6000);
        }
        catch { }
    }

    public void ShowMainWindow()
    {
        _mainWindow.Show();
        _mainWindow.WindowState = System.Windows.WindowState.Normal;
        _mainWindow.Activate();
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        _mainWindow.RequestExit();
    }

    private static Icon LoadTrayIcon()
    {
        try
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var icoPath = Path.Combine(appDir, "Resources", "Icons", "Prayer.ico");

            if (!File.Exists(icoPath))
            {
                icoPath = Path.Combine(appDir, "Prayer.ico");
            }

            if (!File.Exists(icoPath))
            {
                icoPath = Path.Combine(appDir, "Resources", "Icons", "app_icon.ico");
            }

            if (File.Exists(icoPath))
            {
                // Load native small icon frame from multi-resolution .ico for crisp taskbar rendering
                return new Icon(icoPath, SystemInformation.SmallIconSize);
            }
        }
        catch { }

        return CreateFallbackIcon();
    }

    private static Icon CreateFallbackIcon()
    {
        try
        {
            using var bmp = new Bitmap(32, 32);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Background circle
            using var bgBrush = new SolidBrush(Color.FromArgb(13, 59, 46));
            g.FillEllipse(bgBrush, 2, 2, 28, 28);

            // Gold Crescent
            using var goldBrush = new SolidBrush(Color.FromArgb(212, 175, 55));
            g.FillEllipse(goldBrush, 6, 6, 20, 20);
            g.FillEllipse(bgBrush, 11, 4, 18, 18);

            // Gold Star
            g.FillEllipse(goldBrush, 17, 10, 5, 5);

            var iconHandle = bmp.GetHicon();
            return Icon.FromHandle(iconHandle);
        }
        catch
        {
            return SystemIcons.Application;
        }
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _flyoutWindow?.Close();
        GC.SuppressFinalize(this);
    }
}
