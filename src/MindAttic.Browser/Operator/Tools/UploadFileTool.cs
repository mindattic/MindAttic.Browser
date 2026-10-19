using System.IO;
using System.Text.Json;

namespace MindAttic.Browser.Operator.Tools;

/// <summary>
/// Attaches a local file to a file input with no OS file dialog — CDP <c>DOM.setFileInputFiles</c>
/// via <see cref="AutoWebNav.IBrowserSurface.InjectFileAsync"/>, the path KdpPublish's manuscript and
/// cover uploads use. The input is picked by label text (or the first one), marked on
/// <c>window.__mabUploadTarget</c>, and that expression is what the surface injects into.
/// </summary>
public class UploadFileTool : IWebNavTool
{
    public string Name => "upload_file";

    public string Description =>
        "Attach a local file to a file-upload control — no file dialog opens. `file_path` is an " +
        "absolute path on this PC. `field` (optional) is label/aria-label/name/id text to pick the " +
        "right input when there are several; blank uses the first. Hidden inputs behind a styled " +
        "'Upload' button work too. Call read_page afterwards to confirm the site accepted it.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "file_path": { "type": "string" },
        "field": { "type": "string" }
      },
      "required": ["file_path"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, WebNavOperatorContext ctx, CancellationToken ct)
    {
        var path = args.GetProperty("file_path").GetString() ?? "";
        if (!File.Exists(path)) return JsonSerializer.Serialize(new { error = $"File not found: {path}" });
        var field = args.TryGetProperty("field", out var f) ? f.GetString() ?? "" : "";

        var locate = $$"""
        (function() {
            var field = {{JsonSerializer.Serialize(field)}}.trim().toLowerCase();
            var inputs = Array.from(document.querySelectorAll('input[type=file]'));
            function names(el) {
                var t = [el.id, el.name, el.getAttribute('aria-label'), el.accept];
                if (el.labels) for (var i = 0; i < el.labels.length; i++) t.push(el.labels[i].textContent);
                var p = el.closest('div, section, form, fieldset'); if (p) t.push((p.textContent || '').slice(0, 300));
                return t.filter(Boolean).join(' ').toLowerCase();
            }
            var el = field ? inputs.find(function (i) { return names(i).indexOf(field) !== -1; }) : inputs[0];
            window.__mabUploadTarget = el || null;
            return JSON.stringify({ found: !!el, count: inputs.length });
        })()
        """;
        var found = await ctx.Browser.EvalAsync(locate, ct);
        if (!found.Contains("\"found\":true"))
            return JsonSerializer.Serialize(new { error = "No matching file input on this page.", detail = JsonDocument.Parse(found).RootElement });

        await ctx.Browser.InjectFileAsync(path, "window.__mabUploadTarget", ct);
        await Task.Delay(500, ct);
        return JsonSerializer.Serialize(new { ok = true, file = path });
    }
}
