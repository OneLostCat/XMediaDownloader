using System.CommandLine;
using MediaDownloader.Models;
using MediaDownloader.Models.X;

namespace MediaDownloader;

public static class CommandLine
{
    // 获取选项
    public static readonly Option<MediaSource> MediaSourceOption = new("-s", "--source")
        { Description = "媒体来源", Required = true };

    public static readonly Option<string> UsernameOption = new("-u", "--username")
        { Description = "目标用户", Required = true };

    public static readonly Option<FileInfo> CookieFileOption = new("-c", "--cookie")
        { Description = "用于请求的 Cookie", Required = true };
    
    // 下载选项
    public static readonly Option<List<MediaType>> MediaTypeOption = new("-t", "--type")
    {
        Description = "下载媒体类型",
        DefaultValueFactory = _ => [MediaType.All],
        AllowMultipleArgumentsPerToken = true,
        Arity = ArgumentArity.OneOrMore
    };

    // 输出选项
    public static readonly Option<string> OutputOption = new("-o", "--output")
        { Description = "输出目录", DefaultValueFactory = _ => "." };

    public static readonly Option<string> OutputTemplateOption = new("-O", "--output-template")
    {
        Description = "输出文件路径格式", DefaultValueFactory = _ => "{{id}}-{{username}}-{{time}}-{{index}}-{{type}}"
    };
    
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
            MediaSourceOption,
            UsernameOption,
            CookieFileOption,
            OutputOption,
            OutputTemplateOption,
            MediaTypeOption,
            convertCommand
        };

        command.SetAction(Main.RunAsync);

        // 运行
        return await command.Parse(args).InvokeAsync();
    }
}
