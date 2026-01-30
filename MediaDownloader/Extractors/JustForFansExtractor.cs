using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MediaDownloader.Models;
using MediaDownloader.Models.JustForFans;
using MediaDownloader.Models.JustForFans.Api;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using Scriban;

namespace MediaDownloader.Extractors;

public partial class JustForFansExtractor(
    ILogger<JustForFansExtractor> logger,
    CommandLineOptions options,
    TemplateUtilities utilities) :
    IMediaExtractor, IAsyncDisposable
{
    private const string BaseUrl = "https://justfor.fans";
    private IBrowser _browser = null!;
    private IPage _page = null!;
    private string _userHash = null!;
    private string? _playlistId;

    public async Task<List<MediaInfo>> ExtractAsync(CancellationToken cancel)
    {
        // 初始化
        await Init();

        // 获取 Hash 和 UserID
        var (hash, posterId) = await GetHashAndPosterIdAsync(options.User);

        // 获取目标用户信息
        var poster = await GetUserInfoAsync(options.User, hash);

        // 获取原始帖子
        var rawPosts = await GetRawPostsAsync(posterId, poster.UserId);

        // 解析帖子
        var posts = PrasePosts(rawPosts);

        // 获取媒体
        var medias = await GetMediasAsync(options.User, posts);

        // 排除已下载媒体
        ExcludeDownloadedMedias(medias);

        // 创建播放列表
        var playlistTitle = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {options.User} Extract";
        await CreatePlaylistAsync(playlistTitle);

        // 获取播放列表 ID
        _playlistId = await GetPlaylistIdAsync(playlistTitle);

        // 添加视频到播放列表
        await AddVideosToPlaylistAsync(medias);

        // 获取播放列表视频 URL
        var videos = await GetPlaylistVideosAsync();

        // 更新媒体 URL
        foreach (var media in medias)
        {
            if (videos.TryGetValue(media.Id, out var url))
            {
                media.Url = url;
            }
            else
            {
                logger.LogWarning("无法找到媒体 URL: {Id}", media.Id);
            }
        }

        return medias;
    }

    private async Task Init()
    {
        // 下载浏览器
        logger.LogInformation("下载浏览器");

        var fetcher = new BrowserFetcher();
        await fetcher.DownloadAsync();

        // 注册 JSON 序列化上下文
        Puppeteer.ExtraJsonSerializerContext = ResponseContext.Default;

        // 启动浏览器
        logger.LogInformation("启动浏览器");

        _browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args =
            [
                "--disable-blink-features=AutomationControlled", // 移除自动化标志
                "--no-sandbox" // 禁用沙箱模式，适配 Linux 环境
            ],
            DefaultViewport = null // 视口大小与窗口同步
        });

        // 获取页面
        _page = (await _browser.PagesAsync()).First() ?? await _browser.NewPageAsync();

        // 设置 Cookie
        logger.LogInformation("加载 Cookie");

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
        // 删除播放列表
        if (_playlistId != null)
        {
            await DeletePlaylistAsync();
        }

        await _browser.DisposeAsync();
        await _page.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    // 主要方法
    private async Task<(string hash, string userId)> GetHashAndPosterIdAsync(string user)
    {
        logger.LogInformation("获取 Hash 和 PosterID");

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
        logger.LogInformation("获取发布者信息");

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

    private async Task<List<string>> GetRawPostsAsync(string userId, string posterId)
    {
        var posts = new List<string>();
        var index = 0;

        logger.LogInformation("获取帖子:");

        while (true)
        {
            logger.LogInformation("  第 {Index} 个", index + 1);

            // 获取页面内容
            var text = await GetAsync(
                $"{BaseUrl}/ajax/getPosts.php?UserID={userId}&PosterID={posterId}&Type=One&StartAt={index}&Page=Profile&UserHash4={_userHash}&UniquePageInstance=0&Country=&IsMobile=0");

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

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (nodes == null)
            {
                logger.LogWarning("无法找到帖子节点");
                continue;
            }

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

                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (textNode != null)
                {
                    var tagNodes = textNode.SelectNodes(".//a[starts-with(text(), '#')]");

                    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
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
                    Images = images
                });
            }
        }

        return posts;
    }

    private async Task<List<MediaInfo>> GetMediasAsync(string posterName, List<PostInfo> posts)
    {
        var videoTemplate = Template.Parse("{{user}}/{{id}} {{time}} {{text}} {{tags}}{{extension}}");
        var imageTemplate = Template.Parse("{{user}}/{{id}} {{time}} {{text}} {{tags}} {{index}}{{extension}}");

        var medias = new List<MediaInfo>();

        foreach (var post in posts)
        {
            if (post.Type == PostType.Video)
            {
                var context = new TemplateData
                {
                    Id = post.Id,
                    User = posterName,
                    Time = post.Time,
                    Text = ReplaceInvalidFileNameChars(post.Text),
                    Tags = ReplaceInvalidFileNameChars(string.Join(" ", post.Tags)),
                    Type = MediaType.Video,
                };

                var media = new MediaInfo
                {
                    Id = post.Id,
                    Path = (await utilities.RenderAsync(videoTemplate, context)).Trim() + ".mp4",
                    Downloader = Models.MediaDownloader.Http,
                };

                medias.Add(media);
            }
            else
            {
                var index = 1;

                foreach (var url in post.Images)
                {
                    var context = new TemplateData
                    {
                        Id = post.Id,
                        User = posterName,
                        Time = post.Time,
                        Text = ReplaceInvalidFileNameChars(post.Text),
                        Tags = ReplaceInvalidFileNameChars(string.Join(" ", post.Tags)),
                        Index = index++,
                        Type = MediaType.Image,
                    };

                    var media = new MediaInfo
                    {
                        Id = post.Id,
                        Path = (await utilities.RenderAsync(imageTemplate, context)).Trim() + GetExtension(url),
                        Downloader = Models.MediaDownloader.JustForFans,
                        Url = url,
                    };

                    medias.Add(media);
                }
            }
        }

        return medias;
    }

    private void ExcludeDownloadedMedias(List<MediaInfo> medias)
    {
        var remove = new List<MediaInfo>();

        logger.LogInformation("排除已下载媒体:");

        foreach (var media in medias)
        {
            if (File.Exists(Path.Combine(options.Output, media.Path)))
            {
                logger.LogInformation("  {Path}", media.Path);
                remove.Add(media);
            }
        }

        foreach (var media in remove)
        {
            medias.Remove(media);
        }
    }

    private async Task CreatePlaylistAsync(string title)
    {
        var data = new PlaylistBody
        {
            Action = "AddPlaylist",
            UserHash = _userHash,
            Title = title
        };

        logger.LogInformation("创建播放列表 {Title}", title);

        var response = await PostAsync($"{BaseUrl}/ajax/playlists.php", data, PlaylistBodyContext.Default.PlaylistBody);

        if (!response.Ok)
        {
            throw new InvalidOperationException($"创建播放列表失败: {response.Status} {response.Text}");
        }
    }

    private async Task<string> GetPlaylistIdAsync(string title)
    {
        logger.LogInformation("获取播放列表 ID: {Title}", title);

        // 获取
        var text = await GetAsync($"{BaseUrl}/ajax/playlists.php?Action=loadformovie&UserHash={_userHash}&Hash=");

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

    private async Task AddVideosToPlaylistAsync(List<MediaInfo> medias)
    {
        logger.LogInformation("添加到播放列表 {PlaylistId}:", _playlistId);

        // ID 去重
        var set = medias.Select(x=> x.Id).ToHashSet();
        
        foreach (var id in set)
        {
            // 获取 Verify
            var verify = await GetPlaylistVerifyAsync(id);

            // 添加到播放列表
            logger.LogInformation("  {Id}", id);

            var response = await GetAsync(
                $"{BaseUrl}/ajax/playlists.php?Action=AddToPlaylist&UserHash={_userHash}&MovieHash={id}&PlaylistID={_playlistId}&Verify={verify}");

            if (response != "Movie added to Playlist")
            {
                throw new InvalidOperationException($"添加到播放列表失败: {response}");
            }
        }
    }

    private async Task<string> GetPlaylistVerifyAsync(string postHash)
    {
        logger.LogDebug("获取播放列表 Verify");

        // 获取
        var text = await GetAsync($"{BaseUrl}/ajax/playlists.php?Action=loadformovie&UserHash={_userHash}&Hash={postHash}");

        // 解析 HTML
        var html = new HtmlDocument();
        html.LoadHtml(text);

        // 获取播放列表 Verify
        var node = html.DocumentNode.SelectSingleNode($".//li[@data-id='{_playlistId}']");

        if (node == null)
        {
            throw new InvalidOperationException("无法获取播放列表 Verify");
        }

        var verify = node.GetAttributeValue("data-Verify", "");

        logger.LogDebug("播放列表 Verify: {Verify}", verify);

        return verify;
    }

    private async Task<Dictionary<string, string>> GetPlaylistVideosAsync()
    {
        // 获取播放列表内容
        var text = await GetAsync($"{BaseUrl}/ajax/getPlaylistForTray.php?PlaylistID={_playlistId}");

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
        var ids = idNodes.Select(node => node
            .GetAttributeValue("id", "")
            .Replace("remove-video-", "")
            .Split('-')
            .First()
        ).ToList();

        // 获取视频节点
        var videoNodes = html.DocumentNode.SelectNodes("//div[contains(@class, 'playlist-tray-video-item')]");

        if (videoNodes == null)
        {
            throw new InvalidOperationException("无法找到帖子视频");
        }

        var videos = new List<VideoSource>();

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

            videos.Add(best);
        }

        return ids.Zip(videos).ToDictionary(x => x.First, x => x.Second.Src);
    }

    private async Task DeletePlaylistAsync()
    {
        logger.LogInformation("删除播放列表 {PlaylistId}", _playlistId);

        await GetAsync($"{BaseUrl}/ajax/playlists.php?Action=DeletePlaylist&UserHash={_userHash}&PlaylistID={_playlistId}");
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
        var text = await response.TextAsync();

        if (text.Contains("Just a moment..."))
        {
            throw new Exception("请求被 Cloudflare 阻止，请检查 Cookie 是否有效");
        }

        return text;
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
        
        // 检查响应内容
        if (response.Text.Contains("Just a moment..."))
        {
            throw new Exception("请求被 Cloudflare 阻止，请检查 Cookie 是否有效");
        }

        return response;
    }

    private static string GetExtension(string url)
    {
        var uri = new Uri(url);
        var filename = uri.Segments.Last();
        var extension = Path.GetExtension(filename).ToLower();

        return extension;
    }

    private static string ReplaceInvalidFileNameChars(string filename)
    {
        var invalid = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        var pattern = $"[{invalid}]+";

        var result1 = Regex.Replace(filename, pattern, "_");
        var result2 = TextRegex().Replace(result1, " ").Trim();

        return result2;
    }

    // 正则表达式
    [GeneratedRegex(@"var\s+Hash\s*=\s*'([0-9a-fA-F]+)'")]
    private static partial Regex HashRegex();

    [GeneratedRegex(@"window\.jffUserID\s*=\s*'(\d+)'")]
    private static partial Regex UserIdRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex TextRegex();
}
