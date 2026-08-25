using Prayer.Models;

namespace Prayer.Services;

public interface IPrayerApiService
{
    Task<PrayerTimeRecord?> FetchTimingsByCityAsync(DateTime date, string city, string country, int method, CancellationToken cancellationToken = default);
    Task<PrayerTimeRecord?> FetchTimingsByCoordinatesAsync(DateTime date, double latitude, double longitude, int method, CancellationToken cancellationToken = default);
}
