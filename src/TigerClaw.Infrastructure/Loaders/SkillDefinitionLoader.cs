using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Models;

namespace TigerClaw.Infrastructure.Loaders;

/// <summary>
/// Loads skill definitions from JSON file.
/// </summary>
public class SkillDefinitionLoader
{
    private readonly WorkspaceOptions _options;
    private readonly ILogger<SkillDefinitionLoader> _logger;
    private List<SkillDefinition>? _cache;

    public SkillDefinitionLoader(IOptions<TigerClawOptions> options, ILogger<SkillDefinitionLoader> logger)
    {
        _options = options.Value.Workspace;
        _logger = logger;
    }

    public Task<IReadOnlyList<SkillDefinition>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cache != null) return Task.FromResult<IReadOnlyList<SkillDefinition>>(_cache);

        var skillsPath = Path.Combine(ResolveRoot(), _options.SkillsPath, "skills.json");
        if (!File.Exists(skillsPath))
        {
            _logger.LogWarning("Skills file not found at {Path}, returning empty list", skillsPath);
            _cache = new List<SkillDefinition>();
            return Task.FromResult<IReadOnlyList<SkillDefinition>>(_cache);
        }

        try
        {
            var json = File.ReadAllText(skillsPath);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var list = JsonSerializer.Deserialize<List<SkillDefinition>>(json, opts) ?? new List<SkillDefinition>();
            _cache = list;
            _logger.LogInformation("Loaded {Count} skill definitions from {Path}", list.Count, skillsPath);
            return Task.FromResult<IReadOnlyList<SkillDefinition>>(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load skills from {Path}", skillsPath);
            _cache = new List<SkillDefinition>();
            return Task.FromResult<IReadOnlyList<SkillDefinition>>(_cache);
        }
    }

    private string ResolveRoot()
    {
        var root = _options.RootPath;
        if (string.IsNullOrEmpty(root) || root == ".") return Directory.GetCurrentDirectory();
        return Path.IsPathRooted(root) ? root : Path.Combine(Directory.GetCurrentDirectory(), root);
    }
}
