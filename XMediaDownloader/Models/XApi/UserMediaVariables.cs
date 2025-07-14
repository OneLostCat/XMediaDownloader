using System.Text.Json.Serialization;

namespace XMediaDownloader.Models.XApi;

public record UserMediaVariables(
    string UserId,
    int Count,
    string? Cursor,
    bool IncludePromotedContent = false,
    bool WithClientEventToken = false,
    bool WithBirdwatchNotes = false,
    bool WithVoice = true,
    bool WithV2Timeline = true);

// Json 序列化
[JsonSerializable(typeof(UserMediaVariables))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
public partial class UserMediaVariablesContext : JsonSerializerContext;