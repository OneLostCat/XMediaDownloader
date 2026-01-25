using System.Text.Json.Serialization;

namespace MediaDownloader.Models.X.Api;

[JsonSerializable(typeof(UserMediaVariables))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
public partial class UserMediaVariablesContext : JsonSerializerContext;
