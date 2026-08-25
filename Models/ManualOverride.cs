using System.ComponentModel.DataAnnotations;

namespace Prayer.Models;

public class ManualOverride
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string PrayerName { get; set; } = string.Empty; // "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha"

    [Required]
    [MaxLength(10)]
    public string Time { get; set; } = string.Empty; // HH:mm format, e.g. "05:15"

    public bool IsEnabled { get; set; } = true;

    // Optional date restriction (null or empty means applies to all dates)
    [MaxLength(10)]
    public string? EffectiveDate { get; set; }
}
