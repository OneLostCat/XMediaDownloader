using System.Text.Json.Serialization;

namespace XMediaDownloader.Models;

public record User
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("screen_name")]
    public string? ScreenName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("created_time")]
    public long CreatedTime { get; set; }
}