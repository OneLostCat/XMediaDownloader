using System.Text.Json.Serialization;

namespace XMediaDownloader.Models;

public record StorageContent
{
    public Dictionary<string, UserData> Data { get; set; } = new();
    
    // public Dictionary<string, User> Users { get; set; } = new();
    // public Dictionary<string, UserData> Data { get; set; } = new();
}

public record UserData
{
    public required User Info { get; set; }
    public Dictionary<string, Tweet> Tweets { get; set; } = new();
    public string? CurrentCursor { get; set; }
}

// public record UserData
// {
//     public string CurrentCursor { get; set; } = "";
//     public Dictionary<string, UserDataItem> Users { get; set; } = new();
// }
//
// public record UserDataItem
// {
//     public Dictionary<string, User> UserHistory { get; set; } = [];
//     public Dictionary<string, Tweet> Tweets { get; set; } = [];
// }



// Json 序列化
[JsonSerializable(typeof(StorageContent))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class StorageContentContext : JsonSerializerContext;
