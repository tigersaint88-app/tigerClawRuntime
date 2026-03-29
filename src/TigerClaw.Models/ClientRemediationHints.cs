namespace TigerClaw.Models;

/// <summary>Static copy for <see cref="TaskResponse.RemediationHint"/> (API JSON).</summary>
public static class ClientRemediationHints
{
    public const string PreferencesThenRerun =
        "When outcome is needs_user_input: render a form from issues and suggestedPreferenceKeys; POST /memory/preferences with the same userId; then POST /workflows/{id}/run again (or repeat the same /tasks/run text).";
}
