using System.CommandLine;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using XMediaDownloader.Models;
using XMediaDownloader.Services;

namespace XMediaDownloader;

public static class Downloader
{
    public static async Task Run(ParseResult result, CancellationToken cancel)
    {
        // 获取参数
        var username = result.GetRequiredValue(CommandLine.UsernameOption);
        var cookieFile = result.GetRequiredValue(CommandLine.CookieFileOption);
        var withoutDownloadInfo = result.GetRequiredValue(CommandLine.WithoutDownloadInfoOption);
        var outputDir = result.GetRequiredValue(CommandLine.OutputDirOption);
        var outputPathFormat = result.GetRequiredValue(CommandLine.OutputPathFormatOption);
        var downloadType = result.GetRequiredValue(CommandLine.DownloadTypeOption).Aggregate((a, b) => a | b); // 合并
        var withoutDownloadMedia = result.GetRequiredValue(CommandLine.WithoutDownloadMediaOption);
        var storageDir = result.GetRequiredValue(CommandLine.StorageDirOption);
        var workDir = result.GetRequiredValue(CommandLine.WorkDirOption);
        var logLevel = result.GetRequiredValue(CommandLine.LogLevelOption);

        // 设置工作目录
        Environment.CurrentDirectory = workDir;

        // 日志
        await using var logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}")
            // .WriteTo.Console(outputTemplate: "[{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .MinimumLevel.Is(logLevel)
            // 去除多余的日志
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Extensions.Hosting.Internal.Host", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .MinimumLevel.Override("Polly", LogEventLevel.Warning)
            .CreateLogger();

        Log.Logger = logger;

        logger.Information("---------- X 媒体下载工具 ----------");

        try
        {
            // 主机
            var builder = Host.CreateApplicationBuilder(Environment.GetCommandLineArgs());

            // 服务
            builder.Services.AddSerilog();
            await builder.Services.AddHttpClientAsync(cookieFile, cancel);
            builder.Services.AddSingleton<XApiService>();
            builder.Services.AddSingleton<StorageService>();
            builder.Services.AddSingleton<DownloadService>();
            builder.Services.AddHostedService<MainService>();

            // 命令行参数
            builder.Services.AddSingleton(new CommandLineArguments(
                username,
                cookieFile,
                outputDir,
                outputPathFormat,
                downloadType,
                withoutDownloadInfo,
                withoutDownloadMedia,
                storageDir, 
                workDir,
                logLevel
            ));

            // 运行
            await builder.Build().RunAsync(cancel);
        }
        catch (Exception exception)
        {
            logger.Fatal(exception, "错误");
        }

        logger.Debug("应用退出");
    }

    private static async Task AddHttpClientAsync(this IServiceCollection services, string cookieFile, CancellationToken cancel)
    {
        var baseUrl = new Uri(XApiService.BaseUrl);
        const string userAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36 Edg/137.0.0.0";

        // 加载 Cookie
        var cookie = new CookieContainer();
        var cookieString = await File.ReadAllTextAsync(cookieFile, cancel);

        services.AddSingleton(cookie);

        foreach (var cookieItem in cookieString.Split(';', StringSplitOptions.TrimEntries))
        {
            var cookiePair = cookieItem.Split('=');

            if (cookiePair.Length == 2)
            {
                cookie.Add(baseUrl, new Cookie(cookiePair[0], cookiePair[1]));
            }
        }

        // API HttpClient
        var api = services.AddHttpClient("Api").AddAsKeyed();

        api.ConfigureHttpClient(config =>
        {
            // 配置基础地址
            config.BaseAddress = baseUrl;

            // 添加头
            config.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA");

            config.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            config.DefaultRequestHeaders.Add("X-Csrf-Token", cookie.GetCookies(baseUrl)["ct0"]?.Value);
            config.DefaultRequestHeaders.Add("X-Twitter-Active-User", "yes");
            config.DefaultRequestHeaders.Add("X-Twitter-Auth-Type", "OAuth2Session");
            config.DefaultRequestHeaders.Add("X-Twitter-Client-Language", cookie.GetCookies(baseUrl)["lang"]?.Value);

            // 启用 HTTP/2 和 HTTP/3
            config.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        });

        // 配置 Cookie
        api.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { CookieContainer = cookie });

        // 添加弹性
        api.AddStandardResilienceHandler();

        // 下载 HttpClient
        var download = services.AddHttpClient("Download").AddAsKeyed();

        download.ConfigureHttpClient(config =>
        {
            // 添加 User Agent
            config.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

            // 启用 HTTP/2 和 HTTP/3
            config.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        });

        // 添加弹性
        download.AddStandardResilienceHandler();
    }
}
