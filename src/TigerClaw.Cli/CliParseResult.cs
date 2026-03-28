namespace TigerClaw.Cli;

public sealed class CliParseResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public CliCommandRequest? Request { get; init; }

    public static CliParseResult Ok(CliCommandRequest request) => new() { Success = true, Request = request };
    public static CliParseResult Fail(string error) => new() { Success = false, Error = error };
}
