using System.Text.Json.Serialization;

namespace MediaDownloader.Models.JustForFans.Api;

[JsonSerializable(typeof(PlaylistBody))]
[JsonSourceGenerationOptions(PropertyNamingPolicy =  JsonKnownNamingPolicy.Unspecified)]
public partial class PlaylistBodyContext : JsonSerializerContext;
