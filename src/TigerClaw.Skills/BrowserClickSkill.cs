using Microsoft.Extensions.Logging;
using TigerClaw.Models;

namespace TigerClaw.Skills;

/// <summary>
/// Clicks an element.
/// </summary>
public class BrowserClickSkill : Core.ISkill
{
    public string Id => "browser.click";
    private readonly BrowserAutomationSession _session;
    private readonly ILogger<BrowserClickSkill> _logger;

    public BrowserClickSkill(BrowserAutomationSession session, ILogger<BrowserClickSkill> logger)
    {
        _session = session;
        _logger = logger;
    }

    public async Task<SkillExecutionResult> ExecuteAsync(IReadOnlyDictionary<string, object?> inputs, TaskExecutionContext context, CancellationToken cancellationToken = default)
    {
        var selector = inputs.TryGetValue("selector", out var s) ? s?.ToString() : null;
        if (string.IsNullOrWhiteSpace(selector))
            return new SkillExecutionResult { Success = false, Message = "Missing required input: selector" };

        try
        {
            var page = await _session.GetPageAsync(headless: false, cancellationToken);
            await page.ClickAsync(selector);
            _logger.LogDebug("browser.click selector={Selector} taskId={TaskId}", selector, context.TaskId);
            return new SkillExecutionResult { Success = true, Output = new { selector } };
        }
        catch (Exception ex)
        {
            return new SkillExecutionResult { Success = false, Message = ex.Message };
        }
    }
}
