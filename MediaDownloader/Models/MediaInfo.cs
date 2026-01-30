namespace MediaDownloader.Models;

public record MediaInfo
{
    public required string Id { get; set; }
    public required string Path { get; set; }
    public required MediaDownloader Downloader { get; set; }
    public string? Url { get; set; }
}
