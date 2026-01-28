using System.Text.Json.Serialization;

namespace MediaDownloader.Models.JustForFans.Api;

[JsonSerializable(typeof(UserInfo))]
internal partial class UserInfoContext : JsonSerializerContext;
