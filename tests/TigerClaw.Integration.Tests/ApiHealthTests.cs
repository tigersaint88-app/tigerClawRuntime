using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TigerClaw.Core;
using TigerClaw.Infrastructure;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Memory;
using TigerClaw.Models;
using TigerClaw.Runtime;
using TigerClaw.Skills;
using TigerClaw.Workflows;

namespace TigerClaw.Integration.Tests;

public class ApiHealthTests
{
    private static IServiceProvider CreateProvider()
    {
        var sc = new ServiceCollection();
        sc.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        sc.Configure<TigerClawOptions>(opts =>
        {
            opts.Database.DataDirectory = Path.Combine(root, "data");
            opts.Database.ConnectionString = $"Data Source={Path.Combine(root, "data", "tigerclaw.db")}";
            opts.Workspace.RootPath = root;
            opts.Workspace.SkillsPath = "skills";
            opts.Workspace.WorkflowsPath = "workflows";
            opts.Workspace.ArtifactsPath = "artifacts";
        });
        sc.AddTigerClawInfrastructure();
        sc.AddTigerClawMemory();
        sc.AddTigerClawSkills(false);
        sc.AddTigerClawWorkflows();
        sc.AddTigerClawRuntime();
        return sc.BuildServiceProvider();
    }

    [Fact]
    public async Task ListSkills_ReturnsSkills()
    {
        var sp = CreateProvider();
        var init = sp.GetRequiredService<TigerClaw.Infrastructure.Database.DatabaseInitializer>();
        await init.InitializeAsync();

        var facade = sp.GetRequiredService<IRuntimeFacade>();
        var skills = await facade.ListSkillsAsync();
        Assert.NotEmpty(skills);
        Assert.Contains(skills, s => s.Id == "file.read_text");
    }

    [Fact]
    public async Task ListWorkflows_ReturnsWorkflows()
    {
        var sp = CreateProvider();
        var facade = sp.GetRequiredService<IRuntimeFacade>();
        var workflows = await facade.ListWorkflowsAsync();
        Assert.NotEmpty(workflows);
        Assert.Contains(workflows, w => w.Id == "daily_mail_digest");
    }

    [Fact]
    public async Task RunWorkflow_DailyMailDigest_Succeeds()
    {
        var sp = CreateProvider();
        var init = sp.GetRequiredService<TigerClaw.Infrastructure.Database.DatabaseInitializer>();
        await init.InitializeAsync();

        // Satisfy prerequisites for skill `email.fetch_unread`.
        var prefs = sp.GetRequiredService<Core.IPreferenceService>();
        var accountId = "acc1";
        var userId = "local-user";
        await prefs.UpsertAsync("email.default_account_id", accountId, userId);
        await prefs.UpsertAsync($"email.accounts.{accountId}.host", "imap.example.com", userId);
        await prefs.UpsertAsync($"email.accounts.{accountId}.port", "993", userId);
        await prefs.UpsertAsync($"email.accounts.{accountId}.username", "user@example.com", userId);
        await prefs.UpsertAsync($"email.accounts.{accountId}.authProfile", "default", userId);

        var facade = sp.GetRequiredService<IRuntimeFacade>();
        var resp = await facade.RunWorkflowAsync("daily_mail_digest", new Dictionary<string, object?>(), userId);
        Assert.True(resp.Success, resp.Message);
    }

    [Fact]
    public async Task RunWorkflow_DailyMailDigest_WaitsIfPrereqsMissing()
    {
        var sp = CreateProvider();
        var init = sp.GetRequiredService<TigerClaw.Infrastructure.Database.DatabaseInitializer>();
        await init.InitializeAsync();

        var userId = "local-user-wait";
        var facade = sp.GetRequiredService<IRuntimeFacade>();
        var resp = await facade.RunWorkflowAsync("daily_mail_digest", new Dictionary<string, object?>(), userId);

        Assert.False(resp.Success);
        Assert.NotNull(resp.Message);
        Assert.Contains(resp.Steps, s => s.Status == "waiting_human");
    }
}
