using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TigerClaw.Models;

namespace TigerClaw.Skills;

/// <summary>
/// Runs a shell command.
/// </summary>
public class ShellRunSkill : Core.ISkill
{
    public string Id => "shell.run";
    private readonly ILogger<ShellRunSkill> _logger;

    public ShellRunSkill(ILogger<ShellRunSkill> logger) => _logger = logger;

    public async Task<SkillExecutionResult> ExecuteAsync(IReadOnlyDictionary<string, object?> inputs, TaskExecutionContext context, CancellationToken cancellationToken = default)
    {
        var command = inputs.TryGetValue("command", out var c) ? c?.ToString() : null;
        if (string.IsNullOrWhiteSpace(command))
            return new SkillExecutionResult { Success = false, Message = "Missing required input: command" };

        try
        {
            var useShell = Environment.OSVersion.Platform == PlatformID.Win32NT;
            var psi = new ProcessStartInfo
            {
                FileName = useShell ? "cmd.exe" : "/bin/sh",
                Arguments = useShell ? $"/c {command}" : $"-c \"{command.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null)
                return new SkillExecutionResult { Success = false, Message = "Failed to start process" };

            var stdout = await proc.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = await proc.StandardError.ReadToEndAsync(cancellationToken);
            await proc.WaitForExitAsync(cancellationToken);
            _logger.LogDebug("Shell executed: {Command} exitCode={ExitCode} taskId={TaskId}", command, proc.ExitCode, context.TaskId);
            return new SkillExecutionResult
            {
                Success = proc.ExitCode == 0,
                Output = new { stdout, stderr, exitCode = proc.ExitCode },
                Message = proc.ExitCode != 0 ? stderr : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shell execution failed: {Command}", command);
            return new SkillExecutionResult { Success = false, Message = ex.Message };
        }
    }
}
