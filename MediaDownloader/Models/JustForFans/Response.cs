namespace MediaDownloader.Models.JustForFans;

public record Response
{
    public required bool Ok { get; set; }
    public required int Status { get; set; }
    public required string Text { get; set; }
}
