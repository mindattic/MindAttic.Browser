using System.Text.Json;

namespace MindAttic.Browser.Operator.Tools;

/// <summary>
/// Read-only snapshot of the current page — title, heading, visible clickable elements, visible
/// form fields, and a short body-text excerpt — so the agent can decide what to click or type
/// next without ever seeing the raw DOM. Modelled on Prose.Core's GetPageStatusTool/DiagnoseScript,
/// trimmed to generic fields (no KDP-specific processing-banner detection).
/// </summary>
public class ReadPageTool : IWebNavTool
{
    public string Name => "read_page";

    public string Description =>
        "Read the current page: title, URL, main heading, up to 40 visible clickable elements " +
        "(buttons/links/ARIA widgets with their text), up to 30 form fields (label, type, and " +
        "value / checked state / dropdown options; file inputs included even when hidden), and " +
        "a short excerpt of the visible body text. Call this before clicking or " +
        "typing whenever you're not already sure what's on the page.";

    public string ParametersJsonSchema => """{ "type": "object", "properties": {} }""";

    private const string Script = """
    (function() {
        function isVisible(el) {
            var r = el.getBoundingClientRect();
            return r.width > 0 && r.height > 0;
        }
        function short(s, n) {
            s = (s || '').trim().replace(/\s+/g, ' ');
            return s.length > n ? s.slice(0, n) + '…' : s;
        }

        var clickable = Array.from(document.querySelectorAll('button, a, input[type=submit], input[type=button], summary, [role=button], [role=option], [role=menuitem], [role=tab], [role=combobox]'))
            .filter(isVisible)
            .map(function (el) {
                var text = el.tagName === 'INPUT' ? (el.value || '') : (el.textContent || el.getAttribute('aria-label') || '');
                return short(text, 80);
            })
            .filter(function (t) { return t.length > 0; })
            .slice(0, 40);

        // File inputs are usually hidden behind a styled button, so they're listed even when invisible.
        var fields = Array.from(document.querySelectorAll('input:not([type=submit]):not([type=button]):not([type=hidden]), textarea, select, [role=checkbox], [role=radio]'))
            .filter(function (el) { return el.type === 'file' || isVisible(el); })
            .map(function (el) {
                var label = el.labels && el.labels.length ? el.labels[0].textContent
                    : (el.getAttribute('aria-label') || el.getAttribute('placeholder') || el.textContent || el.id || '');
                var role = el.getAttribute('role');
                var type = role || (el.tagName === 'SELECT' ? 'select' : (el.type || 'text'));
                var f = { label: short(label, 60), type: type };
                if (type === 'checkbox' || type === 'radio') f.checked = role ? el.getAttribute('aria-checked') === 'true' : el.checked;
                else if (type === 'select') f.options = Array.from(el.options).map(function (o) { return short(o.textContent, 40); }).slice(0, 15);
                else if (type !== 'file') f.value = short(el.value, 60);
                return f;
            })
            .slice(0, 30);

        var h1 = document.querySelector('h1, h2');
        return JSON.stringify({
            url: location.href,
            title: document.title,
            heading: h1 ? short(h1.textContent, 120) : null,
            clickable: clickable,
            fields: fields,
            bodyTextExcerpt: short(document.body.innerText, 2000),
        });
    })()
    """;

    public Task<string> InvokeAsync(JsonElement args, WebNavOperatorContext ctx, CancellationToken ct) =>
        ctx.Browser.EvalAsync(Script, ct);
}
