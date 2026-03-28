using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TigerClaw.Core;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Models;

namespace TigerClaw.Runtime;

/// <summary>
/// When <see cref="ModelRoutingOptions.UseLlmIntentRouting"/> is true and <see cref="ModelRoutingOptions.ApiKey"/> is set,
/// asks the configured LLM to classify intent and extract slots (URL, keyword, etc.), then maps to <see cref="RoutingResult"/>.
/// On failure or when disabled, delegates to <see cref="RuleBasedIntentRouter"/>.
/// </summary>
public sealed class LlmFirstIntentRouter : IIntentRouter
{
    private const string ExamplesBlock = """
        {"intent":"taobao_search","keyword":"电冰箱"}
        {"intent":"open_url","url":"https://www.taobao.com"}
        {"intent":"open_url","url":"https://www.example.com"}
        """;

    private readonly RuleBasedIntentRouter _rules;
    private readonly IModelAdapter _model;
    private readonly IOptions<TigerClawOptions> _options;
    private readonly ILogger<LlmFirstIntentRouter> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LlmFirstIntentRouter(
        RuleBasedIntentRouter rules,
        IModelAdapter model,
        IOptions<TigerClawOptions> options,
        ILogger<LlmFirstIntentRouter> logger)
    {
        _rules = rules;
        _model = model;
        _options = options;
        _logger = logger;
    }

    public async Task<RoutingResult> RouteAsync(TaskRequest request, CancellationToken cancellationToken = default)
    {
        var mr = _options.Value.ModelRouting;
        if (!mr.UseLlmIntentRouting || string.IsNullOrWhiteSpace(mr.ApiKey))
        {
            if (mr.UseLlmIntentRouting && string.IsNullOrWhiteSpace(mr.ApiKey))
                _logger.LogInformation("UseLlmIntentRouting is enabled but ModelRouting.ApiKey is empty; using rule-based router");
            return await _rules.RouteAsync(request, cancellationToken);
        }

        var text = request.InputText.Trim();
        if (string.IsNullOrEmpty(text))
            return await _rules.RouteAsync(request, cancellationToken);

        try
        {
            var prompt = BuildPrompt(text);
            var response = await _model.CompleteAsync(new ModelRequest
            {
                TaskType = "intent_route",
                Prompt = prompt,
                MaxTokens = 400,
                Temperature = 0
            }, cancellationToken);

            var content = response.Content?.Trim();
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("LLM intent routing returned empty content; using rules");
                return await _rules.RouteAsync(request, cancellationToken);
            }

            var json = ExtractJsonObject(content);
            var dto = JsonSerializer.Deserialize<LlmIntentDto>(json, JsonOpts);
            if (dto?.Intent is null || string.IsNullOrWhiteSpace(dto.Intent))
            {
                _logger.LogWarning("LLM intent JSON missing intent; using rules");
                return await _rules.RouteAsync(request, cancellationToken);
            }

            var mapped = MapToRoutingResult(dto);
            _logger.LogDebug("LLM routed intent={Intent}", mapped.Intent);
            return mapped;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM intent routing failed; using rules");
            return await _rules.RouteAsync(request, cancellationToken);
        }
    }

    private static string BuildPrompt(string userMessage)
    {
        return $"""
            You are an intent router for a local automation runtime. Reply with ONE JSON object only, no markdown fences, no explanation.

            User message (may be Chinese or English):
            {userMessage}

            Allowed "intent" values and required JSON shape:
            - "open_url" — user wants to open a website only (no product search on that site). Include "url" as full https URL. Map common names: taobao -> https://www.taobao.com, baidu -> https://www.baidu.com, google -> https://www.google.com, jd/jingdong -> https://www.jd.com.
            - "taobao_search" — user wants to search on Taobao (淘宝), including phrases like "打开淘宝搜索xxx". Include "keyword" as the search query (e.g. 电冰箱).
            - "email_digest" — unread mail / 未读邮件 / digest.
            - "save_preference" — set language or preference; include "key" and "value" if clear.
            - "generic_task" — anything else or unclear.

            Examples (literal JSON lines):
            {ExamplesBlock}

            Output valid JSON only.
            """;
    }

    private static string ExtractJsonObject(string raw)
    {
        var t = raw.Trim();
        if (t.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNl = t.IndexOf('\n');
            if (firstNl >= 0)
                t = t[(firstNl + 1)..];
            var fence = t.LastIndexOf("```", StringComparison.Ordinal);
            if (fence >= 0)
                t = t[..fence];
        }

        t = t.Trim();
        var start = t.IndexOf('{');
        var end = t.LastIndexOf('}');
        if (start >= 0 && end > start)
            return t[start..(end + 1)];
        return t;
    }

    private static RoutingResult MapToRoutingResult(LlmIntentDto dto)
    {
        var intent = dto.Intent!.Trim().ToLowerInvariant();
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        switch (intent)
        {
            case "open_url":
                if (!string.IsNullOrWhiteSpace(dto.Url))
                    parameters["url"] = dto.Url!.Trim();
                else
                    parameters["url"] = "https://www.example.com";
                return new RoutingResult { Intent = "open_url", Parameters = parameters };

            case "taobao_search":
                parameters["keyword"] = string.IsNullOrWhiteSpace(dto.Keyword) ? "空调" : dto.Keyword!.Trim();
                return new RoutingResult { Intent = "taobao_search", WorkflowId = "taobao_search", Parameters = parameters };

            case "email_digest":
                return new RoutingResult { Intent = "email_digest", WorkflowId = "daily_mail_digest" };

            case "save_preference":
                if (!string.IsNullOrWhiteSpace(dto.Key))
                    parameters["key"] = dto.Key!.Trim();
                if (dto.Value != null)
                    parameters["value"] = dto.Value;
                return new RoutingResult { Intent = "save_preference", Parameters = parameters };

            case "generic_task":
            default:
                return new RoutingResult { Intent = "generic_task" };
        }
    }

    private sealed class LlmIntentDto
    {
        [JsonPropertyName("intent")]
        public string? Intent { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("keyword")]
        public string? Keyword { get; set; }

        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}
