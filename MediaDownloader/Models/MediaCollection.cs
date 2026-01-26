namespace MediaDownloader.Models;

public record MediaCollection
{
    public required List<MediaInfo> Medias { get; set; }
    public required MediaDownloader Downloader { get; set; }
    public required string DefaultTemplate { get; set; }
}
