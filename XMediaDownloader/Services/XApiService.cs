using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XMediaDownloader.Models;
using XMediaDownloader.Models.XApi;

namespace XMediaDownloader.Services;

public class XApiService(ILogger<XApiService> logger, StorageService storage, [FromKeyedServices("Api")] HttpClient httpClient)
{
    #region API

    public const string BaseUrl = "https://x.com/i/api/graphql/";

    private const string UserByScreenNameUrl = "1VOOyvKkiI3FMmkeDNxM9A/UserByScreenName";

    private const string UserMediaUrl = "BGmkmGDG0kZPM-aoQtNTTw/UserMedia";

    // private const string UserTweets = "q6xj5bs0hapm9309hexA_g/UserTweets";

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
        "{\"rweb_video_screen_enabled\":false," +
        "\"payments_enabled\":false," +
        "\"profile_label_improvements_pcf_label_in_post_enabled\":true," +
        "\"rweb_tipjar_consumption_enabled\":true," +
        "\"verified_phone_label_enabled\":false," +
        "\"creator_subscriptions_tweet_preview_api_enabled\":true," +
        "\"responsive_web_graphql_timeline_navigation_enabled\":true," +
        "\"responsive_web_graphql_skip_user_profile_image_extensions_enabled\":false," +
        "\"premium_content_api_read_enabled\":false," +
        "\"communities_web_enable_tweet_community_results_fetch\":true," +
        "\"c9s_tweet_anatomy_moderator_badge_enabled\":true," +
        "\"responsive_web_grok_analyze_button_fetch_trends_enabled\":false," +
        "\"responsive_web_grok_analyze_post_followups_enabled\":true," +
        "\"responsive_web_jetfuel_frame\":true," +
        "\"responsive_web_grok_share_attachment_enabled\":true," +
        "\"articles_preview_enabled\":true," +
        "\"responsive_web_edit_tweet_api_enabled\":true," +
        "\"graphql_is_translatable_rweb_tweet_is_translatable_enabled\":true," +
        "\"view_counts_everywhere_api_enabled\":true," +
        "\"longform_notetweets_consumption_enabled\":true," +
        "\"responsive_web_twitter_article_tweet_consumption_enabled\":true," +
        "\"tweet_awards_web_tipping_enabled\":false," +
        "\"responsive_web_grok_show_grok_translated_post\":false," +
        "\"responsive_web_grok_analysis_button_from_backend\":false," +
        "\"creator_subscriptions_quote_tweet_preview_enabled\":false," +
        "\"freedom_of_speech_not_reach_fetch_enabled\":true," +
        "\"standardized_nudges_misinfo\":true," +
        "\"tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled\":true," +
        "\"longform_notetweets_rich_text_read_enabled\":true," +
        "\"longform_notetweets_inline_media_enabled\":true," +
        "\"responsive_web_grok_image_annotation_enabled\":true," +
        "\"responsive_web_grok_community_note_auto_translation_is_enabled\":false," +
        "\"responsive_web_enhance_cards_enabled\":false," +
        "\"responsive_web_graphql_exclude_directive_enabled\":true," +
        "\"rweb_video_timestamps_enabled\":true}";

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

        return new User
        {
            Id = result.RestId,
            Name = result.Legacy.ScreenName,
            Nickname = result.Legacy.Name,
            Description = result.Legacy.Description,
            CreationTime = DateTimeOffset.ParseExact(result.Legacy.CreatedAt, TimeFormat, CultureInfo.InvariantCulture),
            MediaCount = result.Legacy.MediaCount
        };
    }

    public async Task GetUserMediaAsync(string userId, CancellationToken cancel)
    {
        var tweets = storage.Content.Tweets;
        var cursor = storage.Content.CurrentCursor; // 当前指针

        var totalMediaCount = storage.Content.Users[userId].MediaCount; // 总媒体数量
        var tweetCount = tweets.Count; // 当前帖子数量
        var mediaCount = tweets.Sum(x => x.Value.Media.Count); // 当前媒体数量

        logger.LogInformation("开始获取信息");

        while (true)
        {
            // 检查是否取消
            cancel.ThrowIfCancellationRequested();

            // 获取帖子
            var (list, nextCursor) = await GetUserMediaAsync(userId, cursor, cancel);

            // 如果没有更多帖子则退出
            if (list.Count == 0) break;

            foreach (var (user, tweet) in list)
            {
                // 储存用户
                storage.Content.Users[user.Id] = user;

                // 储存帖子
                var isNew = tweets.TryAdd(tweet.Id, tweet);

                // 增加帖子计数
                if (isNew) tweetCount++;

                logger.LogInformation("获取信息 {Id} {CreationTime:yyyy-MM-dd HH:mm:ss}", tweet.Id, tweet.CreationTime.LocalDateTime);

                foreach (var media in tweet.Media)
                {
                    // 增加媒体计数
                    if (isNew) mediaCount++;

                    logger.LogInformation("  {Type} {Url} ({MediaCount} / {TotalMediaCount})", media.Type, media.Url, mediaCount,
                        totalMediaCount);
                }
            }

            // 更新指针
            storage.Content.CurrentCursor = cursor = nextCursor;

            // 避免请求速率过快
            // await Task.Delay(1000, cancel);
        }

        logger.LogInformation("信息获取完成: 成功获取 {TweetCount} 条帖子 / {MediaCount} 个媒体", tweetCount, mediaCount);
    }

    private async Task<(List<(User, Tweet)> list, string cursor)> GetUserMediaAsync(string userId, string? cursor,
        CancellationToken cancel)
    {
        // 参数
        var variables = JsonSerializer.Serialize(new UserMediaVariables(userId, 40, cursor),
            UserMediaVariablesContext.Default.UserMediaVariables);

        // 发送请求
        var response = await httpClient.GetAsync(
            BuildUrl(UserMediaUrl, variables, UserMediaFeatures, "{\"withArticlePlainText\":false}"), cancel);

        // 解析
        var content = await response.Content.ReadFromJsonAsync<GraphQlResponse<UserMediaResponse>>(
            UserMediaResponseContext.Default.GraphQlResponseUserMediaResponse, cancel);

        if (content?.Data == null) throw new Exception("无法获取用户媒体"); // 无法判断是否会为空

        // 处理帖子
        var list = new List<(User, Tweet)>();
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

    // 工具方法
    private static IEnumerable<(User, Tweet)> ProcessTweets(UserMediaResponseEntry entry)
    {
        foreach (var item in entry.Content.Items)
        {
            var tweetResult = item.Item.ItemContent.TweetResults.Result;
            var userResult = tweetResult.Core.UserResults.Result;

            if (userResult.RestId == null) throw new Exception("无法获取用户 ID"); // 无法判断是否会为空

            // 解析用户
            var user = new User
            {
                Id = userResult.RestId,
                Name = userResult.Legacy.ScreenName,
                Nickname = userResult.Legacy.Name,
                Description = userResult.Legacy.Description,
                CreationTime = DateTimeOffset.ParseExact(userResult.Legacy.CreatedAt, TimeFormat, CultureInfo.InvariantCulture),
                MediaCount = userResult.Legacy.MediaCount
            };

            // 解析帖子
            var tweet = new Tweet
            {
                Id = tweetResult.RestId,
                UserId = userResult.RestId,
                CreationTime = DateTimeOffset.ParseExact(tweetResult.Legacy.CreatedAt, TimeFormat, CultureInfo.InvariantCulture),
                Text = tweetResult.Legacy.FullText,
                Hashtags = tweetResult.Legacy.Entities.Hashtags.Select(x => x.Text).ToList(),
                Media = ProcessMedia(tweetResult.Legacy.ExtendedEntities?.Media ?? throw new Exception("无法获取媒体")) // 无法判断是否会为空
                    .ToList()
            };

            yield return (user, tweet);
        }
    }

    private static (User, Tweet) ProcessTweet(UserMediaResponseItem entry)
    {
        var tweetResult = entry.Item.ItemContent.TweetResults.Result;
        var userResult = tweetResult.Core.UserResults.Result;

        if (userResult.RestId == null) throw new Exception("无法获取用户 ID"); // 无法判断是否会为空

        // 解析用户
        var user = new User
        {
            Id = userResult.RestId,
            Name = userResult.Legacy.ScreenName,
            Nickname = userResult.Legacy.Name,
            Description = userResult.Legacy.Description,
            CreationTime = DateTimeOffset.ParseExact(userResult.Legacy.CreatedAt, TimeFormat, CultureInfo.InvariantCulture),
            MediaCount = userResult.Legacy.MediaCount
        };

        // 解析帖子
        var tweet = new Tweet
        {
            Id = tweetResult.RestId,
            UserId = userResult.RestId,
            CreationTime = DateTimeOffset.ParseExact(tweetResult.Legacy.CreatedAt, TimeFormat, CultureInfo.InvariantCulture),
            Text = tweetResult.Legacy.FullText,
            Hashtags = tweetResult.Legacy.Entities.Hashtags.Select(x => x.Text).ToList(),
            Media = ProcessMedia(tweetResult.Legacy.Entities.Media).ToList()
        };

        return (user, tweet);
    }

    private static IEnumerable<Media> ProcessMedia(List<UserMediaResponseMedia> mediaEntities) => mediaEntities
        .Select(x => new Media
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
        });

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
