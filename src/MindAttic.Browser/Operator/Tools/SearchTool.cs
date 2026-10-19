using System.Text.Json;

namespace MindAttic.Browser.Operator.Tools;

/// <summary>Runs a web search through the search engine the person configured in Settings.</summary>
public class SearchTool(string searchUrlTemplate) : IWebNavTool
{
    public string Name => "search";

    public string Description =>
        "Search the web with the browser's configured search engine and land on the results page. " +
        "Use operators the engine understands, e.g. filetype:pdf or site:archive.org. Follow up " +
        "with read_page to see the results.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": { "query": { "type": "string" } },
      "required": ["query"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, WebNavOperatorContext ctx, CancellationToken ct)
    {
        var query = args.GetProperty("query").GetString() ?? "";
        if (query.Length == 0) return JsonSerializer.Serialize(new { error = "query was empty." });
        var template = searchUrlTemplate.Contains("{query}") ? searchUrlTemplate : BrowserSettings.DefaultSearchUrl;
        var url = template.Replace("{query}", Uri.EscapeDataString(query));
        await ctx.Browser.NavigateAsync(url, ct);
        return JsonSerializer.Serialize(new { ok = true, url = ctx.Browser.CurrentUrl });
    }
}
