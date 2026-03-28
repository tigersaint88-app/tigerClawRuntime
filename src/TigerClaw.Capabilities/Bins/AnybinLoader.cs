using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TigerClaw.Infrastructure.Options;

namespace TigerClaw.Capabilities.Bins;

/// <summary>
/// Loads <c>bins/anybins.json</c> from workspace for anybin capability registration.
/// </summary>
public sealed class AnybinLoader
{
    private readonly WorkspaceOptions _workspace;
    private readonly ILogger<AnybinLoader> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public AnybinLoader(IOptions<TigerClawOptions> options, ILogger<AnybinLoader> logger)
    {
        _workspace = options.Value.Workspace;
        _logger = logger;
    }

    public AnybinManifest Load()
    {
        var root = ResolveRoot();
        var path = Path.Combine(root, "bins", "anybins.json");
        if (!File.Exists(path))
        {
            _logger.LogDebug("anybins manifest not found at {Path}", path);
            return new AnybinManifest();
        }

        try
        {
            var json = File.ReadAllText(path);
            var manifest = JsonSerializer.Deserialize<AnybinManifest>(json, JsonOpts) ?? new AnybinManifest();
            _logger.LogInformation("Loaded {Count} anybin entries from {Path}", manifest.AnyBins.Count, path);
            return manifest;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load anybins from {Path}", path);
            return new AnybinManifest();
        }
    }

    private string ResolveRoot()
    {
        var root = _workspace.RootPath;
        if (string.IsNullOrEmpty(root) || root == ".") return Directory.GetCurrentDirectory();
        return Path.IsPathRooted(root) ? root : Path.Combine(Directory.GetCurrentDirectory(), root);
    }
}
