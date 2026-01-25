using System.CommandLine;
using MediaDownloader.Fetchers;
using MediaDownloader.Models;
using MediaDownloader.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace MediaDownloader;

public static class Main
{
    public static async Task RunAsync(ParseResult result, CancellationToken cancel)
    {
        // 获取参数
        var source = result.GetRequiredValue(CommandLine.MediaSourceOption);
        var username = result.GetRequiredValue(CommandLine.UsernameOption);
        var cookie = result.GetRequiredValue(CommandLine.CookieFileOption);
        var outputDir = result.GetRequiredValue(CommandLine.OutputOption);
        var outputPathTemplate = result.GetRequiredValue(CommandLine.OutputTemplateOption);
        var downloadType = result.GetRequiredValue(CommandLine.MediaTypeOption).Aggregate((a, b) => a | b); // 合并

        // 创建日志
        await using var logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}")
            // .WriteTo.Console(outputTemplate: "[{SourceContext}] {Message:lj}{NewLine}{Exception}")
            // 去除多余的日志
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Extensions.Hosting.Internal.Host", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .MinimumLevel.Override("Polly", LogEventLevel.Warning)
            .CreateLogger();

        Log.Logger = logger;

        try
        {
            // 创建主机
            var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());

            // 注册服务
            builder.Services.AddSerilog();
            builder.Services.AddHostedService<MainService>();
            builder.Services.AddSingleton<DownloadService>();
            
            // 注册获取器
            builder.Services.AddFetcher(source);

            // 注册 HttpClient
            builder.Services.AddHttpClient();
            builder.Services.ConfigureHttpClientDefaults(config =>
            {
                config.ConfigureHttpClient(client =>
                {
                    // 启用高版本 HTTP
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                    
                    // User Agent
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36 Edg/137.0.0.0");
                });

                // 弹性
                config.AddStandardResilienceHandler();
            });
            
            // 注册命令行参数
            builder.Services.AddSingleton(new CommandLineArguments(
                username,
                cookie,
                outputDir,
                outputPathTemplate,
                downloadType
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

    private static void AddFetcher(this IServiceCollection services, MediaSource source)
    {
        switch (source)
        {
            case MediaSource.X:
                services.AddSingleton<IFetcher, XFetcher>();
                break;
            case MediaSource.JustForFans:
                services.AddSingleton<IFetcher, JustForFansFetcher>();
                break;
        }
    }
}
