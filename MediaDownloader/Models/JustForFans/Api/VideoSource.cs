using System.Text.Json.Serialization;

namespace MediaDownloader.Models.JustForFans.Api;

public record VideoSource
{
    [JsonPropertyName("res")] public required string Res { get; set; } // 清晰度
    [JsonPropertyName("src")] public required string Src { get; set; } // 视频地址
}
