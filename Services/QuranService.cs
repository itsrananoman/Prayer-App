using Microsoft.EntityFrameworkCore;
using Prayer.Data;
using Prayer.Models;
using System.Net.Http;
using System.Text.Json;

namespace Prayer.Services;

public class QuranService : IQuranService
{
    private readonly PrayerDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public QuranService(PrayerDbContext dbContext)
    {
        _dbContext = dbContext;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "PrayerApp/2.0 (Windows; C# WPF)");
    }

    public async Task<List<SurahListItem>> GetSurahListAsync(bool forceRefresh = false)
    {
        if (!forceRefresh)
        {
            var localSurahs = await _dbContext.Surahs.OrderBy(s => s.Number).ToListAsync();
            if (localSurahs.Count == 114)
            {
                return localSurahs;
            }
        }

        try
        {
            var response = await _httpClient.GetStringAsync("https://api.alquran.cloud/v1/surah");
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
            {
                var list = new List<SurahListItem>();
                foreach (var item in dataEl.EnumerateArray())
                {
                    list.Add(new SurahListItem
                    {
                        Number = item.GetProperty("number").GetInt32(),
                        ArabicName = item.GetProperty("name").GetString() ?? string.Empty,
                        EnglishName = item.GetProperty("englishName").GetString() ?? string.Empty,
                        EnglishNameTranslation = item.GetProperty("englishNameTranslation").GetString() ?? string.Empty,
                        NumberOfAyahs = item.GetProperty("numberOfAyahs").GetInt32(),
                        RevelationType = item.GetProperty("revelationType").GetString() ?? "Meccan"
                    });
                }

                if (list.Count > 0)
                {
                    _dbContext.Surahs.RemoveRange(_dbContext.Surahs);
                    await _dbContext.Surahs.AddRangeAsync(list);
                    await _dbContext.SaveChangesAsync();
                    return list;
                }
            }
        }
        catch
        {
            // Fallback to whatever is locally cached
            var cached = await _dbContext.Surahs.OrderBy(s => s.Number).ToListAsync();
            if (cached.Count > 0) return cached;
        }

        // Return empty or partial if completely offline and not yet cached
        return await _dbContext.Surahs.OrderBy(s => s.Number).ToListAsync();
    }

    public async Task<List<CachedAyah>> GetSurahAyahsAsync(int surahNumber, bool forceRefresh = false)
    {
        if (!forceRefresh)
        {
            var localAyahs = await _dbContext.CachedAyahs
                .Where(a => a.SurahNumber == surahNumber)
                .OrderBy(a => a.AyahNumber)
                .ToListAsync();

            if (localAyahs.Count > 0)
            {
                return localAyahs;
            }
        }

        try
        {
            // Fetch Arabic (Uthmani), Urdu (Jalandhry), and English (Saheeh) in parallel
            var arabicTask = _httpClient.GetStringAsync($"https://api.alquran.cloud/v1/surah/{surahNumber}/quran-uthmani");
            var urduTask = _httpClient.GetStringAsync($"https://api.alquran.cloud/v1/surah/{surahNumber}/ur.jalandhry");
            var englishTask = _httpClient.GetStringAsync($"https://api.alquran.cloud/v1/surah/{surahNumber}/en.sahih");

            await Task.WhenAll(arabicTask, urduTask, englishTask);

            var arabicDict = ParseAyahs(arabicTask.Result);
            var urduDict = ParseAyahs(urduTask.Result);
            var englishDict = ParseAyahs(englishTask.Result);

            int totalAyahs = arabicDict.Count;
            if (totalAyahs == 0) totalAyahs = Math.Max(urduDict.Count, englishDict.Count);

            var result = new List<CachedAyah>();
            for (int i = 1; i <= totalAyahs; i++)
            {
                result.Add(new CachedAyah
                {
                    SurahNumber = surahNumber,
                    AyahNumber = i,
                    ArabicText = arabicDict.GetValueOrDefault(i, string.Empty),
                    UrduText = urduDict.GetValueOrDefault(i, string.Empty),
                    EnglishText = englishDict.GetValueOrDefault(i, string.Empty)
                });
            }

            if (result.Count > 0)
            {
                // Remove any partial cache and insert fresh
                var existing = _dbContext.CachedAyahs.Where(a => a.SurahNumber == surahNumber);
                _dbContext.CachedAyahs.RemoveRange(existing);
                await _dbContext.CachedAyahs.AddRangeAsync(result);
                await _dbContext.SaveChangesAsync();
                return result;
            }
        }
        catch
        {
            // If offline, check if anything was cached
            var cached = await _dbContext.CachedAyahs
                .Where(a => a.SurahNumber == surahNumber)
                .OrderBy(a => a.AyahNumber)
                .ToListAsync();

            if (cached.Count > 0) return cached;
        }

        return new List<CachedAyah>();
    }

    private static Dictionary<int, string> ParseAyahs(string json)
    {
        var dict = new Dictionary<int, string>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var dataEl) && dataEl.TryGetProperty("ayahs", out var ayahsEl))
            {
                foreach (var ayah in ayahsEl.EnumerateArray())
                {
                    int numInSurah = ayah.GetProperty("numberInSurah").GetInt32();
                    string text = ayah.GetProperty("text").GetString() ?? string.Empty;
                    dict[numInSurah] = text;
                }
            }
        }
        catch { }
        return dict;
    }

    public async Task<SurahReadingProgress?> GetReadingProgressAsync(int surahNumber)
    {
        return await _dbContext.ReadingProgress.FirstOrDefaultAsync(p => p.SurahNumber == surahNumber);
    }

    public async Task SaveReadingProgressAsync(int surahNumber, int ayahNumber)
    {
        var existing = await _dbContext.ReadingProgress.FirstOrDefaultAsync(p => p.SurahNumber == surahNumber);
        if (existing != null)
        {
            existing.LastReadAyah = ayahNumber;
            existing.LastReadAt = DateTime.UtcNow;
        }
        else
        {
            _dbContext.ReadingProgress.Add(new SurahReadingProgress
            {
                SurahNumber = surahNumber,
                LastReadAyah = ayahNumber,
                LastReadAt = DateTime.UtcNow
            });
        }
        await _dbContext.SaveChangesAsync();
    }
}
