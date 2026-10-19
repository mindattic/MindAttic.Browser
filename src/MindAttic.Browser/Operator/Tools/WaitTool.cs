using System.Text.Json;

namespace MindAttic.Browser.Operator.Tools;

public class WaitTool : IWebNavTool
{
    private const int CapMs = 5000;

    public string Name => "wait";
    public string Description => "Pause briefly (capped at 5 seconds) before the next tool call — e.g. to let a page finish loading or an SPA settle after a click.";
    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": { "milliseconds": { "type": "integer", "description": "How long to wait, up to 5000." } },
      "required": ["milliseconds"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, WebNavOperatorContext ctx, CancellationToken ct)
    {
        var requested = args.TryGetProperty("milliseconds", out var m) ? m.GetInt32() : 1000;
        var ms = Math.Clamp(requested, 0, CapMs);
        await Task.Delay(ms, ct);
        return JsonSerializer.Serialize(new { ok = true, waitedMs = ms });
    }
}
