using System.Text.Json.Serialization;

namespace XMediaDownloader.Models.XApi;

public record UserMediaVariables
{
    public required string UserId { get; set; }
    public required string Cursor { get; set; }
    public required int Count { get; set; }
    public bool IncludePromotedContent { get; set; }
    public bool WithClientEventToken { get; set; }
    public bool WithBirdwatchNotes { get; set; }
    public bool WithVoice { get; set; } = true;
    public bool WithV2Timeline { get; set; } = true;
}

// Json 序列化
[JsonSerializable(typeof(UserMediaVariables))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
public partial class UserMediaVariablesContext : JsonSerializerContext;
