namespace TigerClaw.Infrastructure.Options;

/// <summary>
/// Model routing configuration.
/// </summary>
public class ModelRoutingOptions
{
    /// <summary>
    /// When true and <see cref="ApiKey"/> is set, natural-language tasks use the remote LLM to classify intent
    /// (e.g. map "打开淘宝搜索电冰箱" to <c>taobao_search</c>) before workflows run. Falls back to rules on failure.
    /// </summary>
    public bool UseLlmIntentRouting { get; set; }

    public bool UseFakeSummarizer { get; set; } = true;
    public string RemoteApiBaseUrl { get; set; } = "https://api.openai.com/v1";
    public string RemoteModelName { get; set; } = "gpt-4";
    public string? ApiKey { get; set; }
}
