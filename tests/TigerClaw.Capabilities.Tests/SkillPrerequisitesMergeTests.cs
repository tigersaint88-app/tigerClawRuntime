using TigerClaw.Models;

namespace TigerClaw.Capabilities.Tests;

public class SkillPrerequisitesMergeTests
{
    [Fact]
    public void Merge_unions_allOf_anyOf_noneOf_and_prefers_node_first()
    {
        var skill = new SkillDefinition
        {
            Id = "s",
            Name = "S",
            Prerequisites = new SkillPrerequisites
            {
                RequiredCapabilities = new[] { "email.read" },
                Capabilities = new CapabilityRequirements
                {
                    AllOf = new[] { "a" },
                    AnyOf = new[] { "x", "y" },
                    NoneOf = new[] { "n1" },
                    Prefer = new[] { "p_skill" }
                }
            }
        };

        var step = new WorkflowStepDefinition
        {
            Id = "1",
            SkillId = "s",
            CapabilityRequirements = new CapabilityRequirements
            {
                AllOf = new[] { "b" },
                AnyOf = new[] { "z" },
                NoneOf = new[] { "n2" },
                Prefer = new[] { "p_node" }
            }
        };

        var m = SkillPrerequisitesMerge.Merge(skill, step);
        Assert.Equal(new[] { "a", "b", "email.read" }, m.AllOf.OrderBy(x => x).ToArray());
        Assert.Equal(new[] { "x", "y", "z" }, m.AnyOf.OrderBy(x => x).ToArray());
        Assert.Equal(new[] { "n1", "n2" }, m.NoneOf.OrderBy(x => x).ToArray());
        Assert.Equal(new[] { "p_node", "p_skill" }, m.Prefer.ToArray());
    }

    [Fact]
    public void Tag_only_skill_empty_merge()
    {
        var skill = new SkillDefinition { Id = "t", Name = "T", Tags = new[] { "x" } };
        var step = new WorkflowStepDefinition { Id = "1", SkillId = "t" };
        var m = SkillPrerequisitesMerge.Merge(skill, step);
        Assert.Empty(m.AllOf);
        Assert.Empty(m.AnyOf);
    }
}
