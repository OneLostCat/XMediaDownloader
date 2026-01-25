namespace MediaDownloader.Models.X.Api;

public record UserMediaVariables(
    string UserId,
    int Count,
    string? Cursor,
    bool IncludePromotedContent = false,
    bool WithClientEventToken = false,
    bool WithBirdwatchNotes = false,
    bool WithVoice = true,
    bool WithV2Timeline = true
);
