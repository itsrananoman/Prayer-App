using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Prayer.Views;

public partial class TrayFlyoutWindow : Window
{
    private readonly Action _openDashboard;
    private readonly Action _openSettings;
    private readonly Action? _testLock;
    private readonly Action _exitApp;

    public TrayFlyoutWindow(Action openDashboard, Action openSettings, Action? testLock, Action exitApp)
    {
        InitializeComponent();
        _openDashboard = openDashboard;
        _openSettings = openSettings;
        _testLock = testLock;
        _exitApp = exitApp;

#if !DEBUG
        if (TestLockMenuItem != null && TestLockMenuItem.Parent is System.Windows.Controls.Panel panel)
        {
            panel.Children.Remove(TestLockMenuItem);
        }
#endif

        Deactivated += (s, e) => Hide();
        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Hide();
            }
        };
    }

    public void UpdateInfo(string nextPrayerInfo)
    {
        if (!string.IsNullOrWhiteSpace(nextPrayerInfo))
        {
            NextPrayerInfoText.Text = nextPrayerInfo;
        }
    }

    public void ShowAtCursor(System.Drawing.Point cursorPoint)
    {
        UpdateLayout();

        // Get the monitor work area where the cursor is located
        var currentScreen = System.Windows.Forms.Screen.FromPoint(cursorPoint);
        var workArea = currentScreen.WorkingArea;

        // Convert WPF DPI scaling
        var source = PresentationSource.FromVisual(this);
        double dpiScaleX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        double dpiScaleY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

        double flyoutWidth = ActualWidth > 0 ? ActualWidth : Width;
        double flyoutHeight = ActualHeight > 0 ? ActualHeight : 240;

        // Default position: Above cursor
        double targetLeft = cursorPoint.X / dpiScaleX - (flyoutWidth / 2);
        double targetTop = (cursorPoint.Y / dpiScaleY) - flyoutHeight - 8;

        // Keep inside horizontal work area bounds
        double workLeft = workArea.Left / dpiScaleX;
        double workRight = workArea.Right / dpiScaleX;
        double workTop = workArea.Top / dpiScaleY;
        double workBottom = workArea.Bottom / dpiScaleY;

        if (targetLeft + flyoutWidth > workRight)
        {
            targetLeft = workRight - flyoutWidth - 10;
        }
        if (targetLeft < workLeft)
        {
            targetLeft = workLeft + 10;
        }

        // If taskbar is on top or cursor is at top of screen
        if (targetTop < workTop)
        {
            targetTop = (cursorPoint.Y / dpiScaleY) + 12;
        }

        // If bottom overflows
        if (targetTop + flyoutHeight > workBottom)
        {
            targetTop = workBottom - flyoutHeight - 10;
        }

        Left = targetLeft;
        Top = targetTop;

        Show();
        Activate();
    }

    private void OnOpenDashboardClick(object sender, RoutedEventArgs e)
    {
        Hide();
        _openDashboard.Invoke();
    }

    private void OnOpenSettingsClick(object sender, RoutedEventArgs e)
    {
        Hide();
        _openSettings.Invoke();
    }

    private void OnTestLockClick(object sender, RoutedEventArgs e)
    {
        Hide();
        _testLock?.Invoke();
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        Hide();
        _exitApp.Invoke();
    }
}
