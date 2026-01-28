using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using HtmlAgilityPack;
using MediaDownloader.Models;
using MediaDownloader.Models.JustForFans;
using MediaDownloader.Models.JustForFans.Api;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace MediaDownloader.Extractors;

public partial class JustForFansExtractor(ILogger<JustForFansExtractor> logger, CommandLineOptions options)
    : IMediaExtractor, IAsyncDisposable
{
    private const string BaseUrl = "https://justfor.fans";
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public async Task<MediaCollection> ExtractAsync(CancellationToken cancel)
    {
        // 初始化
        await Init();

        // 获取 Hash 和 UserID
        var (hash, posterId) = await GetHashAndPosterIdAsync(options.User, cancel);

        // 获取目标用户信息
        var poster = await GetUserInfoAsync(options.User, hash, cancel);

        // 获取用户 Hash
        var userHash = await GetUserHash();

        // 获取原始帖子
        var rawPosts = await GetRawPostsAsync(posterId, poster.UserId, userHash, cancel);

        // 解析帖子
        var posts = PrasePosts(rawPosts);

        return new MediaCollection
        {
            Medias = [],
            Downloader = Models.MediaDownloader.Http,
            DefaultTemplate = "{user}/{id}-{title}"
        };
    }

    private async Task Init()
    {
        // 下载浏览器
        logger.LogDebug("下载浏览器...");

        var fetcher = new BrowserFetcher();
        await fetcher.DownloadAsync();

        // 启动浏览器
        logger.LogDebug("启动浏览器");

        _browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args =
            [
                "--disable-blink-features=AutomationControlled" // 移除自动化标志
            ],
            DefaultViewport = null // 视口大小与窗口同步
        });

        // 获取页面
        _page = (await _browser.PagesAsync()).First() ?? await _browser.NewPageAsync();

        // 设置 Cookie
        logger.LogDebug("加载 Cookie");

        var cookieText = await File.ReadAllTextAsync(options.Cookie);
        var cookies = cookieText
            .Split(';', StringSplitOptions.TrimEntries)
            .Select(item => item.Split('='))
            .Select(pair => new CookieParam
            {
                Name = pair[0],
                Value = pair[1],
                Domain = "justfor.fans",
                Path = "/"
            })
            .ToArray();

        await _page.SetCookieAsync(cookies);

        // 设置 User-Agent
        await _page.SetUserAgentAsync(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36 Edg/144.0.0.0");

        // 移除自动化标志
        await _page.EvaluateFunctionOnNewDocumentAsync("Object.defineProperty(navigator, 'webdriver', {get: () => undefined});");
    }

    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
        await _page.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    // 主要方法
    private async Task<(string hash, string userId)> GetHashAndPosterIdAsync(string user, CancellationToken cancel)
    {
        logger.LogDebug("开始获取 Hash 和 PosterID");

        // 获取页面内容
        var text = await GetAsync($"{BaseUrl}/{user}");

        // 获取 Hash
        var match1 = HashRegex().Match(text);

        if (!match1.Success)
        {
            throw new InvalidOperationException("无法获取 Hash");
        }

        // 获取 PosterID
        var match2 = UserIdRegex().Match(text);

        if (!match2.Success)
        {
            throw new InvalidOperationException("无法获取 PosterID");
        }

        logger.LogInformation("获取到 Hash: {Hash}, PosterID: {PosterID}", match1.Groups[1].Value, match2.Groups[1].Value);

        return (match1.Groups[1].Value, match2.Groups[1].Value);
    }

    private async Task<UserInfo> GetUserInfoAsync(string user, string hash, CancellationToken cancel)
    {
        logger.LogDebug("开始获取发布者信息");

        // 获取 JSON
        var text = await GetAsync($"{BaseUrl}/ajax/getAssetCount.php?User={user}&Ver={hash}");

        // 解析 JSON
        var poster = JsonSerializer.Deserialize<UserInfo>(text, UserInfoContext.Default.UserInfo);

        if (poster == null)
        {
            throw new InvalidOperationException("无法获取发布者信息");
        }

        logger.LogInformation("获取到发布者信息: {Poster}", poster);

        return poster;
    }

    private async Task<string> GetUserHash()
    {
        logger.LogDebug("开始获取 UserHash");
        
        var cookies = await _page.GetCookiesAsync();
        var userHash = cookies.Where(x => x.Name == "UserHash4").Select(x => x.Value).First();

        logger.LogInformation("获取到 UserHash: {UserHash}", userHash);

        return userHash;
    }

    private async Task<List<string>> GetRawPostsAsync(string userId, string posterId, string userHash, CancellationToken cancel)
    {
        var posts = new List<string>();
        var index = 0;

        logger.LogInformation("获取帖子:");

        while (true)
        {
            logger.LogInformation("  获取第 {Index} 个帖子", index + 1);

            // 获取页面内容
            var text = await GetAsync(
                $"{BaseUrl}/ajax/getPosts.php?UserID={userId}&PosterID={posterId}&Type=One&StartAt={index}&Page=Profile&UserHash4={userHash}&UniquePageInstance=0&Country=&IsMobile=0");

            // 检查结束标志
            if (text.Contains("That's all! We're as sad as you are."))
            {
                break;
            }

            posts.Add(text);
            index += 10;
        }

        return posts;
    }

    private List<PostInfo> PrasePosts(List<string> rawPosts)
    {
        var posts = new List<PostInfo>();

        logger.LogInformation("解析帖子:");

        foreach (var rowPost in rawPosts)
        {
            // 解析 HTML
            var html = new HtmlDocument();
            html.LoadHtml(rowPost);

            // 获取所有节点
            var nodes = html.DocumentNode.SelectNodes(
                ".//div[contains(@class, 'mbsc-card') and contains(@class, 'jffPostClass') and (contains(@class, 'video') or contains(@class, 'photo'))]");

            foreach (var node in nodes)
            {
                // 提取 ID
                var id = node
                    .SelectSingleNode(".//ul[@class='postMenu']")
                    .GetAttributeValue("id", "")
                    .Replace("postMenu", "");

                // 提取时间
                var timeText = node.SelectSingleNode(".//div[contains(@class, 'mbsc-card-subtitle')]")
                    .GetAttributeValue("data-server-time", "");
                var time = DateTime.ParseExact(timeText, "yyyy-MM-dd HH:mm:ss", null);

                // 提取帖子类型
                var type = node
                    .GetAttributeValue("class", "")
                    .Contains("video")
                    ? PostType.Video
                    : PostType.Photo;
                
                // 提取文本
                var text = node
                    .SelectSingleNode(".//div[contains(@class, 'fr-view')]")
                    .InnerText
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace("\t", "")
                    .Trim();
                
                // 提取图片
                List<string>? images = null;

                if (type == PostType.Photo)
                {
                    images = node
                        .SelectNodes(
                            ".//div[contains(@class, 'imageGallery') and contains(@class, 'galleryLarge')]//img[@data-lazy]")
                        .Select(x => x.GetAttributeValue("data-lazy", ""))
                        .ToList();
                }

                logger.LogInformation("  解析 {Id} {Time:yyyy-MM-dd HH:mm:ss} {Type} {Text}", id, time, type, text);

                posts.Add(new PostInfo
                {
                    Id = id,
                    Time = time,
                    Text = text,
                    Type = type,
                    Video = null,
                    Images = images
                });
            }
        }

        return posts;
    }

    private async Task AddPlaylistAsync(string userHash)
    {
    }

    // 工具方法
    private async Task<string> GetAsync(string url)
    {
        // 等待响应
        var response = await _page.GoToAsync(url);

        if (response == null)
        {
            throw new InvalidOperationException("无法获取响应");
        }

        // 获取响应内容
        return await response.TextAsync();
    }


    [GeneratedRegex(@"var\s+Hash\s*=\s*'([0-9a-fA-F]+)'")]
    private static partial Regex HashRegex();

    [GeneratedRegex(@"window\.jffUserID\s*=\s*'(\d+)'")]
    private static partial Regex UserIdRegex();
}
