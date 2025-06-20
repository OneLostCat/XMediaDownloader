using System.Text.Json.Serialization;

namespace XMediaDownloader.Models;

public record StorageContent
{
    public Dictionary<string, User> Users { get; set; } = new();
    public Dictionary<string, UserData> Data { get; set; } = new();
}

public record User
{
    // [JsonPropertyName("id")]
    public required string Id { get; set; }

    // [JsonPropertyName("screen_name")]
    public required string ScreenName { get; set; }

    // [JsonPropertyName("name")]
    public required string Name { get; set; }

    // [JsonPropertyName("description")]
    public required string Description { get; set; }

    // [JsonPropertyName("created_time")]
    public required DateTime CreatedTime { get; set; }
}

public record UserData
{
    public string CurrentCursor { get; set; } = "";
    public Dictionary<string, UserDataItem> Users { get; set; } = new();
}

public record UserDataItem
{
    public Dictionary<string, User> UserHistory { get; set; } = [];
    public Dictionary<string, Tweet> Tweets { get; set; } = [];
}

// Json 序列化
[JsonSerializable(typeof(StorageContent))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class StorageContentContext : JsonSerializerContext;
