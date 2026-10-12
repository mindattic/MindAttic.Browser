using System.IO;
using System.Text;
using MindAttic.Capture;

namespace MindAttic.Browser;

/// <summary>Where a screenshot of the current page goes: {folder}\{site}\{site}_{timestamp}.png, never overwriting.</summary>
internal static class CaptureFiles
{
    /// <summary>The site a page belongs to: its host ("youtube.com"), or the served folder's subject for local sites.</summary>
    public static string Subject(string? url, BrowserTarget target)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Host.Length == 0) return Safe(target.AppName);
        if (target.VirtualHost is { } vhost && string.Equals(uri.Host, vhost, StringComparison.OrdinalIgnoreCase)) return Safe(target.AppName);
        var host = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;
        return Safe(host);
    }

    public static string NextScreenshot(string folder, string subject, DateTimeOffset now, Func<string, bool>? exists = null)
    {
        exists ??= File.Exists;
        var dir = Path.Combine(folder, subject);
        var stem = Path.Combine(dir, $"{subject}_{CaptureNaming.Timestamp(now)}");
        var path = stem + ".png";
        for (var i = 2; exists(path); i++) path = $"{stem}_{i}.png";
        return path;
    }

    /// <summary>A name that is safe as a folder and file name prefix.</summary>
    internal static string Safe(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder();
        foreach (var c in name.Trim()) sb.Append(invalid.Contains(c) || char.IsWhiteSpace(c) ? '_' : c);
        var s = sb.ToString().Trim('.', '_');
        return s.Length == 0 ? "page" : s;
    }
}
