using System.Diagnostics.CodeAnalysis;
using MediaDownloader.Models;
using Scriban;
using Scriban.Functions;
using Scriban.Runtime;

namespace MediaDownloader;

public class TemplateUtilities(CommandLineOptions options)
{
    public async Task<string> RenderAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(Template template, T model)
    {
        // 模型
        var script = new ScriptObject();
        script.Import(model);

        // 创建上下文
        var context = new TemplateContext();
        context.PushGlobal(script);

        // 设置时间格式
        (context.BuiltinObject["date"] as DateTimeFunctions)?.Format = options.DateTimeFormat;

        return await template.RenderAsync(context);
    }
}
