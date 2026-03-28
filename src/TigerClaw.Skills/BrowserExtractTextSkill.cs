using Microsoft.Extensions.Logging;
using TigerClaw.Models;

namespace TigerClaw.Skills;

/// <summary>
/// Extracts visible text from elements matched by selector.
/// </summary>
public class BrowserExtractTextSkill : Core.ISkill
{
    public string Id => "browser.extract_text";
    private readonly BrowserAutomationSession _session;
    private readonly ILogger<BrowserExtractTextSkill> _logger;

    public BrowserExtractTextSkill(BrowserAutomationSession session, ILogger<BrowserExtractTextSkill> logger)
    {
        _session = session;
        _logger = logger;
    }

    public async Task<SkillExecutionResult> ExecuteAsync(IReadOnlyDictionary<string, object?> inputs, TaskExecutionContext context, CancellationToken cancellationToken = default)
    {
        var selector = inputs.TryGetValue("selector", out var s) ? s?.ToString() : null;
        var maxItems = inputs.TryGetValue("maxItems", out var m) && int.TryParse(m?.ToString(), out var x) ? x : 10;
        if (string.IsNullOrWhiteSpace(selector))
            return new SkillExecutionResult { Success = false, Message = "Missing required input: selector" };

        try
        {
            var page = await _session.GetPageAsync(headless: false, cancellationToken);
            var texts = await page.Locator(selector).AllInnerTextsAsync();
            var result = texts.Where(t => !string.IsNullOrWhiteSpace(t)).Take(maxItems).ToList();
            _logger.LogDebug("browser.extract_text selector={Selector} count={Count} taskId={TaskId}", selector, result.Count, context.TaskId);
            return new SkillExecutionResult { Success = true, Output = result };
        }
        catch (Exception ex)
        {
            return new SkillExecutionResult { Success = false, Message = ex.Message };
        }
    }
}
