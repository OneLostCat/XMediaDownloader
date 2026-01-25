using System.Text.Json.Serialization;

namespace MediaDownloader.Models.X.Api;

public record UserMediaResponse
{
    public required UserMediaResponseUser User { get; set; }
}

public record UserMediaResponseUser
{
    public required UserMediaResponseResult Result { get; set; }
}

public record UserMediaResponseResult
{
    [JsonPropertyName("timeline_v2")] public required UserMediaResponseTimelineV2 TimelineV2 { get; set; }
}

public record UserMediaResponseTimelineV2
{
    public required UserMediaResponseTimeline Timeline { get; set; }
}

public record UserMediaResponseTimeline
{
    public required List<UserMediaResponseInstruction> Instructions { get; set; }
}

public record UserMediaResponseInstruction
{
    public required string Type { get; set; }
    public List<UserMediaResponseEntry> Entries { get; set; } = []; // 可能为空
    public List<UserMediaResponseItem> ModuleItems { get; set; } = []; // 可能为空
}

public record UserMediaResponseEntry
{
    public required string EntryId { get; set; }
    public required UserMediaResponseContent Content { get; set; }
}

public record UserMediaResponseContent
{
    public string? Value { get; set; } // 可能为空
    public List<UserMediaResponseItem> Items { get; set; } = []; // 可能为空
}

public record UserMediaResponseItem
{
    public required string EntryId { get; set; }
    public required UserMediaResponseItemItem Item { get; set; }
}

public record UserMediaResponseItemItem
{
    public required UserMediaResponseItemContent ItemContent { get; set; }
}

public record UserMediaResponseItemContent
{
    [JsonPropertyName("tweet_results")] public required UserMediaResponseTweetResults TweetResults { get; set; }
}

public record UserMediaResponseTweetResults
{
    public required UserMediaResponseTweetResult Result { get; set; }
}

public record UserMediaResponseTweetResult
{
    [JsonPropertyName("rest_id")] public required string RestId { get; set; }
    public required UserMediaResponseCore Core { get; set; }
    public required UserMediaResponseLegacy Legacy { get; set; }
}

public record UserMediaResponseCore
{
    [JsonPropertyName("user_results")] public required UserMediaResponseUserResults XUserResults { get; set; }
}

public record UserMediaResponseUserResults
{
    public required UserMediaResponseUserResult Result { get; set; }
}

public record UserMediaResponseUserResult
{
    [JsonPropertyName("rest_id")] public required string RestId { get; set; }
    public required UserMediaResponseUserResultLegacy Legacy { get; set; }
}

public record UserMediaResponseUserResultLegacy
{
    [JsonPropertyName("screen_name")] public required string ScreenName { get; set; }
    public required string Name { get; set; }
    [JsonPropertyName("created_at")] public required string CreatedAt { get; set; }
    public required string Description { get; set; }
    [JsonPropertyName("media_count")] public required int MediaCount { get; set; }
}

public record UserMediaResponseLegacy
{
    [JsonPropertyName("created_at")] public required string CreatedAt { get; set; }
    [JsonPropertyName("full_text")] public required string FullText { get; set; }
    public required UserMediaResponseEntities Entities { get; set; }
    [JsonPropertyName("extended_entities")] public UserMediaResponseExtendedEntities? ExtendedEntities { get; set; } // 可能为空
}

public record UserMediaResponseEntities
{
    public required List<UserMediaResponseHashTag> Hashtags { get; set; }
    public List<UserMediaResponseMedia> Media { get; set; } = []; // 可能为空
}

public record UserMediaResponseExtendedEntities
{
    public required List<UserMediaResponseMedia> Media { get; set; }
}

public record UserMediaResponseHashTag
{
    public required string Text { get; set; }
}

public record UserMediaResponseMedia
{
    public required string Type { get; set; }
    [JsonPropertyName("media_url_https")] public required string MediaUrlHttps { get; set; }
    [JsonPropertyName("video_info")] public XUserMediaResponseVideoInfo? VideoInfo { get; set; } // 可能为空
}

public record XUserMediaResponseVideoInfo
{
    public required List<XUserMediaResponseVideoVariant> Variants { get; set; }
}

public record XUserMediaResponseVideoVariant 
{
    public required string Url { get; set; }
    public int? Bitrate { get; set; } // 可能为空，原类型为 long
}
