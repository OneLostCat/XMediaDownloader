using System.Text.Json.Serialization;

namespace XMediaDownloader.Models.XApi;

public record UserTweetsResponse(UserTweetsResponseUser User);

public record UserTweetsResponseUser(UserTweetsResponseResult Result);

public record UserTweetsResponseResult(UserTweetsResponseTimeline Timeline);

public record UserTweetsResponseTimeline(UserTweetsResponseTimelineTimeline Timeline);

public record UserTweetsResponseTimelineTimeline(List<UserTweetsResponseInstruction> Instructions);

public record UserTweetsResponseInstruction(string Type, UserTweetsResponseEntry Entry, List<UserTweetsResponseEntry> Entries);

public record UserTweetsResponseEntry(string EntryId, UserTweetsResponseContent Content);

public record UserTweetsResponseContent(string EntryType, UserTweetsResponseItemContent ItemContent);

public record UserTweetsResponseItemContent(string ItemType, UserTweetsResponseTweetResults TweetResults);

public record UserTweetsResponseTweetResults(UserTweetsResponseTweetResult TweetResult);

public record UserTweetsResponseTweetResult(string RestId);

public record UserTweetsResponseCore(UserTweetsResponseCoreUserResults UserResults);

public record UserTweetsResponseCoreUserResults(UserTweetsResponseResult Result);

public record UserTweetsResponseCoreUserResult(string RestId, UserTweetsResponseCoreUserResultLegacy Legacy);

public record UserTweetsResponseCoreUserResultLegacy(
    string RestId,
    string ScreenName,
    string Name,
    string Description,
    string CreatedAt,
    int MediaCount);

public record UserTweetsResponseTweetResultLegacy(string CreatedAt, string Text);

// Json 序列化
[JsonSerializable(typeof(GraphQlResponse<UserMediaResponse>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
public partial class UserTweetsResponseContext : JsonSerializerContext;
