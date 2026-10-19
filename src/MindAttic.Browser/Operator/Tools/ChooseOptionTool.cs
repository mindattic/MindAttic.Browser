using System.Text.Json;

namespace MindAttic.Browser.Operator.Tools;

/// <summary>
/// Selects ONE radio button (or checkbox) by its visible text — Prose.Core's SelectFormOptionTool,
/// minus nothing: the ranked exact → tight-substring → wide-substring matching is what keeps a bare
/// "Yes" from landing inside an unrelated "Yes, apply…" sentence, and custom <c>[role=radio]</c>
/// widgets get a real CDP click because they ignore synthetic ones.
/// </summary>
public class ChooseOptionTool : IWebNavTool
{
    public string Name => "choose_option";

    public string Description =>
        "Select ONE radio button (or a single checkbox) whose label text contains any of the " +
        "candidate phrases (case-insensitive), e.g. [\"Standard shipping\"] or [\"No\"]. Exact " +
        "label matches win over partial ones. Works for native radios and custom role=radio " +
        "widgets. An option that's already selected counts as success. Returns " +
        "{selected:true, matchedText} or {selected:false}.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": { "text_candidates": { "type": "array", "items": { "type": "string" } } },
      "required": ["text_candidates"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, WebNavOperatorContext ctx, CancellationToken ct)
    {
        var candidates = args.GetProperty("text_candidates").EnumerateArray()
            .Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToArray();
        if (candidates.Length == 0) return JsonSerializer.Serialize(new { error = "text_candidates was empty." });

        using var doc = JsonDocument.Parse(await ctx.Browser.EvalAsync(Script(JsonSerializer.Serialize(candidates)), ct));
        var root = doc.RootElement;
        if (!root.GetProperty("found").GetBoolean()) return JsonSerializer.Serialize(new { selected = false });

        var text = root.GetProperty("text").GetString();
        if (root.GetProperty("kind").GetString() == "native")
            return JsonSerializer.Serialize(new { selected = true, matchedText = text });

        await ctx.Browser.ClickAtPointAsync(root.GetProperty("centerX").GetDouble(), root.GetProperty("centerY").GetDouble(), ct);
        await Task.Delay(300, ct);
        return JsonSerializer.Serialize(new { selected = true, matchedText = text });
    }

    private static string Script(string candidatesJson) => $$"""
    (function() {
        var candidates = {{candidatesJson}}.map(function (c) { return c.toLowerCase().trim(); });
        var sub = candidates.filter(function (c) { return c.length >= 4; });
        function tight(el) {
            var t = [];
            var l = el.closest('label'); if (l) t.push(l.textContent);
            if (el.id) { var f = document.querySelector('label[for="' + CSS.escape(el.id) + '"]'); if (f) t.push(f.textContent); }
            if (el.parentElement) t.push(el.parentElement.textContent);
            if (el.nextElementSibling) t.push(el.nextElementSibling.textContent);
            return t.map(function (x) { return (x || '').trim().toLowerCase(); }).filter(Boolean);
        }
        function wide(el) {
            var t = tight(el), n = el;
            for (var d = 0; d < 5 && n.parentElement; d++) { n = n.parentElement; t.push((n.textContent || '').trim().toLowerCase()); }
            return t;
        }
        function wText(el) { return ((el.getAttribute('aria-label') || '') + ' ' + (el.textContent || '')).trim().toLowerCase(); }
        var natAll = Array.from(document.querySelectorAll('input[type=radio], input[type=checkbox]'));
        var widAll = Array.from(document.querySelectorAll('[role=radio], [role=checkbox]'));

        for (var i = 0; i < natAll.length; i++) if (natAll[i].checked) {
            var a = tight(natAll[i]); for (var j = 0; j < a.length; j++) if (candidates.indexOf(a[j]) !== -1)
                return JSON.stringify({ found: true, kind: 'native', text: a[j].slice(0, 200) });
        }
        for (var i = 0; i < widAll.length; i++) if (widAll[i].getAttribute('aria-checked') === 'true' && candidates.indexOf(wText(widAll[i])) !== -1)
            return JSON.stringify({ found: true, kind: 'native', text: wText(widAll[i]).slice(0, 200) });

        var nat = natAll.filter(function (e) { return !e.checked; });
        var wid = widAll.filter(function (e) { return e.getAttribute('aria-checked') !== 'true'; });
        function n(el, t) { el.click(); return JSON.stringify({ found: true, kind: 'native', text: t.slice(0, 200) }); }
        function w(el, t) {
            el.scrollIntoView({ block: 'center', inline: 'center' });
            var r = el.getBoundingClientRect();
            return JSON.stringify({ found: true, kind: 'widget', text: t.slice(0, 200), centerX: r.left + r.width / 2, centerY: r.top + r.height / 2 });
        }
        for (var i = 0; i < nat.length; i++) { var a = tight(nat[i]); for (var j = 0; j < a.length; j++) if (candidates.indexOf(a[j]) !== -1) return n(nat[i], a[j]); }
        for (var i = 0; i < wid.length; i++) if (candidates.indexOf(wText(wid[i])) !== -1) return w(wid[i], wText(wid[i]));
        for (var i = 0; i < nat.length; i++) { var a = tight(nat[i]); for (var j = 0; j < a.length; j++) for (var k = 0; k < sub.length; k++) if (a[j].indexOf(sub[k]) !== -1) return n(nat[i], a[j]); }
        for (var i = 0; i < wid.length; i++) for (var k = 0; k < sub.length; k++) if (wText(wid[i]).indexOf(sub[k]) !== -1) return w(wid[i], wText(wid[i]));
        for (var i = 0; i < nat.length; i++) { var a = wide(nat[i]); for (var j = 0; j < a.length; j++) for (var k = 0; k < sub.length; k++) if (a[j].indexOf(sub[k]) !== -1) return n(nat[i], a[j]); }
        return JSON.stringify({ found: false });
    })()
    """;
}
