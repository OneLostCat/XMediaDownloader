using System.Text.Json.Serialization;

namespace XMediaDownloader.Models.GraphQl;

public record ProfileSpotlightsVariables
{
    public required string ScreenName { get; set; }
}

// Json 序列化
[JsonSerializable(typeof(ProfileSpotlightsVariables))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
public partial class ProfileSpotlightsVariablesContext : JsonSerializerContext;
