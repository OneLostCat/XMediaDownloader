using System.Text.Json.Serialization;

namespace MediaDownloader.Models.X.Api;

[JsonSerializable(typeof(UserByScreenNameVariables))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, UseStringEnumConverter = true)]
public partial class UserByScreenNameVariablesContext : JsonSerializerContext;
