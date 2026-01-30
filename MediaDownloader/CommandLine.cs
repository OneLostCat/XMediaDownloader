using System.CommandLine;
using MediaDownloader.Downloaders;
using MediaDownloader.Extractors;
using MediaDownloader.Models;
using MediaDownloader.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace MediaDownloader;

public static class CommandLine
{
    // 获取选项
    private static readonly Option<MediaExtractor> SourceOption = new("-s", "--source")
        { Description = "媒体来源", Required = true };

    private static readonly Option<string> UserOption = new("-u", "--user")
        { Description = "目标用户", Required = true };

    private static readonly Option<string> CookieOption = new("-c", "--cookie")
        { Description = "用于请求的 Cookie", DefaultValueFactory = _ => "cookie.txt" };

    // 输出选项
    public static readonly Option<string> OutputOption = new("-o", "--output")
        { Description = "输出目录", DefaultValueFactory = _ => "." };

    public static readonly Option<string> OutputTemplateOption = new("-O", "--output-template")
        { Description = "输出文件路径格式" };
    
    public static readonly Option<string> DateTimeFormatOption = new("-d", "--date-time-format")
        { Description = "时间日期格式", DefaultValueFactory = _ => "%Y-%m-%d %H-%M-%S" };
    
    // 下载选项
    private static readonly Option<List<MediaType>> TypeOption = new("-t", "--type")
    {
        Description = "下载媒体类型",
        DefaultValueFactory = _ => [MediaType.Image, MediaType.Video, MediaType.Gif],
        AllowMultipleArgumentsPerToken = true,
        Arity = ArgumentArity.OneOrMore
    };
    
    private static readonly Option<int> ConcurrencyOption = new("-C", "--concurrency")
        { Description = "下载并发数量", DefaultValueFactory = _ => 4 };

    // 路径格式转换选项
    public static readonly Option<string> SourceDirOption = new("-s", "--source")
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
            OutputOption,
            OutputTemplateOption,
            DryRunOption,
        };

        convertCommand.SetAction(PathFormatConverter.Run);

        // 根命令
        var command = new RootCommand("X 媒体下载工具")
        {
            SourceOption,
            UserOption,
            CookieOption,
            OutputOption,
            OutputTemplateOption,
            DateTimeFormatOption,
            TypeOption,
            ConcurrencyOption,
            convertCommand
        };

        command.SetAction(RunAsync);

        // 运行
        return await command.Parse(args).InvokeAsync();
    }

    private static async Task RunAsync(ParseResult result, CancellationToken cancel)
    {
        // 创建日志
        await using var logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}")
            // .WriteTo.Console(outputTemplate: "[{SourceContext}] {Message:lj}{NewLine}{Exception}")
            // 去除多余的日志
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Extensions.Hosting.Internal.Host", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .MinimumLevel.Override("Polly", LogEventLevel.Warning)
            .CreateLogger();

        Log.Logger = logger;

        try
        {
            // 创建主机
            var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());

            // 注册服务
            builder.Services.AddSerilog();
            builder.Services.AddHostedService<MainService>();
            builder.Services.AddExtractors();
            builder.Services.AddDownloaders();
            builder.Services.AddSingleton<TemplateUtilities>();

            // 注册命令行参数
            builder.Services.AddSingleton(new CommandLineOptions(
                result.GetRequiredValue(SourceOption),
                result.GetRequiredValue(UserOption),
                result.GetRequiredValue(CookieOption),
                result.GetRequiredValue(OutputOption),
                result.GetValue(OutputTemplateOption),
                result.GetRequiredValue(DateTimeFormatOption),
                result.GetRequiredValue(TypeOption),
                result.GetRequiredValue(ConcurrencyOption)
            ));

            // 运行
            await builder.Build().RunAsync(cancel);
        }
        catch (Exception exception)
        {
            logger.Fatal(exception, "错误");
        }

        logger.Debug("应用退出");
    }
}
