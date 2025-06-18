using System.Text.Json.Serialization;

namespace XMediaDownloader.Models;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public record UserMediaQueryVariables
{
    public required string UserId { get; set; }
    public required int Count { get; set; }
    public required bool IncludePromotedContent { get; set; }
    public required bool WithClientEventToken { get; set; }
    public required bool WithBirdwatchNotes { get; set; }
    public required bool WithVoice { get; set; }
    public required bool WithV2Timeline { get; set; }
    public required string Cursor { get; set; }
}