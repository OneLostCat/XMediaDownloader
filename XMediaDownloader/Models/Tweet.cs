namespace XMediaDownloader.Models;

public record Tweet
{
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required string Text { get; set; }
    public List<string> Hashtags { get; set; } = [];
    public required DateTimeOffset CreationTime { get; set; }
    public List<TweetMedia> Media { get; set; } = [];
}

public record TweetMedia
{
    public required string Type { get; set; }
    public required string Url { get; set; }
    public long? Bitrate { get; set; } // 可能为空，原项目类型为 long
}
