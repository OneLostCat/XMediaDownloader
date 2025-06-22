using System.CommandLine;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using XMediaDownloader;
using XMediaDownloader.Models;

return await BuildCommandLine().InvokeAsync(args);

static RootCommand BuildCommandLine()
{
    // 命令行
    // 信息获取选项
    var usernameOption = new Option<string>(["-u", "--username"], "目标用户") { IsRequired = true };
    var cookieFileOption = new Option<FileInfo>(["-c", "--cookie-file"], "用于访问 API 的 Cookie 文件") { IsRequired = true };


    // 下载选项
    var outputPathFormatOption = new Option<string>(["-o", "--output-path-format"],
        () => @"{Username}\{Username}-{TweetCreationTime}-{TweetId}-{MediaIndex}.{Extension}", "输出路径格式");
    
    var downloadTypeOption = new Option<List<MediaType>>(["-t", "--download-type"], () => [MediaType.All], "媒体下载类型")
        { Arity = ArgumentArity.OneOrMore, AllowMultipleArgumentsPerToken = true };
    
    var withoutDownloadInfoOption = new Option<bool>("--without-download-info", "不下载信息");
    var withoutDownloadMediaOption = new Option<bool>("--without-download-media", "不下载媒体");

    // 日志选项
    var logLevelOption = new Option<LogEventLevel>(["-l", "--log-level"], () => LogEventLevel.Information, "日志级别");

    // 文件路径转换选项
    var originPathFormatOption = new Option<string>("--origin-path-format", "原始路径格式") { IsRequired = true };

    // 文件路径转换命令
    var convertCommand = new Command("convert", "文件路径格式转换工具");

    convertCommand.AddOption(usernameOption);
    convertCommand.AddOption(cookieFileOption);
    convertCommand.AddOption(originPathFormatOption);
    convertCommand.AddOption(outputPathFormatOption);
    convertCommand.AddOption(withoutDownloadInfoOption);
    convertCommand.AddOption(logLevelOption);
    // convertCommand.SetHandler(() => { });

    // 根命令
    var command = new RootCommand("X 媒体下载工具");

    command.AddOption(usernameOption);
    command.AddOption(cookieFileOption);
    command.AddOption(outputPathFormatOption);
    command.AddOption(downloadTypeOption);
    command.AddOption(withoutDownloadInfoOption);
    command.AddOption(withoutDownloadMediaOption);
    command.AddOption(logLevelOption);
    command.AddCommand(convertCommand);

    command.SetHandler(async context =>
    {
        var username = context.ParseResult.GetValueForOption(usernameOption)!;
        var cookieFile = context.ParseResult.GetValueForOption(cookieFileOption)!;
        var outputPath = context.ParseResult.GetValueForOption(outputPathFormatOption)!;
        var downloadType = context.ParseResult.GetValueForOption(downloadTypeOption)!.Aggregate((a, b) => a | b); // 合并参数
        var withoutDownloadInfo = context.ParseResult.GetValueForOption(withoutDownloadInfoOption);
        var withoutDownloadMedia = context.ParseResult.GetValueForOption(withoutDownloadMediaOption);
        var logLevel = context.ParseResult.GetValueForOption(logLevelOption);

        await Main(username, cookieFile, outputPath, downloadType, withoutDownloadInfo, withoutDownloadMedia, logLevel,
            context.GetCancellationToken());
    });

    return command;
}

static async Task Main(string username, FileInfo cookieFile, string outputPath, MediaType mediaType, bool withoutDownloadInfo,
    bool withoutDownloadMedia, LogEventLevel logLevel, CancellationToken cancel)
{
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

    // 启动信息
    logger.Information("---------- X 媒体下载工具 ----------");

    try
    {
        var builder = Host.CreateApplicationBuilder(Environment.GetCommandLineArgs());

        // 命令行参数
        builder.Services.AddSingleton(_ => new CommandLineArguments
        {
            Username = username,
            CookieFile = cookieFile,
            OutputPath = outputPath,
            MediaType = mediaType,
            WithoutDownloadInfo = withoutDownloadInfo,
            WithoutDownloadMedia = withoutDownloadMedia,
        });

        builder.Services.AddSerilog();
        await AddHttpClient(builder.Services, cookieFile, cancel);
        // builder.Services.AddGraphQL(config => { });
        builder.Services.AddSingleton<XApiService>();
        builder.Services.AddSingleton<StorageService>();
        builder.Services.AddSingleton<DownloadService>();
        builder.Services.AddHostedService<MainService>();

        var app = builder.Build();

        await app.RunAsync(cancel);
    }
    catch (Exception exception)
    {
        logger.Fatal(exception, "错误");
    }

    logger.Debug("应用退出");
}

static async Task AddHttpClient(IServiceCollection services, FileInfo cookieFile, CancellationToken cancel)
{
    var baseUrl = new Uri("https://x.com");
    const string userAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36 Edg/137.0.0.0";

    // 加载 Cookie
    var cookie = new CookieContainer();
    var cookieString = await File.ReadAllTextAsync(cookieFile.Name, cancel);

    foreach (var cookieItem in cookieString.Split(';', StringSplitOptions.TrimEntries))
    {
        var cookiePair = cookieItem.Split('=');

        if (cookiePair.Length == 2)
        {
            cookie.Add(baseUrl, new Cookie(cookiePair[0], cookiePair[1]));
        }
    }

    services.AddSingleton(cookie);

    // 下载
    var download = services.AddHttpClient("Download").AddAsKeyed();

    download.ConfigureHttpClient(config =>
    {
        // 启用 HTTP/2 和 HTTP/3
        config.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

        // 添加 User Agent
        config.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
    });

    // 添加弹性
    download.AddStandardResilienceHandler();


    // API
    var api = services.AddHttpClient("Api").AddAsKeyed();

    api.ConfigureHttpClient((services, config) =>
    {
        // 启用 HTTP/2 和 HTTP/3
        config.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

        // 配置基础地址
        config.BaseAddress = baseUrl;
        
        // 添加头
        var cookie = services.GetRequiredService<CookieContainer>();

        config.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA");
        config.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        config.DefaultRequestHeaders.Add("X-Csrf-Token", cookie.GetCookies(baseUrl)["ct0"]?.Value);
        config.DefaultRequestHeaders.Add("X-Twitter-Active-User", "yes");
        config.DefaultRequestHeaders.Add("X-Twitter-Auth-Type", "OAuth2Session");
        config.DefaultRequestHeaders.Add("X-Twitter-Client-Language", cookie.GetCookies(baseUrl)["lang"]?.Value);
    });

    // 配置 Cookie
    api.ConfigurePrimaryHttpMessageHandler(services => new HttpClientHandler
        { CookieContainer = services.GetRequiredService<CookieContainer>() });

    // 添加弹性
    api.AddStandardResilienceHandler();
}