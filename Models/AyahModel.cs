using System.ComponentModel.DataAnnotations;

namespace Prayer.Models;

public class CachedAyah
{
    [Key]
    public int Id { get; set; }

    public int SurahNumber { get; set; }
    public int AyahNumber { get; set; } // 1-based index within the Surah

    public string ArabicText { get; set; } = string.Empty;
    public string UrduText { get; set; } = string.Empty; // Pinned to ur.jalandhry
    public string EnglishText { get; set; } = string.Empty; // Pinned to en.sahih
}

public class SurahReadingProgress
{
    [Key]
    public int SurahNumber { get; set; }

    public int LastReadAyah { get; set; } = 1;
    public DateTime LastReadAt { get; set; } = DateTime.UtcNow;
}
