using System.Text.Json.Serialization;

namespace XMediaDownloader.Models;

[JsonSerializable(typeof(ProfileSpotlightsQueryVariables))]
[JsonSerializable(typeof(GraphQlResponse))]
[JsonSerializable(typeof(UserMediaQueryVariables))]
[JsonSerializable(typeof(BookmarkFeatures))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;