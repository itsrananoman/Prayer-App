using Prayer.Models;
using Prayer.Services;
using System.Windows.Input;

namespace Prayer.ViewModels;

public class ShellViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly MainViewModel _homeViewModel;
    private readonly PrayerTimesViewModel _prayerTimesViewModel;
    private readonly QuranViewModel _quranViewModel;
    private readonly HadithViewModel _hadithViewModel;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly AboutViewModel _aboutViewModel;

    private object _currentPageViewModel;
    private NavPage _selectedNavPage = NavPage.Home;
    private string _lockStatusText = "Focus Lock: App automatically locks full screen at prayer times for Salah.";

    public MainViewModel HomeViewModel => _homeViewModel;
    public PrayerTimesViewModel PrayerTimesViewModel => _prayerTimesViewModel;
    public QuranViewModel QuranViewModel => _quranViewModel;
    public HadithViewModel HadithViewModel => _hadithViewModel;
    public SettingsViewModel SettingsViewModel => _settingsViewModel;
    public AboutViewModel AboutViewModel => _aboutViewModel;

    public object CurrentPageViewModel
    {
        get => _currentPageViewModel;
        set => SetProperty(ref _currentPageViewModel, value);
    }

    public NavPage SelectedNavPage
    {
        get => _selectedNavPage;
        set
        {
            if (SetProperty(ref _selectedNavPage, value))
            {
                OnPropertyChanged(nameof(IsHomeSelected));
                OnPropertyChanged(nameof(IsPrayerTimesSelected));
                OnPropertyChanged(nameof(IsQuranSelected));
                OnPropertyChanged(nameof(IsHadithSelected));
                OnPropertyChanged(nameof(IsSettingsSelected));
                OnPropertyChanged(nameof(IsAboutSelected));
            }
        }
    }

    public bool IsHomeSelected => SelectedNavPage == NavPage.Home;
    public bool IsPrayerTimesSelected => SelectedNavPage == NavPage.PrayerTimes;
    public bool IsQuranSelected => SelectedNavPage == NavPage.Quran;
    public bool IsHadithSelected => SelectedNavPage == NavPage.Hadith;
    public bool IsSettingsSelected => SelectedNavPage == NavPage.Settings;
    public bool IsAboutSelected => SelectedNavPage == NavPage.About;

    public string LockStatusText
    {
        get => _lockStatusText;
        set => SetProperty(ref _lockStatusText, value);
    }

    public ICommand NavigateCommand { get; }

    public ShellViewModel(
        ISettingsService settingsService,
        MainViewModel homeViewModel,
        PrayerTimesViewModel prayerTimesViewModel,
        QuranViewModel quranViewModel,
        HadithViewModel hadithViewModel,
        SettingsViewModel settingsViewModel,
        AboutViewModel aboutViewModel)
    {
        _settingsService = settingsService;
        _homeViewModel = homeViewModel;
        _prayerTimesViewModel = prayerTimesViewModel;
        _quranViewModel = quranViewModel;
        _hadithViewModel = hadithViewModel;
        _settingsViewModel = settingsViewModel;
        _aboutViewModel = aboutViewModel;

        _currentPageViewModel = _homeViewModel;

        NavigateCommand = new RelayCommand(param =>
        {
            if (param is NavPage page)
            {
                NavigateTo(page);
            }
            else if (param is string pageStr && Enum.TryParse<NavPage>(pageStr, true, out var parsed))
            {
                NavigateTo(parsed);
            }
        });
    }

    public async Task InitializeAsync()
    {
        await _homeViewModel.InitializeAsync();
    }

    public void NavigateTo(NavPage page)
    {
        SelectedNavPage = page;
        switch (page)
        {
            case NavPage.Home:
                CurrentPageViewModel = _homeViewModel;
                break;
            case NavPage.PrayerTimes:
                _ = _prayerTimesViewModel.InitializeAsync();
                CurrentPageViewModel = _prayerTimesViewModel;
                break;
            case NavPage.Quran:
                _ = _quranViewModel.InitializeAsync();
                CurrentPageViewModel = _quranViewModel;
                break;
            case NavPage.Hadith:
                _ = _hadithViewModel.InitializeAsync();
                CurrentPageViewModel = _hadithViewModel;
                break;
            case NavPage.Settings:
                _ = _settingsViewModel.LoadSettingsAsync();
                CurrentPageViewModel = _settingsViewModel;
                break;
            case NavPage.About:
                CurrentPageViewModel = _aboutViewModel;
                break;
        }
    }
}
