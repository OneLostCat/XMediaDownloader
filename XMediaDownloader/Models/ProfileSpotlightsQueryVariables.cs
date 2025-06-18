using System.Text.Json.Serialization;

namespace XMediaDownloader.Models;

public record ProfileSpotlightsQueryVariables
{
    [JsonPropertyName("screen_name")]
    public required string ScreenName { get; set; }
}