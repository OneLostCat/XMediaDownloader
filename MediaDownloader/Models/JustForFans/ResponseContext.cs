using System.Text.Json.Serialization;

namespace MediaDownloader.Models.JustForFans;

[JsonSerializable(typeof(Response))]
public partial class ResponseContext : JsonSerializerContext;
