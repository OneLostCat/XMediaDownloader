using System.Diagnostics.CodeAnalysis;
using MediaDownloader.Models;
using Microsoft.Extensions.Logging;
using Scriban;

namespace MediaDownloader.Downloaders;

public class HttpDownloader(ILogger<HttpDownloader>logger, CommandLineArguments args) : IMediaDownloader
{
    private readonly HttpClient _http = BuildHttpClient();

    public async Task DownloadAsync(MediaCollection medias, CancellationToken cancel)
    {
        var template = Template.Parse(args.OutputTemplate ?? medias.DefaultTemplate);
        
        foreach (var media in medias.Medias)
        {
            // 生成路径
            var path = await RenderAsync(template, media) + media.Extension;
            
            logger.LogInformation("下载 {Url} -> {Path}", media.Url, path);
            
            // 检查文件是否存在
            var file = new FileInfo(Path.Combine(args.Output, path));
            
            if (file.Exists)
            {
                logger.LogInformation("文件已存在，跳过");
                continue;
            }

            // 发送请求
            var response = await _http.GetAsync(media.Url, cancel);
            
            // 写入临时文件
            var temp = new FileInfo(Path.GetTempFileName());

            await using (var stream = temp.Create())
                await response.Content.CopyToAsync(stream, cancel);

            // 创建文件夹
            file.Directory?.Create();
            
            // 移动文件
            temp.MoveTo(file.FullName);
        }
    }

    private static HttpClient BuildHttpClient()
    {
        // 创建 HttpClient
        var http = new HttpClient();

        // 启用 HTTP/2 和 HTTP/3
        http.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

        // 设置 User Agent
        http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36 Edg/137.0.0.0");

        return http;
    }
    
    private static async Task<string>
        RenderAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(Template template,
            T model) => await template.RenderAsync(model);
}
