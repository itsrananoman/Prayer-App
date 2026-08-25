using System.Windows;
using Prayer.Interop;

namespace Prayer.Views;

public partial class LockOverlaySecondary : Window
{
    public LockOverlaySecondary()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Topmost = true;
        var helper = new System.Windows.Interop.WindowInteropHelper(this);
        Win32Native.SetWindowPos(
            helper.Handle,
            Win32Native.HWND_TOPMOST,
            0, 0, 0, 0,
            Win32Native.SWP_NOMOVE | Win32Native.SWP_NOSIZE | Win32Native.SWP_SHOWWINDOW);
    }
}
