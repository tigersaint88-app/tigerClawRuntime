using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TigerClaw.Infrastructure;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Memory;

namespace TigerClaw.Memory.Tests;

public class PreferenceServiceTests
{
    private static (IServiceProvider, string) CreateProviderWithTempDb()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"tigerclaw_test_{Guid.NewGuid():N}.db");
        var sc = new ServiceCollection();
        sc.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        sc.Configure<TigerClawOptions>(opts =>
        {
            opts.Database.DataDirectory = Path.GetDirectoryName(dbPath)!;
            opts.Database.ConnectionString = $"Data Source={dbPath}";
            opts.Workspace.RootPath = Path.GetTempPath();
        });
        sc.AddTigerClawInfrastructure();
        sc.AddTigerClawMemory();
        var sp = sc.BuildServiceProvider();
        var init = sp.GetRequiredService<TigerClaw.Infrastructure.Database.DatabaseInitializer>();
        init.InitializeAsync().GetAwaiter().GetResult();
        return (sp, dbPath);
    }

    [Fact]
    public async Task Upsert_And_Get_ReturnsValue()
    {
        var (sp, dbPath) = CreateProviderWithTempDb();
        var prefs = sp.GetRequiredService<Core.IPreferenceService>();
        await prefs.UpsertAsync("test_key", "test_value");
        var val = await prefs.GetAsync("test_key");
        Assert.Equal("test_value", val);
    }
}
