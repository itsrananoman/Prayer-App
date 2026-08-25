using Prayer.Models;
using Prayer.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Prayer.ViewModels;

public class QuranViewModel : ViewModelBase
{
    private readonly IQuranService _quranService;

    private string _searchQuery = string.Empty;
    private bool _isReading = false;
    private bool _isLoading = false;
    private SurahListItem? _selectedSurah;
    private string _offlineNotice = string.Empty;
    private int _lastReadAyah = 1;
    private string _lastReadPrompt = string.Empty;

    public ObservableCollection<SurahListItem> AllSurahs { get; } = new();
    public ObservableCollection<SurahListItem> FilteredSurahs { get; } = new();
    public ObservableCollection<CachedAyah> CurrentAyahs { get; } = new();

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                ApplyFilter();
            }
        }
    }

    public bool IsReading
    {
        get => _isReading;
        set => SetProperty(ref _isReading, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public SurahListItem? SelectedSurah
    {
        get => _selectedSurah;
        set => SetProperty(ref _selectedSurah, value);
    }

    public string OfflineNotice
    {
        get => _offlineNotice;
        set => SetProperty(ref _offlineNotice, value);
    }

    public int LastReadAyah
    {
        get => _lastReadAyah;
        set => SetProperty(ref _lastReadAyah, value);
    }

    public string LastReadPrompt
    {
        get => _lastReadPrompt;
        set => SetProperty(ref _lastReadPrompt, value);
    }

    public ICommand OpenSurahCommand { get; }
    public ICommand BackToListCommand { get; }
    public ICommand BookmarkAyahCommand { get; }
    public ICommand ClearSearchCommand { get; }

    public QuranViewModel(IQuranService quranService)
    {
        _quranService = quranService;

        OpenSurahCommand = new RelayCommand(async param =>
        {
            if (param is SurahListItem surah)
            {
                await OpenSurahAsync(surah);
            }
        });

        BackToListCommand = new RelayCommand(() =>
        {
            IsReading = false;
            CurrentAyahs.Clear();
            SelectedSurah = null;
        });

        BookmarkAyahCommand = new RelayCommand(async param =>
        {
            if (param is CachedAyah ayah && SelectedSurah != null)
            {
                await _quranService.SaveReadingProgressAsync(SelectedSurah.Number, ayah.AyahNumber);
                LastReadAyah = ayah.AyahNumber;
                LastReadPrompt = $"Bookmarked at Ayah {ayah.AyahNumber}";
            }
        });

        ClearSearchCommand = new RelayCommand(() => SearchQuery = string.Empty);
    }

    public async Task InitializeAsync()
    {
        if (AllSurahs.Count == 0)
        {
            IsLoading = true;
            try
            {
                var list = await _quranService.GetSurahListAsync();
                AllSurahs.Clear();
                foreach (var s in list)
                {
                    AllSurahs.Add(s);
                }
                ApplyFilter();
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private void ApplyFilter()
    {
        FilteredSurahs.Clear();
        var query = SearchQuery.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(query))
        {
            foreach (var s in AllSurahs)
            {
                FilteredSurahs.Add(s);
            }
            return;
        }

        foreach (var s in AllSurahs)
        {
            if (s.EnglishName.ToLowerInvariant().Contains(query) ||
                s.EnglishNameTranslation.ToLowerInvariant().Contains(query) ||
                s.ArabicName.Contains(query) ||
                s.Number.ToString() == query)
            {
                FilteredSurahs.Add(s);
            }
        }
    }

    public async Task OpenSurahAsync(SurahListItem surah)
    {
        SelectedSurah = surah;
        IsReading = true;
        IsLoading = true;
        OfflineNotice = string.Empty;
        CurrentAyahs.Clear();

        try
        {
            var progress = await _quranService.GetReadingProgressAsync(surah.Number);
            if (progress != null && progress.LastReadAyah > 1)
            {
                LastReadAyah = progress.LastReadAyah;
                LastReadPrompt = $"Continue reading from Ayah {progress.LastReadAyah}";
            }
            else
            {
                LastReadAyah = 1;
                LastReadPrompt = string.Empty;
            }

            var ayahs = await _quranService.GetSurahAyahsAsync(surah.Number);
            if (ayahs.Count == 0)
            {
                OfflineNotice = "This surah requires an internet connection to load for the first time.";
            }
            else
            {
                foreach (var ayah in ayahs)
                {
                    CurrentAyahs.Add(ayah);
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
