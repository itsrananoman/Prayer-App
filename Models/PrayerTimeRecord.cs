using System.ComponentModel.DataAnnotations;

namespace Prayer.Models;

public class PrayerTimeRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string Date { get; set; } = string.Empty; // Format: yyyy-MM-dd

    public string Fajr { get; set; } = string.Empty;
    public string Sunrise { get; set; } = string.Empty;
    public string Dhuhr { get; set; } = string.Empty;
    public string Asr { get; set; } = string.Empty;
    public string Sunset { get; set; } = string.Empty;
    public string Maghrib { get; set; } = string.Empty;
    public string Isha { get; set; } = string.Empty;

    public string Source { get; set; } = "api"; // "api" or "manual"
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int Method { get; set; } = 1;
}
