using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TigerClaw.Capabilities.Bins;
using TigerClaw.Capabilities.Probes;
using TigerClaw.Infrastructure.Options;

namespace TigerClaw.Capabilities;

/// <summary>
/// Builds a <see cref="ResourceSnapshot"/> by running lightweight probes.
/// </summary>
public sealed class ResourceSnapshotBuilder
{
    private readonly AnybinLoader _anybinLoader;
    private readonly IOptions<TigerClawOptions> _options;
    private readonly ILogger<ResourceSnapshotBuilder> _logger;

    public ResourceSnapshotBuilder(AnybinLoader anybinLoader, IOptions<TigerClawOptions> options, ILogger<ResourceSnapshotBuilder> logger)
    {
        _anybinLoader = anybinLoader;
        _options = options;
        _logger = logger;
    }

    public async Task<ResourceSnapshot> BuildAsync(CancellationToken cancellationToken = default)
    {
        var diag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var osFamily = OsProbe.DetectOsFamily();
        var osCap = CapabilityIds.OsFamily(osFamily);
        diag["os"] = osFamily;

        var desktop = DesktopProbe.IsLikelyDesktopInteractive();
        diag["desktop_interactive"] = desktop.ToString();

        var manifest = _anybinLoader.Load();
        var namesToScan = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "git", "dotnet", "node", "npm", "pwsh", "powershell", "cmd"
        };
        foreach (var e in manifest.AnyBins)
        {
            foreach (var c in e.Candidates)
                if (!string.IsNullOrWhiteSpace(c)) namesToScan.Add(c.Trim());
        }

        var binCaps = BinariesProbe.Probe(namesToScan);
        diag["bin_probe_count"] = binCaps.Count.ToString();

        var anyBinCaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in manifest.AnyBins)
        {
            if (string.IsNullOrWhiteSpace(e.Id)) continue;
            var hit = e.Candidates.Any(c => !string.IsNullOrWhiteSpace(c) && binCaps.Contains(CapabilityIds.Bin(c.Trim())));
            if (hit)
                anyBinCaps.Add(CapabilityIds.AnyBin(e.Id));
        }

        var baseUrl = _options.Value.ModelRouting.RemoteApiBaseUrl;
        var (llmOk, llmDetail) = await LlmProbe.ProbeAsync(baseUrl, TimeSpan.FromSeconds(3), cancellationToken);
        diag["llm"] = llmDetail;
        _logger.LogDebug("Resource snapshot: os={Os} bins={Bins} anybins={Any} llm={Llm}", osFamily, binCaps.Count, anyBinCaps.Count, llmOk);

        return new ResourceSnapshot(
            OsFamily: osFamily,
            OsCapabilityId: osCap,
            DesktopInteractive: desktop,
            BinaryCapabilityIds: binCaps,
            AnyBinCapabilityIds: anyBinCaps,
            LlmEndpointReachable: llmOk,
            LlmProbeDetail: llmDetail,
            ProbeDiagnostics: diag);
    }
}
