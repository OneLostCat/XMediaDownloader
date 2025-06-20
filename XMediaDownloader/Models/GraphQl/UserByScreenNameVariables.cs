using System.Text.Json.Serialization;

namespace XMediaDownloader.Models.GraphQl;

public record UserByScreenNameVariables
{
    public required string ScreenName { get; set; }
}

// Json 序列化
[JsonSerializable(typeof(UserByScreenNameVariables))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
public partial class UserByScreenNameVariablesContext : JsonSerializerContext;
