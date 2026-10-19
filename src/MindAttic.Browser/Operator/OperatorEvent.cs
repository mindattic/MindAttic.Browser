namespace MindAttic.Browser.Operator;

/// <summary>Streaming event contract for an Anthropic tool-use loop, ported verbatim from
/// Prose.Core/Automata.Core's copy of the same shape.</summary>
public abstract record OperatorEvent
{
    public sealed record AssistantText(string Text) : OperatorEvent;
    public sealed record ToolStarted(string Name, string ArgsJson) : OperatorEvent;
    public sealed record ToolCompleted(string Name, string ResultJson, bool IsError) : OperatorEvent;
    public sealed record Error(string Message) : OperatorEvent;
}
