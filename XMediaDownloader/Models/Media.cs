using System.Text.Json.Serialization;

namespace XMediaDownloader.Models;

public record Media
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("bitrate")]
    public long? Bitrate { get; set; }
}