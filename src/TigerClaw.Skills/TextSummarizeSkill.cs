using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TigerClaw.Infrastructure.ModelAdapters;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Models;

namespace TigerClaw.Skills;

/// <summary>
/// Summarizes text using fake summarizer or model adapter.
/// </summary>
public class TextSummarizeSkill : Core.ISkill
{
    public string Id => "text.summarize";
    private readonly Core.IModelAdapter _model;
    private readonly ModelRoutingOptions _options;
    private readonly ILogger<TextSummarizeSkill> _logger;

    public TextSummarizeSkill(Core.IModelAdapter model, IOptions<TigerClawOptions> options, ILogger<TextSummarizeSkill> logger)
    {
        _model = model;
        _options = options.Value.ModelRouting;
        _logger = logger;
    }

    public async Task<SkillExecutionResult> ExecuteAsync(IReadOnlyDictionary<string, object?> inputs, TaskExecutionContext context, CancellationToken cancellationToken = default)
    {
        var text = inputs.TryGetValue("text", out var t) ? t?.ToString() : null;
        if (string.IsNullOrWhiteSpace(text))
            return new SkillExecutionResult { Success = false, Message = "Missing required input: text" };

        if (_options.UseFakeSummarizer)
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var summary = string.Join("\n", lines.Take(Math.Min(5, lines.Length))) + (lines.Length > 5 ? "\n..." : "");
            _logger.LogDebug("Fake summarize completed taskId={TaskId}", context.TaskId);
            return new SkillExecutionResult { Success = true, Output = summary };
        }

        var response = await _model.CompleteAsync(new ModelRequest
        {
            TaskType = "summarize_short",
            Prompt = $"Please summarize the following text concisely:\n\n{text}",
            MaxTokens = 500,
            Temperature = 0.3
        }, cancellationToken);

        return new SkillExecutionResult { Success = true, Output = response.Content };
    }
}
