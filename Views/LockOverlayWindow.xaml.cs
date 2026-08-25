using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Prayer.Interop;

namespace Prayer.Views;

public partial class LockOverlayWindow : Window
{
    public LockOverlayWindow()
    {
        InitializeComponent();

        // Enforce topmost window position
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Topmost = true;
        Activate();
        Focus();

        // Set as topmost via Win32 API to ensure overlay priority
        var helper = new System.Windows.Interop.WindowInteropHelper(this);
        Win32Native.SetWindowPos(
            helper.Handle,
            Win32Native.HWND_TOPMOST,
            0, 0, 0, 0,
            Win32Native.SWP_NOMOVE | Win32Native.SWP_NOSIZE | Win32Native.SWP_SHOWWINDOW);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Allow close only through ViewModel unlock command
        base.OnClosing(e);
    }

    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (DataContext is ViewModels.LockOverlayViewModel vm && vm.IsTestMode && e.Key == Key.Escape)
        {
            vm.DismissTestCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Block Alt+F4 or Escape key presses locally on window
        if ((e.Key == Key.System && e.SystemKey == Key.F4) || e.Key == Key.Escape)
        {
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }
}
