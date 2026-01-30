using System.Diagnostics.CodeAnalysis;
using MediaDownloader.Models;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Scriban;
using Scriban.Functions;
using Scriban.Runtime;

namespace MediaDownloader.Downloaders;

public class HttpDownloader(ILogger<HttpDownloader> logger, CommandLineOptions options) : IMediaDownloader
{
    private readonly HttpClient _http = BuildHttpClient();

    private static HttpClient BuildHttpClient()
    {
        // 弹性
        var resiliencePipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new HttpRetryStrategyOptions
            {
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(2),
                MaxRetryAttempts = 3,
                UseJitter = true
            })
            .Build();

        var socketHandler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(15) }; // 连接池生命周期

        var resilienceHandler = new ResilienceHandler(resiliencePipeline) { InnerHandler = socketHandler };

        // 创建 HttpClient
        var http = new HttpClient(resilienceHandler);

        // 启用 HTTP/2 和 HTTP/3
        http.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

        // 设置 User Agent
        http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36 Edg/144.0.0.0");

        return http;
    }

    // 主要方法
    public async Task DownloadAsync(MediaCollection medias, CancellationToken cancel)
    {
        var template = Template.Parse(options.OutputTemplate ?? medias.DefaultTemplate);

        logger.LogInformation("下载 {Count} 个媒体文件:", medias.Medias.Count);

        // 并行下载
        await Parallel.ForEachAsync(
            medias.Medias,
            new ParallelOptions { MaxDegreeOfParallelism = options.Concurrency, CancellationToken = cancel },
            async (media, token) => await DownloadTaskAsync(media, template, token)
        );
    }

    private async ValueTask DownloadTaskAsync(MediaInfo media, Template template, CancellationToken cancel)
    {
        // 生成路径
        var path = await RenderAsync(template, media) + media.Extension;

        // 检查文件是否存在
        var file = new FileInfo(Path.Combine(options.Output, path));

        if (file.Exists)
        {
            logger.LogInformation("  文件已存在: {Path}", path);
            return;
        }

        logger.LogInformation("  {Url} -> {Path}", media.Url, path);

        // 发送请求
        var response = await _http.GetAsync(media.Url, cancel);

        // 写入临时文件
        var temp = new FileInfo(Path.GetTempFileName());

        await using (var stream = temp.Create())
        {
            await response.Content.CopyToAsync(stream, cancel);
        }

        // 移动文件
        file.Directory?.Create();
        temp.MoveTo(file.FullName);
    }

    // 工具方法
    private async Task<string>
        RenderAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(Template template, T model)
    {
        // 模型
        var script = new ScriptObject();
        script.Import(model);

        // 创建上下文
        var context = new TemplateContext();
        context.PushGlobal(script);

        // 设置时间格式
        (context.BuiltinObject["date"] as DateTimeFunctions)?.Format = options.DateTimeFormat;

        return await template.RenderAsync(context);
    }
}
