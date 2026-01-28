using System.Text.Json.Serialization;

namespace MediaDownloader.Models.JustForFans.Api;

// 转换成属性形式

public record UserInfo
{
    [JsonPropertyName("UserID")] public required string UserId { get; set; }
    public required string Photos { get; set; }
    public required string Videos { get; set; }
    public required string Posts { get; set; }
    public required string Followers { get; set; }
    public required string FreeFollowers { get; set; }
    public required string LastUpdate { get; set; }
    public required string Likes { get; set; }
};
