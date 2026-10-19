using System.Text.Json.Nodes;

namespace MindAttic.Browser.Operator;

/// <summary>Every <see cref="IWebNavTool"/> the chat agent can call, looked up by name. Modelled on
/// Prose.Core's <c>KdpToolRegistry</c>.</summary>
public class WebNavToolRegistry
{
    private readonly Dictionary<string, IWebNavTool> byName;

    public WebNavToolRegistry(IEnumerable<IWebNavTool> tools) => byName = tools.ToDictionary(t => t.Name, StringComparer.Ordinal);

    public IWebNavTool? Get(string name) => byName.TryGetValue(name, out var t) ? t : null;

    public IReadOnlyList<ToolDefinition> BuildToolDefinitions() =>
        byName.Values.Select(t => new ToolDefinition(
            t.Name,
            t.Description,
            JsonNode.Parse(t.ParametersJsonSchema) ?? throw new InvalidOperationException($"Tool {t.Name}: ParametersJsonSchema is not valid JSON")))
        .ToList();
}
