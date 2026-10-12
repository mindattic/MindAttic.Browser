using System.IO;
using MindAttic.Vault.Settings;
using NUnit.Framework;

namespace MindAttic.Browser.Tests;

public class BrowserTargetTests
{
    private static BrowserTarget Parse(params string[] args) =>
        BrowserTarget.Parse(args, d => d == @"D:\Web\mindattic.com", f => f is @"D:\Web\mindattic.com\index.htm" or @"D:\Web\page.html");

    [Test]
    public void Folder_is_served_from_a_virtual_host()
    {
        var t = Parse(@"D:\Web\mindattic.com", "-capture", "video", "screen", "5");
        Assert.That(t, Is.EqualTo(new BrowserTarget("https://mindattic.com.local/index.htm", @"D:\Web\mindattic.com", "mindattic.com.local", "mindattic.com")));
    }

    [Test]
    public void Capture_words_are_not_mistaken_for_the_target()
    {
        var t = Parse("-capture", "screen", "5", "https://example.com/x");
        Assert.That((t.Url, t.AppName), Is.EqualTo(("https://example.com/x", "example.com")));
    }

    [Test]
    public void Html_file_maps_its_folder()
    {
        var t = Parse(@"D:\Web\page.html", "--host", "demo.local");
        Assert.That((t.Url, t.SiteFolder, t.VirtualHost), Is.EqualTo(("https://demo.local/page.html", @"D:\Web", "demo.local")));
    }

    [Test]
    public void Site_folder_names_the_captures_and_generic_folders_fall_back_to_the_repo()
    {
        Assert.That(BrowserTarget.SubjectName(@"D:\Projects\MindAttic\MindAttic.Web\mindattic.com", "mindattic.com"), Is.EqualTo("mindattic.com"));
        var repo = Path.Combine(Path.GetTempPath(), "mab-repo-" + Guid.NewGuid().ToString("N"));
        var dist = Path.Combine(repo, "MySite", "dist");
        Directory.CreateDirectory(Path.Combine(repo, ".git"));
        Directory.CreateDirectory(dist);
        try
        {
            Assert.That(BrowserTarget.SubjectName(dist, "dist"), Is.EqualTo(Path.GetFileName(repo)));
        }
        finally
        {
            Directory.Delete(repo, recursive: true);
        }
    }

    [Test]
    public void No_target_opens_the_home_page_from_settings()
    {
        Assert.That(Parse().Url, Is.EqualTo(BrowserTarget.Home));
        var t = BrowserTarget.Parse([], _ => false, _ => false, home: "example.org");
        Assert.That((t.Url, t.AppName), Is.EqualTo(("https://example.org", "example.org")));
    }

    [TestCase("mindattic.com", "https://mindattic.com")]
    [TestCase("localhost:5173", "http://localhost:5173")]
    [TestCase("https://a.b/c?d", "https://a.b/c?d")]
    [TestCase("parasite eve", "https://www.bing.com/search?q=parasite%20eve")]
    public void Address_bar_text_becomes_a_url(string text, string url) => Assert.That(BrowserTarget.NormalizeUrl(text), Is.EqualTo(url));

    [Test]
    public void Search_uses_the_configured_engine_and_falls_back_without_a_query_slot()
    {
        Assert.That(BrowserTarget.NormalizeUrl("red fox", "https://duckduckgo.com/?q={query}"), Is.EqualTo("https://duckduckgo.com/?q=red%20fox"));
        Assert.That(BrowserTarget.NormalizeUrl("red fox", "https://broken.example/"), Is.EqualTo("https://www.bing.com/search?q=red%20fox"));
    }
}

public class FavoritesTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void Star_and_unstar_keep_the_entry_soft_disabled()
    {
        var s = new BrowserSettings();
        s.Star("https://a.com/", "A", T0);
        Assert.That(s.IsFavorite("https://a.com"), Is.True, "trailing slash is the same page");
        s.Unstar("https://a.com", T0.AddMinutes(1));
        Assert.That(s.IsFavorite("https://a.com/"), Is.False);
        Assert.That(s.Favorites, Has.Count.EqualTo(1), "unstarring never deletes (HOUSE-LAW-2)");
        Assert.That(s.Favorites[0].RemovedAt, Is.EqualTo(T0.AddMinutes(1)));

        s.Star("https://a.com/", "A again", T0.AddMinutes(2));
        Assert.That((s.Favorites.Count, s.IsFavorite("https://a.com/"), s.Favorites[0].Title), Is.EqualTo((1, true, "A again")));
    }

    [Test]
    public void Active_favorites_are_newest_first_and_untitled_pages_use_the_url()
    {
        var s = new BrowserSettings();
        s.Star("https://old.example/", "Old", T0);
        s.Star("https://new.example/", "  ", T0.AddHours(1));
        s.Star("https://gone.example/", "Gone", T0.AddHours(2));
        s.Unstar("https://gone.example/", T0.AddHours(3));
        Assert.That(s.ActiveFavorites().Select(f => f.Title), Is.EqualTo(new[] { "https://new.example/", "Old" }));
    }

    [Test]
    public void Settings_round_trip_through_the_vault_store_keeping_unknown_keys()
    {
        var dir = Directory.CreateTempSubdirectory("mab-settings-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(dir, "settings.json"), """{ "homePage": "https://x.example/", "futureKey": 42 }""");
            var store = new BrowserSettingsStore(new JsonSettingsStore<BrowserSettings>(dir));
            var s = store.Load();
            Assert.That((s.HomePage, s.CopyToClipboard, s.CropScreenshots), Is.EqualTo(("https://x.example/", true, true)));
            s.Star("https://a.com/", "A", T0);
            store.Save(s);
            var text = File.ReadAllText(store.FilePath);
            Assert.That(text, Does.Contain("futureKey").And.Contain("https://a.com/"));
            Assert.That(store.Load().IsFavorite("https://a.com/"), Is.True);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public void A_capture_folder_on_a_missing_drive_falls_back_to_pictures()
    {
        var missing = Enumerable.Range('D', 23).Select(c => $"{(char)c}:\\").First(d => !Directory.Exists(d));
        var s = new BrowserSettings { CaptureFolder = Path.Combine(missing, "Capture") };
        Assert.That(s.ResolvedCaptureFolder(), Does.EndWith("MindAttic Browser"));
        var here = new BrowserSettings { CaptureFolder = Path.GetTempPath() };
        Assert.That(here.ResolvedCaptureFolder(), Is.EqualTo(Path.GetTempPath()));
    }
}

public class CaptureFilesTests
{
    private static readonly BrowserTarget Local = new("https://mindattic.com.local/index.htm", @"D:\Web\mindattic.com", "mindattic.com.local", "mindattic.com");

    [TestCase("https://www.youtube.com/watch?v=1", "youtube.com")]
    [TestCase("https://mindattic.com.local/about.htm", "mindattic.com")]
    [TestCase("about:blank", "mindattic.com")]
    [TestCase(null, "mindattic.com")]
    public void Screenshots_are_named_after_the_page_shown(string? url, string subject) =>
        Assert.That(CaptureFiles.Subject(url, Local), Is.EqualTo(subject));

    [Test]
    public void Screenshot_names_never_overwrite()
    {
        var now = new DateTimeOffset(2026, 10, 11, 14, 30, 5, 123, TimeSpan.Zero);
        var first = CaptureFiles.NextScreenshot(@"R:\Cap", "youtube.com", now, _ => false);
        Assert.That(first, Is.EqualTo(@"R:\Cap\youtube.com\youtube.com_2026-10-11T14.30.05.123.png"));
        var taken = new HashSet<string> { first };
        Assert.That(CaptureFiles.NextScreenshot(@"R:\Cap", "youtube.com", now, taken.Contains), Does.EndWith("_2.png"));
    }

    [TestCase("a:b*c", "a_b_c")]
    [TestCase("  ", "page")]
    public void Unsafe_names_are_cleaned(string name, string expected) => Assert.That(CaptureFiles.Safe(name), Is.EqualTo(expected));
}

public class CropMathTests
{
    [Test]
    public void Fit_never_enlarges()
    {
        Assert.That(CropMath.FitScale(1000, 500, 2000, 2000), Is.EqualTo(1));
        Assert.That(CropMath.FitScale(2000, 1000, 1000, 1000), Is.EqualTo(0.5));
    }

    [Test]
    public void Drag_in_any_direction_makes_a_clamped_box() =>
        Assert.That(CropMath.FromPoints(300, 250, -20, 10, 200, 200), Is.EqualTo(new PixelBox(0, 10, 200, 190)));

    [Test]
    public void Move_stays_inside_the_image() =>
        Assert.That(CropMath.Move(new PixelBox(10, 10, 50, 50), 500, -500, 200, 100), Is.EqualTo(new PixelBox(150, 0, 50, 50)));

    [Test]
    public void Resize_moves_only_the_dragged_edges_and_keeps_a_minimum()
    {
        var box = new PixelBox(20, 20, 100, 100);
        Assert.That(CropMath.Resize(box, "lt", -10, 5, 300, 300), Is.EqualTo(new PixelBox(10, 25, 110, 95)));
        Assert.That(CropMath.Resize(box, "r", -500, 0, 300, 300), Is.EqualTo(new PixelBox(20, 20, CropMath.MinSize, 100)));
        Assert.That(CropMath.Resize(box, "b", 0, 999, 300, 300).Bottom, Is.EqualTo(300));
    }

    [Test]
    public void A_stray_click_keeps_the_whole_image() =>
        Assert.That(CropMath.OrWhole(new PixelBox(5, 5, 2, 2), 640, 480), Is.EqualTo(new PixelBox(0, 0, 640, 480)));
}
