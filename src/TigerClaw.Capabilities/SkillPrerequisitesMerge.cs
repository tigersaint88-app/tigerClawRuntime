using TigerClaw.Models;

namespace TigerClaw.Capabilities;

/// <summary>
/// Merges skill-level and workflow-node capability requirements.
/// Rules: allOf/anyOf/noneOf union(unique); prefer: node first, then skill, unique order preserved.
/// Legacy <see cref="SkillPrerequisites.RequiredCapabilities"/> are appended to allOf.
/// </summary>
public static class SkillPrerequisitesMerge
{
    public static CapabilityRequirements Merge(SkillDefinition? skill, WorkflowStepDefinition? step)
    {
        var skillCaps = skill?.Prerequisites?.Capabilities;
        var legacy = skill?.Prerequisites?.RequiredCapabilities ?? Array.Empty<string>();
        var stepCaps = step?.CapabilityRequirements;

        return new CapabilityRequirements
        {
            AllOf = UnionUnique(skillCaps?.AllOf, legacy, stepCaps?.AllOf),
            AnyOf = UnionUnique(skillCaps?.AnyOf, stepCaps?.AnyOf),
            NoneOf = UnionUnique(skillCaps?.NoneOf, stepCaps?.NoneOf),
            Prefer = PreferMerge(stepCaps?.Prefer, skillCaps?.Prefer)
        };
    }

    static IReadOnlyList<string> PreferMerge(IReadOnlyList<string>? nodeFirst, IReadOnlyList<string>? skillSecond)
    {
        var list = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var x in nodeFirst ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(x)) continue;
            if (seen.Add(x)) list.Add(x);
        }
        foreach (var x in skillSecond ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(x)) continue;
            if (seen.Add(x)) list.Add(x);
        }
        return list;
    }

    static IReadOnlyList<string> UnionUnique(params IEnumerable<string>?[] sequences)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<string>();
        foreach (var seq in sequences)
        {
            if (seq == null) continue;
            foreach (var x in seq)
            {
                if (string.IsNullOrWhiteSpace(x)) continue;
                if (seen.Add(x)) list.Add(x);
            }
        }
        return list;
    }
}
