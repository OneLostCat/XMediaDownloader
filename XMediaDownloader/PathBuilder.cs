using System.Text;
using XMediaDownloader.Models;

namespace XMediaDownloader;

public static class PathBuilder
{
    private static readonly Dictionary<string, string> Placeholders = new()
    {
        { "UserId", "0" },
        { "Username", "1" },
        // { "UserNickname", "2" },
        // { "UserDescription", "3" },
        { "UserCreationTime", "4" },
        { "UserMediaCount", "5" },
        { "TweetId", "6" },
        { "TweetCreationTime", "7" },
        // { "TweetText", "8" },
        // { "TweetHashtags", "9" },
        { "MediaIndex", "10" },
        { "MediaType", "11" },
        // { "MediaUrl", "12" },
        { "MediaExtension", "13" },
        { "MediaBitrate", "14" },
        // 设置默认时间格式
        { "{4}", "{4:yyyy-MM-dd_HH-mm-ss}" },
        { "{7}", "{7:yyyy-MM-dd_HH-mm-ss}" }
    };

    public static string Build(
        string format,
        string? userId,
        string? username,
        string? userNickname,
        string? userDescription,
        DateTimeOffset? userCreationTime,
        int? userMediaCount,
        string? tweetId,
        DateTimeOffset? tweetCreationTime,
        string? tweetText,
        List<string> tweetHashtags,
        int? mediaIndex,
        MediaType? mediaType,
        string? mediaUrl,
        string? mediaExtension,
        int? mediaBitrate)
    {
        var sb = new StringBuilder(format);

        // 替换占位符
        foreach (var pair in Placeholders) sb.Replace(pair.Key, pair.Value);
        
        // 生成路径
        return string.Format(
            sb.ToString(),
            userId,
            username,
            userNickname,
            userDescription,
            userCreationTime,
            userMediaCount,
            tweetId,
            tweetCreationTime,
            tweetText,
            tweetHashtags,
            mediaIndex,
            mediaType,
            mediaUrl,
            mediaExtension,
            mediaBitrate
        );
    }
}
