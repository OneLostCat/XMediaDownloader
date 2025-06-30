using System.Globalization;
using System.Text.RegularExpressions;
using Serilog;
using Serilog.Events;
using XMediaDownloader.Models;

namespace XMediaDownloader;

public static partial class PathFormatConverter
{
    public static async Task Run(
        DirectoryInfo sourceDir, 
        DirectoryInfo outputDir, 
        string outputPathFormat, 
        bool dryRun,
        // DirectoryInfo workDir, 
        LogEventLevel logLevel, 
        CancellationToken cancel)
    {
        // 设置工作目录
        // Environment.CurrentDirectory = workDir.FullName;
        
        // 日志
        await using var logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}")
            // .WriteTo.Console(outputTemplate: "[{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .MinimumLevel.Is(logLevel)
            .CreateLogger();

        Log.Logger = logger;

        logger.Information("---------- 路径格式转换工具 ----------");
        logger.Information("参数:");
        logger.Information("  源目录: {OriginDir}", sourceDir);
        logger.Information("  输出目录: {OutputDir}", outputDir);
        logger.Information("  输出路径格式: {OutputPathFormat}", outputPathFormat);
        logger.Information("  空运行: {DryRun}", dryRun);
        // logger.Information("  工作目录: {WorkDir}", workDir);
        logger.Information("  日志级别: {LogLevel}", logLevel);

        logger.Information("开始转换");

        // 遍历文件
        var sources = sourceDir.EnumerateFiles("*", SearchOption.AllDirectories);

        foreach (var source in sources)
        {
            // 检查是否取消
            cancel.ThrowIfCancellationRequested();

            // 获取文件路径
            var sourcePath = Path.GetRelativePath(sourceDir.FullName, source.FullName);

            // 匹配文件名
            var match = OriginPathRegex().Match(source.Name);

            if (!match.Success)
            {
                logger.Warning("文件名无效 {SourcePath}", sourcePath);
                continue;
            }

            try
            {
                // 获取信息
                var groups = match.Groups;

                var username = groups["Username"].Value;

                var tweetId = groups["TweetId"].Value;

                var tweetCreationTime = new DateTimeOffset(
                    DateTime.ParseExact(groups["TweetCreationTime"].Value, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture),
                    TimeSpan.FromHours(8)
                ).ToUniversalTime(); // 将 UTC+8 转换为 UTC

                var mediaIndex = int.Parse(groups["MediaIndex"].Value);

                var mediaType = groups["MediaType"].Value switch
                {
                    "img" => MediaType.Image,
                    "vid" => MediaType.Video,
                    "gif" => MediaType.Gif,
                    _ => throw new ArgumentException("无法识别的媒体类型", groups["MediaType"].Value)
                };

                var extension = groups["Extension"].Value;

                // 生成文件路径
                var outputPath = Path.Combine(
                    outputDir.ToString() != "." ? outputDir.ToString() : "", // 避免使用默认目录时输出多余的 ".\"
                    PathBuilder.Build(
                        outputPathFormat,
                        null,
                        username,
                        null,
                        null,
                        null,
                        null,
                        tweetId,
                        tweetCreationTime,
                        null,
                        [],
                        mediaIndex,
                        mediaType,
                        null,
                        extension,
                        null
                    )
                );

                // 创建目录
                Directory.GetParent(outputPath)?.Create();

                // 移动文件
                logger.Information("移动 {SourcePath} -> {OutputPath}", sourcePath, outputPath);

                if (!dryRun) source.MoveTo(outputPath);
            }
            catch (OperationCanceledException)
            {
                logger.Information("任务取消");
            }
            catch (Exception exception)
            {
                logger.Error(exception, "错误");
            }
        }

        logger.Information("转换完成");
    }

    [GeneratedRegex(
        @"^(?<Username>[^-]+)-(?<TweetId>\d+)-(?<TweetCreationTime>\d{8}_\d{6})-(?<MediaType>[^-]{3})(?<MediaIndex>\d)\.(?<Extension>[^-]{3})$")]
    private static partial Regex OriginPathRegex();
}
