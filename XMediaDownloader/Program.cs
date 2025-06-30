using System.CommandLine;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using XMediaDownloader;
using XMediaDownloader.Models;

// 主命令
// 信息获取选项
var usernameOption = new Option<string>("-u", "--username") { Description = "目标用户", Required = true };
var cookieFileOption = new Option<FileInfo>("-c", "--cookie-file") { Description = "用于请求 API 的 Cookie 文件", Required = true };

// 下载选项
var outputDirOption = new Option<DirectoryInfo>("-o", "--output-dir")
{
    Description = "输出目录",
    DefaultValueFactory = _ => new DirectoryInfo(".")
};

var outputPathFormatOption = new Option<string>("-O", "--output-path-format")
{
    Description = "输出文件路径格式",
    DefaultValueFactory = _ =>
        $"{{Username}}{Path.DirectorySeparatorChar}{{Username}}-{{TweetCreationTime}}-{{TweetId}}-{{MediaIndex}}-{{MediaType}}{{MediaExtension}}"
};

var downloadTypeOption = new Option<List<MediaType>>("-t", "--download-type")
{
    Description = "目标媒体类型",
    DefaultValueFactory = _ => [MediaType.All],
    Arity = ArgumentArity.OneOrMore,
    AllowMultipleArgumentsPerToken = true
};

var withoutDownloadInfoOption = new Option<bool>("--without-download-info")
    { Description = "无需获取信息", DefaultValueFactory = _ => false };

var withoutDownloadMediaOption = new Option<bool>("--without-download-media")
    { Description = "无需下载媒体", DefaultValueFactory = _ => false };

// 其他选项
var storageDirOption = new Option<DirectoryInfo>("-s", "--storage-dir")
    { Description = "状态存储目录", DefaultValueFactory = _ => new DirectoryInfo(".") };

// var workDirOption = new Option<DirectoryInfo>("-w", "--work-dir")
//     { Description = "工作目录", DefaultValueFactory = _ => new DirectoryInfo(".") };

var logLevelOption = new Option<LogEventLevel>("-l", "--log-level")
    { Description = "日志级别", DefaultValueFactory = _ => LogEventLevel.Information };


var command = new RootCommand("X 媒体下载工具")
{
    usernameOption,
    cookieFileOption,
    outputDirOption,
    outputPathFormatOption,
    downloadTypeOption,
    withoutDownloadInfoOption,
    withoutDownloadMediaOption,
    storageDirOption,
    // workDirOption,
    logLevelOption
};

command.SetAction((result, cancel) => Run(
    result.GetRequiredValue(usernameOption),
    result.GetRequiredValue(cookieFileOption),
    result.GetRequiredValue(outputDirOption),
    result.GetRequiredValue(outputPathFormatOption),
    result.GetRequiredValue(downloadTypeOption).Aggregate((a, b) => a | b), // 合并参数
    result.GetRequiredValue(withoutDownloadInfoOption),
    result.GetRequiredValue(withoutDownloadMediaOption),
    result.GetRequiredValue(storageDirOption),
    // result.GetRequiredValue(workDirOption),
    result.GetRequiredValue(logLevelOption),
    cancel));

// 媒体路径格式转换
var sourceDirOption = new Option<DirectoryInfo>("-s", "--source-dir") { Description = "源目录", Required = true };
var dryRunOption = new Option<bool>("-n", "--dry-run") { Description = "试运行", DefaultValueFactory = _ => false };

var convertCommand = new Command("convert", "X 媒体路径格式转换工具")
{
    sourceDirOption,
    outputDirOption,
    outputPathFormatOption,
    dryRunOption,
    // workDirOption,
    logLevelOption
};

command.Add(convertCommand);

convertCommand.SetAction((result, cancel) => PathFormatConverter.Run(
    result.GetRequiredValue(sourceDirOption),
    result.GetRequiredValue(outputDirOption),
    result.GetRequiredValue(outputPathFormatOption),
    result.GetRequiredValue(dryRunOption),
    // result.GetRequiredValue(workDirOption),
    result.GetRequiredValue(logLevelOption),
    cancel
));

return command.Parse(args).Invoke();


async Task Run(
    string username,
    FileInfo cookieFile,
    DirectoryInfo outputDir,
    string outputPathFormat,
    MediaType downloadType,
    bool withoutDownloadInfo,
    bool withoutDownloadMedia,
    DirectoryInfo storageDir,
    // DirectoryInfo workDir,
    LogEventLevel logLevel,
    CancellationToken cancel)
{
    // 设置工作目录
    // Environment.CurrentDirectory = workDir.Name;
    
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
        var builder = Host.CreateApplicationBuilder(args);

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
            // workDir,
            logLevel
        ));

        // 服务
        builder.Services.AddSerilog();
        builder.Services.AddSingleton<XApiService>();
        builder.Services.AddSingleton<StorageService>();
        builder.Services.AddSingleton<DownloadService>();
        builder.Services.AddHostedService<MainService>();
        await AddHttpClient(builder.Services, cookieFile, cancel);

        // 运行
        await builder.Build().RunAsync(cancel);
    }
    catch (Exception exception)
    {
        logger.Fatal(exception, "错误");
    }

    logger.Debug("应用退出");
}

async Task AddHttpClient(IServiceCollection services, FileInfo cookieFile, CancellationToken cancel)
{
    var baseUrl = new Uri(XApiService.BaseUrl);
    const string userAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36 Edg/137.0.0.0";

    // 加载 Cookie
    var cookie = new CookieContainer();
    var cookieString = await File.ReadAllTextAsync(cookieFile.FullName, cancel);

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
