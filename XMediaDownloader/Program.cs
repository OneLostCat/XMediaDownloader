using System.CommandLine;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using XMediaDownloader;
using XMediaDownloader.Models;
using AppJsonSerializerContext = XMediaDownloader.Models.AppJsonSerializerContext;

// 日志
await using var logger = new LoggerConfiguration()
    .WriteTo.Console()
    // .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

Log.Logger = logger;

// 下载选项
var accountOption = new Option<string>(["-a", "--account"], "目标账户") { IsRequired = true };
var mediaTypeOption = new Option<List<MediaType>>(["-t", "--type"], "目标媒体类型")
    { IsRequired = true, Arity = ArgumentArity.OneOrMore, AllowMultipleArgumentsPerToken = true };
var cookieOption = new Option<FileInfo>(["-c", "--cookie"], "Cookie 文件，用于请求 API") { IsRequired = true };

// 输出选项
var dirnameOption = new Option<string>(["-d", "--dirname"], "目录名称格式") { IsRequired = true };
var filenameOption = new Option<string>(["-f", "--filename"], "文件名称格式") { IsRequired = true };

// 根命令
var rootCommand = new RootCommand("X 媒体下载工具");

rootCommand.AddOption(accountOption);
rootCommand.AddOption(mediaTypeOption);
rootCommand.AddOption(cookieOption);
rootCommand.AddOption(dirnameOption);
rootCommand.AddOption(filenameOption);

rootCommand.SetHandler(async context =>
{
    var account = context.ParseResult.GetValueForOption(accountOption) ?? throw new Exception("无法解析目标账户");
    var mediaType = context.ParseResult.GetValueForOption(mediaTypeOption) ?? throw new Exception("无法解析媒体类型");
    var cookieFile = context.ParseResult.GetValueForOption(cookieOption) ?? throw new Exception("无法解析 Cookie 文件");
    var dirname = context.ParseResult.GetValueForOption(dirnameOption) ?? throw new Exception("无法解析目录名称格式");
    var filename = context.ParseResult.GetValueForOption(filenameOption) ?? throw new Exception("无法解析文件名称格式");

    await Handle(account, mediaType, cookieFile, dirname, filename, context.GetCancellationToken());
});

try
{
    return await rootCommand.InvokeAsync(args);
}
catch (Exception exception)
{
    logger.Fatal(exception, "错误");
    return 1;
}

static async Task Handle(string username, List<MediaType> mediaType, FileInfo cookieFile, string dirname, string filename,
    CancellationToken cancel)
{
    var serviceCollection = new ServiceCollection();

    serviceCollection.AddSerilog();

    // 读取 Cookie
    var cookie = await GetCookie(cookieFile, cancel);

    // 添加 HttpClient
    AddHttpClient(serviceCollection, cookie);

    // 构建服务
    var services = serviceCollection.BuildServiceProvider();
    var logger = services.GetRequiredService<ILogger>();
    var httpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient("X");

    // 获取目标 UserId
    var userId = await GetUserId(logger, httpClient, username, cancel);

    // 获取所有媒体
}

static async Task<CookieContainer> GetCookie(FileInfo cookieFile, CancellationToken cancel)
{
    var cookie = new CookieContainer();
    var cookieString = await File.ReadAllTextAsync(cookieFile.FullName, cancel);

    // 解析 Cookie 文件
    foreach (var cookieItem in cookieString.Split(';', StringSplitOptions.TrimEntries))
    {
        var cookiePair = cookieItem.Split('=');

        if (cookiePair.Length == 2)
        {
            cookie.Add(new Uri(XApiEndpoints.BaseUrl), new Cookie(cookiePair[0], cookiePair[1]));
        }
    }

    return cookie;
}

static void AddHttpClient(ServiceCollection serviceCollection, CookieContainer cookie)
{
    var httpClientBuilder = serviceCollection.AddHttpClient("X", config =>
    {
        // 基础地址
        config.BaseAddress = new Uri(XApiEndpoints.BaseUrl);

        // User-Agent
        config.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36 Edg/137.0.0.0");

        // 验证
        config.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", XApiEndpoints.Bearer);
        config.DefaultRequestHeaders.Add("x-csrf-token", cookie.GetCookies(new Uri(XApiEndpoints.BaseUrl))["ct0"]?.Value);
        config.DefaultRequestHeaders.Add("x-twitter-active-user", "yes");
        config.DefaultRequestHeaders.Add("x-twitter-auth-type", "OAuth2Session");
        config.DefaultRequestHeaders.Add("x-twitter-client-language", cookie.GetCookies(new Uri(XApiEndpoints.BaseUrl))["lang"]?.Value);
    });

    // 设置 Cookie
    httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { CookieContainer = cookie });

    // 弹性
    httpClientBuilder.AddStandardResilienceHandler();
}

static async Task<string> GetUserId(ILogger logger, HttpClient httpClient, string username, CancellationToken cancel)
{
    // 参数
    var variables = JsonSerializer.Serialize(new ProfileSpotlightsQueryVariables { ScreenName = username },
        AppJsonSerializerContext.Default.ProfileSpotlightsQueryVariables);

    // 请求
    var request = new HttpRequestMessage(HttpMethod.Get, $"{XApiEndpoints.ProfileSpotlightsUrl}?variables={variables}");

    // 发送请求
    var response = await httpClient.SendAsync(request, cancel);

    // 解析回应
    var content = await response.Content.ReadFromJsonAsync<GraphQlResponse>(
        AppJsonSerializerContext.Default.GraphQlResponse, cancel);

    // 获取用户 Id
    var userId = content?.Data.UserResultByScreenName.Result.RestId;

    if (userId == null)
    {
        throw new Exception("无法获取目标用户 ID");
    }

    return userId;
}

static async Task GetAllMedia(ILogger logger, HttpClient httpClient, string userId, CancellationToken cancel)
{
    var cursor = "";
    var count = 20;

    while (true)
    {
    }
    
}

static async Task<List<Tweet>> GetMedia(ILogger logger, HttpClient httpClient, string userId, int count, string cursor, CancellationToken cancel)
{
    // 参数
    var variables = JsonSerializer.Serialize(new UserMediaQueryVariables
    {
        UserId = userId,
        Count = count,
        IncludePromotedContent = false,
        WithClientEventToken = false,
        WithBirdwatchNotes = false,
        WithVoice = true,
        WithV2Timeline = true,
        Cursor = cursor
    }, AppJsonSerializerContext.Default.UserMediaQueryVariables);

    var features = JsonSerializer.Serialize(new BookmarkFeatures(), AppJsonSerializerContext.Default.BookmarkFeatures);

    // 请求
    var request = new HttpRequestMessage(HttpMethod.Get,
        $"{XApiEndpoints.UserMediaUrl}?variables={variables}&features={features}&fieldToggles={{\"withArticlePlainText\":false}}");

    // 发送请求
    var response = await httpClient.SendAsync(request, cancel);
}