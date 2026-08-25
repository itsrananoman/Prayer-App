using Microsoft.Extensions.DependencyInjection;
using Prayer.Data;
using Prayer.Interop;
using Prayer.Services;
using Prayer.ViewModels;
using Prayer.Views;
using System.IO;
using System.Threading;
using System.Windows;

namespace Prayer;

public partial class App : System.Windows.Application
{
    private static Mutex? _mutex;
    private IServiceProvider? _serviceProvider;
    private TrayIconManager? _trayIconManager;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. Set explicit Application User Model ID for Windows Taskbar, Search, and Toast Notifications
        try
        {
            Win32Native.SetCurrentProcessExplicitAppUserModelID("DevCrafters.Prayer.FocusLock.1.0");
        }
        catch { }

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_crash.log"), ex?.ToString() ?? "Unknown exception");
            System.Windows.MessageBox.Show($"Startup Error:\n{ex?.Message}\n\nStack:\n{ex?.StackTrace}", "Prayer App Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dispatcher_crash.log"), args.Exception.ToString());
            System.Windows.MessageBox.Show($"Application Error:\n{args.Exception.Message}\n\nStack:\n{args.Exception.StackTrace}", "Prayer App Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            // 2. Single instance enforcement
            const string mutexName = "Prayer_SalahFocusLock_SingleInstance_Mutex";
            _mutex = new Mutex(true, mutexName, out bool createdNew);
            if (!createdNew)
            {
                System.Windows.MessageBox.Show("Prayer app is already running in the background. Check your system tray near the clock.", "Prayer App", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);

            // 3. Configure Dependency Injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // 4. Initialize SQLite Database & seed data
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<PrayerDbContext>();
                DatabaseInitializer.Initialize(dbContext);
            }

            // 5. Create Main Shell Window & Tray Icon
            _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();

            _trayIconManager = new TrayIconManager(_mainWindow, mainVm, OpenSettingsPage);

            // Connect pre-lock reminder notifications & taskbar recreation
            mainVm.SetNotificationHandler((title, message) => _trayIconManager.ShowNotification(title, message));
            _mainWindow.SetTaskbarCreatedCallback(() => _trayIconManager.OnTaskbarCreated());

            // 6. Check if launched with --minimized flag (from autostart)
            bool startMinimized = e.Args.Any(arg => arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase));
            if (!startMinimized)
            {
                _mainWindow.Show();
                _mainWindow.Activate();
            }
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_crash.log"), ex.ToString());
            System.Windows.MessageBox.Show($"Critical Startup Failure:\n{ex.Message}\n\n{ex.StackTrace}", "Prayer App Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // DbContext
        services.AddDbContext<PrayerDbContext>();

        // Core Services
        services.AddSingleton<IPrayerApiService, PrayerApiService>();
        services.AddSingleton<IPrayerCalculationService, PrayerCalculationService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IVerseService, VerseService>();
        services.AddSingleton<IAudioService, AudioService>();
        services.AddSingleton<IQuranService, QuranService>();
        services.AddSingleton<KeyboardHookService>();
        services.AddSingleton<LockManager>();

        // Page ViewModels
        services.AddSingleton<MainViewModel>(sp => new MainViewModel(
            sp.GetRequiredService<IPrayerApiService>(),
            sp.GetRequiredService<IPrayerCalculationService>(),
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<IVerseService>(),
            sp.GetRequiredService<LockManager>(),
            OpenSettingsPage
        ));

        services.AddSingleton<PrayerTimesViewModel>();
        services.AddSingleton<QuranViewModel>();
        services.AddSingleton<HadithViewModel>();
        services.AddSingleton<AboutViewModel>();

        services.AddSingleton<SettingsViewModel>(sp => new SettingsViewModel(
            sp.GetRequiredService<ISettingsService>(),
            sp.GetRequiredService<IAudioService>(),
            onSettingsSaved: () =>
            {
                var mainVm = sp.GetRequiredService<MainViewModel>();
                _ = mainVm.LoadDataAsync(forceApi: true);
            }
        ));

        // Shell ViewModel
        services.AddSingleton<ShellViewModel>();

        // Windows
        services.AddSingleton<MainWindow>();
    }

    public void OpenSettingsPage()
    {
        if (_mainWindow != null)
        {
            _mainWindow.NavigateTo(NavPage.Settings);
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconManager?.Dispose();
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (_mutex != null)
        {
            try { _mutex.ReleaseMutex(); } catch { }
            _mutex.Dispose();
        }

        base.OnExit(e);
    }
}
