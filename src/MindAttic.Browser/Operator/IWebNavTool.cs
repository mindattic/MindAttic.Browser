using System.Text.Json;
using AutoWebNav;

namespace MindAttic.Browser.Operator;

/// <summary>
/// One callable surface the AutoWebNav chat agent can invoke against the live browser pane.
/// Modelled on Prose.Core's <c>IKdpTool</c> — same registry/loop pattern, generalized from one
/// site's flow to open-ended browsing.
/// </summary>
public interface IWebNavTool
{
    string Name { get; }
    string Description { get; }
    string ParametersJsonSchema { get; }

    Task<string> InvokeAsync(JsonElement args, WebNavOperatorContext context, CancellationToken ct);
}

/// <summary>Per-call context handed to every tool: the live browser surface to act on.</summary>
public sealed class WebNavOperatorContext
{
    public required IBrowserSurface Browser { get; init; }
}
