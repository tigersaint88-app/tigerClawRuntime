namespace TigerClaw.Cli;

/// <summary>
/// Normalized CLI command (OpenClaw-compatible object model).
/// </summary>
public sealed class CliCommandRequest
{
    public string Group { get; init; } = "";
    public string Action { get; init; } = "";
    public string Target { get; init; } = "";
    /// <summary>Positional tokens after flags (e.g. workflow run &lt;id&gt; ...).</summary>
    public IReadOnlyList<string> Positional { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> Options { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public bool JsonOutput { get; init; }
    public bool Help { get; init; }
    /// <summary>When true, entire command line was treated as a natural-language run.</summary>
    public bool IsFallbackRun { get; init; }
}
