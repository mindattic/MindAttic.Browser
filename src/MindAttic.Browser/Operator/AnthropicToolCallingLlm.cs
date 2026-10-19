using System.Text.Json.Nodes;
using MindAttic.Legion;

namespace MindAttic.Browser.Operator;

/// <summary>
/// <see cref="IToolCallingLlm"/> adapter over <see cref="AnthropicToolClient"/>, ported from
/// Prose.Core/Automata.Core's copy. Credentials come from the shared MindAttic keyring under the
/// "claude" id — the same store every other MindAttic app's Claude calls already use, so a key set
/// up once (via MindAttic.Launcher's Settings, or ANTHROPIC_API_KEY) works here with no extra setup.
/// </summary>
public class AnthropicToolCallingLlm : IToolCallingLlm
{
    private readonly AnthropicToolClient client;
    private readonly string model;

    public string Name => "Claude";
    public string Model => model;

    public AnthropicToolCallingLlm(AnthropicToolClient client, string model = "claude-sonnet-5-5")
    {
        this.client = client;
        this.model = model;
    }

    public Task<bool> IsConfiguredAsync() => Task.FromResult(ResolveApiKeys().Count > 0);

    public async Task<ToolTurnResult> CreateTurnAsync(
        string systemPrompt,
        IReadOnlyList<ToolLoopMessage> history,
        IReadOnlyList<ToolDefinition> tools,
        int maxTokens,
        CancellationToken ct)
    {
        var keys = ResolveApiKeys();
        if (keys.Count == 0)
            throw new InvalidOperationException(
                "No Anthropic API key configured — add a 'claude' provider key to the shared MindAttic credential store.");

        var messages = ToAnthropicMessages(history);
        var toolsArray = ToAnthropicTools(tools);

        return await KeyPoolFailover.ExecuteAsync(keys, ct, async key =>
        {
            var turn = await client.CreateAsync(key, model, systemPrompt, messages, toolsArray, maxTokens, ct);
            return new ToolTurnResult(FromAnthropicContent(turn.Content), turn.Usage);
        });
    }

    /// <summary>Every key currently configured under the shared "claude" id, in priority order.</summary>
    private static IReadOnlyList<string> ResolveApiKeys() =>
        MindAtticCredentialStore.GetKeys("claude").Select(k => k.Key).ToList();

    private static JsonArray ToAnthropicTools(IReadOnlyList<ToolDefinition> tools)
    {
        var arr = new JsonArray();
        foreach (var t in tools)
            arr.Add(new JsonObject { ["name"] = t.Name, ["description"] = t.Description, ["input_schema"] = t.InputSchema.DeepClone() });
        return arr;
    }

    private static JsonArray ToAnthropicMessages(IReadOnlyList<ToolLoopMessage> history)
    {
        var messages = new JsonArray();
        foreach (var msg in history)
        {
            switch (msg)
            {
                case ToolLoopMessage.UserText u:
                    messages.Add(new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = new JsonArray { new JsonObject { ["type"] = "text", ["text"] = u.Text } },
                    });
                    break;

                case ToolLoopMessage.AssistantTurn a:
                    var assistantContent = new JsonArray();
                    foreach (var part in a.Parts)
                    {
                        assistantContent.Add(part switch
                        {
                            AssistantPart.Text t => new JsonObject { ["type"] = "text", ["text"] = t.Value },
                            AssistantPart.ToolCall c => new JsonObject
                            {
                                ["type"] = "tool_use",
                                ["id"] = c.Id,
                                ["name"] = c.Name,
                                ["input"] = JsonNode.Parse(string.IsNullOrWhiteSpace(c.ArgumentsJson) ? "{}" : c.ArgumentsJson),
                            },
                            _ => throw new InvalidOperationException($"Unknown AssistantPart type: {part.GetType()}"),
                        });
                    }
                    messages.Add(new JsonObject { ["role"] = "assistant", ["content"] = assistantContent });
                    break;

                case ToolLoopMessage.ToolResults r:
                    var resultsContent = new JsonArray();
                    foreach (var res in r.Results)
                    {
                        resultsContent.Add(new JsonObject
                        {
                            ["type"] = "tool_result",
                            ["tool_use_id"] = res.ToolCallId,
                            ["content"] = res.Content,
                            ["is_error"] = res.IsError,
                        });
                    }
                    messages.Add(new JsonObject { ["role"] = "user", ["content"] = resultsContent });
                    break;

                default:
                    throw new InvalidOperationException($"Unknown ToolLoopMessage type: {msg.GetType()}");
            }
        }
        return messages;
    }

    private static List<AssistantPart> FromAnthropicContent(JsonArray content)
    {
        var parts = new List<AssistantPart>();
        foreach (var block in content)
        {
            if (block is null) continue;
            var type = block["type"]?.GetValue<string>();
            if (type == "text")
            {
                var text = block["text"]?.GetValue<string>() ?? "";
                if (!string.IsNullOrEmpty(text)) parts.Add(new AssistantPart.Text(text));
            }
            else if (type == "tool_use")
            {
                var id = block["id"]?.GetValue<string>() ?? "";
                var name = block["name"]?.GetValue<string>() ?? "";
                var input = block["input"];
                parts.Add(new AssistantPart.ToolCall(id, name, input?.ToJsonString() ?? "{}"));
            }
        }
        return parts;
    }
}
