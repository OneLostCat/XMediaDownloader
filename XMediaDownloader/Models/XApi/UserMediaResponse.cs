using System.Text.Json.Serialization;

namespace XMediaDownloader.Models.XApi;

public record UserMediaResponse(UserMediaResponseUser User);

public record UserMediaResponseUser(UserMediaResponseResult Result);

public record UserMediaResponseResult([property: JsonPropertyName("timeline_v2")] UserMediaResponseTimelineV2 TimelineV2);

public record UserMediaResponseTimelineV2(UserMediaResponseTimeline Timeline);

public record UserMediaResponseTimeline(List<UserMediaResponseInstruction> Instructions);

public record UserMediaResponseInstruction(
    string Type,
    List<UserMediaResponseEntry> Entries,
    List<UserMediaResponseItem> ModuleItems);

public record UserMediaResponseEntry(string EntryId, UserMediaResponseContent Content);

public record UserMediaResponseContent(
    string? Value, // 可能为空
    List<UserMediaResponseItem> Items);

public record UserMediaResponseItem(
    string EntryId,
    UserMediaResponseItemItem Item);

public record UserMediaResponseItemItem(UserMediaResponseItemContent ItemContent);

public record UserMediaResponseItemContent(
    [property: JsonPropertyName("tweet_results")] UserMediaResponseTweetResults TweetResults);

public record UserMediaResponseTweetResults(UserMediaResponseTweetResult Result);

public record UserMediaResponseTweetResult(
    [property: JsonPropertyName("rest_id")] string RestId,
    UserMediaResponseCore Core,
    UserMediaResponseLegacy Legacy);

public record UserMediaResponseCore([property: JsonPropertyName("user_results")] UserMediaResponseUserResults UserResults);

public record UserMediaResponseUserResults(UserMediaResponseUserResult Result);

public record UserMediaResponseUserResult([property: JsonPropertyName("rest_id")] string? RestId); // 可能为空

public record UserMediaResponseLegacy(
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("full_text")] string FullText,
    UserMediaResponseEntities Entities,
    [property: JsonPropertyName("extended_entities")] UserMediaResponseExtendedEntities? ExtendedEntities); // 可能为空

public record UserMediaResponseEntities(List<UserMediaResponseHashTag> Hashtags, List<UserMediaResponseMedia> Media);

public record UserMediaResponseExtendedEntities(List<UserMediaResponseMedia> Media);

public record UserMediaResponseHashTag(string Text);

public record UserMediaResponseMedia(
    string Type,
    [property: JsonPropertyName("media_url_https")] string MediaUrlHttps,
    [property: JsonPropertyName("video_info")] UserMediaResponseVideoInfo? VideoInfo); // 可能为空

public record UserMediaResponseVideoInfo(List<UserMediaResponseVideoVariant> Variants);

public record UserMediaResponseVideoVariant(string Url, int? Bitrate); // 可能为空，原类型为 long?

// Json 序列化
[JsonSerializable(typeof(GraphQlResponse<UserMediaResponse>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
public partial class UserMediaResponseContext : JsonSerializerContext;
