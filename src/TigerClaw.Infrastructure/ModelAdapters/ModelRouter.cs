using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Models;

namespace TigerClaw.Infrastructure.ModelAdapters;

/// <summary>
/// Routes model requests to local or remote adapter based on task type.
/// </summary>
public class ModelRouter : Core.IModelAdapter
{
    private readonly Core.IModelAdapter _local;
    private readonly Core.IModelAdapter _remote;
    private readonly ModelRoutingOptions _options;
    private readonly ILogger<ModelRouter> _logger;

    public ModelRouter(LocalModelAdapter local, RemoteOpenAiCompatibleModelAdapter remote, IOptions<TigerClawOptions> options, ILogger<ModelRouter> logger)
    {
        _local = local;
        _remote = remote;
        _options = options.Value.ModelRouting;
        _logger = logger;
    }

    public Task<ModelResponse> CompleteAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        var useLocal = request.TaskType switch
        {
            "classify" or "extract" or "summarize_short" or "sensitive" => true,
            "complex_plan" => false,
            // Intent routing needs a real model when ApiKey is configured
            "intent_route" => string.IsNullOrEmpty(_options.ApiKey),
            _ => true
        };

        if (_options.UseFakeSummarizer && request.TaskType == "summarize_short")
        {
            _logger.LogDebug("Using fake summarizer for {TaskType}", request.TaskType);
            return Task.FromResult(new ModelResponse
            {
                Content = FakeSummarize(request.Prompt),
                PromptTokens = 0,
                CompletionTokens = 0,
                Model = "fake"
            });
        }

        return useLocal ? _local.CompleteAsync(request, cancellationToken) : _remote.CompleteAsync(request, cancellationToken);
    }

    private static string FakeSummarize(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var take = Math.Min(5, lines.Length);
        return string.Join("\n", lines.Take(take)) + (lines.Length > take ? "\n..." : "");
    }
}
