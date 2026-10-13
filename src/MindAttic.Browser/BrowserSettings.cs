using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MindAttic.Vault.Settings;

namespace MindAttic.Browser;

/// <summary>The browser's settings, saved through MindAttic.Vault (%APPDATA%\MindAttic\MindAttic.Browser\settings.json).</summary>
public sealed class BrowserSettings
{
    public const string DefaultHomePage = "https://mindattic.com/";
    public const string DefaultSearchUrl = "https://www.bing.com/search?q={query}";
    public const string DefaultCaptureFolder = @"R:\MindAttic\Capture";

    /// <summary>Opened at start (without a URL argument) and by the Home button.</summary>
    public string HomePage { get; set; } = DefaultHomePage;

    /// <summary>Address-bar text that isn't a URL is searched here; {query} is replaced by the text.</summary>
    public string SearchUrl { get; set; } = DefaultSearchUrl;

    /// <summary>Screenshots and recordings go to {CaptureFolder}\{site}\{site}_{timestamp}.{ext}.</summary>
    public string CaptureFolder { get; set; } = DefaultCaptureFolder;

    /// <summary>Every saved screenshot or recording is also put on the clipboard.</summary>
    public bool CopyToClipboard { get; set; } = true;

    /// <summary>Screenshots open in the crop lightbox before saving; off saves the whole page at once.</summary>
    public bool CropScreenshots { get; set; } = true;

    /// <summary>Starred pages. Unstarring marks one removed rather than deleting it (HOUSE-LAW-2).</summary>
    public List<Favorite> Favorites { get; set; } = [];

    /// <summary>Ids of the actions under the cog that also get their own toolbar button.</summary>
    public List<string> PinnedActions { get; set; } = ["screenshot", "record"];

    /// <summary>Keys written by a newer version survive a save.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }

    /// <summary>The capture folder, or Pictures\MindAttic Browser when its drive doesn't exist on this machine.</summary>
    public string ResolvedCaptureFolder()
    {
        var root = Path.GetPathRoot(CaptureFolder);
        if (!string.IsNullOrWhiteSpace(CaptureFolder) && (string.IsNullOrEmpty(root) || Directory.Exists(root))) return CaptureFolder;
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "MindAttic Browser");
    }

    /// <summary>Starred pages, newest first.</summary>
    public IReadOnlyList<Favorite> ActiveFavorites() =>
        Favorites.Where(f => f.RemovedAt is null).OrderByDescending(f => f.AddedAt).ToList();

    public bool IsFavorite(string? url) => Find(url) is { RemovedAt: null };

    /// <summary>Stars <paramref name="url"/>; a page starred before gets its old entry back.</summary>
    public void Star(string url, string? title, DateTimeOffset now)
    {
        var name = string.IsNullOrWhiteSpace(title) ? url : title!.Trim();
        if (Find(url) is { } existing)
        {
            existing.RemovedAt = null;
            existing.Title = name;
            existing.AddedAt = now;
            return;
        }
        Favorites.Add(new Favorite { Url = url, Title = name, AddedAt = now });
    }

    public void Unstar(string url, DateTimeOffset now)
    {
        if (Find(url) is { RemovedAt: null } f) f.RemovedAt = now;
    }

    private Favorite? Find(string? url) =>
        url is null ? null : Favorites.FirstOrDefault(f => string.Equals(Normalize(f.Url), Normalize(url), StringComparison.OrdinalIgnoreCase));

    /// <summary>"https://a.com/" and "https://a.com" are the same page.</summary>
    private static string Normalize(string url) => url.Trim().TrimEnd('/');
}

public sealed class Favorite
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTimeOffset AddedAt { get; set; }
    public DateTimeOffset? RemovedAt { get; set; }
}

/// <summary>Loads and saves <see cref="BrowserSettings"/> via MindAttic.Vault.</summary>
public sealed class BrowserSettingsStore(JsonSettingsStore<BrowserSettings> store)
{
    public const string AppBucket = "MindAttic.Browser";

    public BrowserSettingsStore() : this(JsonSettingsStore<BrowserSettings>.ForApp(AppBucket)) { }

    public string FilePath => store.FilePath;

    public BrowserSettings Load() => store.Load();

    public void Save(BrowserSettings settings) => store.Save(settings);
}
