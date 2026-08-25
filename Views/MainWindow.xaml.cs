using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using Prayer.Services;
using Prayer.ViewModels;

namespace Prayer.Views;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _viewModel;
    private bool _isExplicitClose = false;
    private Action? _onTaskbarCreated;

    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += async (s, e) => await _viewModel.InitializeAsync();
    }

    public void NavigateTo(NavPage page)
    {
        _viewModel.NavigateTo(page);
    }

    public void SetTaskbarCreatedCallback(Action onTaskbarCreated)
    {
        _onTaskbarCreated = onTaskbarCreated;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var source = (HwndSource?)PresentationSource.FromVisual(this);
        source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if ((uint)msg == TrayIconManager.WmTaskbarCreated)
        {
            _onTaskbarCreated?.Invoke();
        }
        return IntPtr.Zero;
    }

    public void RequestExit()
    {
        _isExplicitClose = true;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExplicitClose)
        {
            // Minimize to tray instead of closing
            e.Cancel = true;
            Hide();
        }
        else
        {
            base.OnClosing(e);
        }
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
