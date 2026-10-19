using System.Windows;
using System.Windows.Input;

namespace MindAttic.Browser;

/// <summary>
/// Shows <see cref="BrowserSettings.History"/> and lets Ctrl+H clear it one widening bucket at a
/// time. MainWindow's own Ctrl+H opens/focuses this window; once it has focus, Ctrl+H is handled
/// HERE instead — so "open it, then keep pressing Ctrl+H" falls out of normal WPF focus handling
/// rather than needing any chord-timing logic of its own.
/// </summary>
public partial class HistoryWindow : Window
{
    private readonly BrowserSettings _settings;
    private readonly Action _save;
    private readonly Action<string> _navigate;

    public HistoryWindow(BrowserSettings settings, Action save, Action<string> navigate, Window owner)
    {
        _settings = settings;
        _save = save;
        _navigate = navigate;
        InitializeComponent();
        Owner = owner;
        KeyDown += OnKeyDown;
        Refresh();
    }

    private sealed record HistoryRow(string Title, string Url, string When);

    private void Refresh()
    {
        var now = DateTimeOffset.Now;
        Entries.ItemsSource = _settings.RecentHistory()
            .Select(h => new HistoryRow(h.Title, h.Url, FormatWhen(now - h.VisitedAt)))
            .ToList();
    }

    private static string FormatWhen(TimeSpan ago) => ago switch
    {
        { TotalMinutes: < 1 } => "just now",
        { TotalMinutes: < 60 } m => $"{(int)m.TotalMinutes} min ago",
        { TotalHours: < 24 } h => $"{(int)h.TotalHours} hr ago",
        _ => $"{(int)ago.TotalDays} day(s) ago",
    };

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.H || Keyboard.Modifiers != ModifierKeys.Control) return;
        e.Handled = true;
        FlushNewest();
    }

    private void OnClearNewest(object sender, RoutedEventArgs e) => FlushNewest();

    private void FlushNewest()
    {
        var (count, label) = _settings.FlushNextHistoryBucket(DateTimeOffset.Now);
        _save();
        Refresh();
        FlushStatus.Text = count == 0
            ? "History is already empty."
            : $"Cleared {count} page(s) from {label}.";
    }

    private void OnOpenSelected(object sender, MouseButtonEventArgs e)
    {
        if (Entries.SelectedItem is HistoryRow row) _navigate(row.Url);
    }
}
