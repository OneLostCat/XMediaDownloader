using System.Text.Json.Serialization;

namespace MediaDownloader.Models.X.Api;

[JsonSerializable(typeof(GraphQlResponse<UserByScreenNameResponse>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, UseStringEnumConverter = true)]
public partial class UserByScreenNameResponseContext : JsonSerializerContext;
