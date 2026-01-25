using System.Text.Json.Serialization;

namespace MediaDownloader.Models.X.Api;

public record UserByScreenNameResponse
{
    public required UserByScreenNameResponseUser User { get; set; }
}

public record UserByScreenNameResponseUser
{
    public required UserByScreenNameResponseResult Result { get; set; }
}

public record UserByScreenNameResponseResult
{
    public required string Id { get; set; }
    public required string RestId { get; set; }
    public required UserByScreenNameResponseLegacy Legacy { get; set; }
}

public record UserByScreenNameResponseLegacy
{
    public required string ScreenName { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string CreatedAt { get; set; }
    public required int MediaCount { get; set; }
}
