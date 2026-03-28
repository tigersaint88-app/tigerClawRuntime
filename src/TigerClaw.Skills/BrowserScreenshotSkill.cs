using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Models;

namespace TigerClaw.Skills;

/// <summary>
/// Captures a screenshot from the current page.
/// </summary>
public class BrowserScreenshotSkill : Core.ISkill
{
    public string Id => "browser.screenshot";
    private readonly BrowserAutomationSession _session;
    private readonly WorkspaceOptions _workspace;
    private readonly ILogger<BrowserScreenshotSkill> _logger;

    public BrowserScreenshotSkill(BrowserAutomationSession session, IOptions<TigerClawOptions> options, ILogger<BrowserScreenshotSkill> logger)
    {
        _session = session;
        _workspace = options.Value.Workspace;
        _logger = logger;
    }

    public async Task<SkillExecutionResult> ExecuteAsync(IReadOnlyDictionary<string, object?> inputs, TaskExecutionContext context, CancellationToken cancellationToken = default)
    {
        var fileName = inputs.TryGetValue("fileName", out var f) ? f?.ToString() : $"screen_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
        try
        {
            var root = string.IsNullOrEmpty(_workspace.RootPath) || _workspace.RootPath == "." ? Directory.GetCurrentDirectory() : _workspace.RootPath;
            var dir = Path.Combine(root, _workspace.ArtifactsPath);
            Directory.CreateDirectory(dir);
            var fullPath = Path.Combine(dir, fileName!);
            var page = await _session.GetPageAsync(headless: false, cancellationToken);
            await page.ScreenshotAsync(new() { Path = fullPath, FullPage = true });
            _logger.LogDebug("browser.screenshot saved={Path} taskId={TaskId}", fullPath, context.TaskId);
            return new SkillExecutionResult { Success = true, ArtifactPath = fullPath };
        }
        catch (Exception ex)
        {
            return new SkillExecutionResult { Success = false, Message = ex.Message };
        }
    }
}
