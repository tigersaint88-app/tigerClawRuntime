namespace TigerClaw.Cli;

/// <summary>
/// CLI exit result (text or JSON-serializable payload).
/// </summary>
public sealed class CliCommandResult
{
    public int ExitCode { get; init; }
    public bool Success { get; init; }
    public object? Data { get; init; }
    public string? Text { get; init; }
    public string? Error { get; init; }

    public static CliCommandResult Ok(string? text = null, object? data = null, int exitCode = 0) =>
        new() { Success = true, Text = text, Data = data, ExitCode = exitCode };

    public static CliCommandResult Fail(string message, int exitCode = 2, object? data = null) =>
        new() { Success = false, Error = message, Text = null, Data = data, ExitCode = exitCode };
}
