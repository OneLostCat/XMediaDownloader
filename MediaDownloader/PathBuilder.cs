using System.Text;
using MediaDownloader.Models.X;

namespace MediaDownloader;

public static class PathBuilder
{
    private static readonly Dictionary<string, string> Placeholders = new()
    {
        { "UserId", "0" },
        { "Username", "1" },
        { "UserCreationTime", "4" },
        { "UserMediaTweetCount", "5" },
        { "Id", "6" },
        { "Time", "7" },
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
        int? userMediaTweetCount,
        string? tweetId,
        DateTimeOffset? tweetCreationTime,
        string? tweetText,
        List<string> tweetHashtags,
        int? mediaIndex,
        XMediaType? mediaType,
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
            userMediaTweetCount,
            tweetId,
            tweetCreationTime,
            tweetText,
            tweetHashtags,
            mediaIndex,
            mediaType?.ToString().ToLower(),
            mediaUrl,
            mediaExtension,
            mediaBitrate
        );
    }
}
