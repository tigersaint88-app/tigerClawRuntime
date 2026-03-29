namespace TigerClaw.Models;

/// <summary>Machine-readable workflow/task result for frontends (branch on <see cref="TaskResponse.Outcome"/>).</summary>
public static class TaskOutcomes
{
    public const string Completed = "completed";
    public const string Failed = "failed";

    /// <summary>Missing preferences/env or recoverable IMAP/config; caller must PATCH preferences and re-run.</summary>
    public const string NeedsUserInput = "needs_user_input";
}
