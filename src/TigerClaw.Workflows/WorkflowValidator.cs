using TigerClaw.Models;

namespace TigerClaw.Workflows;

/// <summary>
/// Validates workflow definitions.
/// </summary>
public static class WorkflowValidator
{
    public static IReadOnlyList<string> Validate(WorkflowDefinition def)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(def.Id)) errors.Add("Id is required");
        if (string.IsNullOrWhiteSpace(def.Name)) errors.Add("Name is required");
        if (def.Steps == null || def.Steps.Count == 0) errors.Add("At least one step is required");
        foreach (var step in def.Steps ?? Array.Empty<WorkflowStepDefinition>())
        {
            if (string.IsNullOrWhiteSpace(step.Id)) errors.Add($"Step missing Id in workflow {def.Id}");
            if (string.IsNullOrWhiteSpace(step.SkillId)) errors.Add($"Step {step.Id} missing SkillId");
        }
        return errors;
    }
}
