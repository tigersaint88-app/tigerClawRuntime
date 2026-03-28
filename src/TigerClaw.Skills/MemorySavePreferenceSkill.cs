using Microsoft.Extensions.Logging;
using TigerClaw.Models;

namespace TigerClaw.Skills;

/// <summary>
/// Saves a user preference to memory.
/// </summary>
public class MemorySavePreferenceSkill : Core.ISkill
{
    public string Id => "memory.save_preference";
    private readonly Core.IPreferenceService _preferences;
    private readonly ILogger<MemorySavePreferenceSkill> _logger;

    public MemorySavePreferenceSkill(Core.IPreferenceService preferences, ILogger<MemorySavePreferenceSkill> logger)
    {
        _preferences = preferences;
        _logger = logger;
    }

    public async Task<SkillExecutionResult> ExecuteAsync(IReadOnlyDictionary<string, object?> inputs, TaskExecutionContext context, CancellationToken cancellationToken = default)
    {
        var key = inputs.TryGetValue("key", out var k) ? k?.ToString() : null;
        var value = inputs.TryGetValue("value", out var v) ? v?.ToString() : null;
        if (string.IsNullOrWhiteSpace(key))
            return new SkillExecutionResult { Success = false, Message = "Missing required input: key" };

        await _preferences.UpsertAsync(key, value ?? "", context.UserId, cancellationToken);
        _logger.LogDebug("Preference saved: {Key} taskId={TaskId}", key, context.TaskId);
        return new SkillExecutionResult { Success = true, Output = new { key, value } };
    }
}
