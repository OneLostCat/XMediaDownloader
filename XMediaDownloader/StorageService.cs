using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using XMediaDownloader.Models;

namespace XMediaDownloader;

public class StorageService(ILogger<StorageService> logger) : IAsyncDisposable
{
    private const string FilePath = "storage.json";

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        TypeInfoResolver = StorageContentContext.Default,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public StorageContent Content { get; set; } = new();

    public async Task SaveAsync()
    {
        try
        {
            // 创建目录
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath))!); // 使用 ! 禁用警告

            // 打开文件
            await using var fs = File.Open(FilePath, FileMode.Create); // 使用 FileMode.Create 覆盖文件

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

            // 反序列化
#pragma warning disable IL2026
#pragma warning disable IL3050
            var data = await JsonSerializer.DeserializeAsync(fs, typeof(StorageContent), _jsonSerializerOptions) as StorageContent;
#pragma warning restore IL3050
#pragma warning restore IL2026
            
            if (data == null)
            {
                logger.LogError("数据加载失败");
                return;
            }
            
            logger.LogDebug("数据加载成功");
            Content = data;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "数据加载失败");
        }
    }

    public void AddData(User user, string cursor, List<Tweet> tweets)
    {
        // 如果用户不存在则创建
        if (!Content.Data.TryGetValue(user.Id, out var data))
        {
            Content.Data[user.Id] = new UserData
            {
                Info = user,
                Tweets = tweets.ToDictionary(x => x.Id),
                CurrentCursor = cursor
            };
            return;
        }
        
        // 更新指针
        data.CurrentCursor = cursor;
        
        // 合并推文
        foreach (var tweet in tweets)
        {
            data.Tweets.TryAdd(tweet.Id, tweet);
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

    public async ValueTask DisposeAsync()
    {
        await SaveAsync();

        GC.SuppressFinalize(this);
    }
}