using Microsoft.Extensions.Logging;
using TigerClaw.Infrastructure.Loaders;
using TigerClaw.Models;

namespace TigerClaw.Skills;

/// <summary>
/// Skill registry that loads from JSON and resolves to ISkill implementations.
/// </summary>
public class JsonSkillRegistry : Core.ISkillRegistry
{
    private readonly SkillDefinitionLoader _loader;
    private readonly IReadOnlyDictionary<string, Core.ISkill> _skills;
    private readonly ILogger<JsonSkillRegistry> _logger;
    private List<SkillDefinition>? _definitions;

    public JsonSkillRegistry(SkillDefinitionLoader loader, IEnumerable<Core.ISkill> skills, ILogger<JsonSkillRegistry> logger)
    {
        _loader = loader;
        _skills = skills.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public Core.ISkill? GetSkill(string skillId)
    {
        return _skills.TryGetValue(skillId, out var s) ? s : null;
    }

    public SkillDefinition? GetDefinition(string skillId)
    {
        var defs = LoadDefinitions();
        return defs.FirstOrDefault(d => string.Equals(d.Id, skillId, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<SkillDefinition> ListAll()
    {
        return LoadDefinitions();
    }

    public IReadOnlyList<SkillDefinition> SearchByTag(string tag)
    {
        return LoadDefinitions().Where(d => d.Tags?.Contains(tag, StringComparer.OrdinalIgnoreCase) == true).ToList();
    }

    private IReadOnlyList<SkillDefinition> LoadDefinitions()
    {
        if (_definitions != null) return _definitions;
        _definitions = _loader.LoadAllAsync().GetAwaiter().GetResult().ToList();
        return _definitions;
    }
}
