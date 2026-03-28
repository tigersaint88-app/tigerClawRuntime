using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Models;

namespace TigerClaw.Infrastructure.ModelAdapters;

/// <summary>
/// OpenAI-compatible API adapter for remote LLM.
/// </summary>
public class RemoteOpenAiCompatibleModelAdapter : Core.IModelAdapter
{
    private readonly HttpClient _http;
    private readonly ModelRoutingOptions _options;
    private readonly ILogger<RemoteOpenAiCompatibleModelAdapter> _logger;

    public RemoteOpenAiCompatibleModelAdapter(HttpClient http, IOptions<TigerClawOptions> options, ILogger<RemoteOpenAiCompatibleModelAdapter> logger)
    {
        _http = http;
        _options = options.Value.ModelRouting;
        _logger = logger;
        if (_http.BaseAddress == null)
            _http.BaseAddress = new Uri(_options.RemoteApiBaseUrl.TrimEnd('/') + "/");
        if (!string.IsNullOrEmpty(_options.ApiKey) && !_http.DefaultRequestHeaders.Contains("Authorization"))
            _http.DefaultRequestHeaders.Add("Authorization", "Bearer " + _options.ApiKey);
    }

    public async Task<ModelResponse> CompleteAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        var messages = (request.Messages?.Count > 0
            ? request.Messages.Select(m => new { role = m.Role, content = m.Content })
            : new[] { new { role = "user", content = request.Prompt } }).ToList();

        var body = new
        {
            model = _options.RemoteModelName,
            messages,
            max_tokens = request.MaxTokens,
            temperature = request.Temperature
        };

        try
        {
            var resp = await _http.PostAsJsonAsync("chat/completions", body, cancellationToken: cancellationToken);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            var content = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
            var pt = 0;
            var ct = 0;
            if (json.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var ptok)) pt = ptok.GetInt32();
                if (usage.TryGetProperty("completion_tokens", out var ctok)) ct = ctok.GetInt32();
            }
            return new ModelResponse
            {
                Content = content,
                PromptTokens = pt,
                CompletionTokens = ct,
                Model = _options.RemoteModelName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remote model call failed");
            return new ModelResponse
            {
                Content = $"[Error] {ex.Message}",
                PromptTokens = 0,
                CompletionTokens = 0,
                Model = _options.RemoteModelName
            };
        }
    }
}
