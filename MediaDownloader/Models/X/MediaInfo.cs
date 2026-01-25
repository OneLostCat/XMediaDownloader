namespace MediaDownloader.Models.X;

public class MediaInfo
{
    public required string Id { get; set; }
    public required string User { get; set; }
    public required DateTimeOffset Time { get; set; }
    public required int Index { get; set; }
    public required MediaType Type { get; set; }
}
