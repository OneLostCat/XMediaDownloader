using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XMediaDownloader.Models;

namespace XMediaDownloader.Services;

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
        var mediaCount = 0; // 当前媒体数量
        var downloadCount = 0; // 下载媒体数量

        logger.LogInformation("开始下载媒体");

        // 遍历帖子
        foreach (var tweet in tweets.Select(x => x.Value))
        {
            logger.LogInformation("下载媒体 {CreationTime:yyyy-MM-dd HH:mm:ss zzz} {Id}", tweet.CreationTime, tweet.Id);

            // 遍历媒体
            for (var i = 0; i < tweet.Media.Count; i++)
            {
                // 检查是否取消
                cancel.ThrowIfCancellationRequested();

                // 增加媒体计数
                mediaCount++;

                var media = tweet.Media[i];
                var downloads = new List<DownloadItem>();

                // 获取图片 (包括视频和 GIF 的封面)
                var index = media.Url.LastIndexOf('.'); // 获取最后一个 "." 的位置

                if (index == -1) throw new ArgumentException("无法获取原始图片 Url", media.Url);

                var format = media.Url[(index + 1)..];

                downloads.Add(new DownloadItem($"{media.Url[..index]}?format={format}&name=orig", $".{format}"));

                // 检查下载类型
                if (!args.DownloadType.HasFlag(media.Type))
                {
                    logger.LogInformation("  {Type} {Url} 跳过 ({mediaCount} / {totalMediaCount})", media.Type,
                        downloads.First().Url, mediaCount, totalMediaCount);

                    continue;
                }

                // 获取视频和 GIF
                if (media.Type != MediaType.Image)
                {
                    // 获取最高质量视频
                    var video = media.Video
                        .OrderByDescending(x => x.Bitrate)
                        .First();

                    downloads.Add(new DownloadItem(video.Url, Path.GetExtension(new Uri(video.Url).Segments.Last()),
                        video.Bitrate));
                }

                // 下载所有文件
                var downloaded = false;

                foreach (var item in downloads)
                {
                    // 获取文件
                    var filePath = PathBuilder.Build(
                        args.OutputPathFormat,
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
                        i + 1,
                        media.Type,
                        item.Url,
                        item.Extension,
                        item.Bitrate
                    );

                    var file = new FileInfo(Path.Combine(args.OutputDir, filePath));

                    // 检查文件是否存在
                    if (file.Exists)
                    {
                        logger.LogInformation("  {Type} {Url} 文件已存在 {FilePath} ({mediaCount} / {totalMediaCount})", media.Type,
                            item.Url, filePath, mediaCount, totalMediaCount);
                        
                        continue;
                    }

                    logger.LogInformation("  {Type} {Url} -> {FilePath} ({mediaCount} / {totalMediaCount})", media.Type, item.Url,
                        filePath, mediaCount, totalMediaCount);
                    
                    downloaded = true;

                    // 发送请求
                    var response = await httpClient.GetAsync(item.Url, cancel);

                    // 创建文件夹
                    file.Directory?.Create(); // 无法判断是否为空

                    // 写入临时文件
                    var tempFile = new FileInfo(Path.GetTempFileName());

                    await using (var fs = tempFile.Create())
                        await response.Content.CopyToAsync(fs, CancellationToken.None); // 不传递取消令牌，避免下载操作只执行一半

                    // 移动文件
                    tempFile.MoveTo(file.FullName);
                }

                // 增加下载计数
                if (downloaded) downloadCount++;
            }
        }

        logger.LogInformation("媒体下载完成: 成功下载 {DownloadCount} / {TotalMediaCount}", downloadCount, totalMediaCount);
    }
}
