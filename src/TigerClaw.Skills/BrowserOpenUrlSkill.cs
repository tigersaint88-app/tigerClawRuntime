using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using TigerClaw.Models;

namespace TigerClaw.Skills;

/// <summary>
/// Opens or navigates the shared browser page to a URL.
/// </summary>
public class BrowserOpenUrlSkill : Core.ISkill
{
    public string Id => "browser.open_url";
    private readonly BrowserAutomationSession _session;
    private readonly ILogger<BrowserOpenUrlSkill> _logger;

    public BrowserOpenUrlSkill(BrowserAutomationSession session, ILogger<BrowserOpenUrlSkill> logger)
    {
        _session = session;
        _logger = logger;
    }

    public async Task<SkillExecutionResult> ExecuteAsync(IReadOnlyDictionary<string, object?> inputs, TaskExecutionContext context, CancellationToken cancellationToken = default)
    {
        var url = inputs.TryGetValue("url", out var u) ? u?.ToString() : null;
        var headless = inputs.TryGetValue("headless", out var h) && bool.TryParse(h?.ToString(), out var parsed) && parsed;
        if (string.IsNullOrWhiteSpace(url))
            return new SkillExecutionResult { Success = false, Message = "Missing required input: url" };

        try
        {
            var page = await _session.GetPageAsync(headless, cancellationToken);
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            _logger.LogInformation("Browser navigated to {Url} taskId={TaskId}", url, context.TaskId);
            return new SkillExecutionResult { Success = true, Output = new { url, title = await page.TitleAsync() } };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "browser.open_url failed for {Url}", url);
            return new SkillExecutionResult { Success = false, Message = ex.Message };
        }
    }
}
