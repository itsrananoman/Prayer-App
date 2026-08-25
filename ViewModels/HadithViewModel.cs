using Prayer.Models;
using Prayer.Services;

namespace Prayer.ViewModels;

public class HadithViewModel : ViewModelBase
{
    private readonly IVerseService _verseService;
    private DailyVerse _currentVerse = new();
    private bool _isLoading = false;

    public DailyVerse CurrentVerse
    {
        get => _currentVerse;
        set => SetProperty(ref _currentVerse, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public HadithViewModel(IVerseService verseService)
    {
        _verseService = verseService;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            CurrentVerse = await _verseService.GetTodayVerseAsync(DateTime.Today);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
