namespace XMediaDownloader.Models;

public record Tweet
{
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required DateTimeOffset CreationTime { get; set; }
    public required string Text { get; set; }
    public List<string> Hashtags { get; set; } = [];
    public List<Media> Media { get; set; } = [];
}

public record Media
{
    public required MediaType Type { get; set; }
    public required string Url { get; set; }
    public List<Video> Video { get; set; } = [];
}

public record Video
{
    public required string Url { get; set; }
    public required int? Bitrate { get; set; } // 可能为空，原项目类型为 long
}
