using System.Text.Json;

namespace MindAttic.Browser.Operator.Tools;

public class NavigateTool : IWebNavTool
{
    public string Name => "navigate";
    public string Description => "Navigate the browser pane to a URL. Use this to go to a new site or a different page on the current one.";
    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": { "url": { "type": "string", "description": "Absolute URL, e.g. https://example.com" } },
      "required": ["url"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, WebNavOperatorContext ctx, CancellationToken ct)
    {
        var url = args.GetProperty("url").GetString() ?? "";
        if (url.Length == 0) return JsonSerializer.Serialize(new { error = "url was empty." });
        await ctx.Browser.NavigateAsync(url, ct);
        return JsonSerializer.Serialize(new { ok = true, url = ctx.Browser.CurrentUrl });
    }
}
