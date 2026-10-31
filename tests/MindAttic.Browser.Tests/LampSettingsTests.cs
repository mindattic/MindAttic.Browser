using System.IO;
using System.Reflection;
using MindAttic.Vault.Launch;
using MindAttic.Vault.Paths;
using MindAttic.Vault.Settings;
using MindAttic.Vault.Settings.Manifest;
using NUnit.Framework;

namespace MindAttic.Browser.Tests;

/// <summary>Every test in this assembly sees temp Vault roots, so nothing can write the real AppData.</summary>
[SetUpFixture]
public sealed class TestVaultRoots
{
    private string root = "";

    [OneTimeSetUp]
    public void SetUp()
    {
        root = Path.Combine(Path.GetTempPath(), "BrowserVault-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable(VaultPaths.RoamingRootEnvVar, Path.Combine(root, "roaming"));
        Environment.SetEnvironmentVariable(VaultPaths.LocalRootEnvVar, Path.Combine(root, "local"));
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(VaultPaths.RoamingRootEnvVar, null);
        Environment.SetEnvironmentVariable(VaultPaths.LocalRootEnvVar, null);
        try { Directory.Delete(root, recursive: true); } catch { }
    }
}

/// <summary>LAMP (M.BRWS-T23): the browser's settings edited in Vault, one end-to-end test per setting.</summary>
[NonParallelizable]
public class LampSettingsTests
{
    private string dir = "";
    private BrowserSettingsStore Store() => new(new JsonSettingsStore<BrowserSettings>(dir));

    [SetUp]
    public void SetUp() => dir = Path.Combine(Path.GetTempPath(), "browser-lamp-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(dir, recursive: true); } catch { }
    }

    /// <summary>Today's settings.json (written by the pre-LAMP browser, same file and names).</summary>
    private void WriteToday(string json)
    {
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "settings.json"), json);
    }

    /// <summary>Vault.Desktop saving one field while the browser runs, then the window picking it up.</summary>
    private BrowserSettings EditInVault(BrowserSettings running, Action<BrowserSettings> edit)
    {
        var file = new JsonSettingsStore<BrowserSettings>(dir);
        file.Update(edit);
        Assert.That(Store().RefreshSettingsInto(running), Is.True);
        return running;
    }

    [Test]
    public void Setting_HomePage_ImportsRoundTripsAndIsWhereTheBrowserStarts()
    {
        WriteToday("""{ "homePage": "https://example.org/" }""");
        var running = Store().Load();
        Assert.That(running.HomePage, Is.EqualTo("https://example.org/"), "today's value kept");
        EditInVault(running, s => s.HomePage = "https://news.example/");
        Assert.That(BrowserTarget.Parse([], home: running.HomePage).Url, Is.EqualTo("https://news.example/"));
    }

    [Test]
    public void Setting_SearchUrl_ImportsRoundTripsAndSearchesTheAddressBar()
    {
        WriteToday("""{ "searchUrl": "https://duckduckgo.com/?q={query}" }""");
        var running = Store().Load();
        Assert.That(running.SearchUrl, Is.EqualTo("https://duckduckgo.com/?q={query}"));
        EditInVault(running, s => s.SearchUrl = "https://www.google.com/search?q={query}");
        Assert.That(BrowserTarget.NormalizeUrl("ants", running.SearchUrl), Is.EqualTo("https://www.google.com/search?q=ants"));
        Assert.That(BrowserTarget.NormalizeUrl("ants", "https://no-placeholder/"), Does.StartWith("https://www.bing.com/search?q="), "missing {query} falls back");
    }

    [Test]
    public void Setting_CaptureFolder_ImportsRoundTripsAndIsWhereCapturesGo()
    {
        WriteToday("""{ "captureFolder": "C:\\Caps" }""");
        var running = Store().Load();
        Assert.That(running.CaptureFolder, Is.EqualTo(@"C:\Caps"));
        var folder = Path.Combine(dir, "caps");
        EditInVault(running, s => s.CaptureFolder = folder);
        Assert.That(running.ResolvedCaptureFolder(), Is.EqualTo(folder));
    }

    [Test]
    public void Setting_CopyToClipboard_ImportsRoundTripsAndReachesTheRunningWindow()
    {
        WriteToday("""{ "copyToClipboard": false }""");
        var running = Store().Load();
        Assert.That(running.CopyToClipboard, Is.False);
        Assert.That(EditInVault(running, s => s.CopyToClipboard = true).CopyToClipboard, Is.True, "MainWindow reads App.Settings.CopyToClipboard per capture");
    }

    [Test]
    public void Setting_CropScreenshots_ImportsRoundTripsAndReachesTheRunningWindow()
    {
        WriteToday("""{ "cropScreenshots": false }""");
        var running = Store().Load();
        Assert.That(running.CropScreenshots, Is.False);
        Assert.That(EditInVault(running, s => s.CropScreenshots = true).CropScreenshots, Is.True, "MainWindow reads App.Settings.CropScreenshots per screenshot");
    }

    [Test]
    public void Setting_PinnedActions_ImportsRoundTripsAndPinsToolbarButtons()
    {
        WriteToday("""{ "pinnedActions": ["record"] }""");
        var running = Store().Load();
        Assert.That(running.PinnedActions, Is.EqualTo(new[] { "record" }));
        Assert.That(EditInVault(running, s => s.PinnedActions = ["screenshot", "autowebnav"]).PinnedActions.Contains("autowebnav"), Is.True,
            "the cog menu's pinned predicate is Settings.PinnedActions.Contains");
    }

    [Test]
    public void Setting_PinnedActions_VaultEditReachesThePredicateTheOpenWindowBoundAtStartup()
    {
        // MainWindow builds its cog menu once, with Settings.PinnedActions.Contains bound to the list it had then
        // (M.BRWS-T24): a Vault edit has to land in that same list, or the toolbar keeps the old buttons.
        WriteToday("""{ "pinnedActions": ["screenshot", "record", "autowebnav"] }""");
        var running = Store().Load();
        Func<string, bool> isPinned = running.PinnedActions.Contains;
        EditInVault(running, s => s.PinnedActions = ["screenshot", "record"]);
        Assert.That(isPinned("autowebnav"), Is.False, "unpinned in Vault, so its toolbar button goes away");
        Assert.That(isPinned("record"), Is.True);
    }

    [Test]
    public void EverySettingHasItsOwnTest()
    {
        var tested = GetType().GetMethods().Select(m => m.Name).Where(n => n.StartsWith("Setting_")).ToList();
        foreach (var field in SettingsManifestBuilder.Build<BrowserSettings>().Fields)
            Assert.That(tested.Any(t => t.StartsWith("Setting_" + char.ToUpperInvariant(field.Property[0]) + field.Property[1..] + "_")), Is.True, field.Property);
    }

    [Test]
    public void FavoritesAndHistoryAreBrowsingDataNotSettings_AndSurviveAVaultEdit()
    {
        var m = SettingsManifestBuilder.Build<BrowserSettings>();
        Assert.That(m.Fields.Select(f => f.Property), Is.EquivalentTo(new[] { "homePage", "searchUrl", "captureFolder", "copyToClipboard", "cropScreenshots", "pinnedActions" }));
        var running = Store().Load();
        running.RecordVisit("https://a.example/", "A", DateTimeOffset.Now);
        Store().Save(running);
        EditInVault(running, s => s.HomePage = "https://b.example/");
        Assert.That(running.History, Has.Count.EqualTo(1));
        Store().Save(running);
        var reread = Store().Load();
        Assert.That((reread.HomePage, reread.History.Count), Is.EqualTo(("https://b.example/", 1)), "the browser's next save keeps Vault's edit");
    }

    [Test]
    public void FirstStartSeedsDefaultsAndTheManifest()
    {
        var s = Store().Load();
        Assert.That((s.HomePage, s.SearchUrl, s.CaptureFolder), Is.EqualTo((BrowserSettings.DefaultHomePage, BrowserSettings.DefaultSearchUrl, BrowserSettings.DefaultCaptureFolder)));
        Assert.That(SettingsManifestFile.TryRead(Path.Combine(dir, SettingsManifest.FileName))!.AppId, Is.EqualTo("MindAttic.Browser"));
    }

    [Test]
    public void TheCogOpensVaultAndTheOldWindowIsGone()
    {
        Assert.That(typeof(BrowserSettings).Assembly.GetType("MindAttic.Browser.SettingsWindow"), Is.Null);
        Assert.That(VaultSettings.Link(BrowserSettingsStore.AppBucket), Is.EqualTo("mindattic-vault://open?app=MindAttic.Browser"));
    }
}
