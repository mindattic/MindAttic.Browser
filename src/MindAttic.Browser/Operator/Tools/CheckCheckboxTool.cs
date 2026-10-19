using System.Text.Json;

namespace MindAttic.Browser.Operator.Tools;

/// <summary>
/// Ticks (or unticks) every checkbox whose label contains a candidate phrase. Ported from Prose.Core's
/// CheckCheckboxTool/KdpFormHelpers: matches both native <c>input[type=checkbox]</c> (a synthetic
/// click works) and custom <c>[role=checkbox]</c> widgets, which ignore synthetic clicks — those get a
/// real CDP mouse click at their center through <see cref="AutoWebNav.IBrowserSurface.ClickAtPointAsync"/>.
/// </summary>
public class CheckCheckboxTool : IWebNavTool
{
    public string Name => "check_checkbox";

    public string Description =>
        "Tick EVERY checkbox whose label text contains any of the candidate phrases " +
        "(case-insensitive) — or untick them with checked:false. Works for native checkboxes and " +
        "custom role=checkbox widgets alike. Returns {changedCount, matches}; changedCount:0 means " +
        "nothing matched or every match was already in the requested state.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "text_candidates": { "type": "array", "items": { "type": "string" } },
        "checked": { "type": "boolean", "description": "true (default) ticks, false unticks." }
      },
      "required": ["text_candidates"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, WebNavOperatorContext ctx, CancellationToken ct)
    {
        var candidates = args.GetProperty("text_candidates").EnumerateArray()
            .Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToArray();
        if (candidates.Length == 0) return JsonSerializer.Serialize(new { error = "text_candidates was empty." });
        var want = !args.TryGetProperty("checked", out var c) || c.GetBoolean();

        var script = $$"""
        (function() {
            var candidates = {{JsonSerializer.Serialize(candidates)}}.map(function (s) { return s.toLowerCase(); });
            var want = {{(want ? "true" : "false")}};
            function labelOf(el) {
                var t = [];
                var l = el.closest('label'); if (l) t.push(l.textContent);
                if (el.id) { var f = document.querySelector('label[for="' + CSS.escape(el.id) + '"]'); if (f) t.push(f.textContent); }
                t.push(el.getAttribute('aria-label') || '');
                t.push(el.textContent || '');
                if (el.parentElement) t.push(el.parentElement.textContent);
                return t.join(' ').replace(/\s+/g, ' ').trim().toLowerCase();
            }
            function matches(el) { var t = labelOf(el); return candidates.some(function (c) { return t.indexOf(c) !== -1; }); }
            var native = [], widgets = [];
            document.querySelectorAll('input[type=checkbox]').forEach(function (el) {
                if (el.checked !== want && matches(el)) { el.click(); native.push(labelOf(el).slice(0, 120)); }
            });
            document.querySelectorAll('[role=checkbox]').forEach(function (el) {
                if ((el.getAttribute('aria-checked') === 'true') !== want && matches(el)) {
                    el.scrollIntoView({ block: 'center', inline: 'center' });
                    var r = el.getBoundingClientRect();
                    widgets.push({ text: labelOf(el).slice(0, 120), x: r.left + r.width / 2, y: r.top + r.height / 2 });
                }
            });
            return JSON.stringify({ native: native, widgets: widgets });
        })()
        """;

        using var doc = JsonDocument.Parse(await ctx.Browser.EvalAsync(script, ct));
        var matched = doc.RootElement.GetProperty("native").EnumerateArray().Select(e => e.GetString() ?? "").ToList();
        foreach (var w in doc.RootElement.GetProperty("widgets").EnumerateArray())
        {
            await ctx.Browser.ClickAtPointAsync(w.GetProperty("x").GetDouble(), w.GetProperty("y").GetDouble(), ct);
            await Task.Delay(200, ct);
            matched.Add(w.GetProperty("text").GetString() ?? "");
        }
        return JsonSerializer.Serialize(new { changedCount = matched.Count, matches = matched });
    }
}
