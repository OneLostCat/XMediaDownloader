using System.Text.Json.Serialization;

namespace MediaDownloader.Models.JustForFans.Api;

[JsonSerializable(typeof(List<VideoSource>))]
public partial class ListVideoSourceContext : JsonSerializerContext;
