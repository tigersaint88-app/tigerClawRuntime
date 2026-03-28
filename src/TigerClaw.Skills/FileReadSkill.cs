using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Models;

namespace TigerClaw.Skills;

/// <summary>
/// Reads text from a file.
/// </summary>
public class FileReadSkill : Core.ISkill
{
    public string Id => "file.read_text";
    private readonly WorkspaceOptions _workspace;
    private readonly ILogger<FileReadSkill> _logger;

    public FileReadSkill(IOptions<TigerClawOptions> options, ILogger<FileReadSkill> logger)
    {
        _workspace = options.Value.Workspace;
        _logger = logger;
    }

    public async Task<SkillExecutionResult> ExecuteAsync(IReadOnlyDictionary<string, object?> inputs, TaskExecutionContext context, CancellationToken cancellationToken = default)
    {
        var path = inputs.TryGetValue("path", out var p) ? p?.ToString() : null;
        if (string.IsNullOrWhiteSpace(path))
            return new SkillExecutionResult { Success = false, Message = "Missing required input: path" };

        try
        {
            var fullPath = ResolvePath(path);
            var content = await File.ReadAllTextAsync(fullPath, cancellationToken);
            _logger.LogDebug("File read: {Path} taskId={TaskId}", path, context.TaskId);
            return new SkillExecutionResult { Success = true, Output = content };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read file {Path}", path);
            return new SkillExecutionResult { Success = false, Message = ex.Message };
        }
    }

    private string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        var root = _workspace.RootPath;
        if (string.IsNullOrEmpty(root) || root == ".") root = Directory.GetCurrentDirectory();
        return Path.Combine(root, path);
    }
}
