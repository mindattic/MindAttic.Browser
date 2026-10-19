using System.Runtime.CompilerServices;
using System.Text.Json;

namespace MindAttic.Browser.Operator;

/// <summary>
/// Drives the browser pane from natural-language chat: the user types an instruction, Claude
/// calls tools (search, navigate, read_page, click, forms, upload_file…) against the live page
/// until there's nothing left to do, then replies in plain language. This is "Chrome inside
/// Claude" rather than "Claude inside Chrome" — the person drives the conversation, Claude drives
/// the page. Modelled on Prose.Core's <c>KdpOperatorService</c>, generalized from one hard-coded
/// KDP flow to open-ended browsing, and — unlike that one-shot-per-book service — keeps the
/// conversation alive across calls, so a follow-up like "now click the second one" has context.
/// </summary>
public class WebNavOperatorService
{
    private readonly IToolCallingLlm llm;
    private readonly WebNavToolRegistry tools;
    private readonly List<ToolLoopMessage> history = [];

    private const int MaxTokens = 4096;
    // Open-ended research ("find me X") legitimately takes many search/open/read rounds.
    private const int MaxToolIterations = 80;

    private const string SystemPrompt = """
        You are driving a live web browser pane on the person's screen, on their behalf, from
        their typed instructions. You have tools, not raw DOM access: search, navigate, read_page,
        click, set_field, press_enter, check_checkbox, choose_option, select_dropdown, upload_file,
        wait. Call read_page whenever you're unsure what's actually on the page before acting —
        don't guess at what's there, and don't assume an action worked without checking.

        Open-ended requests ("find me X") are yours to run end to end: search, read the results,
        open the promising ones, refine the query (filetype:pdf, site:, quoted titles), and try
        other sources when one dead-ends — don't stop after the first page or hand the search back
        to the person. Stop when you've found it, or when you've genuinely exhausted reasonable
        places, and say where you looked. A PDF opens in the pane's own viewer when you navigate
        to it; say so and give its URL. The pane is a Chromium browser: ftp:// links don't open in
        it, so report an FTP location as a link rather than trying to browse it.

        Never help obtain pirated copies of copyrighted material — books, papers, music, film,
        software. Free copies are fine when they are legitimately free: public domain (e.g.
        Project Gutenberg, Internet Archive's public-domain collections), open access, the
        author's or publisher's own free release, or a library loan. For a work still under
        copyright with no legitimate free copy, say that plainly and point to legitimate ways to
        get it (official stores, the publisher, a library or Open Library loan) instead of
        visiting or linking to shadow libraries, torrent indexes or "free PDF" mirror sites.

        Ask before anything irreversible or costly on the person's behalf — buying, sending,
        posting, deleting, submitting a form that commits them to something.

        When you're done, or if you get stuck, reply with a short plain-language summary of what
        happened (not another tool call) — that's what ends your turn and lets the person reply.
        """;

    public WebNavOperatorService(IToolCallingLlm llm, WebNavToolRegistry tools)
    {
        this.llm = llm;
        this.tools = tools;
    }

    public void ResetConversation() => history.Clear();

    public async IAsyncEnumerable<OperatorEvent> RunAsync(
        string instruction, WebNavOperatorContext ctx, [EnumeratorCancellation] CancellationToken ct = default)
    {
        history.Add(new ToolLoopMessage.UserText(instruction));
        var toolDefs = tools.BuildToolDefinitions();

        for (var iter = 0; iter < MaxToolIterations; iter++)
        {
            ct.ThrowIfCancellationRequested();

            ToolTurnResult? turn = null;
            string? callError = null;
            try
            {
                turn = await llm.CreateTurnAsync(SystemPrompt, history, toolDefs, MaxTokens, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                callError = ex.Message;
            }
            if (turn is null)
            {
                yield return new OperatorEvent.Error(callError ?? $"{llm.Name} call returned null.");
                yield break;
            }

            history.Add(new ToolLoopMessage.AssistantTurn(turn.Parts));

            var toolResults = new List<ToolResultPart>();
            var sawToolUse = false;

            foreach (var part in turn.Parts)
            {
                if (part is AssistantPart.Text text)
                {
                    if (!string.IsNullOrEmpty(text.Value)) yield return new OperatorEvent.AssistantText(text.Value);
                }
                else if (part is AssistantPart.ToolCall call)
                {
                    sawToolUse = true;
                    yield return new OperatorEvent.ToolStarted(call.Name, call.ArgumentsJson);

                    string resultJson;
                    bool isError = false;
                    try
                    {
                        var tool = tools.Get(call.Name) ?? throw new InvalidOperationException($"Unknown tool: {call.Name}");
                        using var argsDoc = JsonDocument.Parse(call.ArgumentsJson);
                        resultJson = await tool.InvokeAsync(argsDoc.RootElement, ctx, ct);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        resultJson = JsonSerializer.Serialize(new { error = ex.Message });
                        isError = true;
                    }

                    yield return new OperatorEvent.ToolCompleted(call.Name, resultJson, isError);
                    toolResults.Add(new ToolResultPart(call.Id, resultJson, isError));
                }
            }

            if (!sawToolUse) yield break;
            history.Add(new ToolLoopMessage.ToolResults(toolResults));
        }

        yield return new OperatorEvent.Error(
            $"Stopped after {MaxToolIterations} tool calls without finishing — tell me to continue if there's more to do.");
    }
}
