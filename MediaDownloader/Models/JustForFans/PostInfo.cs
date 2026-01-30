namespace MediaDownloader.Models.JustForFans;

public record PostInfo
{
    public required string Id { get; set; }
    public required DateTime Time { get; set; }
    public required string Text { get; set; }
    public required List<string> Tags { get; set; }
    public required PostType Type { get; set; }
    public required List<string> Images { get; set; }
}
