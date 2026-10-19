using System.Text.Json.Nodes;

namespace MindAttic.Browser.Operator;

/// <summary>
/// Provider-neutral agentic tool-calling contract, ported from Prose.Core/Automata.Core's copy of
/// the same shape (the established pattern in this workspace: this plumbing is small enough that
/// every app carries its own copy rather than a shared package). <see cref="WebNavOperatorService"/>
/// speaks only this shape — plain text turns, tool calls, tool results — so the loop mechanics
/// never change if a vendor other than Anthropic is added later.
/// </summary>
public interface IToolCallingLlm
{
    /// <summary>Display name for logging/diagnostics (e.g. "Claude").</summary>
    string Name { get; }

    /// <summary>The resolved model id this instance calls.</summary>
    string Model { get; }

    /// <summary>True if this provider has usable credentials right now.</summary>
    Task<bool> IsConfiguredAsync();

    /// <summary>
    /// One turn: given the system prompt, the full conversation so far, and the tool catalog, get
    /// back everything the assistant wants to do this turn — any text it said, plus any tool calls
    /// it wants made. The caller invokes the tools and appends a <see cref="ToolLoopMessage.ToolResults"/>
    /// message before calling this again.
    /// </summary>
    Task<ToolTurnResult> CreateTurnAsync(
        string systemPrompt,
        IReadOnlyList<ToolLoopMessage> history,
        IReadOnlyList<ToolDefinition> tools,
        int maxTokens,
        CancellationToken ct);
}

/// <summary>One callable tool. <paramref name="InputSchema"/> is a standard JSON Schema object.</summary>
public sealed record ToolDefinition(string Name, string Description, JsonNode InputSchema);

/// <summary>One entry in the conversation history, in whichever order they occurred.</summary>
public abstract record ToolLoopMessage
{
    /// <summary>The initial (or any subsequent) plain-text instruction from the caller.</summary>
    public sealed record UserText(string Text) : ToolLoopMessage;

    /// <summary>Everything the assistant did in one turn — text said and/or tools called.</summary>
    public sealed record AssistantTurn(IReadOnlyList<AssistantPart> Parts) : ToolLoopMessage;

    /// <summary>The results of every tool call from the immediately preceding assistant turn.</summary>
    public sealed record ToolResults(IReadOnlyList<ToolResultPart> Results) : ToolLoopMessage;
}

/// <summary>One piece of an assistant turn.</summary>
public abstract record AssistantPart
{
    public sealed record Text(string Value) : AssistantPart;

    /// <summary><paramref name="ArgumentsJson"/> is the tool's arguments as a JSON object string.</summary>
    public sealed record ToolCall(string Id, string Name, string ArgumentsJson) : AssistantPart;
}

/// <summary>One tool's result, keyed back to the <see cref="AssistantPart.ToolCall"/> that requested it.</summary>
public sealed record ToolResultPart(string ToolCallId, string Content, bool IsError);

/// <summary>What a turn actually consumed, as the provider reported it.</summary>
public sealed record TokenUsage(int InputTokens, int OutputTokens);

/// <summary>Everything the assistant did in the turn just completed.</summary>
public sealed record ToolTurnResult(IReadOnlyList<AssistantPart> Parts, TokenUsage? Usage = null);
