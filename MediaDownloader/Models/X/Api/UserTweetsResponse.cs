namespace MediaDownloader.Models.X.Api;

public record UserTweetsResponse
{
    public required UserTweetsResponseUser User { get; set; }
}

public record UserTweetsResponseUser
{
    public required UserTweetsResponseResult Result { get; set; }
}

public record UserTweetsResponseResult
{
    public required UserTweetsResponseTimeline Timeline { get; set; }
}

public record UserTweetsResponseTimeline
{
    public required UserTweetsResponseTimelineTimeline Timeline { get; set; }
}

public record UserTweetsResponseTimelineTimeline
{
    public required List<UserTweetsResponseInstruction> Instructions { get; set; }
}

public record UserTweetsResponseInstruction
{
    public required string Type { get; set; }
    public required UserTweetsResponseEntry Entry { get; set; }
    public required List<UserTweetsResponseEntry> Entries { get; set; }
}

public record UserTweetsResponseEntry
{
    public required string EntryId { get; set; }
    public required UserTweetsResponseContent Content { get; set; }
}

public record UserTweetsResponseContent
{
    public required string EntryType { get; set; }
    public required UserTweetsResponseItemContent ItemContent { get; set; }
}

public record UserTweetsResponseItemContent
{
    public required string ItemType { get; set; }
    public required UserTweetsResponseTweetResults TweetResults { get; set; }
}

public record UserTweetsResponseTweetResults
{
    public required UserTweetsResponseTweetResult TweetResult { get; set; }
}

public record UserTweetsResponseTweetResult
{
    public required string RestId { get; set; }
}

public record UserTweetsResponseCore
{
    public required UserTweetsResponseCoreUserResults UserResults { get; set; }
}

public record UserTweetsResponseCoreUserResults
{
    public required UserTweetsResponseResult Result { get; set; }
}

public record UserTweetsResponseCoreUserResult
{
    public required string RestId { get; set; }
    public required UserTweetsResponseCoreUserResultLegacy Legacy { get; set; }
}

public record UserTweetsResponseCoreUserResultLegacy
{
    public required string RestId { get; set; }
    public required string ScreenName { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string CreatedAt { get; set; }
    public required int MediaCount { get; set; }
}

public record UserTweetsResponseTweetResultLegacy
{
    public required string CreatedAt { get; set; }
    public required string Text { get; set; }
}
