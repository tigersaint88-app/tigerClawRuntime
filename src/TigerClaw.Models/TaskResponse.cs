namespace TigerClaw.Models;

/// <summary>
/// Represents the result of a task execution.
/// </summary>
public record TaskResponse
{
    public required string RequestId { get; init; }
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? FinalText { get; init; }
    public IReadOnlyList<string> Artifacts { get; init; } = Array.Empty<string>();
    public string? WorkflowId { get; init; }
    public IReadOnlyList<StepExecutionResult> Steps { get; init; } = Array.Empty<StepExecutionResult>();
    public string? ErrorCode { get; init; }

    /// <summary>True when execution stopped for missing prefs/env/capability setup; caller should supply data and re-run the same workflow.</summary>
    public bool WaitingHuman { get; init; }

    public IReadOnlyList<PrerequisiteIssue> Issues { get; init; } = Array.Empty<PrerequisiteIssue>();

    /// <summary>Consolidated prompt for UIs (Chinese default).</summary>
    public string? InteractionMessage { get; init; }

    /// <summary>
    /// Use this for routing: <see cref="TaskOutcomes.Completed"/>, <see cref="TaskOutcomes.Failed"/>, <see cref="TaskOutcomes.NeedsUserInput"/>.
    /// When <c>needs_user_input</c>, frontends must collect <see cref="SuggestedPreferenceKeys"/> (and read <see cref="Issues"/>) then POST preferences and re-run.
    /// </summary>
    public string Outcome { get; init; } = TaskOutcomes.Completed;

    /// <summary>Explicit alias of <see cref="WaitingHuman"/> for SPA: show a form before retry.</summary>
    public bool RequiresUserInput { get; init; }

    /// <summary>Preference keys to offer as inputs (from issues); POST <c>/memory/preferences</c> with the same <c>userId</c> as the workflow run.</summary>
    public IReadOnlyList<string> SuggestedPreferenceKeys { get; init; } = Array.Empty<string>();

    /// <summary>Non-null when <see cref="Outcome"/> is <see cref="TaskOutcomes.NeedsUserInput"/>; tells the client which API to call next.</summary>
    public string? RemediationHint { get; init; }
}
