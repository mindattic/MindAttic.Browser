using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace MindAttic.Browser.Operator;

/// <summary>
/// Minimal Anthropic Messages API client with tool-use support. Ported from Prose.Core/
/// Automata.Core's copy (trimmed to the non-streaming call — a tool-use turn here is dominated
/// by browser round-trips anyway, so there's nothing to gain showing partial text as it streams).
/// </summary>
public class AnthropicToolClient
{
    private readonly HttpClient http;
    private readonly ILogger<AnthropicToolClient> log;
    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    // Rate limits (429) and transient capacity errors (529 overloaded) are retried with backoff
    // rather than failing the whole turn immediately.
    private const int MaxRetries = 5;
    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(60);

    public AnthropicToolClient(HttpClient http, ILogger<AnthropicToolClient> log)
    {
        this.http = http;
        this.log = log;
    }

    public async Task<AnthropicTurnResponse> CreateAsync(
        string apiKey,
        string model,
        string systemPrompt,
        JsonArray messages,
        JsonArray tools,
        int maxTokens,
        CancellationToken ct)
    {
        var body = new JsonObject
        {
            ["model"] = model,
            ["max_tokens"] = maxTokens,
            ["system"] = systemPrompt,
            ["messages"] = messages.DeepClone(),
        };
        if (tools.Count > 0) body["tools"] = tools.DeepClone();

        for (int attempt = 0; ; attempt++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = JsonContent.Create(body) };
            // OAuth access tokens (prefix "sk-ant-oat") authenticate via Authorization: Bearer;
            // raw pay-per-token API keys use x-api-key.
            if (apiKey.StartsWith("sk-ant-oat", StringComparison.OrdinalIgnoreCase))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            else
                req.Headers.Add("x-api-key", apiKey);
            req.Headers.Add("anthropic-version", AnthropicVersion);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                var retryable = resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests || (int)resp.StatusCode == 529;
                if (retryable && attempt < MaxRetries)
                {
                    var delay = ResolveRetryDelay(resp, attempt);
                    log.LogWarning("Anthropic {Status} (attempt {Attempt}/{Max}) — retrying in {Delay}s",
                        (int)resp.StatusCode, attempt + 1, MaxRetries, delay.TotalSeconds);
                    await Task.Delay(delay, ct);
                    continue;
                }
                log.LogWarning("Anthropic {Status}: {Body}", (int)resp.StatusCode, raw);
                throw new HttpRequestException($"Anthropic API {(int)resp.StatusCode}: {Truncate(raw, 500)}", inner: null, statusCode: resp.StatusCode);
            }

            var doc = JsonNode.Parse(raw) ?? throw new InvalidOperationException("Anthropic response was null JSON");
            var content = doc["content"] as JsonArray ?? [];
            var stopReason = doc["stop_reason"]?.GetValue<string>() ?? "";
            var usage = doc["usage"] is { } u
                ? new TokenUsage(u["input_tokens"]?.GetValue<int>() ?? 0, u["output_tokens"]?.GetValue<int>() ?? 0)
                : null;
            return new AnthropicTurnResponse(content, stopReason, usage);
        }
    }

    private static TimeSpan ResolveRetryDelay(HttpResponseMessage resp, int attempt)
    {
        if (resp.Headers.RetryAfter?.Delta is { } delta) return delta;
        var backoff = TimeSpan.FromSeconds(BaseDelay.TotalSeconds * Math.Pow(2, attempt));
        return backoff > MaxDelay ? MaxDelay : backoff;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}

/// <summary>One Anthropic Messages API turn. <see cref="Content"/> is the raw content-blocks array.</summary>
public sealed record AnthropicTurnResponse(JsonArray Content, string StopReason, TokenUsage? Usage = null);
