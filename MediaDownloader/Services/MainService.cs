using MediaDownloader.Downloaders;
using MediaDownloader.Extractors;
using MediaDownloader.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediaDownloader.Services;

public class MainService(
    IServiceProvider services,
    ILogger<MainService> logger,
    CommandLineOptions args,
    IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancel)
    {
        try
        {
            logger.LogInformation("---------- 媒体下载器 ----------");
            
            // 提取媒体
            var extractor = GetExtractor(args.Extractor);
            var medias = await extractor.ExtractAsync(cancel);

            // 按下载器分类
            var lookup = medias.ToLookup(media => media.Downloader);
            
            // 下载媒体
            foreach (var grouping in lookup)
            {
                var downloader = GetDownloader(grouping.Key);
                await downloader.DownloadAsync(grouping.ToList(), cancel);
            }
            
            logger.LogInformation("下载完成");
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("下载取消");
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "下载错误");
        }

        // 退出
        lifetime.StopApplication();
    }

    private IMediaExtractor GetExtractor(MediaExtractor extractor) => extractor switch
    {
        MediaExtractor.X => services.GetRequiredService<XExtractor>(),
        MediaExtractor.JustForFans => services.GetRequiredService<JustForFansExtractor>(),
        _ => throw new ArgumentOutOfRangeException(nameof(extractor), extractor, "无效的媒体来源")
    };

    private IMediaDownloader GetDownloader(Models.MediaDownloader downloader) => downloader switch
    {
        Models.MediaDownloader.Http => services.GetRequiredService<HttpDownloader>(),
        Models.MediaDownloader.JustForFans => services.GetRequiredService<JustForFansDownloader>(),
        _ => throw new ArgumentOutOfRangeException(nameof(downloader), downloader, "无效的下载器")
    };
}
