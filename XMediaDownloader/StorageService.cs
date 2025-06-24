using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using XMediaDownloader.Models;

namespace XMediaDownloader;

public class StorageService(ILogger<StorageService> logger, CommandLineArguments args)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        TypeInfoResolver = StorageContentContext.Default,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public static readonly Comparer<string> IdComparer = Comparer<string>.Create((a, b) =>
        string.Compare(b, a, StringComparison.Ordinal)); // 使用降序排列

    private readonly DirectoryInfo _dir = args.StorageDir;
    private readonly FileInfo _file = new(Path.Combine(args.StorageDir.FullName, "storage.json"));

    // 公开成员
    public StorageContent Content { get; private set; } = new();

    public async Task SaveAsync()
    {
        try
        {
            // 创建目录
            _dir.Create();

            // 打开文件
            await using var fs = _file.Create(); // 使用 Create 覆盖文件

            // 序列化并写入文件
#pragma warning disable IL2026
#pragma warning disable IL3050
            await JsonSerializer.SerializeAsync(fs, Content, JsonSerializerOptions);
#pragma warning restore IL3050
#pragma warning restore IL2026

            logger.LogDebug("数据保存成功");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "数据保存失败");
        }
    }

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
#pragma warning disable IL2026
#pragma warning disable IL3050
            if (await JsonSerializer.DeserializeAsync(fs, typeof(StorageContent), JsonSerializerOptions) is not StorageContent
                data)
#pragma warning restore IL3050
#pragma warning restore IL2026
            {
                logger.LogError("数据加载失败");
                return;
            }

            // 将帖子转换成降序排列
            foreach (var pair in data.Users)
            {
                pair.Value.Tweets = new SortedDictionary<string, Tweet>(pair.Value.Tweets, IdComparer);
            }

            logger.LogDebug("数据加载成功");
            Content = data;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "数据加载失败");
        }
    }
}
