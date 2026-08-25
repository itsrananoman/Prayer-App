using System.ComponentModel.DataAnnotations;

namespace Prayer.Models;

public class SurahListItem
{
    [Key]
    public int Number { get; set; }

    public string ArabicName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public string EnglishNameTranslation { get; set; } = string.Empty;
    public int NumberOfAyahs { get; set; }
    public string RevelationType { get; set; } = "Meccan"; // Meccan or Medinan
}
