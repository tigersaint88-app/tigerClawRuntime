using TigerClaw.Models;

namespace TigerClaw.Core;

/// <summary>
/// Interface for executable skills.
/// </summary>
public interface ISkill
{
    /// <summary>
    /// Unique skill identifier (e.g., "file.read_text").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Executes the skill with given inputs and context.
    /// </summary>
    Task<SkillExecutionResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?> inputs,
        TigerClaw.Models.TaskExecutionContext context,
        CancellationToken cancellationToken = default);
}
