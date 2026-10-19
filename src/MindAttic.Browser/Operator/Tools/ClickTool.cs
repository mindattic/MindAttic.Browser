using System.Text.Json;

namespace MindAttic.Browser.Operator.Tools;

/// <summary>
/// Finds and clicks the first clickable element whose visible text or aria-label contains any of
/// the given candidate phrases. Modelled on Prose.Core's ClickButtonTool (minus its KDP-specific
/// confirmation-checkbox auto-tick) — generalized for any site, since wording a human would click
/// on is more resilient to a guessed CSS selector the agent has no way to verify in advance.
/// </summary>
public class ClickTool : IWebNavTool
{
    public string Name => "click";

    public string Description =>
        "Find and click the first visible clickable element (button, link, submit/button input, " +
        "or an ARIA button/option/menu item/tab/combobox — so custom dropdowns are opened and " +
        "picked with two clicks) whose text or aria-label contains any of the given candidate phrases " +
        "(case-insensitive). Pass a few plausible variants, e.g. [\"sign in\", \"log in\"]. " +
        "Real buttons are preferred over links, and links inside a footer/nav are skipped as a " +
        "fallback-only source of false matches. Returns {clicked:true, matchedText} or " +
        "{clicked:false} if nothing matched.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "text_candidates": { "type": "array", "items": { "type": "string" }, "description": "Candidate substrings to match, e.g. [\"sign in\"]." }
      },
      "required": ["text_candidates"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, WebNavOperatorContext ctx, CancellationToken ct)
    {
        var candidates = args.GetProperty("text_candidates").EnumerateArray()
            .Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToArray();
        if (candidates.Length == 0) return JsonSerializer.Serialize(new { error = "text_candidates was empty." });

        var candidatesJson = JsonSerializer.Serialize(candidates);
        var script = $$"""
        (function() {
            var candidates = {{candidatesJson}}.map(function (c) { return c.toLowerCase(); });

            function textOf(el) {
                var direct = (el.tagName === 'INPUT' ? (el.value || '') : (el.textContent || '')).trim();
                if (direct) return direct.toLowerCase();
                return (el.getAttribute('aria-label') || el.getAttribute('title') || '').trim().toLowerCase();
            }
            function inFooterOrNav(el) {
                return !!el.closest('footer, nav, [class*="footer" i], [id*="footer" i], [class*="nav" i], [id*="nav" i]');
            }
            function isVisible(el) {
                var r = el.getBoundingClientRect();
                return r.width > 0 && r.height > 0;
            }
            function tryMatch(elements) {
                for (var i = 0; i < elements.length; i++) {
                    var el = elements[i];
                    if (!isVisible(el)) continue;
                    var text = textOf(el);
                    if (!text || text.length > 80) continue;
                    for (var j = 0; j < candidates.length; j++) {
                        if (text.indexOf(candidates[j]) !== -1) return { el: el, text: text };
                    }
                }
                return null;
            }

            // ARIA widgets count as buttons: a custom dropdown's trigger and its options, menu items
            // and tabs are opened and picked exactly like a real button.
            var found = tryMatch(document.querySelectorAll('button, input[type=submit], input[type=button], summary, ' +
                '[role=button], [role=option], [role=menuitem], [role=menuitemradio], [role=tab], [role=combobox], [role=treeitem], [role=link]'));
            if (!found) {
                var links = Array.from(document.querySelectorAll('a')).filter(function (a) { return !inFooterOrNav(a); });
                found = tryMatch(links);
            }

            if (found) { found.el.click(); return JSON.stringify({ clicked: true, matchedText: found.text }); }
            return JSON.stringify({ clicked: false });
        })()
        """;

        var result = await ctx.Browser.EvalAsync(script, ct);
        // A real click commonly triggers a client-side transition — a short settle delay gives the
        // page a moment to start rendering its next state before the agent's next tool call reads it.
        if (result.Contains("\"clicked\":true")) await Task.Delay(1000, ct);
        return result;
    }
}
