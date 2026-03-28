namespace TigerClaw.Models;

/// <summary>
/// Defines a named workflow with its steps and parameters.
/// </summary>
public record WorkflowDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, WorkflowParameter> Parameters { get; init; } = new Dictionary<string, WorkflowParameter>();
    public required IReadOnlyList<WorkflowStepDefinition> Steps { get; init; }
}

/// <summary>
/// Defines a workflow parameter.
/// </summary>
public record WorkflowParameter
{
    public string? Type { get; init; }
    public string? Default { get; init; }
    public bool Required { get; init; }
}
