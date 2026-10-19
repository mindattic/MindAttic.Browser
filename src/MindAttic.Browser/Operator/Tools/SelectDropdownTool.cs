using System.Text.Json;

namespace MindAttic.Browser.Operator.Tools;

/// <summary>
/// Picks an option in a native <c>&lt;select&gt;</c>, found by its label, by visible option text
/// (exact first, then contains) — the same matching and input/change dispatch as AutoWebNav's
/// <c>BrowserActions.SelectOptionOnResolvedAsync</c>. Custom (non-native) dropdowns are just
/// clickable elements: open with click, then click the option.
/// </summary>
public class SelectDropdownTool : IWebNavTool
{
    public string Name => "select_dropdown";

    public string Description =>
        "Choose an option in a native <select> dropdown. `field` is the dropdown's label, " +
        "aria-label, name or id (blank = the only/first visible dropdown); `option` is the visible " +
        "option text (exact match first, then contains). Returns {selected:true, value} or an " +
        "error listing the options that exist. For a custom (non-<select>) dropdown, use click to " +
        "open it and click again on the option text.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "field": { "type": "string" },
        "option": { "type": "string" }
      },
      "required": ["option"]
    }
    """;

    public Task<string> InvokeAsync(JsonElement args, WebNavOperatorContext ctx, CancellationToken ct)
    {
        var field = args.TryGetProperty("field", out var f) ? f.GetString() ?? "" : "";
        var option = args.GetProperty("option").GetString() ?? "";
        var script = $$"""
        (function() {
            var field = {{JsonSerializer.Serialize(field)}}.trim().toLowerCase();
            var want = {{JsonSerializer.Serialize(option)}}.trim().toLowerCase();
            function visible(el) { var r = el.getBoundingClientRect(); return r.width > 0 && r.height > 0; }
            function names(el) {
                var t = [el.id, el.name, el.getAttribute('aria-label')];
                if (el.labels) for (var i = 0; i < el.labels.length; i++) t.push(el.labels[i].textContent);
                return t.filter(Boolean).join(' ').replace(/\s+/g, ' ').toLowerCase();
            }
            var selects = Array.from(document.querySelectorAll('select')).filter(visible);
            var el = field ? selects.find(function (s) { return names(s).indexOf(field) !== -1; }) : selects[0];
            if (!el) return JSON.stringify({ selected: false, error: 'no matching <select>', dropdowns: selects.map(names).slice(0, 10) });
            var opts = Array.from(el.options), idx = -1;
            for (var i = 0; i < opts.length; i++) if ((opts[i].textContent || '').trim().toLowerCase() === want) { idx = i; break; }
            if (idx < 0) for (var i = 0; i < opts.length; i++) if ((opts[i].textContent || '').toLowerCase().indexOf(want) !== -1) { idx = i; break; }
            if (idx < 0) return JSON.stringify({ selected: false, error: 'no option matches', options: opts.map(function (o) { return o.textContent.trim(); }).slice(0, 40) });
            el.selectedIndex = idx;
            el.dispatchEvent(new Event('input', { bubbles: true }));
            el.dispatchEvent(new Event('change', { bubbles: true }));
            return JSON.stringify({ selected: true, value: opts[idx].textContent.trim() });
        })()
        """;
        return ctx.Browser.EvalAsync(script, ct);
    }
}
