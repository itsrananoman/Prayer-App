using System.ComponentModel.DataAnnotations;

namespace Prayer.Models;

public class DailyVerse
{
    [Key]
    public int Id { get; set; }

    public int DayOfYear { get; set; } // 1 to 366 for deterministic daily rotation

    public string ArabicText { get; set; } = string.Empty;
    public string TranslationUrdu { get; set; } = string.Empty;
    public string TranslationEnglish { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string SourceAttribution { get; set; } = string.Empty; // e.g. "Text: Tanzil (quran-uthmani) · Urdu: Fateh M. Jalandhry · English: Saheeh Int."
    public string Type { get; set; } = "Quran"; // "Quran" or "Hadith"
}
