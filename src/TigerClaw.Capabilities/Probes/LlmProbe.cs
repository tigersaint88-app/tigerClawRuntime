namespace TigerClaw.Capabilities.Probes;

/// <summary>
/// Lightweight reachability probe for configured LLM HTTP API base URL.
/// </summary>
public static class LlmProbe
{
    public static async Task<(bool Reachable, string Detail)> ProbeAsync(string baseUrl, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var url = (baseUrl ?? "").Trim();
        if (string.IsNullOrEmpty(url))
            return (false, "empty base url");

        if (!Uri.TryCreate(url.TrimEnd('/') + "/", UriKind.Absolute, out var uri))
            return (false, "invalid uri");

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);
            using var client = new HttpClient { Timeout = timeout };
            using var req = new HttpRequestMessage(HttpMethod.Head, uri);
            var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            var code = (int)resp.StatusCode;
            return (true, $"HEAD {uri} -> {code}");
        }
        catch (Exception ex)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);
                using var client = new HttpClient { Timeout = timeout };
                using var req = new HttpRequestMessage(HttpMethod.Get, uri);
                var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                return (true, $"GET {uri} -> {(int)resp.StatusCode}");
            }
            catch (Exception ex2)
            {
                return (false, ex2.Message.Length > 0 ? ex2.Message : ex.Message);
            }
        }
    }
}
