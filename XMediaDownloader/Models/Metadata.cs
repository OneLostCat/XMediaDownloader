using System.Text.Json.Serialization;

namespace XMediaDownloader.Models;

public record Metadata
{
    [JsonPropertyName("current_page")]
    public string? CurrentPage { get; set; }

    [JsonPropertyName("users")]
    public Dictionary<string, UserData> Users { get; set; } = new();
}

public record UserData
{
    [JsonPropertyName("user")]
    public List<User> UserHistory { get; set; } = [];

    [JsonPropertyName("tweet")]
    public List<Tweet> Tweets { get; set; } = [];
}
