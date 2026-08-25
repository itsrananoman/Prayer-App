using Microsoft.Win32;
using Prayer.Models;
using Prayer.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Prayer.ViewModels;

public class CalculationMethodOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CityPresetOption
{
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string DisplayName => $"{City}, {Country}";
}

public class ReminderOption
{
    public int Minutes { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public class ManualOverrideItem : ViewModelBase
{
    private int _selectedHour = 5;
    private string _selectedMinute = "00";
    private string _selectedAmPm = "AM";
    private bool _isEnabled = false;

    public string PrayerName { get; set; } = string.Empty;
    public string PrayerUrduName { get; set; } = string.Empty;

    public ObservableCollection<int> Hours { get; } = new(Enumerable.Range(1, 12));
    public ObservableCollection<string> Minutes { get; } = new(Enumerable.Range(0, 60).Select(m => m.ToString("D2")));
    public ObservableCollection<string> AmPmOptions { get; } = new() { "AM", "PM" };

    public int SelectedHour
    {
        get => _selectedHour;
        set => SetProperty(ref _selectedHour, value);
    }

    public string SelectedMinute
    {
        get => _selectedMinute;
        set => SetProperty(ref _selectedMinute, value);
    }

    public string SelectedAmPm
    {
        get => _selectedAmPm;
        set => SetProperty(ref _selectedAmPm, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string To24HourTime()
    {
        int hour24 = SelectedHour % 12;
        if (string.Equals(SelectedAmPm, "PM", StringComparison.OrdinalIgnoreCase))
        {
            hour24 += 12;
        }

        int minute = int.TryParse(SelectedMinute, out var m) ? m : 0;
        return $"{hour24:D2}:{minute:D2}";
    }

    public void FromTimeString(string timeStr, string defaultAmPm)
    {
        if (string.IsNullOrWhiteSpace(timeStr))
        {
            SelectedAmPm = defaultAmPm;
            return;
        }

        var ts = PrayerCalculationService.ParseTime(timeStr, TimeSpan.Zero, PrayerName);
        if (ts != TimeSpan.Zero || timeStr.Contains("00:00") || timeStr.Contains("12:00"))
        {
            int hour12 = ts.Hours % 12;
            if (hour12 == 0) hour12 = 12;
            SelectedHour = hour12;
            SelectedMinute = ts.Minutes.ToString("D2");
            SelectedAmPm = ts.Hours >= 12 ? "PM" : "AM";
        }
        else
        {
            SelectedAmPm = defaultAmPm;
        }
    }
}

public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IAudioService _audioService;
    private readonly Action _onSettingsSaved;

    private UserSetting _currentSetting = new();
    private string _city = "Karachi";
    private string _country = "PK";
    private CalculationMethodOption? _selectedMethod;
    private int _lockDurationMinutes = 15;
    private ReminderOption? _selectedReminderLead;
    private string _theme = "Dark";
    private string? _azaanFilePath;
    private bool _playDefaultChime = true;
    private bool _autostartEnabled = true;
    private bool _minimizeToTrayOnClose = true;
    private string _statusMessage = string.Empty;

    public ObservableCollection<CalculationMethodOption> CalculationMethods { get; } = new();
    public ObservableCollection<CityPresetOption> CityPresets { get; } = new();
    public ObservableCollection<ReminderOption> ReminderOptions { get; } = new();
    public ObservableCollection<ManualOverrideItem> OverrideItems { get; } = new();

    public string City
    {
        get => _city;
        set => SetProperty(ref _city, value);
    }

    public string Country
    {
        get => _country;
        set => SetProperty(ref _country, value);
    }

    public CalculationMethodOption? SelectedMethod
    {
        get => _selectedMethod;
        set => SetProperty(ref _selectedMethod, value);
    }

    public int LockDurationMinutes
    {
        get => _lockDurationMinutes;
        set => SetProperty(ref _lockDurationMinutes, value);
    }

    public ReminderOption? SelectedReminderLead
    {
        get => _selectedReminderLead;
        set => SetProperty(ref _selectedReminderLead, value);
    }

    public string Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }

    public bool IsDarkTheme
    {
        get => _theme == "Dark";
        set
        {
            Theme = value ? "Dark" : "Light";
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(IsLightTheme));
        }
    }

    public bool IsLightTheme
    {
        get => _theme == "Light";
        set
        {
            Theme = value ? "Light" : "Dark";
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(IsLightTheme));
        }
    }

    public string? AzaanFilePath
    {
        get => _azaanFilePath;
        set
        {
            if (SetProperty(ref _azaanFilePath, value))
            {
                OnPropertyChanged(nameof(AzaanFileNameDisplay));
            }
        }
    }

    public string AzaanFileNameDisplay => string.IsNullOrWhiteSpace(_azaanFilePath)
        ? "No custom file selected (Default Chime)"
        : System.IO.Path.GetFileName(_azaanFilePath);

    public bool PlayDefaultChime
    {
        get => _playDefaultChime;
        set => SetProperty(ref _playDefaultChime, value);
    }

    public bool AutostartEnabled
    {
        get => _autostartEnabled;
        set => SetProperty(ref _autostartEnabled, value);
    }

    public bool MinimizeToTrayOnClose
    {
        get => _minimizeToTrayOnClose;
        set => SetProperty(ref _minimizeToTrayOnClose, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string AboutVersion => "Version 1.1.0";
    public string AboutPublisher => "DevCrafters";
    public string AboutAuthor => "Developed by Rana Noman";

    public ICommand BrowseAzaanFileCommand { get; }
    public ICommand ClearAzaanFileCommand { get; }
    public ICommand TestAudioCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand SelectPresetCommand { get; }

    public SettingsViewModel(
        ISettingsService settingsService,
        IAudioService audioService,
        Action onSettingsSaved)
    {
        _settingsService = settingsService;
        _audioService = audioService;
        _onSettingsSaved = onSettingsSaved;

        BrowseAzaanFileCommand = new RelayCommand(BrowseAzaanFile);
        ClearAzaanFileCommand = new RelayCommand(() => AzaanFilePath = null);
        TestAudioCommand = new RelayCommand(TestAudio);
        SaveSettingsCommand = new RelayCommand(async () => await SaveSettingsAsync());
        SelectPresetCommand = new RelayCommand(param =>
        {
            if (param is CityPresetOption preset)
            {
                City = preset.City;
                Country = preset.Country;
            }
        });

        InitializeOptions();
    }

    private void InitializeOptions()
    {
        CalculationMethods.Add(new() { Id = 1, Name = "University of Islamic Sciences, Karachi" });
        CalculationMethods.Add(new() { Id = 2, Name = "Islamic Society of North America (ISNA)" });
        CalculationMethods.Add(new() { Id = 3, Name = "Muslim World League (MWL)" });
        CalculationMethods.Add(new() { Id = 4, Name = "Umm Al-Qura University, Makkah" });
        CalculationMethods.Add(new() { Id = 5, Name = "Egyptian General Authority of Survey" });
        CalculationMethods.Add(new() { Id = 7, Name = "Institute of Geophysics, University of Tehran" });
        CalculationMethods.Add(new() { Id = 8, Name = "Gulf Region" });
        CalculationMethods.Add(new() { Id = 9, Name = "Kuwait" });
        CalculationMethods.Add(new() { Id = 10, Name = "Qatar" });
        CalculationMethods.Add(new() { Id = 11, Name = "Majlis Ugama Islam Singapura, Singapore" });
        CalculationMethods.Add(new() { Id = 13, Name = "Diyanet İşleri Başkanlığı, Turkey" });
        CalculationMethods.Add(new() { Id = 15, Name = "Moonsighting Committee Worldwide" });

        CityPresets.Add(new() { City = "Karachi", Country = "PK" });
        CityPresets.Add(new() { City = "Lahore", Country = "PK" });
        CityPresets.Add(new() { City = "Islamabad", Country = "PK" });
        CityPresets.Add(new() { City = "Rawalpindi", Country = "PK" });
        CityPresets.Add(new() { City = "Faisalabad", Country = "PK" });
        CityPresets.Add(new() { City = "Peshawar", Country = "PK" });
        CityPresets.Add(new() { City = "Quetta", Country = "PK" });
        CityPresets.Add(new() { City = "Makkah", Country = "SA" });
        CityPresets.Add(new() { City = "Madinah", Country = "SA" });
        CityPresets.Add(new() { City = "Dubai", Country = "AE" });
        CityPresets.Add(new() { City = "London", Country = "GB" });
        CityPresets.Add(new() { City = "New York", Country = "US" });
        CityPresets.Add(new() { City = "Toronto", Country = "CA" });
        CityPresets.Add(new() { City = "Istanbul", Country = "TR" });

        ReminderOptions.Add(new() { Minutes = 0, DisplayName = "Disabled (No Pre-Lock Reminder)" });
        ReminderOptions.Add(new() { Minutes = 3, DisplayName = "3 Minutes Before Lock" });
        ReminderOptions.Add(new() { Minutes = 5, DisplayName = "5 Minutes Before Lock (Recommended)" });
        ReminderOptions.Add(new() { Minutes = 10, DisplayName = "10 Minutes Before Lock" });
        ReminderOptions.Add(new() { Minutes = 15, DisplayName = "15 Minutes Before Lock" });

        // Sensible defaults based on prayer times
        OverrideItems.Add(new() { PrayerName = "Fajr", PrayerUrduName = "فجر", SelectedHour = 5, SelectedMinute = "00", SelectedAmPm = "AM", IsEnabled = false });
        OverrideItems.Add(new() { PrayerName = "Dhuhr", PrayerUrduName = "ظہر", SelectedHour = 12, SelectedMinute = "30", SelectedAmPm = "PM", IsEnabled = false });
        OverrideItems.Add(new() { PrayerName = "Asr", PrayerUrduName = "عصر", SelectedHour = 4, SelectedMinute = "45", SelectedAmPm = "PM", IsEnabled = false });
        OverrideItems.Add(new() { PrayerName = "Maghrib", PrayerUrduName = "مغرب", SelectedHour = 7, SelectedMinute = "00", SelectedAmPm = "PM", IsEnabled = false });
        OverrideItems.Add(new() { PrayerName = "Isha", PrayerUrduName = "عشاء", SelectedHour = 8, SelectedMinute = "15", SelectedAmPm = "PM", IsEnabled = false });
    }

    public async Task LoadSettingsAsync()
    {
        _currentSetting = await _settingsService.GetSettingsAsync();
        City = _currentSetting.City;
        Country = _currentSetting.Country;
        SelectedMethod = CalculationMethods.FirstOrDefault(m => m.Id == _currentSetting.CalculationMethod) ?? CalculationMethods.First();
        LockDurationMinutes = _currentSetting.LockDurationMinutes;
        SelectedReminderLead = ReminderOptions.FirstOrDefault(r => r.Minutes == _currentSetting.ReminderLeadMinutes)
                               ?? ReminderOptions.FirstOrDefault(r => r.Minutes == 5)
                               ?? ReminderOptions.First();
        Theme = _currentSetting.Theme;
        AzaanFilePath = _currentSetting.AzaanFilePath;
        PlayDefaultChime = _currentSetting.PlayDefaultChime;
        AutostartEnabled = _currentSetting.AutostartEnabled;
        MinimizeToTrayOnClose = _currentSetting.MinimizeToTrayOnClose;

        var overrides = await _settingsService.GetOverridesAsync();
        foreach (var item in OverrideItems)
        {
            var existing = overrides.FirstOrDefault(o => string.Equals(o.PrayerName, item.PrayerName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                string defaultAmPm = item.PrayerName.Equals("Fajr", StringComparison.OrdinalIgnoreCase) ? "AM" : "PM";
                item.FromTimeString(existing.Time, defaultAmPm);
                item.IsEnabled = existing.IsEnabled;
            }
        }
    }

    private void BrowseAzaanFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Audio Files (*.mp3;*.wav;*.wma)|*.mp3;*.wav;*.wma|All Files (*.*)|*.*",
            Title = "Select Azaan Audio File"
        };

        if (dialog.ShowDialog() == true)
        {
            AzaanFilePath = dialog.FileName;
        }
    }

    private void TestAudio()
    {
        _audioService.PlayTestSound(AzaanFilePath);
    }

    public async Task SaveSettingsAsync()
    {
        _currentSetting.City = City.Trim();
        _currentSetting.Country = Country.Trim();
        _currentSetting.CalculationMethod = SelectedMethod?.Id ?? 1;
        _currentSetting.LockDurationMinutes = LockDurationMinutes;
        _currentSetting.ReminderLeadMinutes = SelectedReminderLead?.Minutes ?? 5;
        _currentSetting.Theme = Theme;
        _currentSetting.AzaanFilePath = AzaanFilePath;
        _currentSetting.PlayDefaultChime = PlayDefaultChime;
        _currentSetting.AutostartEnabled = AutostartEnabled;
        _currentSetting.MinimizeToTrayOnClose = MinimizeToTrayOnClose;

        await _settingsService.SaveSettingsAsync(_currentSetting);

        var overridesToSave = OverrideItems.Select(item => new ManualOverride
        {
            PrayerName = item.PrayerName,
            Time = item.To24HourTime(),
            IsEnabled = item.IsEnabled
        }).ToList();

        await _settingsService.SaveOverridesAsync(overridesToSave);

        StatusMessage = "Settings saved successfully! Updating prayer times...";
        _onSettingsSaved.Invoke();
    }
}
