using System.Text.Json;

namespace MindAttic.Browser.Operator.Tools;

public class PressEnterTool : IWebNavTool
{
    public string Name => "press_enter";

    public string Description =>
        "Press Enter as a real keystroke into whatever element currently has focus — e.g. to " +
        "submit a search box after set_field typed into it. set_field's value-setter alone " +
        "doesn't fire a real keydown, so a page listening for Enter to submit needs this.";

    public string ParametersJsonSchema => """{ "type": "object", "properties": {} }""";

    public async Task<string> InvokeAsync(JsonElement args, WebNavOperatorContext ctx, CancellationToken ct)
    {
        await ctx.Browser.PressEnterAsync(ct);
        await Task.Delay(500, ct);
        return JsonSerializer.Serialize(new { ok = true });
    }
}
