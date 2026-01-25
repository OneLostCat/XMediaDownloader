using MediaDownloader.Fetchers;
using MediaDownloader.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediaDownloader.Services;

public class MainService(
    ILogger<MainService> logger,
    CommandLineArguments args,
    IFetcher fetcher,
    DownloadService download,
    IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancel)
    {
        try
        {
            // 获取媒体
            var medias = await fetcher.FetchMediaAsync(args.Username, cancel);
            
            // 询问是否继续
            AskNextStep(cancel);
            
            // 下载媒体
            await download.DownloadAsync(medias, cancel);
            
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("取消");
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "错误");
        }
        
        // 退出
        lifetime.StopApplication();
    }
    
    private static void AskNextStep(CancellationToken cancel)
    {
        while (true)
        {
            cancel.ThrowIfCancellationRequested();

            Console.Write("是否继续? ([y] 继续 [n] 取消): ");

            // 获取输入
            var input = Console.ReadLine();
            if (input?.ToLower() == "y") break;
            if (input?.ToLower() == "n") throw new OperationCanceledException();
        }
    }
    
    // private void OutputUserInfo(User user)
    // {
    //     logger.LogInformation("用户信息:");
    //     logger.LogInformation("  ID: {Id}", user.Id);
    //     logger.LogInformation("  名称: {Name}", user.Name);
    //     logger.LogInformation("  昵称: {Nickname}", user.Nickname);
    //     logger.LogInformation("  描述: {Description}", user.Description);
    //     logger.LogInformation("  注册时间: {Time:yyyy-MM-dd HH:mm:ss}", user.Time.LocalDateTime);
    //     logger.LogInformation("  媒体帖子数量: {MediaCount}", user.MediaTweetCount);
    // }
}