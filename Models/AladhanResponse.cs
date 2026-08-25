using System.Text.Json.Serialization;

namespace Prayer.Models;

public class AladhanApiResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public AladhanData? Data { get; set; }
}

public class AladhanData
{
    [JsonPropertyName("timings")]
    public AladhanTimings? Timings { get; set; }

    [JsonPropertyName("date")]
    public AladhanDateInfo? Date { get; set; }

    [JsonPropertyName("meta")]
    public AladhanMeta? Meta { get; set; }
}

public class AladhanTimings
{
    [JsonPropertyName("Fajr")]
    public string Fajr { get; set; } = string.Empty;

    [JsonPropertyName("Sunrise")]
    public string Sunrise { get; set; } = string.Empty;

    [JsonPropertyName("Dhuhr")]
    public string Dhuhr { get; set; } = string.Empty;

    [JsonPropertyName("Asr")]
    public string Asr { get; set; } = string.Empty;

    [JsonPropertyName("Sunset")]
    public string Sunset { get; set; } = string.Empty;

    [JsonPropertyName("Maghrib")]
    public string Maghrib { get; set; } = string.Empty;

    [JsonPropertyName("Isha")]
    public string Isha { get; set; } = string.Empty;
}

public class AladhanDateInfo
{
    [JsonPropertyName("readable")]
    public string Readable { get; set; } = string.Empty;

    [JsonPropertyName("hijri")]
    public AladhanHijri? Hijri { get; set; }
}

public class AladhanHijri
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;

    [JsonPropertyName("month")]
    public AladhanHijriMonth? Month { get; set; }

    [JsonPropertyName("year")]
    public string Year { get; set; } = string.Empty;
}

public class AladhanHijriMonth
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("en")]
    public string En { get; set; } = string.Empty;

    [JsonPropertyName("ar")]
    public string Ar { get; set; } = string.Empty;
}

public class AladhanMeta
{
    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("method")]
    public AladhanMethod? Method { get; set; }
}

public class AladhanMethod
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
