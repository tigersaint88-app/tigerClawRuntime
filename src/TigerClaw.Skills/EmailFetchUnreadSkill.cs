using Microsoft.Extensions.Logging;
using TigerClaw.Models;

namespace TigerClaw.Skills;

/// <summary>
/// Stub: Fetches unread emails. V1 returns mock data.
/// TODO: Integrate with real email provider.
/// </summary>
public class EmailFetchUnreadSkill : Core.ISkill
{
    public string Id => "email.fetch_unread";
    private readonly ILogger<EmailFetchUnreadSkill> _logger;

    public EmailFetchUnreadSkill(ILogger<EmailFetchUnreadSkill> logger) => _logger = logger;

    public Task<SkillExecutionResult> ExecuteAsync(IReadOnlyDictionary<string, object?> inputs, TaskExecutionContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Email fetch_unread (stub) taskId={TaskId}", context.TaskId);
        var mockEmails = new[]
        {
            new { subject = "Meeting reminder", sender = "calendar@example.com", date = DateTime.UtcNow.AddHours(-1).ToString("O"), bodySnippet = "Your meeting starts in 1 hour." },
            new { subject = "Newsletter", sender = "news@example.com", date = DateTime.UtcNow.AddHours(-2).ToString("O"), bodySnippet = "This week's updates..." }
        };
        return Task.FromResult(new SkillExecutionResult
        {
            Success = true,
            Output = new { count = mockEmails.Length, emails = mockEmails }
        });
    }
}
