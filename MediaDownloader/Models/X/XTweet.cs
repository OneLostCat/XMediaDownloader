namespace MediaDownloader.Models.X;

public record XTweet
{
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required DateTimeOffset CreationTime { get; set; }
    public required string Text { get; set; }
    public List<string> Hashtags { get; set; } = [];
    public List<XMedia> Media { get; set; } = [];
}

public record XMedia
{
    public required XMediaType Type { get; set; }
    public required string Url { get; set; }
    public List<XVideo> Video { get; set; } = [];
}

public record XVideo
{
    public required string Url { get; set; }
    public required int? Bitrate { get; set; } // 可能为空，原项目类型为 long
}
