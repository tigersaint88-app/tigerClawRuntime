using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Models;

namespace TigerClaw.Infrastructure.Loaders;

/// <summary>
/// Loads workflow definitions from JSON files.
/// </summary>
public class WorkflowDefinitionLoader
{
    private readonly WorkspaceOptions _options;
    private readonly ILogger<WorkflowDefinitionLoader> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WorkflowDefinitionLoader(IOptions<TigerClawOptions> options, ILogger<WorkflowDefinitionLoader> logger)
    {
        _options = options.Value.Workspace;
        _logger = logger;
    }

    public Task<WorkflowDefinition?> LoadAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(ResolveRoot(), _options.WorkflowsPath, $"{workflowId}.json");
        if (!File.Exists(path))
        {
            _logger.LogDebug("Workflow file not found: {Path}", path);
            return Task.FromResult<WorkflowDefinition?>(null);
        }

        try
        {
            var json = File.ReadAllText(path);
            var def = JsonSerializer.Deserialize<WorkflowDefinition>(json, JsonOptions);
            return Task.FromResult<WorkflowDefinition?>(def);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workflow {Id} from {Path}", workflowId, path);
            return Task.FromResult<WorkflowDefinition?>(null);
        }
    }

    public Task<IReadOnlyList<WorkflowDefinition>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        var workflowsDir = Path.Combine(ResolveRoot(), _options.WorkflowsPath);
        if (!Directory.Exists(workflowsDir))
        {
            _logger.LogDebug("Workflows directory not found: {Path}", workflowsDir);
            return Task.FromResult<IReadOnlyList<WorkflowDefinition>>(Array.Empty<WorkflowDefinition>());
        }

        var list = new List<WorkflowDefinition>();
        foreach (var file in Directory.GetFiles(workflowsDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var def = JsonSerializer.Deserialize<WorkflowDefinition>(json, JsonOptions);
                if (def != null) list.Add(def);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load workflow from {File}", file);
            }
        }
        return Task.FromResult<IReadOnlyList<WorkflowDefinition>>(list);
    }

    private string ResolveRoot()
    {
        var root = _options.RootPath;
        if (string.IsNullOrEmpty(root) || root == ".") return Directory.GetCurrentDirectory();
        return Path.IsPathRooted(root) ? root : Path.Combine(Directory.GetCurrentDirectory(), root);
    }
}
