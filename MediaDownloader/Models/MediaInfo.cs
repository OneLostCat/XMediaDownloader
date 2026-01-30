namespace MediaDownloader.Models;

public record MediaInfo
{
    public required string Url { get; set; }
    public required string Extension { get; set; }
    public required MediaDownloader Downloader { get; set; }
    public required string DefaultTemplate { get; set; }
    public string? Id { get; set; }
    public string? User { get; set; }
    public DateTime? Time { get; set; }
    public MediaType? Type { get; set; }
    public int? Index { get; set; }
    public string? Text { get; set; }
    public string? Tags { get; set; }
}
