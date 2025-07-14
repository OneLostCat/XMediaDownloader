using System.CommandLine;
using System.Globalization;
using System.Text.RegularExpressions;
using Serilog;
using XMediaDownloader.Models;

namespace XMediaDownloader;

public static partial class PathFormatConverter
{
    public static async Task Run(ParseResult result, CancellationToken cancel)
    {
        // 获取参数
        var sourceDir = result.GetRequiredValue(CommandLine.SourceDirOption);
        var outputDir = result.GetRequiredValue(CommandLine.OutputDirOption);
        var outputPathFormat = result.GetRequiredValue(CommandLine.OutputPathFormatOption);
        var dryRun = result.GetRequiredValue(CommandLine.DryRunOption);
        var workDir = result.GetRequiredValue(CommandLine.WorkDirOption);
        var logLevel = result.GetRequiredValue(CommandLine.LogLevelOption);

        // 设置工作目录
        Directory.CreateDirectory(workDir);
        Environment.CurrentDirectory = workDir;

        // 日志
        await using var logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}")
            // .WriteTo.Console(outputTemplate: "[{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .MinimumLevel.Is(logLevel)
            .CreateLogger();

        Log.Logger = logger;

        // 输出启动信息
        logger.Information("---------- 路径格式转换工具 ----------");
        logger.Information("参数:");
        logger.Information("  源目录: {OriginDir}", sourceDir);
        logger.Information("  输出目录: {OutputDir}", outputDir);
        logger.Information("  输出路径格式: {OutputPathFormat}", outputPathFormat);
        logger.Information("  空运行: {DryRun}", dryRun);
        logger.Information("  工作目录: {WorkDir}", workDir);
        logger.Information("  日志级别: {LogLevel}", logLevel);

        // 转换
        logger.Information("开始转换");

        try
        {
            foreach (var sourceFullPath in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                // 检查是否取消
                cancel.ThrowIfCancellationRequested();

                // 获取文件
                var source = new FileInfo(sourceFullPath);
                var sourcePath = Path.GetRelativePath(sourceDir, sourceFullPath);

                // 匹配文件名
                var match = OriginPathRegex().Match(source.Name);

                if (!match.Success)
                {
                    logger.Warning("文件名无效 {SourcePath}", sourcePath);
                    continue;
                }

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

                var extension = $".{groups["Extension"].Value}";

                // 生成文件路径
                var outputPath = PathBuilder.Build(
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
                );

                var output = new FileInfo(Path.Combine(outputDir, outputPath));

                // 创建目录
                output.Directory?.Create();

                // 移动文件
                logger.Information("移动 {SourcePath} -> {OutputPath}", sourcePath, outputPath);

                if (!dryRun) source.MoveTo(output.FullName);
            }

            logger.Information("转换完成");
        }
        catch (OperationCanceledException)
        {
            logger.Information("操作取消");
        }
        catch (Exception exception)
        {
            logger.Error(exception, "错误");
        }
    }

    [GeneratedRegex(
        @"^(?<Username>[^-]+)-(?<TweetId>\d+)-(?<TweetCreationTime>\d{8}_\d{6})-(?<MediaType>[^-]{3})(?<MediaIndex>\d)\.(?<Extension>[^-]{3})$")]
    private static partial Regex OriginPathRegex();
}
