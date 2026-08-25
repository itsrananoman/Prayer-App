using System.ComponentModel.DataAnnotations;

namespace Prayer.Models;

public class UserSetting
{
    [Key]
    public int Id { get; set; }

    public string City { get; set; } = "Karachi";
    public string Country { get; set; } = "PK";
    public int CalculationMethod { get; set; } = 1; // 1 = University of Islamic Sciences, Karachi

    public bool UseGps { get; set; } = false;
    public double Latitude { get; set; } = 24.8607;
    public double Longitude { get; set; } = 67.0011;

    public int LockDurationMinutes { get; set; } = 15; // default 15 minutes
    public int ReminderLeadMinutes { get; set; } = 5; // 0 = off, 3, 5, 10, 15 minutes prior to lock
    public string Theme { get; set; } = "Dark"; // "Dark" or "Light"

    public string? AzaanFilePath { get; set; } = null;
    public bool PlayDefaultChime { get; set; } = true;
    public bool AutostartEnabled { get; set; } = true;
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public bool ShowLockPreviewNotification { get; set; } = true;
}
