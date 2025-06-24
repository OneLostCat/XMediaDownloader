using System.Text.Json.Serialization;

namespace XMediaDownloader.Models.XApi;

public record UserByScreenNameResponse(UserByScreenNameResponseUser User);

public record UserByScreenNameResponseUser(UserByScreenNameResponseResult Result);

public record UserByScreenNameResponseResult(string Id, string RestId, UserByScreenNameResponseLegacy Legacy);

public record UserByScreenNameResponseLegacy(
    string ScreenName,
    string Name,
    string Description,
    string CreatedAt,
    int MediaCount);

// Json 序列化
[JsonSerializable(typeof(GraphQlResponse<UserByScreenNameResponse>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, UseStringEnumConverter = true)]
public partial class UserByScreenNameResponseContext : JsonSerializerContext;
