using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XMediaDownloader.Models;

namespace XMediaDownloader;

public class MainService(
    CommandLineArguments arguments,
    ILogger<MainService> logger,
    XApiService api,
    IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancel)
    {
        // 输出信息
        OutputArgumentsInfo();

        // 获取目标 UserId
        var user = await api.GetUserAsync(arguments.Username, cancel);
        
        OutputUserInfo(user);

        // 获取媒体
        await api.GetMediaAsync(user.Id, cancel);

        // 下载媒体

        // 退出
        lifetime.StopApplication();
    }

    private void OutputArgumentsInfo()
    {
        logger.LogInformation("目标用户: {User}", arguments.Username);
        logger.LogInformation("下载类型: {DownloadType}", arguments.DownloadType == DownloadType.All ? "All" : arguments.DownloadType);
        logger.LogInformation("Cookie 文件: {CookieFile}", arguments.CookieFile.Name);
        logger.LogInformation("输出目录格式: {Dirname}", arguments.Dir);
        logger.LogInformation("输出文件名格式: {Filename}", arguments.Filename);
    }

    private void OutputUserInfo(User user)
    {
        logger.LogInformation("用户昵称: {Name}", user.Name);
        logger.LogInformation("用户名称: {ScreenName}", user.ScreenName);
        logger.LogInformation("用户 ID: {UserId}", user.Id);
    }
}