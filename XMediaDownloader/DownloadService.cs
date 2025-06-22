using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XMediaDownloader.Models;

namespace XMediaDownloader;

public class DownloadService(
    ILogger<DownloadService> logger,
    CommandLineArguments args,
    StorageService storage,
    [FromKeyedServices("Download")] HttpClient httpClient)
{
    public async Task DownloadMediaAsync(string userId, CancellationToken cancel)
    {
        var user = storage.Content.Users[userId].Info;
        var tweets = storage.Content.Users[userId].Tweets;

        var totalMediaCount = tweets.Select(x => x.Value.Media.Count).Sum(); // 总媒体数量
        var tweetCount = 0; // 当前帖子数量
        var mediaCount = 0; // 当前媒体数量
        var downloadCount = 0; // 下载媒体数量

        logger.LogInformation("开始下载媒体: 总计 {TweetCount} 条帖子 / {MediaCount} 个媒体", tweets.Count, totalMediaCount);

        // 遍历帖子
        foreach (var tweet in tweets.Select(x => x.Value))
        {
            logger.LogInformation("下载媒体 {CreationTime:yyyy-MM-dd HH:mm:ss zzz} {Id}", tweet.CreationTime, tweet.Id);

            // 增加帖子计数
            tweetCount++;

            // 遍历媒体
            for (var i = 0; i < tweet.Media.Count; i++)
            {
                // 检查是否取消
                cancel.ThrowIfCancellationRequested();

                var media = tweet.Media[i];

                // 增加媒体计数
                mediaCount++;

                // 获取 Url 和扩展名
                string url;
                string extension;
                int? videoIndex = null;

                if (media.Type != MediaType.Video) // 图片或动图
                {
                    // 获取原图 Url
                    var index = media.Url.LastIndexOf('.'); // 获取最后一个 "." 的位置

                    if (index == -1) throw new ArgumentException("无法获取原始图片 Url", media.Url);

                    var baseUrl = media.Url[..index]; // 获取基础 Url
                    extension = media.Url[(index + 1)..]; // 获取扩展名

                    url = $"{baseUrl}?format={extension}&name=orig";
                }
                else // 视频
                {
                    // 获取最高质量视频索引
                    videoIndex = media.Video
                        .Index()
                        .Where(x => x.Item.Bitrate != null)
                        .OrderByDescending(x => x.Item.Bitrate)
                        .First()
                        .Index;

                    // 获取视频 Url
                    url = media.Video[(int)videoIndex].Url; // 强制转换避免报错

                    // 获取拓展名
                    extension = new Uri(url).Segments.Last().Split('.').Last();
                }

                // 检查下载类型
                if (!args.MediaType.HasFlag(media.Type))
                {
                    logger.LogInformation("  {Type} {Url} 跳过 ({mediaCount} / {totalMediaCount})", media.Type, url, mediaCount,
                        totalMediaCount);
                    continue;
                }

                // 生成路径
                var filePath = BuildPath(args.OutputPath, user, tweet, i, videoIndex, extension);

                // 检查文件是否存在
                if (File.Exists(filePath))
                {
                    logger.LogInformation("  {Type} {Url} 文件已存在 ({mediaCount} / {totalMediaCount})", media.Type, url, mediaCount,
                        totalMediaCount);
                    continue;
                }

                logger.LogInformation("  {Type} {Url} -> {FilePath} ({mediaCount} / {totalMediaCount})", media.Type, url,
                    filePath, mediaCount, totalMediaCount);

                // 增加下载计数
                downloadCount++;

                // 发送请求
                var response = await httpClient.GetAsync(url, cancel);

                // 创建文件夹
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? "");

                // 写入临时文件
                var temp = Path.GetTempFileName();
                await using (var fs = File.Create(temp))
                    await response.Content.CopyToAsync(fs, CancellationToken.None); // 不传递取消令牌，避免下载操作只执行一半

                // 移动文件
                File.Move(temp, filePath);
            }
        }

        logger.LogInformation("媒体下载完成: 成功下载 {DownloadCount} / {TotalMediaCount}", downloadCount, totalMediaCount);
    }

    // 工具方法
    // 生成路径
    private static readonly Dictionary<string, string> Placeholders = new()
    {
        { "UserId", "0" },
        { "Username", "1" },
        // { "UserNickname", "2" }, TODO
        // { "UserDescription", "3" }, TODO
        { "UserCreationTime", "4" },
        { "UserMediaCount", "5" },
        { "TweetId", "6" },
        { "TweetCreationTime", "7" },
        // { "TweetText", "8" }, TODO
        // { "TweetHashtags", "9" }, TODO
        { "MediaIndex", "10" },
        { "MediaType", "11" },
        // { "MediaUrl", "12" }, TODO
        { "VideoIndex", "13" },
        // { "VideoUrl", "14" }, TODO
        { "VideoBitrate", "15" },
        { "Extension", "16" },
        // 设置默认时间格式
        { "{4}", "{4:yyyy-MM-dd_HH-mm-ss}" },
        { "{7}", "{7:yyyy-MM-dd_HH-mm-ss}" }
    };

    private static string BuildPath(string format, User user, Tweet tweet, int mediaIndex, int? videoIndex, string extension)
    {
        var sb = new StringBuilder(format);

        // 替换占位符
        foreach (var pair in Placeholders) sb.Replace(pair.Key, pair.Value);

        // 生成路径
        return string.Format(
            sb.ToString(),
            user.Id,
            user.Name,
            user.Nickname,
            user.Description,
            user.CreationTime,
            user.MediaCount,
            tweet.Id,
            tweet.CreationTime,
            tweet.Text,
            tweet.Hashtags,
            mediaIndex,
            tweet.Media[mediaIndex].Type.ToString().ToLower(),
            tweet.Media[mediaIndex].Url,
            videoIndex != null ? videoIndex : "",
            videoIndex != null ? tweet.Media[mediaIndex].Video[(int)videoIndex].Url : "",
            videoIndex != null ? tweet.Media[mediaIndex].Video[(int)videoIndex].Bitrate : "",
            extension
        );
    }
}