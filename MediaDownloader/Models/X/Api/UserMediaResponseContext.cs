using System.Text.Json.Serialization;

namespace MediaDownloader.Models.X.Api;

[JsonSerializable(typeof(GraphQlResponse<UserMediaResponse>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
public partial class UserMediaResponseContext : JsonSerializerContext;
