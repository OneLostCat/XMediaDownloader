using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XMediaDownloader.Models;

namespace XMediaDownloader.Services;

public class MainService(
    CommandLineArguments args,
    ILogger<MainService> logger,
    StorageService storage,
    XApiService api,
    DownloadService download,
    IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancel)
    {
        OutputArgumentsInfo();

        // 加载数据
        await storage.LoadAsync();

        try
        {
            // 获取用户信息
            var user = await GetUserAsync(cancel);

            // 获取媒体信息
            if (!args.WithoutDownloadInfo)
            {
                await api.GetUserMediaAsync(user.Id, cancel);
            }

            // 下载媒体
            if (!args.WithoutDownloadMedia)
            {
                await download.DownloadMediaAsync(user.Id, cancel);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("操作取消");
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "错误");
        }

        // 保存数据
        await storage.SaveAsync();

        // 退出
        lifetime.StopApplication();
    }

    private async Task<User> GetUserAsync(CancellationToken cancel)
    {
        User user;

        if (!args.WithoutDownloadInfo)
        {
            // 获取用户信息
            user = await api.GetUserByScreenNameAsync(args.Username, cancel);

            // 储存用户信息
            if (!storage.Content.Users.TryGetValue(user.Id, out var data))
            {
                // 创建用户
                storage.Content.Users[user.Id] = new UserData { Info = user };
            }
            else
            {
                // 更新用户信息
                data.Info = user;
            }
        }
        else
        {
            // 从存储中获取用户信息
            user = storage.Content.Users.Select(x => x.Value.Info).First(x => x.Name == args.Username);
        }
        
        logger.LogInformation("用户信息:");
        logger.LogInformation("  ID: {Id}", user.Id);
        logger.LogInformation("  名称: {Name}", user.Name);
        logger.LogInformation("  昵称: {Nickname}", user.Nickname);
        logger.LogInformation("  描述: {Description}", user.Description);
        logger.LogInformation("  注册时间: {CreationTime:yyyy-MM-dd HH:mm:ss zzz}", user.CreationTime);
        logger.LogInformation("  媒体数量: {MediaCount}", user.MediaCount);

        return user;
    }

    // 工具方法
    private void OutputArgumentsInfo()
    {
        logger.LogInformation("参数:");
        logger.LogInformation("  目标用户: {Username}", args.Username);
        logger.LogInformation("  Cookie 文件: {CookieFile}", args.CookieFile);
        logger.LogInformation("  输出目录: {OutputDir}", args.OutputDir);
        logger.LogInformation("  输出路径格式: {OutputPathFormat}", args.OutputPathFormat);
        logger.LogInformation("  目标媒体类型: {DownloadType}", args.DownloadType.HasFlag(MediaType.All) ? "All" : args.DownloadType);
        logger.LogInformation("  无需获取信息: {OnlyDownloadInfo}", args.WithoutDownloadInfo);
        logger.LogInformation("  无需下载媒体: {OnlyDownloadMedia}", args.WithoutDownloadMedia);
        logger.LogInformation("  状态存储目录: {StorageDir}", args.StorageDir);
        logger.LogInformation("  工作目录: {WorkDir}", args.WorkDir);
        logger.LogInformation("  日志级别: {LogLevel}", args.LogLevel);
    }
}