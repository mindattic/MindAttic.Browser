using System.Text.Json;

namespace MindAttic.Browser.Operator.Tools;

/// <summary>
/// Sets a plain text/number input or textarea's value by element id or label text, via the native
/// property setter + dispatched input/change events (a plain <c>el.value = x</c> doesn't notify a
/// React-controlled input's own state). Modelled on Prose.Core's SetFieldTool, generalized: no
/// site-specific known-id map, since there's no one site to special-case here. Not for
/// checkboxes/radios/selects or rich-text editors.
/// </summary>
public class SetFieldTool : IWebNavTool
{
    public string Name => "set_field";

    public string Description =>
        "Set a visible text/number input or textarea's value. `field` is tried two ways in " +
        "order: (1) a literal DOM element id, (2) a label-text substring match (case-insensitive) " +
        "against the nearest input on the page — use this for a field with no known id, e.g. " +
        "\"Email\" or \"Search\". Not for checkboxes, radio buttons, dropdowns, or a rich-text " +
        "editor. Returns {found:false, tried:[...]} if nothing matched.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "field": { "type": "string", "description": "A DOM element id, or label/placeholder text to match." },
        "value": { "type": "string" }
      },
      "required": ["field", "value"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, WebNavOperatorContext ctx, CancellationToken ct)
    {
        var field = args.GetProperty("field").GetString() ?? "";
        var value = args.GetProperty("value").GetString() ?? "";
        if (field.Length == 0) return JsonSerializer.Serialize(new { error = "field was empty." });

        var idJs = JsonSerializer.Serialize(field);
        var labelJs = JsonSerializer.Serialize(field);
        var valueJs = JsonSerializer.Serialize(value);

        var script = $$"""
        (function() {
            function apply(el) {
                var proto = el.tagName === 'TEXTAREA' ? window.HTMLTextAreaElement.prototype : window.HTMLInputElement.prototype;
                var setter = Object.getOwnPropertyDescriptor(proto, 'value').set;
                setter.call(el, {{valueJs}});
                el.dispatchEvent(new Event('input', { bubbles: true }));
                el.dispatchEvent(new Event('change', { bubbles: true }));
                el.blur();
            }

            var byId = document.getElementById({{idJs}});
            if (byId) { apply(byId); return JSON.stringify({ found: true, strategy: 'id', elementId: {{idJs}}, value: byId.value }); }

            var query = {{labelJs}}.trim().toLowerCase();
            var labelEls = Array.from(document.querySelectorAll('label, span, div, legend')).filter(function (el) {
                var t = (el.textContent || '').trim().toLowerCase();
                return t === query || t.indexOf(query) === 0;
            });
            for (var i = 0; i < labelEls.length; i++) {
                var label = labelEls[i];
                var input = null;
                if (label.htmlFor) {
                    var byFor = document.getElementById(label.htmlFor);
                    if (byFor && (byFor.tagName === 'INPUT' || byFor.tagName === 'TEXTAREA')) input = byFor;
                }
                if (!input) {
                    var container = label.closest('div, form') || label.parentElement;
                    if (container) input = container.querySelector('input[type="text"], input[type="email"], input[type="search"], input[type="number"], input:not([type]), textarea');
                }
                if (input) { apply(input); return JSON.stringify({ found: true, strategy: 'label', matchedLabel: label.textContent.trim(), value: input.value }); }
            }

            // Last resort: a matching placeholder or name/aria-label attribute.
            var byAttr = Array.from(document.querySelectorAll('input, textarea')).find(function (el) {
                var t = ((el.placeholder || '') + ' ' + (el.name || '') + ' ' + (el.getAttribute('aria-label') || '')).toLowerCase();
                return t.indexOf(query) !== -1;
            });
            if (byAttr) { apply(byAttr); return JSON.stringify({ found: true, strategy: 'attribute', value: byAttr.value }); }

            return JSON.stringify({ found: false, tried: [{{idJs}}, {{labelJs}} + ' (label/placeholder search)'] });
        })()
        """;

        return await ctx.Browser.EvalAsync(script, ct);
    }
}
