using System.CommandLine;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using XMediaDownloader;
using XMediaDownloader.Models;

// 命令行
var logLevelOption = new Option<LogEventLevel>(["-l", "--log-level"], () => LogEventLevel.Information, "日志级别");

// 下载选项
var usernameOption = new Option<string>(["-u", "--username"], "目标用户") { IsRequired = true };
var downloadTypeListOption = new Option<List<DownloadType>>(["-t", "--download-type"], "下载类型")
    { IsRequired = true, Arity = ArgumentArity.OneOrMore, AllowMultipleArgumentsPerToken = true };
var cookieFileOption = new Option<FileInfo>(["-c", "--cookie-file"], "Cookie 文件，用于请求 API") { IsRequired = true };

// 输出选项
var dirOption = new Option<string>(["-d", "--dir"], "输出目录格式") { IsRequired = true };
var filenameOption = new Option<string>(["-f", "--filename"], "输出文件名格式") { IsRequired = true };

// 根命令
var command = new RootCommand("X 媒体下载工具");

command.AddOption(logLevelOption);
command.AddOption(usernameOption);
command.AddOption(downloadTypeListOption);
command.AddOption(cookieFileOption);
command.AddOption(dirOption);
command.AddOption(filenameOption);

command.SetHandler(async context =>
{
    var logLevel = context.ParseResult.GetValueForOption(logLevelOption);
    var username = context.ParseResult.GetValueForOption(usernameOption)!;
    var downloadType = context.ParseResult.GetValueForOption(downloadTypeListOption)!.Aggregate((a, b) => a | b); // 合并参数
    var cookieFile = context.ParseResult.GetValueForOption(cookieFileOption)!;
    var dir = context.ParseResult.GetValueForOption(dirOption)!;
    var filename = context.ParseResult.GetValueForOption(filenameOption)!;

    await RunAsync(logLevel, username, downloadType, cookieFile, dir, filename, context.GetCancellationToken());
});

return await command.InvokeAsync(args);


static async Task RunAsync(LogEventLevel logLevel, string username, DownloadType downloadType, FileInfo cookieFile, string dir, string filename,
    CancellationToken cancel)
{
    // 日志
    await using var logger = new LoggerConfiguration()
        .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}")
        // .WriteTo.Console(outputTemplate: "[{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        .MinimumLevel.Is(logLevel)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
        // .MinimumLevel.Override("Microsoft.Extensions.Hosting.Internal.Host", LogEventLevel.Warning)
        // .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
        .CreateLogger();

    Log.Logger = logger;

    logger.Information("---------- X 媒体下载工具 ----------");

    try
    {
        var builder = Host.CreateApplicationBuilder(Environment.GetCommandLineArgs());

        // 命令行参数
        builder.Services.AddSingleton(new CommandLineArguments
        {
            Username = username,
            DownloadType = downloadType,
            CookieFile = cookieFile,
            Dir = dir,
            Filename = filename
        });

        // 主机日志
        builder.Services.AddSerilog();

        // 主服务
        builder.Services.AddHostedService<MainService>();

        // X API 服务
        builder.Services.AddSingleton<XApiService>();

        // 存储服务
        builder.Services.AddSingleton<StorageService>();

        // HttpClient
        await AddHttpClientAsync(builder.Services, cookieFile, cancel);

        // GraphQL
        // builder.Services.AddGraphQL(config => { });

        var app = builder.Build();

        await app.RunAsync(cancel);
    }
    catch (Exception exception)
    {
        logger.Fatal(exception, "错误");
    }

    logger.Information("应用退出");
}

static async Task AddHttpClientAsync(IServiceCollection serviceCollection, FileInfo cookieFile, CancellationToken cancel)
{
    // 加载 Cookie
    var cookie = new CookieContainer();
    var cookieString = await File.ReadAllTextAsync(cookieFile.Name, cancel);

    foreach (var cookieItem in cookieString.Split(';', StringSplitOptions.TrimEntries))
    {
        var cookiePair = cookieItem.Split('=');

        if (cookiePair.Length == 2)
        {
            cookie.Add(new Uri(XApiService.BaseUrl), new Cookie(cookiePair[0], cookiePair[1]));
        }
    }

    serviceCollection.AddSingleton(cookie);

    // 创建 HttpClient
    var httpClientBuilder = serviceCollection.AddHttpClient("X", config =>
    {
        // 基础地址
        config.BaseAddress = new Uri(XApiService.BaseUrl);

        // User-Agent
        config.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36 Edg/137.0.0.0");

        // 验证
        config.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA");
        config.DefaultRequestHeaders.Add("x-csrf-token", cookie.GetCookies(new Uri(XApiService.BaseUrl))["ct0"]?.Value);
        config.DefaultRequestHeaders.Add("x-twitter-active-user", "yes");
        config.DefaultRequestHeaders.Add("x-twitter-auth-type", "OAuth2Session");
        config.DefaultRequestHeaders.Add("x-twitter-client-language",
            cookie.GetCookies(new Uri(XApiService.BaseUrl))["lang"]?.Value);
    });

    // 设置 Cookie
    httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { CookieContainer = cookie });

    // 弹性
    httpClientBuilder.AddStandardResilienceHandler();
}