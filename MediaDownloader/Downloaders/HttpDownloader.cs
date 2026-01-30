using MediaDownloader.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace MediaDownloader.Downloaders;

public class HttpDownloader(ILogger<HttpDownloader> logger, CommandLineOptions options) : IMediaDownloader
{
    private readonly HttpClient _http = BuildHttpClient();

    private static HttpClient BuildHttpClient()
    {
        // 连接池生命周期
        var handler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(15) }; 

        // 创建 HttpClient
        var http = new HttpClient(handler);

        // 启用 HTTP/2 和 HTTP/3
        http.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

        // 设置 User Agent
        http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36 Edg/144.0.0.0");

        return http;
    }

    // 主要方法
    public async Task DownloadAsync(List<MediaInfo> medias, CancellationToken cancel)
    {
        logger.LogInformation("下载 {Count} 个媒体文件:", medias.Count);

        // 并行下载
        await Parallel.ForEachAsync(
            medias,
            new ParallelOptions { MaxDegreeOfParallelism = options.Concurrency, CancellationToken = cancel },
            async (media, token) => await DownloadTaskAsync(media, token)
        );
    }

    private async ValueTask DownloadTaskAsync(MediaInfo media, CancellationToken cancel)
    {
        // 弹性
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(2),
                MaxRetryAttempts = 5,
                UseJitter = true
            })
            .Build();

        // 执行下载任务
        await pipeline.ExecuteAsync(async token =>
        {
            logger.LogInformation("  {Url} -> {Path}", media.Url, media.Path);

            // 发送请求
            var response = await _http.GetAsync(media.Url, token);
            
            // 确保成功状态码
            response.EnsureSuccessStatusCode();

            // 写入临时文件
            var temp = new FileInfo(Path.GetTempFileName());

            await using (var stream = temp.Create())
            {
                await response.Content.CopyToAsync(stream, token);
            }

            // 移动文件
            var file = new FileInfo(Path.Combine(options.Output, media.Path));
            file.Directory?.Create();
            temp.MoveTo(file.FullName);
        }, cancel);
    }
}
