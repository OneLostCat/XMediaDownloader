using System.Text.Json.Serialization;

namespace MediaDownloader.Models.JustForFans.Api;

[JsonSerializable(typeof(UserInfo))]
public partial class UserInfoContext : JsonSerializerContext;
