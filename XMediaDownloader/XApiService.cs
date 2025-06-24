using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XMediaDownloader.Models;
using XMediaDownloader.Models.XApi;

namespace XMediaDownloader;

public class XApiService(ILogger<XApiService> logger, StorageService storage, [FromKeyedServices("Api")] HttpClient httpClient)
{
    // API 端点
    public const string BaseUrl = "https://x.com/i/api/graphql/";
    private const string UserByScreenNameUrl = "1VOOyvKkiI3FMmkeDNxM9A/UserByScreenName";
    private const string UserTweets = "q6xj5bs0hapm9309hexA_g/UserTweets";
    private const string UserMediaUrl = "BGmkmGDG0kZPM-aoQtNTTw/UserMedia";

    #region Features

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

    #endregion

    private const string TimeFormat = "ddd MMM dd HH:mm:ss zzz yyyy";

    // 主要方法
    public async Task<User> GetUserByScreenNameAsync(string username, CancellationToken cancel)
    {
        // 参数
        var variables = JsonSerializer.Serialize(new UserByScreenNameVariables(username),
            UserByScreenNameVariablesContext.Default.UserByScreenNameVariables);

        // 发送请求
        var response = await httpClient.GetAsync(
            BuildUrl(UserByScreenNameUrl, variables, UserByScreenNameFeatures, "{\"withAuxiliaryUserLabels\":true}"), cancel);

        // 解析响应
        var content = await response.Content.ReadFromJsonAsync<GraphQlResponse<UserByScreenNameResponse>>(
            UserByScreenNameResponseContext.Default.GraphQlResponseUserByScreenNameResponse, cancel);

        if (content?.Data == null) throw new Exception("无法获取用户信息"); // 无法判断是否会为空

        // 解析用户信息
        var result = content.Data.User.Result;
        var user = new User
        {
            Id = result.RestId,
            Name = result.Legacy.ScreenName,
            Nickname = result.Legacy.Name,
            Description = result.Legacy.Description,
            CreationTime = DateTimeOffset.ParseExact(result.Legacy.CreatedAt, TimeFormat, CultureInfo.InvariantCulture),
            MediaCount = result.Legacy.MediaCount
        };

        return user;
    }

    public async Task GetUserTweetsAsync(string userId, CancellationToken cancel)
    {
    }

    public async Task GetUserMediaAsync(string userId, CancellationToken cancel)
    {
        var data = storage.Content.Users[userId];
        var tweets = data.Tweets;
        var cursor = data.CurrentCursor ?? ""; // 当前指针

        var totalMediaCount = data.Info.MediaCount; // 总媒体数量
        var tweetCount = tweets.Count; // 当前帖子数量
        var mediaCount = tweets.Sum(x => x.Value.Media.Count); // 当前媒体数量

        logger.LogInformation("开始获取信息");

        while (true)
        {
            // 检查是否取消
            cancel.ThrowIfCancellationRequested();

            // 获取媒体
            var (newTweets, nextCursor) = await GetUserMediaAsync(userId, cursor, cancel);

            // 如果没有更多媒体则退出
            if (newTweets.Count == 0) break;

            foreach (var tweet in newTweets)
            {
                // 合并帖子
                var isNew = tweets.TryAdd(tweet.Id, tweet);

                // 增加帖子计数
                if (isNew) tweetCount++;

                logger.LogInformation("获取信息 {CreationTime:yyyy-MM-dd HH:mm:ss zzz} {Id}", tweet.CreationTime, tweet.Id);

                foreach (var media in tweet.Media)
                {
                    // 增加媒体计数
                    if (isNew) mediaCount++;

                    logger.LogInformation("  {Type} {Url} ({MediaCount} / {TotalMediaCount})", media.Type, media.Url, mediaCount,
                        totalMediaCount);
                }
            }

            // 更新指针
            data.CurrentCursor = cursor = nextCursor;

            // 避免请求速率过快
            // await Task.Delay(1000, cancel);
        }

        logger.LogInformation("信息获取完成: 成功获取 {TweetCount} 条帖子 / {MediaCount} 个媒体", tweetCount, mediaCount);
    }

    private async Task<(List<Tweet>, string)> GetUserMediaAsync(string userId, string cursor, CancellationToken cancel)
    {
        // 参数
        var variables = JsonSerializer.Serialize(new UserMediaVariables(userId, cursor),
            UserMediaVariablesContext.Default.UserMediaVariables);

        // 发送请求
        var response = await httpClient.GetAsync(
            BuildUrl(UserMediaUrl, variables, UserMediaFeatures, "{\"withArticlePlainText\":false}"), cancel);

        // 解析
        var content = await response.Content.ReadFromJsonAsync<GraphQlResponse<UserMediaResponse>>(
            UserMediaResponseContext.Default.GraphQlResponseUserMediaResponse, cancel);

        if (content?.Data == null) throw new Exception("无法获取用户媒体"); // 无法判断是否会为空

        // 获取帖子
        var newTweets = new List<Tweet>();
        var nextCursor = "";

        foreach (var instruction in content.Data.User.Result.TimelineV2.Timeline.Instructions)
        {
            switch (instruction.Type)
            {
                case "TimelineAddEntries":
                    var result = ProcessTimelineAddEntries(instruction);

                    newTweets.AddRange(result.tweets);
                    nextCursor = result.cursor;

                    break;
                case "TimelineAddToModule":
                    newTweets.AddRange(ProcessTimelineAddToModule(instruction));
                    break;
            }
        }

        return (newTweets, nextCursor);
    }

    // 工具方法
    private static (List<Tweet> tweets, string cursor) ProcessTimelineAddEntries(UserMediaResponseInstruction instruction)
    {
        var tweets = new List<Tweet>();
        var cursor = "";

        foreach (var entry in instruction.Entries)
        {
            if (entry.EntryId.StartsWith("profile-"))
            {
                var tweet = ProcessTweets(entry);

                if (tweet.Count != 0)
                {
                    tweets.AddRange(tweet);
                }
            }
            else if (entry.EntryId.StartsWith("cursor-bottom-"))
            {
                cursor = entry.Content.Value ?? throw new Exception("无法获取下一个指针"); // 无法判断是否会为空
            }
        }

        return (tweets, cursor);
    }

    private static IEnumerable<Tweet> ProcessTimelineAddToModule(UserMediaResponseInstruction instruction) => instruction
        .ModuleItems
        .Where(entry => entry.EntryId.StartsWith("profile-"))
        .Select(ProcessTweet);

    private static List<Tweet> ProcessTweets(UserMediaResponseEntry entry)
    {
        var tweets = new List<Tweet>();

        if (entry.Content.Items.Count == 0) return tweets;

        foreach (var item in entry.Content.Items)
        {
            var tweetResult = item.Item.ItemContent.TweetResults.Result;

            var userInfo = tweetResult.Core.UserResults.Result;

            if (userInfo.RestId == null) throw new Exception("无法获取用户 ID"); // 无法判断是否会为空

            var tweet = new Tweet
            {
                Id = tweetResult.RestId,
                UserId = userInfo.RestId,
                CreationTime = DateTimeOffset.ParseExact(tweetResult.Legacy.CreatedAt, TimeFormat, CultureInfo.InvariantCulture),
                Text = tweetResult.Legacy.FullText,
                Hashtags = tweetResult.Legacy.Entities.Hashtags.Select(x => x.Text).ToList(),
                Media = ProcessMedia(tweetResult.Legacy.ExtendedEntities?.Media ?? throw new Exception("无法获取媒体")) // 无法判断是否会为空
            };

            tweets.Add(tweet);
        }

        return tweets;
    }

    private static Tweet ProcessTweet(UserMediaResponseItem entry)
    {
        var tweetResult = entry.Item.ItemContent.TweetResults.Result;

        var userInfo = tweetResult.Core.UserResults.Result;

        if (userInfo.RestId == null) throw new Exception("无法获取用户 ID"); // 无法判断是否会为空

        return new Tweet
        {
            Id = tweetResult.RestId,
            UserId = userInfo.RestId,
            CreationTime = DateTimeOffset.ParseExact(tweetResult.Legacy.CreatedAt, TimeFormat, CultureInfo.InvariantCulture),
            Text = tweetResult.Legacy.FullText,
            Hashtags = tweetResult.Legacy.Entities.Hashtags.Select(x => x.Text).ToList(),
            Media = ProcessMedia(tweetResult.Legacy.Entities.Media)
        };
    }

    private static List<Media> ProcessMedia(List<UserMediaResponseMedia> mediaEntities) =>
        mediaEntities.Select(x => new Media
            {
                Type = x.Type switch
                {
                    "photo" => MediaType.Image,
                    "video" => MediaType.Video,
                    "animated_gif" => MediaType.Gif,
                    _ => throw new ArgumentException("未知媒体类型", x.Type)
                },
                Url = x.MediaUrlHttps,
                Video = x.VideoInfo?.Variants.Select(y => new Video { Url = y.Url, Bitrate = y.Bitrate }).ToList() ?? []
            }
        ).ToList();

    private static string BuildUrl(string endpoint, string variables, string? features = null, string? fieldToggles = null)
    {
        var sb = new StringBuilder(endpoint + $"?variables={variables}");

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
}
