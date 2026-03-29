namespace TigerClaw.Models;

/// <summary>
/// One missing or invalid preset item for callers (CLI/API) to show forms or retry after <c>POST /memory/preferences</c>.
/// </summary>
public record PrerequisiteIssue
{
    /// <summary>preference | env | capability | resource | network</summary>
    public required string Kind { get; init; }

    /// <summary>Preference key, env name, capability id, or resource key.</summary>
    public string? Key { get; init; }

    public required string Code { get; init; }
    public required string Message { get; init; }

    /// <summary>Default text for interactive clients (what to ask the user).</summary>
    public string? InteractionHint { get; init; }

    /// <summary>
    /// When true, clients should show <see cref="Key"/> (and any value typed for that key) masked (e.g. <c>********</c>) until the user toggles reveal.
    /// Set for password / secret preference keys; see <see cref="PrerequisiteSensitive"/>.
    /// </summary>
    public bool MaskKeyInUi { get; init; }
}
