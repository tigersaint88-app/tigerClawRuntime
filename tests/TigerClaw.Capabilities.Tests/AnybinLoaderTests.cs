using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TigerClaw.Capabilities.Bins;
using TigerClaw.Infrastructure.Options;

namespace TigerClaw.Capabilities.Tests;

public class AnybinLoaderTests
{
    [Fact]
    public void Load_reads_workspace_bins_anybins_json()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var opts = Options.Create(new TigerClawOptions
        {
            Workspace = { RootPath = root }
        });
        var loader = new AnybinLoader(opts, NullLogger<AnybinLoader>.Instance);
        var m = loader.Load();
        Assert.Contains(m.AnyBins, e => e.Id == "node");
    }
}
