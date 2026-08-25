using Microsoft.EntityFrameworkCore;
using Prayer.Data;
using Prayer.Models;

namespace Prayer.Services;

public class VerseService : IVerseService
{
    private readonly PrayerDbContext _dbContext;

    public VerseService(PrayerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DailyVerse> GetTodayVerseAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        int dayOfYear = date.DayOfYear;

        // Retrieve from verified database collection
        var verse = await _dbContext.DailyVerses
            .FirstOrDefaultAsync(v => v.DayOfYear == dayOfYear, cancellationToken);

        if (verse != null && !string.IsNullOrEmpty(verse.SourceAttribution))
        {
            return verse;
        }

        // Modulo rotation over available verified verses
        var totalVerses = await _dbContext.DailyVerses.CountAsync(cancellationToken);
        if (totalVerses > 0)
        {
            int index = dayOfYear % totalVerses;
            var rotated = await _dbContext.DailyVerses.Skip(index).FirstOrDefaultAsync(cancellationToken);
            if (rotated != null && !string.IsNullOrEmpty(rotated.SourceAttribution))
            {
                return rotated;
            }
        }

        // Fallback to verified primary verse
        return new DailyVerse
        {
            DayOfYear = dayOfYear,
            Type = "Quran",
            ArabicText = "إِنَّ الصَّلَاةَ كَانَتْ عَلَى الْمُؤْمِنِينَ كِتَابًا مَّوْقُوتًا",
            TranslationUrdu = "بے شک نماز مومنوں پر مقررہ اوقات میں فرض کی گئی ہے۔",
            TranslationEnglish = "Indeed, prayer has been decreed upon the believers a decree of specified times.",
            Reference = "Surah An-Nisa (4:103)",
            SourceAttribution = "Source: Tanzil Project (quran-uthmani) · Urdu: Fateh Muhammad Jalandhry · English: Saheeh International"
        };
    }
}
