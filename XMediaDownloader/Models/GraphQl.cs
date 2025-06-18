using System.Text.Json.Serialization;

namespace XMediaDownloader.Models;

public record GraphQlResponse
{
    [JsonPropertyName("data")]
    public DataContent Data { get; set; }
}

public record DataContent
{
    [JsonPropertyName("user")]
    public UserContent User { get; set; }

    [JsonPropertyName("bookmark_timeline_v2")]
    public TimelineV2Content BookmarkTimelineV2 { get; set; }

    [JsonPropertyName("user_result_by_screen_name")]
    public UserResults UserResultByScreenName { get; set; }

    [JsonPropertyName("threaded_conversation_with_injections_v2")]
    public TimelineContent ThreadedConversationV2 { get; set; }
}

public record UserContent
{
    [JsonPropertyName("result")]
    public UserResult Result { get; set; }
}

public record UserResult
{
    [JsonPropertyName("__typename")]
    public string TypeName { get; set; }

    [JsonPropertyName("timeline_v2")]
    public TimelineV2Content TimelineV2 { get; set; }
}

public record TimelineV2Content
{
    [JsonPropertyName("timeline")]
    public TimelineContent Timeline { get; set; }
}

public record TimelineContent
{
    [JsonPropertyName("instructions")]
    public List<Instruction> Instructions { get; set; }

    [JsonPropertyName("metadata")]
    public TimelineMetadata Metadata { get; set; }
}

public record TimelineMetadata
{
    [JsonPropertyName("scribeConfig")]
    public ScribeConfig ScribeConfig { get; set; }
}

public record ScribeConfig
{
    [JsonPropertyName("page")]
    public string Page { get; set; }
}

public record Instruction
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("direction")]
    public string Direction { get; set; }

    [JsonPropertyName("entries")]
    public List<TimelineEntry> Entries { get; set; }

    [JsonPropertyName("moduleItems")]
    public List<ItemMedia> ModuleItems { get; set; }
}

public record TimelineEntry
{
    [JsonPropertyName("entryId")]
    public string EntryId { get; set; }

    [JsonPropertyName("sortIndex")]
    public string SortIndex { get; set; }

    [JsonPropertyName("content")]
    public EntryContent Content { get; set; }
}

public record EntryContent
{
    [JsonPropertyName("entryType")]
    public string EntryType { get; set; }

    [JsonPropertyName("__typename")]
    public string TypeName { get; set; }

    [JsonPropertyName("itemContent")]
    public ItemContent ItemContent { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    // Media API使用
    [JsonPropertyName("items")]
    public List<ItemMedia> Items { get; set; }

    [JsonPropertyName("cursorType")]
    public string CursorType { get; set; }
}

public record ItemMedia
{
    [JsonPropertyName("entryId")]
    public string EntryId { get; set; }

    [JsonPropertyName("item")]
    public ItemDetail Item { get; set; }
}

public record ItemDetail
{
    [JsonPropertyName("itemContent")]
    public ItemContent ItemContent { get; set; }
}

public record ItemContent
{
    [JsonPropertyName("itemType")]
    public string ItemType { get; set; }

    [JsonPropertyName("__typename")]
    public string TypeName { get; set; }

    [JsonPropertyName("tweet_results")]
    public TweetResults TweetResults { get; set; }

    [JsonPropertyName("tweetDisplayType")]
    public string TweetDisplayType { get; set; }
}

public record TweetResults
{
    [JsonPropertyName("result")]
    public TweetResult Result { get; set; }
}

public record TweetResult
{
    [JsonPropertyName("__typename")]
    public string TypeName { get; set; }

    [JsonPropertyName("rest_id")]
    public string RestId { get; set; }

    [JsonPropertyName("core")]
    public CoreInfo Core { get; set; }

    [JsonPropertyName("legacy")]
    public LegacyInfo Legacy { get; set; }

    [JsonPropertyName("tweet")]
    public TweetResult Tweet { get; set; }

    [JsonPropertyName("tombstone")]
    public object Tombstone { get; set; }

    [JsonPropertyName("views")]
    public ViewInfo Views { get; set; }
}

public record ViewInfo
{
    [JsonPropertyName("count")]
    public string Count { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }
}

public record CoreInfo
{
    [JsonPropertyName("user_results")]
    public UserResults UserResults { get; set; }
}

public record UserResults
{
    [JsonPropertyName("result")]
    public UserResultInfo Result { get; set; }
}

public record UserResultInfo
{
    [JsonPropertyName("__typename")]
    public string TypeName { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("rest_id")]
    public string RestId { get; set; }

    [JsonPropertyName("legacy")]
    public UserLegacy Legacy { get; set; }
}

public record UserLegacy
{
    [JsonPropertyName("screen_name")]
    public string ScreenName { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}

public record LegacyInfo
{
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; }

    [JsonPropertyName("full_text")]
    public string FullText { get; set; }

    [JsonPropertyName("entities")]
    public Entities Entities { get; set; }

    [JsonPropertyName("extended_entities")]
    public ExtendedEntities ExtendedEntities { get; set; }
}

public record Entities
{
    [JsonPropertyName("hashtags")]
    public List<HashTag> Hashtags { get; set; }

    [JsonPropertyName("media")]
    public List<MediaEntity> Media { get; set; }
}

public record ExtendedEntities
{
    [JsonPropertyName("media")]
    public List<MediaEntity> Media { get; set; }
}

public record HashTag
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}

public record MediaEntity
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("media_url_https")]
    public string MediaUrlHttps { get; set; }

    [JsonPropertyName("video_info")]
    public VideoInfo VideoInfo { get; set; }
}

public record VideoInfo
{
    [JsonPropertyName("variants")]
    public List<VideoVariant> Variants { get; set; }
}

public record VideoVariant
{
    [JsonPropertyName("bitrate")]
    public long? Bitrate { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}