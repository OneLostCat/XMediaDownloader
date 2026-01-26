namespace MediaDownloader.Models.X;

public class XMediaInfo
{
    public required string Id { get; set; }
    public required string User { get; set; }
    public required DateTimeOffset Time { get; set; }
    public required int Index { get; set; }
    public required XMediaType Type { get; set; }
}
