using System.Text.Json.Serialization;

namespace XMediaDownloader.Models.XApi;

public record UserByScreenNameVariables(string ScreenName);

// Json 序列化
[JsonSerializable(typeof(UserByScreenNameVariables))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, UseStringEnumConverter = true)]
public partial class UserByScreenNameVariablesContext : JsonSerializerContext;
