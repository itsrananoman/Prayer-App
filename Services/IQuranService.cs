using Prayer.Models;

namespace Prayer.Services;

public interface IQuranService
{
    Task<List<SurahListItem>> GetSurahListAsync(bool forceRefresh = false);
    Task<List<CachedAyah>> GetSurahAyahsAsync(int surahNumber, bool forceRefresh = false);
    Task<SurahReadingProgress?> GetReadingProgressAsync(int surahNumber);
    Task SaveReadingProgressAsync(int surahNumber, int ayahNumber);
}
