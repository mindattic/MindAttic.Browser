using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MindAttic.Vault.Settings;
using MindAttic.Vault.Settings.Manifest;

namespace MindAttic.Browser;

/// <summary>
/// The browser's settings, saved through MindAttic.Vault (%APPDATA%\MindAttic\MindAttic.Browser\settings.json) and
/// edited in MindAttic.Vault.Desktop (the cog's Settings). Favorites and history share the file but are browsing data,
/// not settings, so they stay out of the manifest.
/// </summary>
[SettingsApp(BrowserSettingsStore.AppBucket, "MindAttic.Browser", Icon = "globe",
    Description = "WebView2 browser with page capture and AutoWebNav.")]
public sealed class BrowserSettings
{
    public const string DefaultHomePage = "https://mindattic.com/";
    public const string DefaultSearchUrl = "https://www.bing.com/search?q={query}";
    public const string DefaultCaptureFolder = @"R:\MindAttic\Capture";

    /// <summary>Opened at start (without a URL argument) and by the Home button.</summary>
    [Setting("Home page", Group = "Browsing", Order = 1, Help = "Opened at start (without a URL) and by the Home button.")]
    [UrlSetting]
    public string HomePage { get; set; } = DefaultHomePage;

    /// <summary>Address-bar text that isn't a URL is searched here; {query} is replaced by the text.</summary>
    [Setting("Search URL", Group = "Browsing", Order = 2,
        Help = "Address-bar text that isn't a URL is searched here; {query} marks where the text goes, e.g. https://www.google.com/search?q={query}. Without {query} the default search is used.")]
    public string SearchUrl { get; set; } = DefaultSearchUrl;

    /// <summary>Screenshots and recordings go to {CaptureFolder}\{site}\{site}_{timestamp}.{ext}.</summary>
    [Setting("Capture folder", Group = "Capture", Order = 1,
        Help = "Screenshots and recordings go to {folder}\\{site}. Screenshots use a change at once; recordings from the next launch.")]
    [PathPicker(PathKind.Folder)]
    public string CaptureFolder { get; set; } = DefaultCaptureFolder;

    /// <summary>Every saved screenshot or recording is also put on the clipboard.</summary>
    [Setting("Copy captures to the clipboard", Group = "Capture", Order = 2)]
    public bool CopyToClipboard { get; set; } = true;

    /// <summary>Screenshots open in the crop lightbox before saving; off saves the whole page at once.</summary>
    [Setting("Crop screenshots before saving", Group = "Capture", Order = 3, Help = "Off saves the whole page at once.")]
    public bool CropScreenshots { get; set; } = true;

    /// <summary>Starred pages. Unstarring marks one removed rather than deleting it (HOUSE-LAW-2).</summary>
    [SettingIgnore]
    public List<Favorite> Favorites { get; set; } = [];

    /// <summary>Pages visited, newest last. Unlike Favorites, Ctrl+H really deletes entries —
    /// clearing browsing history is the whole point of the feature, not data loss to guard
    /// against (HOUSE-LAW-2 is about business data, not a mandate to keep a forensic log of
    /// every page the user asked to forget).</summary>
    [SettingIgnore]
    public List<HistoryEntry> History { get; set; } = [];

    /// <summary>Ids of the actions under the cog that also get their own toolbar button.</summary>
    [Setting("Toolbar buttons", Group = "Toolbar", Order = 1,
        Help = "Cog actions that also get their own toolbar button: screenshot, record, autowebnav.")]
    public List<string> PinnedActions { get; set; } = ["screenshot", "record", "autowebnav"];

    /// <summary>Keys written by a newer version survive a save.</summary>
    [JsonExtensionData]
    [SettingIgnore]
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

    // ---------------------------------------------------------------- history

    public void RecordVisit(string url, string? title, DateTimeOffset now) =>
        History.Add(new HistoryEntry { Url = url, Title = string.IsNullOrWhiteSpace(title) ? url : title!.Trim(), VisitedAt = now });

    /// <summary>History, newest first.</summary>
    public IReadOnlyList<HistoryEntry> RecentHistory() => History.OrderByDescending(h => h.VisitedAt).ToList();

    /// <summary>Widening windows Ctrl+H tries in order, each paired with the label its status
    /// line reports. Null means "no lower bound" — all of it.</summary>
    private static readonly (string Label, TimeSpan? Window)[] FlushWindows =
    [
        ("the last 15 minutes", TimeSpan.FromMinutes(15)),
        ("the last hour", TimeSpan.FromHours(1)),
        ("the last day", TimeSpan.FromDays(1)),
        ("the last week", TimeSpan.FromDays(7)),
        ("all time", null),
    ];

    /// <summary>
    /// Clears the narrowest window (15 minutes) that actually has something in it, widening to an
    /// hour, a day, a week, then everything, until one does. Repeated calls — one per Ctrl+H —
    /// work outward through the whole history a bucket at a time, ending with nothing left.
    /// </summary>
    public (int Count, string Label) FlushNextHistoryBucket(DateTimeOffset now)
    {
        foreach (var (label, window) in FlushWindows)
        {
            var matches = window is null ? History.ToList() : History.Where(h => now - h.VisitedAt <= window.Value).ToList();
            if (matches.Count == 0) continue;
            foreach (var m in matches) History.Remove(m);
            return (matches.Count, label);
        }
        return (0, "all time");
    }
}

public sealed class HistoryEntry
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTimeOffset VisitedAt { get; set; }
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

    /// <summary>First start seeds the defaults and writes the manifest MindAttic.Vault.Desktop renders.</summary>
    public BrowserSettings Load() => store.LoadOrSeed();

    public string Directory => store.Directory;

    /// <summary>
    /// Copies the setting fields (not favorites or history, which this window owns) from the file into
    /// <paramref name="target"/>, so an edit saved in MindAttic.Vault.Desktop survives the browser's next save.
    /// Returns true when anything changed.
    /// </summary>
    public bool RefreshSettingsInto(BrowserSettings target)
    {
        // store.Load() answers defaults for a file it can't read or parse (mid-edit, a typo in a hand edit);
        // copying those in would reset every setting, and the next history save would write the reset to disk.
        if (!IsReadableJson(store.FilePath)) return false;
        var saved = store.Load();
        var changed = target.HomePage != saved.HomePage || target.SearchUrl != saved.SearchUrl || target.CaptureFolder != saved.CaptureFolder
                      || target.CopyToClipboard != saved.CopyToClipboard || target.CropScreenshots != saved.CropScreenshots
                      || !target.PinnedActions.SequenceEqual(saved.PinnedActions);
        target.HomePage = saved.HomePage;
        target.SearchUrl = saved.SearchUrl;
        target.CaptureFolder = saved.CaptureFolder;
        target.CopyToClipboard = saved.CopyToClipboard;
        target.CropScreenshots = saved.CropScreenshots;
        // In place: the open window's cog menu holds a delegate bound to this list instance (M.BRWS-T24).
        target.PinnedActions.Clear();
        target.PinnedActions.AddRange(saved.PinnedActions);
        return changed;
    }

    private static bool IsReadableJson(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }

    public void Save(BrowserSettings settings) => store.Save(settings);
}
