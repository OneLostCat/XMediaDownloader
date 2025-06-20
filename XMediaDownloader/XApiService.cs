using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using XMediaDownloader.Models;
using XMediaDownloader.Models.GraphQl;

namespace XMediaDownloader;

public class XApiService(ILogger<XApiService> logger, IHttpClientFactory httpClientFact, StorageService storage)
{
    // API 信息
    public const string BaseUrl = "https://x.com";
    
    private const string UserByScreenNameUrl = "/i/api/graphql/1VOOyvKkiI3FMmkeDNxM9A/UserByScreenName";
    private const string UserMediaUrl = "/i/api/graphql/BGmkmGDG0kZPM-aoQtNTTw/UserMedia";
    
    private const string UserByScreenNameFeatures =
        "{\"creator_subscriptions_tweet_preview_api_enabled\":true," +
        "\"hidden_profile_subscriptions_enabled\":true," +
        "\"highlights_tweets_tab_ui_enabled\":true," +
        "\"profile_label_improvements_pcf_label_in_post_enabled\":true," +
        "\"responsive_web_graphql_skip_user_profile_image_extensions_enabled\":false," +
        "\"responsive_web_graphql_timeline_navigation_enabled\":true," +
        "\"responsive_web_twitter_article_notes_tab_enabled\":true," +
        "\"rweb_tipjar_consumption_enabled\":true," +
        "\"subscriptions_feature_can_gift_premium\":true," +
        "\"subscriptions_verification_info_is_identity_verified_enabled\":true," +
        "\"subscriptions_verification_info_verified_since_enabled\":true," +
        "\"verified_phone_label_enabled\":false}";
    private const string UserMediaFeatures =
        "{\"profile_label_improvements_pcf_label_in_post_enabled\":false," +
        "\"rweb_tipjar_consumption_enabled\":true," +
        "\"responsive_web_graphql_exclude_directive_enabled\":true," +
        "\"verified_phone_label_enabled\":false," +
        "\"creator_subscriptions_tweet_preview_api_enabled\":true," +
        "\"responsive_web_graphql_timeline_navigation_enabled\":true," +
        "\"responsive_web_graphql_skip_user_profile_image_extensions_enabled\":false," +
        "\"premium_content_api_read_enabled\":false," +
        "\"communities_web_enable_tweet_community_results_fetch\":true," +
        "\"c9s_tweet_anatomy_moderator_badge_enabled\":true," +
        "\"responsive_web_grok_analyze_button_fetch_trends_enabled\":false," +
        "\"responsive_web_grok_analyze_post_followups_enabled\":true," +
        "\"responsive_web_grok_share_attachment_enabled\":true," +
        "\"articles_preview_enabled\":true," +
        "\"responsive_web_edit_tweet_api_enabled\":true," +
        "\"graphql_is_translatable_rweb_tweet_is_translatable_enabled\":true," +
        "\"view_counts_everywhere_api_enabled\":true," +
        "\"longform_notetweets_consumption_enabled\":true," +
        "\"responsive_web_twitter_article_tweet_consumption_enabled\":true," +
        "\"tweet_awards_web_tipping_enabled\":false," +
        "\"creator_subscriptions_quote_tweet_preview_enabled\":false," +
        "\"freedom_of_speech_not_reach_fetch_enabled\":true," +
        "\"standardized_nudges_misinfo\":true," +
        "\"tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled\":true," +
        "\"rweb_video_timestamps_enabled\":true," +
        "\"longform_notetweets_rich_text_read_enabled\":true," +
        "\"longform_notetweets_inline_media_enabled\":true," +
        "\"responsive_web_enhance_cards_enabled\":false}";
    
    private const string TimeFormat = "ddd MMM dd HH:mm:ss zzz yyyy"; 

    private readonly HttpClient _httpClient = httpClientFact.CreateClient("X");

    // 公开方法
    public async Task<User> GetUserAsync(string username, CancellationToken cancel)
    {
        logger.LogInformation("获取用户信息: {Username}", username);

        // 参数
        var variables = JsonSerializer.Serialize(new UserByScreenNameVariables { ScreenName = username },
            UserByScreenNameVariablesContext.Default.UserByScreenNameVariables);

        // 创建请求
        var request = new HttpRequestMessage(HttpMethod.Get,
            BuildUrl(UserByScreenNameUrl, variables, UserByScreenNameFeatures, "{\"withAuxiliaryUserLabels\":true}"));

        // 发送请求
        var response = await _httpClient.SendAsync(request, cancel);

        // 解析响应
        var content = await response.Content.ReadFromJsonAsync<GraphQlResponse<UserByScreenNameResponse>>(
            UserByScreenNameResponseContext.Default.GraphQlResponseUserByScreenNameResponse, cancel);

        if (content?.Data == null) throw new Exception("无法获取用户信息"); // 无法判断是否会为空


        // 生成用户信息
        var user = content.Data.User.Result;

        return new User
        {
            Id = user.RestId,
            Username = user.Legacy.ScreenName,
            Name = user.Legacy.Name,
            Description = user.Legacy.Description,
            CreationTime = DateTimeOffset.ParseExact(user.Legacy.CreatedAt, TimeFormat, CultureInfo.InvariantCulture),
            MediaCount = user.Legacy.MediaCount
        };
    }

    public async Task GetMediaAsync(User user, CancellationToken cancel)
    {
        // 加载数据
        await storage.LoadAsync();
        var cursor = storage.Content.Data.GetValueOrDefault(user.Id)?.CurrentCursor ?? "";

        while (true)
        {
            var (nextCursor, newTweets) = await GetMediaAsync(user.Id, cursor, 40, cancel);

            if (newTweets.Count == 0) break;

            // 储存帖子
            storage.AddData(user, nextCursor, newTweets);
            await storage.SaveAsync();

            cursor = nextCursor;

            await Task.Delay(1000, cancel); // 等待避免请求速率过快
        }
    }

    // 私有方法
    private async Task<(string, List<Tweet>)> GetMediaAsync(string userId, string cursor, int count, CancellationToken cancel)
    {
        // 参数
        var variables = JsonSerializer.Serialize(new UserMediaVariables { UserId = userId, Cursor = cursor, Count = count },
            UserMediaVariablesContext.Default.UserMediaVariables);

        // 创建请求
        var request = new HttpRequestMessage(HttpMethod.Get,
            BuildUrl(UserMediaUrl, variables, UserMediaFeatures, "{\"withArticlePlainText\":false}"));

        // 发送请求
        var response = await _httpClient.SendAsync(request, cancel);

        // 解析
        var content = await response.Content.ReadFromJsonAsync<GraphQlResponse<UserMediaResponse>>(
            UserMediaResponseContext.Default.GraphQlResponseUserMediaResponse, cancel);

        if (content?.Data == null) throw new Exception("无法获取媒体"); // 无法判断是否会为空

        // 获取帖子
        var newTweets = new List<Tweet>();
        var nextCursor = cursor;

        foreach (var instruction in content.Data.User.Result.TimelineV2.Timeline.Instructions)
        {
            if (instruction.Type == "TimelineAddEntries")
            {
                foreach (var entry in instruction.Entries)
                {
                    if (entry.EntryId.StartsWith("profile-"))
                    {
                        var tweet = ProcessTweetMedia(entry);

                        if (tweet.Count != 0)
                        {
                            newTweets.AddRange(tweet);
                        }
                    }
                    else if (entry.EntryId.StartsWith("cursor-bottom-"))
                    {
                        nextCursor = entry.Content.Value ?? throw new Exception("无法获取下一个指针"); // 无法判断是否会为空
                    }
                }
            }
            else if (instruction.Type == "TimelineAddToModule")
            {
                newTweets.AddRange(instruction.ModuleItems
                        .Where(entry => entry.EntryId.StartsWith("profile-"))
                        .Select(ProcessTweetMediaSingle)
                    // .Where(x => x != null)
                    // .OfType<Tweet>()
                );
            }
        }

        return (nextCursor, newTweets);
    }

    private List<Tweet> ProcessTweetMedia(TimelineEntry entry)
    {
        var tweets = new List<Tweet>();

        if (entry.Content.Items.Count == 0) return tweets;

        foreach (var item in entry.Content.Items)
        {
            // if (item.Item.ItemContent.TweetResults.Result == null) continue;

            var tweetResult = item.Item.ItemContent.TweetResults.Result;

            // if (tweetResult.Tombstone != null) continue;

            var userInfo = tweetResult.Core.UserResults.Result;

            if (userInfo.RestId == null) throw new Exception("无法获取用户 ID"); // 无法判断是否会为空

            // 储存用户信息
            // storage.Content.Users[userInfo.RestId] = GetUserInfo(userInfo);

            var tweet = new Tweet
            {
                Id = tweetResult.RestId,
                UserId = userInfo.RestId,
                Text = tweetResult.Legacy.FullText,
                Hashtags = tweetResult.Legacy.Entities.Hashtags.Select(x => x.Text).ToList(),
                CreationTime = DateTimeOffset.ParseExact(tweetResult.Legacy.CreatedAt,TimeFormat,CultureInfo.InvariantCulture),
                Media = ProcessMedia(tweetResult.Legacy.ExtendedEntities.Media)
            };

            tweets.Add(tweet);
        }

        return tweets;
    }

    private Tweet ProcessTweetMediaSingle(ItemMedia entry)
    {
        var tweetResult = entry.Item.ItemContent.TweetResults.Result;

        // if (tweetResult == null) return null;

        var userInfo = tweetResult.Core.UserResults.Result;

        if (userInfo.RestId == null) throw new Exception("无法获取用户 ID"); // 无法判断是否会为空

        // 储存用户信息
        // storage.Content.Users[userInfo.RestId] = GetUserInfo(userInfo);

        return new Tweet
        {
            Id = tweetResult.RestId,
            UserId = userInfo.RestId,
            Text = tweetResult.Legacy.FullText,
            Hashtags = tweetResult.Legacy.Entities.Hashtags.Select(x => x.Text).ToList(),
            CreationTime = DateTimeOffset.ParseExact(tweetResult.Legacy.CreatedAt,TimeFormat,CultureInfo.InvariantCulture),
            Media = ProcessMedia(tweetResult.Legacy.Entities.Media)
        };
    }

    private List<TweetMedia> ProcessMedia(List<MediaEntity> mediaEntities)
    {
        if (mediaEntities.Count == 0) return [];

        return mediaEntities.Select(x => new TweetMedia
        {
            Type = x.Type,
            Url = x.Type == "photo"
                ? GetOriginalImageUrl(x.MediaUrlHttps)
                : GetHighestQualityVideoUrl(x.VideoInfo ?? throw new Exception("无法获取视频信息")), // 无法判断是否会为空
            Bitrate = x.Type == "video" ? GetHighestBitrate(x.VideoInfo ?? throw new Exception("无法获取视频信息")) : null // 无法判断是否会为空
        }).ToList();
    }

    // 工具方法
    private string BuildUrl(string endpoint, string variables, string? features = null, string? fieldToggles = null)
    {
        var sb = new StringBuilder(BaseUrl + endpoint + $"?variables={variables}");

        if (features != null)
        {
            sb.Append($"&features={features}");
        }

        if (fieldToggles != null)
        {
            sb.Append($"&fieldToggles={fieldToggles}");
        }

        return sb.ToString();
    }

    // private User GetUserInfo(MediaUserResult user) => new()
    // {
    //     Id = user.RestId ?? throw new Exception("用户 ID 不能为空"), // 无法判断是否会为空
    //     Username = user.Legacy.ScreenName ?? throw new Exception("用户名不能为空"), // 无法判断是否会为空
    //     Name = user.Legacy.Name,
    //     Description = user.Legacy.Description != null ? Regex.Unescape(user.Legacy.Description) : null, // Regex.Unescape 移除转义
    //     CreationTime = DateTimeOffset.UtcNow,
    //     MediaCount = 0 // TODO
    // };

    private string GetOriginalImageUrl(string url)
    {
        var parts = url.Split('.');
        var ext = parts.Last();
        var basePath = string.Join(".", parts.Take(parts.Length - 1));

        return $"{basePath}?format={ext}&name=orig";
    }

    private string GetHighestQualityVideoUrl(VideoInfo videoInfo) => videoInfo.Variants
        .Where(x => x.Bitrate.HasValue)
        .OrderByDescending(x => x.Bitrate)
        .First()
        .Url;

    private long? GetHighestBitrate(VideoInfo videoInfo) => videoInfo.Variants
        .Where(v => v.Bitrate.HasValue)
        .Max(v => v.Bitrate);
}