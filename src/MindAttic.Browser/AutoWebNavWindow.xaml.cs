using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using AutoWebNav;
using AutoWebNav.WebView2;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Web.WebView2.Core;
using MindAttic.Browser.Operator;
using MindAttic.Browser.Operator.Tools;

namespace MindAttic.Browser;

/// <summary>
/// "Chrome inside Claude": a chat panel next to the live browser pane. The person types what they
/// want done; <see cref="WebNavOperatorService"/> calls tools (navigate/read_page/click/set_field/
/// press_enter/wait) against MainWindow's own WebView2 — the SAME pane the person is looking at,
/// not a hidden copy — until it reports back in plain language. One instance per MainWindow,
/// reused across sends so the conversation (and the page's state) carries forward.
/// </summary>
public partial class AutoWebNavWindow : Window
{
    private static readonly HttpClient Http = new();

    private readonly Func<CoreWebView2?> getCore;
    private WebNavOperatorService? service;
    private CancellationTokenSource? runCts;

    public AutoWebNavWindow(Func<CoreWebView2?> getCore, Window owner)
    {
        this.getCore = getCore;
        InitializeComponent();
        Owner = owner;
        Left = owner.Left + owner.Width - Width - 20;
        Top = owner.Top + 80;
    }

    private WebNavOperatorService Service => service ??= new WebNavOperatorService(
        new AnthropicToolCallingLlm(new AnthropicToolClient(Http, NullLogger<AnthropicToolClient>.Instance)),
        new WebNavToolRegistry(
        [
            new SearchTool(App.Settings.SearchUrl), new NavigateTool(), new ReadPageTool(), new ClickTool(),
            new SetFieldTool(), new PressEnterTool(), new CheckCheckboxTool(), new ChooseOptionTool(),
            new SelectDropdownTool(), new UploadFileTool(), new WaitTool(),
        ]));

    private void OnInstructionKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
        {
            e.Handled = true;
            OnSend(sender, new RoutedEventArgs());
        }
    }

    private async void OnSend(object sender, RoutedEventArgs e)
    {
        var instruction = Instruction.Text.Trim();
        if (instruction.Length == 0 || runCts is not null) return;

        if (getCore() is not { } core)
        {
            AppendLine("⚠ The browser pane isn't ready yet — wait for the page to finish loading and try again.");
            return;
        }

        Instruction.Clear();
        AppendLine($"You: {instruction}");

        runCts = new CancellationTokenSource();
        SendButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        try
        {
            var surface = new WebView2BrowserSurface(core, setZoom: null, installToolkit: false);
            var ctx = new WebNavOperatorContext { Browser = surface };
            await foreach (var evt in Service.RunAsync(instruction, ctx, runCts.Token))
            {
                AppendLine(Format(evt));
            }
        }
        catch (OperationCanceledException)
        {
            AppendLine("⏹ Stopped.");
        }
        catch (Exception ex)
        {
            AppendLine($"⚠ {ex.Message}");
        }
        finally
        {
            runCts?.Dispose();
            runCts = null;
            SendButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }
    }

    private void OnStop(object sender, RoutedEventArgs e) => runCts?.Cancel();

    private static string Format(OperatorEvent evt) => evt switch
    {
        OperatorEvent.AssistantText t => $"Claude: {t.Text}",
        OperatorEvent.ToolStarted s => $"  → {s.Name}({Truncate(s.ArgsJson, 120)})",
        OperatorEvent.ToolCompleted c => $"  {(c.IsError ? "✗" : "✓")} {c.Name} → {Truncate(c.ResultJson, 160)}",
        OperatorEvent.Error er => $"⚠ {er.Message}",
        _ => evt.ToString() ?? "",
    };

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";

    private void AppendLine(string line)
    {
        Log.AppendText((Log.Text.Length == 0 ? "" : "\n") + line);
        Log.ScrollToEnd();
    }
}
