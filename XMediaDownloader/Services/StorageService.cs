using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using XMediaDownloader.Models;

namespace XMediaDownloader.Services;

public class StorageService(ILogger<StorageService> logger, CommandLineArguments args)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        TypeInfoResolver = StorageContentContext.Default,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };
    
    private readonly FileInfo _file = new(Path.Combine(args.OutputDir, "metadata.json"));

    // 公开成员
    public StorageContent Content { get; private set; } = new();

    // 抑制 JsonSerializer.SerializeAsync 的警告
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public async Task SaveAsync()
    {
        try
        {
            // 创建目录
            Directory.CreateDirectory(args.OutputDir);

            // 打开文件
            await using var fs = _file.Create(); // 使用 Create 覆盖文件

            // 序列化并写入文件
            await JsonSerializer.SerializeAsync(fs, Content, JsonSerializerOptions);

            logger.LogDebug("数据保存成功");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "数据保存失败");
        }
    }

    // 抑制 JsonSerializer.DeserializeAsync 的警告
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public async Task LoadAsync()
    {
        if (!_file.Exists)
        {
            logger.LogDebug("数据文件不存在，无法加载数据");
            return;
        }

        try
        {
            // 打开文件
            await using var fs = _file.OpenRead();

            // 读取并反序列化文件
            if (await JsonSerializer.DeserializeAsync(fs, typeof(StorageContent), JsonSerializerOptions) is not StorageContent
                data)
            {
                logger.LogError("数据加载失败");
                return;
            }

            // 将帖子转换成降序排列
            data.Tweets = new SortedDictionary<string, Tweet>(data.Tweets, StorageContent.IdComparer);

            Content = data;
            
            logger.LogDebug("数据加载成功");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "数据加载失败");
        }
    }
}
