using MediaDownloader.Models;
using Microsoft.Extensions.Logging;

namespace MediaDownloader.Services;

public class DownloadService(ILogger<DownloadService> logger, HttpClient http, CommandLineArguments args)
{
    public async Task DownloadAsync(List<MediaItem> medias, CancellationToken cancel)
    {
        foreach (var media in medias)
        {
            logger.LogInformation("下载 {Url} -> {Path}", media.Url, media.Path);
            
            // 检查文件是否存在
            var file = new FileInfo(Path.Combine(args.OutputDir, media.Path));
            
            if (file.Exists)
            {
                logger.LogInformation("文件已存在，跳过");
                continue;
            }

            // 发送请求
            var response = await http.GetAsync(media.Url, cancel);
            
            // 写入临时文件
            var temp = new FileInfo(Path.GetTempFileName());

            await using (var stream = temp.Create())
                await response.Content.CopyToAsync(stream, cancel);

            // 创建文件夹
            file.Directory?.Create();
            
            // 移动文件
            temp.MoveTo(file.FullName);
        }
    }
}
