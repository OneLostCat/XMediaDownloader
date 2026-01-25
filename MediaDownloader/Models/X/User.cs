namespace MediaDownloader.Models.X;

public record User
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Nickname { get; set; }
    public required string Description { get; set; }
    public required DateTimeOffset CreationTime { get; set; }
    public required int MediaTweetCount { get; set; }
}
