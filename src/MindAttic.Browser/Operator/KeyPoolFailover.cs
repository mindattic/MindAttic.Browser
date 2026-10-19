using System.Net.Http;

namespace MindAttic.Browser.Operator;

/// <summary>
/// Shared "sticky failover" loop for an <see cref="IToolCallingLlm"/> adapter backed by more than
/// one API key: the first key is used until it fails with an auth/rate-limit/server/network
/// error, then the next is tried. Ported verbatim from Prose.Core/Automata.Core's copy.
/// </summary>
public static class KeyPoolFailover
{
    public static async Task<T> ExecuteAsync<T>(
        IReadOnlyList<string> keys, CancellationToken ct, Func<string, Task<T>> callWithKey)
    {
        if (keys.Count == 0)
            throw new InvalidOperationException("No API key configured.");

        for (var i = 0; i < keys.Count; i++)
        {
            try
            {
                return await callWithKey(keys[i]);
            }
            catch (Exception ex) when (i < keys.Count - 1 && !ct.IsCancellationRequested && IsKeyLevelFailure(ex))
            {
                // More keys remain and this one looks bad — fall through to try the next.
            }
        }
        throw new InvalidOperationException("Key pool exhausted with no successful call.");
    }

    public static bool IsKeyLevelFailure(Exception ex) => ex switch
    {
        HttpRequestException hre => hre.StatusCode is null
            || (int)hre.StatusCode is 401 or 408 or 429
            || (int)hre.StatusCode >= 500,
        TaskCanceledException => true,
        _ => false,
    };
}
