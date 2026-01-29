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
            // 提取媒体
            var extractor = GetExtractor(args.Extractor);
            var medias = await extractor.ExtractAsync(cancel);

            // 下载媒体
            var downloader = GetDownloader(medias.Downloader);
            await downloader.DownloadAsync(medias, cancel);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("操作取消");
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "错误");
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
        _ => throw new ArgumentOutOfRangeException(nameof(downloader), downloader, "无效的下载器")
    };
}
