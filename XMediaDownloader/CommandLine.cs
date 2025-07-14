using System.CommandLine;
using Serilog.Events;
using XMediaDownloader.Models;

namespace XMediaDownloader;

public static class CommandLine
{
    // 信息获取选项
    public static readonly Option<string> UsernameOption = new("-u", "--username")
        { Description = "目标用户", Required = true };

    public static readonly Option<string> CookieFileOption = new("-c", "--cookie-file")
        { Description = "用于请求 API 的 Cookie 文件", Required = true };

    public static readonly Option<bool> WithoutDownloadInfoOption = new("--without-download-info")
        { Description = "无需获取信息", DefaultValueFactory = _ => false };

    // 输出选项
    public static readonly Option<string> OutputDirOption = new("-o", "--output-dir")
        { Description = "输出目录", DefaultValueFactory = _ => "." };

    public static readonly Option<string> OutputPathFormatOption = new("-O", "--output-path-format")
    {
        Description = "输出文件路径格式",
        DefaultValueFactory = _ => "{TweetId}-{Username}-{TweetCreationTime}-{MediaIndex}-{MediaType}{MediaExtension}"
    };

    // 下载选项
    public static readonly Option<List<MediaType>> DownloadTypeOption = new("-t", "--download-type")
    {
        Description = "目标媒体类型",
        DefaultValueFactory = _ => [MediaType.All],
        Arity = ArgumentArity.OneOrMore,
        AllowMultipleArgumentsPerToken = true
    };

    public static readonly Option<bool> WithoutDownloadMediaOption = new("--without-download-media")
        { Description = "无需下载媒体", DefaultValueFactory = _ => false };

    // 其他选项
    public static readonly Option<string> WorkDirOption = new("-w", "--work-dir")
        { Description = "工作目录", DefaultValueFactory = _ => "." };

    public static readonly Option<LogEventLevel> LogLevelOption = new("-l", "--log-level")
        { Description = "日志级别", DefaultValueFactory = _ => LogEventLevel.Information };

    // 路径格式转换选项
    public static readonly Option<string> SourceDirOption = new("-s", "--source-dir")
        { Description = "源目录", Required = true };

    public static readonly Option<bool> DryRunOption = new("-n", "--dry-run")
        { Description = "试运行", DefaultValueFactory = _ => false };


    // 方法
    public static async Task<int> RunAsync(string[] args)
    {
        // 路径格式转换命令
        var convertCommand = new Command("convert", "X 媒体路径格式转换工具")
        {
            SourceDirOption,
            OutputDirOption,
            OutputPathFormatOption,
            DryRunOption,
            WorkDirOption,
            LogLevelOption,
        };

        convertCommand.SetAction(PathFormatConverter.Run);

        // 根命令
        var command = new RootCommand("X 媒体下载工具")
        {
            UsernameOption,
            CookieFileOption,
            WithoutDownloadInfoOption,
            OutputDirOption,
            OutputPathFormatOption,
            DownloadTypeOption,
            WithoutDownloadMediaOption,
            WorkDirOption,
            LogLevelOption,
            convertCommand
        };

        command.SetAction(Downloader.RunAsync);

        // 运行
        return await command.Parse(args).InvokeAsync();
    }
}
