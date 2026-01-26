namespace MediaDownloader.Models;

public record MediaInfo
{
    public required string Url { get; set; }
    public required string Extension { get; set; }
    public string? User { get; set; }
    public string? Id { get; set; }
    public string? Year { get; set; }
    public string? Month { get; set; }
    public string? Day { get; set; }
    public string? Hour { get; set; }
    public string? Minute { get; set; }
    public string? Second { get; set; }
    public MediaType? Type { get; set; }
    public int? Index { get; set; }
    public string? Title { get; set; }

}
