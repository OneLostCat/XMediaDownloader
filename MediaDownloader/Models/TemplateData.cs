namespace MediaDownloader.Models;

public record TemplateData
{
    public string? Id { get; set; }
    public string? User { get; set; }
    public DateTime? Time { get; set; }
    public string? Text { get; set; }
    public string? Tags { get; set; }
    public MediaType? Type { get; set; }
    public int? Index { get; set; }
}
