using System.Text.Json.Serialization;

namespace XMediaDownloader.Models;

public record BookmarkFeatures
{
    [JsonPropertyName("graphql_timeline_v2_bookmark_timeline")]
    public bool GraphqlTimelineV2BookmarkTimeline { get; set; } = true;
    
    [JsonPropertyName("profile_label_improvements_pcf_label_in_post_enabled")]
    public bool ProfileLabelImprovementsPcfLabelInPostEnabled { get; set; } = false;
    
    [JsonPropertyName("rweb_tipjar_consumption_enabled")]
    public bool RwebTipjarConsumptionEnabled { get; set; } = true;
    
    [JsonPropertyName("responsive_web_graphql_exclude_directive_enabled")]
    public bool ResponsiveWebGraphqlExcludeDirectiveEnabled { get; set; } = true;
    
    [JsonPropertyName("verified_phone_label_enabled")]
    public bool VerifiedPhoneLabelEnabled { get; set; } = false;
    
    [JsonPropertyName("creator_subscriptions_tweet_preview_api_enabled")]
    public bool CreatorSubscriptionsTweetPreviewApiEnabled { get; set; } = true;
    
    [JsonPropertyName("responsive_web_graphql_timeline_navigation_enabled")]
    public bool ResponsiveWebGraphqlTimelineNavigationEnabled { get; set; } = true;
    
    [JsonPropertyName("responsive_web_graphql_skip_user_profile_image_extensions_enabled")]
    public bool ResponsiveWebGraphqlSkipUserProfileImageExtensionsEnabled { get; set; } = false;
    
    [JsonPropertyName("premium_content_api_read_enabled")]
    public bool PremiumContentApiReadEnabled { get; set; } = false;
    
    [JsonPropertyName("communities_web_enable_tweet_community_results_fetch")]
    public bool CommunitiesWebEnableTweetCommunityResultsFetch { get; set; } = true;
    
    [JsonPropertyName("c9s_tweet_anatomy_moderator_badge_enabled")]
    public bool C9sTweetAnatomyModeratorBadgeEnabled { get; set; } = true;
    
    [JsonPropertyName("responsive_web_grok_analyze_button_fetch_trends_enabled")]
    public bool ResponsiveWebGrokAnalyzeButtonFetchTrendsEnabled { get; set; } = false;
    
    [JsonPropertyName("articles_preview_enabled")]
    public bool ArticlesPreviewEnabled { get; set; } = true;
    
    [JsonPropertyName("responsive_web_edit_tweet_api_enabled")]
    public bool ResponsiveWebEditTweetApiEnabled { get; set; } = true;
    
    [JsonPropertyName("graphql_is_translatable_rweb_tweet_is_translatable_enabled")]
    public bool GraphqlIsTranslatableRwebTweetIsTranslatableEnabled { get; set; } = true;
    
    [JsonPropertyName("view_counts_everywhere_api_enabled")]
    public bool ViewCountsEverywhereApiEnabled { get; set; } = true;
    
    [JsonPropertyName("longform_notetweets_consumption_enabled")]
    public bool LongformNotetweetsConsumptionEnabled { get; set; } = true;
    
    [JsonPropertyName("responsive_web_twitter_article_tweet_consumption_enabled")]
    public bool ResponsiveWebTwitterArticleTweetConsumptionEnabled { get; set; } = true;
    
    [JsonPropertyName("tweet_awards_web_tipping_enabled")]
    public bool TweetAwardsWebTippingEnabled { get; set; } = false;
    
    [JsonPropertyName("creator_subscriptions_quote_tweet_preview_enabled")]
    public bool CreatorSubscriptionsQuoteTweetPreviewEnabled { get; set; } = false;
    
    [JsonPropertyName("freedom_of_speech_not_reach_fetch_enabled")]
    public bool FreedomOfSpeechNotReachFetchEnabled { get; set; } = true;
    
    [JsonPropertyName("standardized_nudges_misinfo")]
    public bool StandardizedNudgesMisinfo { get; set; } = true;
    
    [JsonPropertyName("tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled")]
    public bool TweetWithVisibilityResultsPreferGqlLimitedActionsPolicyEnabled { get; set; } = true;
    
    [JsonPropertyName("rweb_video_timestamps_enabled")]
    public bool RwebVideoTimestampsEnabled { get; set; } = true;
    
    [JsonPropertyName("longform_notetweets_rich_text_read_enabled")]
    public bool LongformNotetweetsRichTextReadEnabled { get; set; } = true;
    
    [JsonPropertyName("longform_notetweets_inline_media_enabled")]
    public bool LongformNotetweetsInlineMediaEnabled { get; set; } = true;
    
    [JsonPropertyName("responsive_web_enhance_cards_enabled")]
    public bool ResponsiveWebEnhanceCardsEnabled { get; set; } = false;
}