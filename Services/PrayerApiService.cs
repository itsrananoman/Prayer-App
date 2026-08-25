using System.Net.Http;
using System.Text.Json;
using Prayer.Models;

namespace Prayer.Services;

public class PrayerApiService : IPrayerApiService
{
    private readonly HttpClient _httpClient;

    public PrayerApiService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<PrayerTimeRecord?> FetchTimingsByCityAsync(DateTime date, string city, string country, int method, CancellationToken cancellationToken = default)
    {
        try
        {
            var formattedDate = date.ToString("dd-MM-yyyy");
            var url = $"https://api.aladhan.com/v1/timingsByCity/{formattedDate}?city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(country)}&method={method}&iso8601=true";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Accept-Encoding", "");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResult = JsonSerializer.Deserialize<AladhanApiResponse>(json);

            if (apiResult?.Data?.Timings == null)
            {
                return null;
            }

            var t = apiResult.Data.Timings;
            var record = new PrayerTimeRecord
            {
                Date = date.ToString("yyyy-MM-dd"),
                Fajr = CleanTimeString(t.Fajr),
                Sunrise = CleanTimeString(t.Sunrise),
                Dhuhr = CleanTimeString(t.Dhuhr),
                Asr = CleanTimeString(t.Asr),
                Sunset = CleanTimeString(t.Sunset),
                Maghrib = CleanTimeString(t.Maghrib),
                Isha = CleanTimeString(t.Isha),
                Source = "api",
                FetchedAt = DateTime.UtcNow,
                City = city,
                Country = country,
                Method = method
            };

            return record;
        }
        catch (Exception)
        {
            // Network failure / DNS error / timeout -> return null to allow graceful fallback to SQLite cache
            return null;
        }
    }

    public async Task<PrayerTimeRecord?> FetchTimingsByCoordinatesAsync(DateTime date, double latitude, double longitude, int method, CancellationToken cancellationToken = default)
    {
        try
        {
            var formattedDate = date.ToString("dd-MM-yyyy");
            var url = $"https://api.aladhan.com/v1/timings/{formattedDate}?latitude={latitude}&longitude={longitude}&method={method}&iso8601=true";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Accept-Encoding", "");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResult = JsonSerializer.Deserialize<AladhanApiResponse>(json);

            if (apiResult?.Data?.Timings == null)
            {
                return null;
            }

            var t = apiResult.Data.Timings;
            var record = new PrayerTimeRecord
            {
                Date = date.ToString("yyyy-MM-dd"),
                Fajr = CleanTimeString(t.Fajr),
                Sunrise = CleanTimeString(t.Sunrise),
                Dhuhr = CleanTimeString(t.Dhuhr),
                Asr = CleanTimeString(t.Asr),
                Sunset = CleanTimeString(t.Sunset),
                Maghrib = CleanTimeString(t.Maghrib),
                Isha = CleanTimeString(t.Isha),
                Source = "api",
                FetchedAt = DateTime.UtcNow,
                City = $"{latitude:F2}, {longitude:F2}",
                Country = "GPS",
                Method = method
            };

            return record;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static string CleanTimeString(string rawTime)
    {
        if (string.IsNullOrWhiteSpace(rawTime)) return "00:00";

        // If it's ISO 8601 e.g. "2026-08-24T05:12:00+05:00"
        if (DateTimeOffset.TryParse(rawTime, out var dto))
        {
            return dto.ToString("HH:mm");
        }

        // If it's in format "05:12 (PKT)" or "05:12"
        var parts = rawTime.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && TimeSpan.TryParse(parts[0], out var ts))
        {
            return $"{ts.Hours:D2}:{ts.Minutes:D2}";
        }

        return rawTime.Trim();
    }
}
