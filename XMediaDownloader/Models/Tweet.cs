using System.Text.Json.Serialization;

namespace XMediaDownloader.Models;

public record Tweet
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    public string? UserId { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("hashtags")]
    public List<string> Hashtags { get; set; } = [];

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("media")]
    public List<Media> Media { get; set; } = [];
}