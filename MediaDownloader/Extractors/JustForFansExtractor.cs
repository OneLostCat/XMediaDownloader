using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
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
    private string _userHash = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public async Task<MediaCollection> ExtractAsync(CancellationToken cancel)
    {
        // 初始化
        await Init();

        // 获取 Hash 和 UserID
        var (hash, posterId) = await GetHashAndPosterIdAsync(options.User);

        // 获取目标用户信息
        var poster = await GetUserInfoAsync(options.User, hash);

        // 获取原始帖子
        var rawPosts = await GetRawPostsAsync(posterId, poster.UserId, _userHash);

        // 解析帖子
        var posts = PrasePosts(rawPosts);

        // 创建播放列表
        var playlistTitle = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {options.User} Extract";
        await CreatePlaylistAsync(_userHash, playlistTitle);

        // 获取播放列表 ID
        var playlistId = await GetPlaylistIdAsync(_userHash, playlistTitle);

        // 添加视频到播放列表
        await AddVideosToPlaylistAsync(posts, _userHash, playlistId);

        // 获取播放列表视频 URL
        var videos = await GetPlaylistVideosAsync(playlistId);
        
        // 删除播放列表
        await DeletePlaylistAsync(_userHash, playlistId);

        // 合并视频信息
        MergeVideoInfo(ref posts, videos);
        
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

        // 注册 JSON 序列化上下文
        Puppeteer.ExtraJsonSerializerContext = ResponseContext.Default;

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
                Domain = ".justfor.fans",
                Path = "/"
            })
            .ToArray();

        await _page.SetCookieAsync(cookies);

        // 设置 User-Agent
        await _page.SetUserAgentAsync(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36 Edg/144.0.0.0");

        // 移除自动化标志
        await _page.EvaluateFunctionOnNewDocumentAsync("Object.defineProperty(navigator, 'webdriver', {get: () => undefined});");

        // 获取 UserHash
        var userHash = cookies
            .Where(x => x.Name == "UserHash4")
            .Select(x => x.Value)
            .FirstOrDefault();

        _userHash = userHash ?? throw new InvalidOperationException("无法获取 UserHash");

        logger.LogInformation("UserHash: {UserHash}", userHash);
    }

    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
        await _page.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    // 主要方法
    private async Task<(string hash, string userId)> GetHashAndPosterIdAsync(string user)
    {
        logger.LogDebug("获取 Hash 和 PosterID");

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

        logger.LogInformation("Hash: {Hash}, PosterID: {PosterID}", match1.Groups[1].Value, match2.Groups[1].Value);

        return (match1.Groups[1].Value, match2.Groups[1].Value);
    }

    private async Task<UserInfo> GetUserInfoAsync(string user, string hash)
    {
        logger.LogDebug("获取发布者信息");

        // 获取 JSON
        var text = await GetAsync($"{BaseUrl}/ajax/getAssetCount.php?User={user}&Ver={hash}");

        // 解析 JSON
        var poster = JsonSerializer.Deserialize<UserInfo>(text, UserInfoContext.Default.UserInfo);

        if (poster == null)
        {
            throw new InvalidOperationException("无法获取发布者");
        }

        logger.LogInformation("发布者信息: {Poster}", poster);

        return poster;
    }

    private async Task<List<string>> GetRawPostsAsync(string userId, string posterId, string userHash)
    {
        var posts = new List<string>();
        var index = 0;

        logger.LogInformation("获取帖子:");

        while (true)
        {
            logger.LogInformation("  第 {Index} 个", index + 1);

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

                // 提取标签和文本
                var textNode = node.SelectSingleNode(".//div[contains(@class, 'fr-view')]");

                var text = "";
                var tags = new List<string>();

                if (textNode != null)
                {
                    var tagNodes = textNode.SelectNodes(".//a[starts-with(text(), '#')]");

                    if (tagNodes != null)
                    {
                        tags = tagNodes
                            .Select(link => link.InnerText.Trim())
                            .ToList();

                        // 删除标签
                        tagNodes
                            .ToList()
                            .ForEach(x => x.Remove());
                    }

                    text = TextRegex().Replace(textNode.InnerText, " ").Trim(); // 去除多余的空白字符
                }

                // 提取图片
                var images = new List<string>();

                if (type == PostType.Photo)
                {
                    images = node
                        .SelectNodes(
                            ".//div[contains(@class, 'imageGallery') and contains(@class, 'galleryLarge')]//img[@data-lazy]")
                        .Select(x => x.GetAttributeValue("data-lazy", ""))
                        .ToList();
                }

                logger.LogInformation("  {Id} {Time:yyyy-MM-dd HH:mm:ss} {Type} {Text} {Tags}", id, time, type, text,
                    string.Join(" ", tags));

                posts.Add(new PostInfo
                {
                    Id = id,
                    Time = time,
                    Type = type,
                    Text = text,
                    Tags = tags,
                    Video = "",
                    Images = images
                });
            }
        }

        return posts;
    }

    private async Task CreatePlaylistAsync(string userHash, string title)
    {
        var data = new PlaylistBody
        {
            Action = "AddPlaylist",
            UserHash = userHash,
            Title = title
        };

        logger.LogInformation("创建播放列表 {Title}", title);

        var response = await PostAsync($"{BaseUrl}/ajax/playlists.php", data, PlaylistBodyContext.Default.PlaylistBody);

        if (!response.Ok)
        {
            throw new InvalidOperationException($"创建播放列表失败: {response.Status} {response.Text}");
        }
    }

    private async Task<string> GetPlaylistIdAsync(string userHash, string title)
    {
        logger.LogDebug("获取播放列表 ID: {Title}", title);

        // 获取
        var text = await GetAsync($"{BaseUrl}/ajax/playlists.php?Action=loadformovie&UserHash={userHash}&Hash=");

        // 解析 HTML
        var html = new HtmlDocument();
        html.LoadHtml(text);

        // 获取播放列表 ID
        var node = html.DocumentNode.SelectSingleNode($".//li[text()='{title}']");

        if (node == null)
        {
            throw new InvalidOperationException("无法获取播放列表 ID");
        }

        var id = node.GetAttributeValue("data-ID", "");

        logger.LogInformation("播放列表 ID: {Id}", id);

        return id;
    }

    private async Task AddVideosToPlaylistAsync(List<PostInfo> posts, string userHash, string playlistId)
    {
        logger.LogInformation("添加到播放列表 {PlaylistId}:", playlistId);

        foreach (var post in posts)
        {
            // 获取 Verify
            var verify = await GetPlaylistVerifyAsync(userHash, post.Id, playlistId);

            // 添加到播放列表
            logger.LogInformation("  {Id}", post.Id);

            var response = await GetAsync(
                $"{BaseUrl}/ajax/playlists.php?Action=AddToPlaylist&UserHash={userHash}&MovieHash={post.Id}&PlaylistID={playlistId}&Verify={verify}");

            if (response != "Movie added to Playlist")
            {
                throw new InvalidOperationException($"添加到播放列表失败: {response}");
            }
        }
    }

    private async Task<string> GetPlaylistVerifyAsync(string userHash, string postHash, string playlistId)
    {
        logger.LogDebug("获取播放列表 Verify");

        // 获取
        var text = await GetAsync($"{BaseUrl}/ajax/playlists.php?Action=loadformovie&UserHash={userHash}&Hash={postHash}");

        // 解析 HTML
        var html = new HtmlDocument();
        html.LoadHtml(text);

        // 获取播放列表 Verify
        var node = html.DocumentNode.SelectSingleNode($".//li[@data-id='{playlistId}']");

        if (node == null)
        {
            throw new InvalidOperationException("无法获取播放列表 Verify");
        }

        var verify = node.GetAttributeValue("data-Verify", "");

        logger.LogDebug("播放列表 Verify: {Verify}", verify);

        return verify;
    }

    private async Task<Dictionary<string, string>> GetPlaylistVideosAsync(string playlistId)
    {
        // 获取播放列表内容
        var text = await GetAsync($"{BaseUrl}/ajax/getPlaylistForTray.php?PlaylistID={playlistId}");

        // 解析 HTML
        var html = new HtmlDocument();
        html.LoadHtml(text);

        // 获取 ID 节点
        var idNodes = html.DocumentNode.SelectNodes("//div[contains(@class, 'playlist-remove-video-item')]");

        if (idNodes == null)
        {
            throw new InvalidOperationException("无法找到帖子 ID");
        }

        // 解析 ID
        var ids = new List<string>();

        foreach (var node in idNodes)
        {
            var id = node
                .GetAttributeValue("id", "")
                .Replace("remove-video-", "")
                .Split('-')
                [0];

            ids.Add(id);
        }

        // 获取视频节点
        var videoNodes = html.DocumentNode.SelectNodes("//div[contains(@class, 'playlist-tray-video-item')]");

        if (videoNodes == null)
        {
            throw new InvalidOperationException("无法找到帖子视频");
        }

        var videos = new List<string>();

        foreach (var node in videoNodes)
        {
            // 获取视频 url
            var json = HtmlEntity.DeEntitize(node.GetAttributeValue("data-sources", ""));
            var sources = JsonSerializer.Deserialize<List<VideoSource>>(json, ListVideoSourceContext.Default.ListVideoSource);

            if (sources == null)
            {
                throw new InvalidOperationException("无法解析视频源");
            }

            // 选择清晰度最高的
            var best = sources
                .OrderByDescending(source => int.Parse(source.Res))
                .First();

            videos.Add(best.Src);
        }

        return ids.Zip(videos).ToDictionary(x => x.First, x => x.Second);
    }

    private async Task DeletePlaylistAsync(string userHash, string playlistId)
    {
        logger.LogInformation("删除播放列表 {PlaylistId}", playlistId);
        
        await GetAsync($"{BaseUrl}/ajax/playlists.php?Action=DeletePlaylist&UserHash={userHash}&PlaylistID={playlistId}");
    }
    
    private void MergeVideoInfo(ref List<PostInfo> posts, Dictionary<string, string> videos)
    {
        foreach (var post in posts)
        {
            if (videos.TryGetValue(post.Id, out var url))
            {
                post.Video = url;
            }
        }
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

    private async Task<Response> PostAsync<T>(string url, T body, JsonTypeInfo<T> info)
    {
        // 转换为 JSON
        var json = JsonSerializer.Serialize(body, info);

        // 发送请求
        var response = await _page.EvaluateFunctionAsync<Response>(
            """
            async (url, json) => {
                const body = JSON.parse(json);
                
                const params = new URLSearchParams();
                for (const key in body) {
                    params.append(key, body[key]);
                }
                
                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'X-Requested-With': 'XMLHttpRequest' // 模拟 AJAX 请求
                    },
                    body: params
                });

                return { ok: response.ok, status: response.status, text: await response.text() };
            }
            """,
            url,
            json
        );

        return response;
    }

    // 正则表达式
    [GeneratedRegex(@"var\s+Hash\s*=\s*'([0-9a-fA-F]+)'")]
    private static partial Regex HashRegex();

    [GeneratedRegex(@"window\.jffUserID\s*=\s*'(\d+)'")]
    private static partial Regex UserIdRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex TextRegex();
}
