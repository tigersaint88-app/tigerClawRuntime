using Microsoft.Extensions.Logging;
using TigerClaw.Models;

namespace TigerClaw.Skills;

/// <summary>
/// Types text into an element selected by CSS/XPath.
/// </summary>
public class BrowserTypeSkill : Core.ISkill
{
    public string Id => "browser.type";
    private readonly BrowserAutomationSession _session;
    private readonly ILogger<BrowserTypeSkill> _logger;

    public BrowserTypeSkill(BrowserAutomationSession session, ILogger<BrowserTypeSkill> logger)
    {
        _session = session;
        _logger = logger;
    }

    public async Task<SkillExecutionResult> ExecuteAsync(IReadOnlyDictionary<string, object?> inputs, TaskExecutionContext context, CancellationToken cancellationToken = default)
    {
        var selector = inputs.TryGetValue("selector", out var s) ? s?.ToString() : null;
        var text = inputs.TryGetValue("text", out var t) ? t?.ToString() : null;
        if (string.IsNullOrWhiteSpace(selector) || text is null)
            return new SkillExecutionResult { Success = false, Message = "Missing required inputs: selector/text" };

        try
        {
            var page = await _session.GetPageAsync(headless: false, cancellationToken);
            await page.FillAsync(selector, text);
            _logger.LogDebug("browser.type selector={Selector} taskId={TaskId}", selector, context.TaskId);
            return new SkillExecutionResult { Success = true, Output = new { selector, textLength = text.Length } };
        }
        catch (Exception ex)
        {
            return new SkillExecutionResult { Success = false, Message = ex.Message };
        }
    }
}
