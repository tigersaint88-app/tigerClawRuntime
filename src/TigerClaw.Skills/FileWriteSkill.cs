using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Models;

namespace TigerClaw.Skills;

/// <summary>
/// Writes text to a file.
/// </summary>
public class FileWriteSkill : Core.ISkill
{
    public string Id => "file.write_text";
    private readonly WorkspaceOptions _workspace;
    private readonly ILogger<FileWriteSkill> _logger;

    public FileWriteSkill(IOptions<TigerClawOptions> options, ILogger<FileWriteSkill> logger)
    {
        _workspace = options.Value.Workspace;
        _logger = logger;
    }

    public async Task<SkillExecutionResult> ExecuteAsync(IReadOnlyDictionary<string, object?> inputs, TaskExecutionContext context, CancellationToken cancellationToken = default)
    {
        var path = inputs.TryGetValue("path", out var p) ? p?.ToString() : null;
        var content = inputs.TryGetValue("content", out var c) ? c?.ToString() : null;
        if (string.IsNullOrWhiteSpace(path))
            return new SkillExecutionResult { Success = false, Message = "Missing required input: path" };

        try
        {
            var artifactsDir = Path.Combine(ResolveRoot(), _workspace.ArtifactsPath);
            Directory.CreateDirectory(artifactsDir);
            var fullPath = path.Contains(Path.DirectorySeparatorChar) ? ResolvePath(path) : Path.Combine(artifactsDir, path);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(fullPath, content ?? "", cancellationToken);
            _logger.LogDebug("File written: {Path} taskId={TaskId}", fullPath, context.TaskId);
            return new SkillExecutionResult { Success = true, ArtifactPath = fullPath };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write file {Path}", path);
            return new SkillExecutionResult { Success = false, Message = ex.Message };
        }
    }

    private string ResolveRoot()
    {
        var root = _workspace.RootPath;
        return string.IsNullOrEmpty(root) || root == "." ? Directory.GetCurrentDirectory() : (Path.IsPathRooted(root) ? root : Path.Combine(Directory.GetCurrentDirectory(), root));
    }

    private string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        return Path.Combine(ResolveRoot(), path);
    }
}
