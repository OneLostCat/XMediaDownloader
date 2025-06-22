using System.Text.Json.Serialization;

namespace XMediaDownloader.Models.GraphQl;

public record UserMediaResponse
{
    [JsonPropertyName("user")] public required UserContent User { get; set; }
}

public record UserContent
{
    [JsonPropertyName("result")] public required UserResult Result { get; set; }
}

public record UserResult
{
    [JsonPropertyName("timeline_v2")] public required TimelineV2Content TimelineV2 { get; set; }
}

public record TimelineV2Content
{
    [JsonPropertyName("timeline")] public required TimelineContent Timeline { get; set; }
}

public record TimelineContent
{
    [JsonPropertyName("metadata")] public required TimelineMetadata Metadata { get; set; }
    [JsonPropertyName("instructions")] public List<Instruction> Instructions { get; set; } = [];
}

public record TimelineMetadata
{
    [JsonPropertyName("scribeConfig")] public required ScribeConfig ScribeConfig { get; set; }
}

public record ScribeConfig
{
    [JsonPropertyName("page")] public required string Page { get; set; }
}

public record Instruction
{
    [JsonPropertyName("type")] public required string Type { get; set; }
    [JsonPropertyName("direction")] public string? Direction { get; set; } // 可能为空
    [JsonPropertyName("entries")] public List<TimelineEntry> Entries { get; set; } = [];
    [JsonPropertyName("moduleItems")] public List<ItemMedia> ModuleItems { get; set; } = [];
}

public record TimelineEntry
{
    [JsonPropertyName("entryId")] public required string EntryId { get; set; }
    [JsonPropertyName("sortIndex")] public required string SortIndex { get; set; }
    [JsonPropertyName("content")] public required EntryContent Content { get; set; }
}

public record EntryContent
{
    [JsonPropertyName("entryType")] public required string EntryType { get; set; }
    [JsonPropertyName("itemContent")] public ItemContent? ItemContent { get; set; } // 可能为空
    [JsonPropertyName("value")] public string? Value { get; set; } // 可能为空

    // Media API 使用
    [JsonPropertyName("items")] public List<ItemMedia> Items { get; set; } = [];
    [JsonPropertyName("cursorType")] public string? CursorType { get; set; } // 可能为空
}

public record ItemMedia
{
    [JsonPropertyName("entryId")] public required string EntryId { get; set; }
    [JsonPropertyName("item")] public required ItemDetail Item { get; set; }
}

public record ItemDetail
{
    [JsonPropertyName("itemContent")] public required ItemContent ItemContent { get; set; }
}

public record ItemContent
{
    [JsonPropertyName("itemType")] public required string ItemType { get; set; }
    [JsonPropertyName("tweet_results")] public required TweetResults TweetResults { get; set; }
    [JsonPropertyName("tweetDisplayType")] public required string TweetDisplayType { get; set; }
}

public record TweetResults
{
    [JsonPropertyName("result")] public required TweetResult Result { get; set; }
}

public record TweetResult
{
    [JsonPropertyName("rest_id")] public required string RestId { get; set; }
    [JsonPropertyName("core")] public required CoreInfo Core { get; set; }
    [JsonPropertyName("legacy")] public required LegacyInfo Legacy { get; set; }
    [JsonPropertyName("tweet")] public TweetResult? Tweet { get; set; } // 可能为空 
    [JsonPropertyName("tombstone")] public object? Tombstone { get; set; } // 可能为空
    [JsonPropertyName("views")] public required ViewInfo Views { get; set; }
}

public record CoreInfo
{
    [JsonPropertyName("user_results")] public required MediaUserResults UserResults { get; set; }
}

public record MediaUserResults
{
    public required MediaUserResult Result { get; set; }
}

public record MediaUserResult
{
    [JsonPropertyName("id")] public required string Id { get; set; }
    [JsonPropertyName("rest_id")] public string? RestId { get; set; } // 可能为空
    [JsonPropertyName("legacy")] public required MediaUserLegacy Legacy { get; set; }
}

public record MediaUserLegacy
{
    [JsonPropertyName("screen_name")] public string? ScreenName { get; set; } // 可能为空
    [JsonPropertyName("name")] public required string Name { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; } // 可能为空
}

public record LegacyInfo
{
    [JsonPropertyName("created_at")] public required string CreatedAt { get; set; }
    [JsonPropertyName("full_text")] public required string FullText { get; set; }
    [JsonPropertyName("entities")] public required Entities Entities { get; set; }
    [JsonPropertyName("extended_entities")]
    public ExtendedEntities? ExtendedEntities { get; set; } // 可能为空
}

public record ViewInfo
{
    [JsonPropertyName("count")] public string? Count { get; set; } // 可能为空
    [JsonPropertyName("state")] public required string State { get; set; }
}

public record Entities
{
    [JsonPropertyName("hashtags")] public List<HashTag> Hashtags { get; set; } = [];
    [JsonPropertyName("media")] public List<MediaEntity> Media { get; set; } = [];
}

public record ExtendedEntities
{
    [JsonPropertyName("media")] public List<MediaEntity> Media { get; set; } = [];
}

public record HashTag
{
    [JsonPropertyName("text")] public required string Text { get; set; }
}

public record MediaEntity
{
    [JsonPropertyName("type")] public required string Type { get; set; }
    [JsonPropertyName("media_url_https")] public required string MediaUrlHttps { get; set; }
    [JsonPropertyName("video_info")] public VideoInfo? VideoInfo { get; set; } // 可能为空
}

public record VideoInfo
{
    [JsonPropertyName("variants")] public List<VideoVariant> Variants { get; set; } = [];
}

public record VideoVariant
{
    [JsonPropertyName("url")] public required string Url { get; set; }
    [JsonPropertyName("bitrate")] public int? Bitrate { get; set; } // 可能为空，原类型为 long?
}

// Json 序列化
[JsonSerializable(typeof(GraphQlResponse<UserMediaResponse>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
public partial class UserMediaResponseContext : JsonSerializerContext;
