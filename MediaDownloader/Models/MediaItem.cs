namespace MediaDownloader.Models;

public record MediaItem
{
    public required string Url { get; set; }
    public required string Path { get; set; }
}
