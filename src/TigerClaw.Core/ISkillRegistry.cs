using TigerClaw.Models;

namespace TigerClaw.Core;

/// <summary>
/// Registry of available skills.
/// </summary>
public interface ISkillRegistry
{
    /// <summary>
    /// Gets a skill by ID.
    /// </summary>
    ISkill? GetSkill(string skillId);

    /// <summary>
    /// Gets skill definition by ID.
    /// </summary>
    SkillDefinition? GetDefinition(string skillId);

    /// <summary>
    /// Lists all registered skills.
    /// </summary>
    IReadOnlyList<SkillDefinition> ListAll();

    /// <summary>
    /// Searches skills by tag.
    /// </summary>
    IReadOnlyList<SkillDefinition> SearchByTag(string tag);
}
