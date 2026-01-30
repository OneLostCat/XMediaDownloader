using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MediaDownloader.Models;
using MediaDownloader.Models.X;
using MediaDownloader.Models.X.Api;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

namespace MediaDownloader.Extractors;

public class XExtractor(ILogger<XExtractor> logger, CommandLineOptions options) : IMediaExtractor
{
    private readonly HttpClient _http = BuildHttpClient(options.Cookie);

    private static HttpClient BuildHttpClient(string cookieFile)
    {
        var baseUrl = new Uri("https://x.com/i/api/graphql/");
        
        // 读取 Cookie
        var cookie = new CookieContainer();
        var cookieHeader = File.ReadAllText(cookieFile);

        foreach (var item in cookieHeader.Split(';', StringSplitOptions.TrimEntries))
        {
            var pair = item.Split('=');
            cookie.Add(baseUrl, new Cookie(pair[0], pair[1]));
        }
        
        var socketHandler = new SocketsHttpHandler
        {
            UseCookies = true,
            CookieContainer = cookie,
            PooledConnectionLifetime = TimeSpan.FromMinutes(15) // 连接池生命周期
        };
        
        // 弹性
        var resiliencePipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new HttpRetryStrategyOptions
            {
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(2),
                MaxRetryAttempts = 5,
                UseJitter = true
            })
            .Build();
        
        var resilienceHandler = new ResilienceHandler(resiliencePipeline) { InnerHandler = socketHandler };
        
        // 创建 HttpClient
        var http = new HttpClient(resilienceHandler);
        
        // 设置基地址
        http.BaseAddress = baseUrl;

        // 启用 HTTP/2 和 HTTP/3
        http.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

        // 设置 User Agent
        http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36 Edg/137.0.0.0");

        // 设置认证头
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA");

        http.DefaultRequestHeaders.Add("X-Csrf-Token", cookie.GetCookies(baseUrl)["ct0"]?.Value);
        http.DefaultRequestHeaders.Add("X-Twitter-Active-User", "yes");
        http.DefaultRequestHeaders.Add("X-Twitter-Auth-Type", "OAuth2Session");
        http.DefaultRequestHeaders.Add("X-Twitter-Client-Language", cookie.GetCookies(baseUrl)["lang"]?.Value);

        return http;
    }
    
    // 主要方法
    public async Task<List<MediaInfo>> ExtractAsync(CancellationToken cancel)
    {
        // 获取用户信息
        var user = await GetUserByScreenNameAsync(options.User, cancel);

        // 获取帖子
        var tweets = await GetUserMediaTweetsAsync(user, cancel);

        // 获取媒体
        var medias = GetMedias(user, tweets, cancel);

        return medias;
    }

    private async Task<XUser> GetUserByScreenNameAsync(string username, CancellationToken cancel)
    {
        // 参数
        var variables = JsonSerializer.Serialize(new UserByScreenNameVariables(username),
            UserByScreenNameVariablesContext.Default.UserByScreenNameVariables);

        // 发送请求
        var response = await _http.GetAsync(
            BuildUrl(UserByScreenNameUrl, variables, UserByScreenNameFeatures, "{\"withAuxiliaryUserLabels\":true}"), cancel);


        // 解析响应
        var content = await response.Content.ReadFromJsonAsync<GraphQlResponse<UserByScreenNameResponse>>(
            UserByScreenNameResponseContext.Default.GraphQlResponseUserByScreenNameResponse, cancel);

        if (content?.Data == null) throw new Exception("无法获取用户信息"); // 无法判断是否会为空

        // 解析用户
        var result = content.Data.User.Result;
        var user = new XUser
        {
            Id = result.RestId,
            Name = result.Legacy.ScreenName,
            Nickname = result.Legacy.Name,
            Description = result.Legacy.Description,
            CreationTime = DateTimeOffset.ParseExact(result.Legacy.CreatedAt, TimeFormat, CultureInfo.InvariantCulture),
            MediaTweetCount = result.Legacy.MediaCount
        };

        return user;
    }

    private async Task<List<XTweet>> GetUserMediaTweetsAsync(XUser user, CancellationToken cancel)
    {
        var tweets = new List<XTweet>();
        var cursor = "";

        var total = user.MediaTweetCount; // 总帖子数量
        var count = tweets.Count; // 当前帖子数量
        var mediaCount = tweets.Sum(tweet => tweet.Media.Count); // 当前媒体数量

        logger.LogInformation("开始获取信息");

        while (true)
        {
            // 检查是否取消
            cancel.ThrowIfCancellationRequested();

            // 获取帖子
            var (list, nextCursor) = await GetUserMediaTweetsAsync(user.Id, cursor, cancel);

            // 如果没有更多帖子则退出
            if (list.Count == 0) break;

            foreach (var tweet in list)
            {
                // 储存
                tweets.Add(tweet);

                // 增加计数
                count++;
                mediaCount += tweet.Media.Count;

                // 输出信息
                logger.LogInformation("获取信息 {Id} {Time:yyyy-MM-dd HH:mm:ss} ({Count} / {Total})", tweet.Id,
                    tweet.CreationTime.LocalDateTime, count, total);

                foreach (var media in tweet.Media)
                {
                    logger.LogInformation("  {Type} {Url}", media.Type, media.Url);
                }
            }

            // 更新指针
            cursor = nextCursor;
        }

        // 排序
        tweets.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.Ordinal));

        // 输出信息
        logger.LogInformation("信息获取完成: 成功获取 {TweetCount} 条帖子，{MediaCount} 个媒体", count, mediaCount);

        return tweets;
    }

    private async Task<(List<XTweet> list, string cursor)> GetUserMediaTweetsAsync(string userId, string cursor,
        CancellationToken cancel)
    {
        // 参数
        var variables = JsonSerializer.Serialize(new UserMediaVariables(userId, 40, cursor),
            UserMediaVariablesContext.Default.UserMediaVariables);

        // 发送请求
        var response = await _http.GetAsync(
            BuildUrl(UserMediaUrl, variables, UserMediaFeatures, "{\"withArticlePlainText\":false}"), cancel);

        // 解析
        var content = await response.Content.ReadFromJsonAsync<GraphQlResponse<UserMediaResponse>>(
            UserMediaResponseContext.Default.GraphQlResponseUserMediaResponse, cancel);

        if (content?.Data == null) throw new Exception("无法获取用户媒体"); // 无法判断是否会为空

        // 处理帖子
        var list = new List<XTweet>();
        var nextCursor = "";

        foreach (var instruction in content.Data.User.Result.TimelineV2.Timeline.Instructions)
        {
            switch (instruction.Type)
            {
                case "TimelineAddEntries":
                {
                    foreach (var entry in instruction.Entries)
                    {
                        if (entry.EntryId.StartsWith("profile-"))
                        {
                            list.AddRange(ProcessTweets(entry));
                        }
                        else if (entry.EntryId.StartsWith("cursor-bottom-"))
                        {
                            nextCursor = entry.Content.Value ?? throw new Exception("无法获取下一个指针"); // 无法判断是否会为空
                        }
                    }

                    break;
                }
                case "TimelineAddToModule":
                {
                    list.AddRange(instruction.ModuleItems
                        .Where(entry => entry.EntryId.StartsWith("profile-"))
                        .Select(ProcessTweet)
                    );

                    break;
                }
            }
        }

        return (list, nextCursor);
    }

    private List<MediaInfo> GetMedias(XUser user, List<XTweet> tweets, CancellationToken cancel)
    {
        var medias = new List<MediaInfo>();

        // 遍历帖子
        foreach (var tweet in tweets)
        {
            // 遍历媒体
            for (var i = 0; i < tweet.Media.Count; i++)
            {
                // 检查是否取消
                cancel.ThrowIfCancellationRequested();

                // 获取媒体
                var media = tweet.Media[i];
                var items = new List<(string Url, string Extension)>();

                // 获取图片 (包括视频和 GIF 的封面)
                var index = media.Url.LastIndexOf('.'); // 获取最后一个 "." 的位置

                if (index == -1)
                {
                    throw new ArgumentException("无法获取原始图片 Url", media.Url);
                }

                // 获取图片格式
                var format = media.Url[(index + 1)..];

                items.Add(($"{media.Url[..index]}?format={format}&name=orig", $".{format}"));

                // 跳过无需下载的媒体类型
                if (options.Type.All(x => x != media.Type switch
                    {
                        XMediaType.Image => MediaType.Image,
                        XMediaType.Video => MediaType.Video,
                        XMediaType.Gif => MediaType.Gif,
                        _ => throw new ArgumentOutOfRangeException(nameof(x), x, "未知的媒体类型")
                    }))
                {
                    continue;
                }

                // 获取视频和 GIF
                if (media.Type != XMediaType.Image)
                {
                    // 获取最高质量视频
                    var video = media.Video
                        .OrderByDescending(x => x.Bitrate)
                        .First();

                    items.Add((video.Url, Path.GetExtension(new Uri(video.Url).Segments.Last())));
                }

                // 添加媒体项
                medias.AddRange(items.Select(item => new MediaInfo
                {
                    Url = item.Url,
                    Extension = item.Extension,
                    Downloader = Models.MediaDownloader.Http,
                    DefaultTemplate = "{{user}}/{{id}} {{time}} {{index}}",
                    Id = tweet.Id,
                    User = user.Name,
                    Time = tweet.CreationTime.LocalDateTime,
                    Index = i + 1,
                    Type = media.Type switch
                    {
                        XMediaType.Image => MediaType.Image,
                        XMediaType.Video => MediaType.Video,
                        XMediaType.Gif => MediaType.Gif,
                        _ => throw new ArgumentOutOfRangeException(nameof(media.Type), media.Type, "未知媒体类型")
                    }
                }));
            }
        }

        return medias;
    }

    // 工具方法
    

    private static string BuildUrl(string endpoint, string variables, string? features = null, string? fieldToggles = null)
    {
        var sb = new StringBuilder(endpoint + $"?variables={variables}");

        if (features != null) sb.Append($"&features={features}");

        if (fieldToggles != null) sb.Append($"&fieldToggles={fieldToggles}");

        return sb.ToString();
    }

    private static IEnumerable<XTweet> ProcessTweets(UserMediaResponseEntry entry)
    {
        foreach (var item in entry.Content.Items)
        {
            var tweetResult = item.Item.ItemContent.TweetResults.Result;

            // 处理特别的帖子
            var restid = tweetResult.RestId ?? tweetResult.Tweet!.RestId;
            var core = tweetResult.Core ?? tweetResult.Tweet!.Core;
            var legacy = tweetResult.Legacy ?? tweetResult.Tweet!.Legacy;

            // 获取用户信息
            var userResult = core.XUserResults.Result;

            if (userResult.RestId == null) throw new Exception("无法获取用户 ID"); // 无法判断是否会为空

            // 解析帖子
            var tweet = new XTweet
            {
                Id = restid,
                UserId = userResult.RestId,
                CreationTime = DateTimeOffset.ParseExact(legacy.CreatedAt, TimeFormat, CultureInfo.InvariantCulture),
                Text = legacy.FullText,
                Hashtags = legacy.Entities.Hashtags.Select(x => x.Text).ToList(),
                Media = ProcessMedia(legacy.ExtendedEntities?.Media ?? throw new Exception("无法获取媒体")) // 无法判断是否会为空
                    .ToList()
            };

            yield return tweet;
        }
    }

    private static XTweet ProcessTweet(UserMediaResponseItem entry)
    {
        var tweetResult = entry.Item.ItemContent.TweetResults.Result;

        // 处理特别的帖子
        var restid = tweetResult.RestId ?? tweetResult.Tweet!.RestId;
        var core = tweetResult.Core ?? tweetResult.Tweet!.Core;
        var legacy = tweetResult.Legacy ?? tweetResult.Tweet!.Legacy;

        // 获取用户信息
        var userResult = core.XUserResults.Result;

        if (userResult.RestId == null) throw new Exception("无法获取用户 ID"); // 无法判断是否会为空

        // 解析帖子
        var tweet = new XTweet
        {
            Id = restid,
            UserId = userResult.RestId,
            CreationTime = DateTimeOffset.ParseExact(legacy.CreatedAt, TimeFormat, CultureInfo.InvariantCulture),
            Text = legacy.FullText,
            Hashtags = legacy.Entities.Hashtags.Select(x => x.Text).ToList(),
            Media = ProcessMedia(legacy.Entities.Media).ToList()
        };

        return tweet;
    }

    private static IEnumerable<XMedia> ProcessMedia(List<UserMediaResponseMedia> mediaEntities) => mediaEntities
        .Select(x => new XMedia
        {
            Type = x.Type switch
            {
                "photo" => XMediaType.Image,
                "video" => XMediaType.Video,
                "animated_gif" => XMediaType.Gif,
                _ => throw new ArgumentException("未知媒体类型", x.Type)
            },
            Url = x.MediaUrlHttps,
            Video = x.VideoInfo?.Variants.Select(y => new XVideo { Url = y.Url, Bitrate = y.Bitrate }).ToList() ?? []
        });

    #region 常量

    private const string TimeFormat = "ddd MMM dd HH:mm:ss zzz yyyy";

    private const string UserByScreenNameUrl = "1VOOyvKkiI3FMmkeDNxM9A/UserByScreenName";
    private const string UserMediaUrl = "BGmkmGDG0kZPM-aoQtNTTw/UserMedia";
    // private const string UserTweets = "q6xj5bs0hapm9309hexA_g/UserTweets";

    private const string UserByScreenNameFeatures =
        """
        {
            "creator_subscriptions_tweet_preview_api_enabled":true,
            "hidden_profile_subscriptions_enabled":true,
            "highlights_tweets_tab_ui_enabled":true,
            "profile_label_improvements_pcf_label_in_post_enabled":true,
            "responsive_web_graphql_skip_user_profile_image_extensions_enabled":false,
            "responsive_web_graphql_timeline_navigation_enabled":true,
            "responsive_web_twitter_article_notes_tab_enabled":true,
            "rweb_tipjar_consumption_enabled":true,
            "subscriptions_feature_can_gift_premium":true,
            "subscriptions_verification_info_is_identity_verified_enabled":true,
            "subscriptions_verification_info_verified_since_enabled":true,
            "verified_phone_label_enabled":false
        }
        """;

    private const string UserMediaFeatures =
        """
        {
            "rweb_video_screen_enabled":false,
            "payments_enabled":false,
            "profile_label_improvements_pcf_label_in_post_enabled":true,
            "rweb_tipjar_consumption_enabled":true,
            "verified_phone_label_enabled":false,
            "creator_subscriptions_tweet_preview_api_enabled":true,
            "responsive_web_graphql_timeline_navigation_enabled":true,
            "responsive_web_graphql_skip_user_profile_image_extensions_enabled":false,
            "premium_content_api_read_enabled":false,
            "communities_web_enable_tweet_community_results_fetch":true,
            "c9s_tweet_anatomy_moderator_badge_enabled":true,
            "responsive_web_grok_analyze_button_fetch_trends_enabled":false,
            "responsive_web_grok_analyze_post_followups_enabled":true,
            "responsive_web_jetfuel_frame":true,
            "responsive_web_grok_share_attachment_enabled":true,
            "articles_preview_enabled":true,
            "responsive_web_edit_tweet_api_enabled":true,
            "graphql_is_translatable_rweb_tweet_is_translatable_enabled":true,
            "view_counts_everywhere_api_enabled":true,
            "longform_notetweets_consumption_enabled":true,
            "responsive_web_twitter_article_tweet_consumption_enabled":true,
            "tweet_awards_web_tipping_enabled":false,
            "responsive_web_grok_show_grok_translated_post":false,
            "responsive_web_grok_analysis_button_from_backend":false,
            "creator_subscriptions_quote_tweet_preview_enabled":false,
            "freedom_of_speech_not_reach_fetch_enabled":true,
            "standardized_nudges_misinfo":true,
            "tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled":true,
            "longform_notetweets_rich_text_read_enabled":true,
            "longform_notetweets_inline_media_enabled":true,
            "responsive_web_grok_image_annotation_enabled":true,
            "responsive_web_grok_community_note_auto_translation_is_enabled":false,
            "responsive_web_enhance_cards_enabled":false,
            "responsive_web_graphql_exclude_directive_enabled":true,
            "rweb_video_timestamps_enabled":true
        }
        """;

    #endregion
}
