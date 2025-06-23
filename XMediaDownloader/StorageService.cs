using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using XMediaDownloader.Models;

namespace XMediaDownloader;

public class StorageService(ILogger<StorageService> logger)
{
    private const string FilePath = "storage.json";

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        TypeInfoResolver = StorageContentContext.Default,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    // 使用降序排列
    public static readonly Comparer<string> IdComparer =
        Comparer<string>.Create((a, b) => string.Compare(b, a, StringComparison.Ordinal));

    // 公开成员
    public StorageContent Content { get; set; } = new();

    public async Task SaveAsync()
    {
        try
        {
            // 创建目录
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath))!); // 使用 ! 禁用警告

            // 打开文件
            await using var fs = File.Create(FilePath); // 使用 File.Create 覆盖文件

            // 序列化并写入文件
#pragma warning disable IL2026
#pragma warning disable IL3050
            await JsonSerializer.SerializeAsync(fs, Content, _jsonSerializerOptions);
#pragma warning restore IL3050
#pragma warning restore IL2026

            logger.LogDebug("数据保存成功");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "数据保存失败");
        }
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(FilePath))
        {
            logger.LogDebug("数据文件不存在，无法加载数据");
            return;
        }

        try
        {
            // 打开文件
            await using var fs = File.OpenRead(FilePath);

            // 读取并反序列化文件
#pragma warning disable IL2026
#pragma warning disable IL3050
            if (await JsonSerializer.DeserializeAsync(fs, typeof(StorageContent), _jsonSerializerOptions) is not StorageContent data)
#pragma warning restore IL3050
#pragma warning restore IL2026
            {
                logger.LogError("数据加载失败");
                return;
            }

            // 将帖子转换成降序排列
            foreach (var pair in data.Users)
            {
                pair.Value.Tweets = new SortedDictionary<string, Tweet>(pair.Value.Tweets, IdComparer);
            }

            logger.LogDebug("数据加载成功");
            Content = data;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "数据加载失败");
        }
    }

    // public void AddUserData(string userId, string cursor, List<Tweet> tweets)
    // {
    //     // 处理推文
    //     var newData = new UserData { CurrentCursor = cursor, Users = new Dictionary<string, UserDataItem>() };
    //
    //     var users = Content.Users;
    //
    //     foreach (var tweet in tweets)
    //     {
    //         // 使用 Tweet 的用户 ID 找到对应的用户信息
    //         var tweetUserId = tweet.UserId;
    //
    //         if (!users.TryGetValue(tweetUserId, out var user)) continue;
    //
    //         if (!newData.Users.TryGetValue(tweetUserId, out var userDataItem))
    //         {
    //             userDataItem = new UserDataItem { UserHistory = new Dictionary<string, User> { [user.Id] = user }, };
    //
    //             newData.Users[tweetUserId] = userDataItem;
    //         }
    //
    //         userDataItem.Tweets[tweet.Id] = tweet;
    //     }
    //
    //     // 合并推文
    //     var data = Content.Data.GetValueOrDefault(userId) ?? new UserData();
    //
    //     data.CurrentCursor = newData.CurrentCursor;
    //
    //     foreach (var (newUserId, newUserData) in newData.Users)
    //     {
    //         // 创建用户
    //         if (!data.Users.TryGetValue(newUserId, out var userDataItem))
    //         {
    //             data.Users[newUserId] = newUserData;
    //             continue;
    //         }
    //
    //         // 合并用户历史
    //         foreach (var pair in newUserData.UserHistory)
    //         {
    //             userDataItem.UserHistory[pair.Key] = pair.Value;
    //         }
    //
    //         // 合并推文
    //         foreach (var pair in newUserData.Tweets)
    //         {
    //             data.Users[newUserId].Tweets[pair.Key] = pair.Value;
    //         }
    //     }
    // }
}