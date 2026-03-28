using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using TigerClaw.Cli;
using Microsoft.Extensions.Logging;
using TigerClaw.Infrastructure;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Memory;
using TigerClaw.Runtime;
using TigerClaw.Skills;
using TigerClaw.Workflows;

var services = new ServiceCollection();
ConfigureServices(services);
var provider = services.BuildServiceProvider();

await EnsureDatabaseAsync(provider);

var cmdArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
if (cmdArgs.Length == 0)
{
    Console.WriteLine(CliHelp.Global);
    return 0;
}

var parsed = CliCommandParser.Parse(cmdArgs);
if (!parsed.Success || parsed.Request == null)
{
    Console.Error.WriteLine(parsed.Error ?? "Invalid arguments.");
    return 1;
}

var router = new CliCommandRouter(provider);
var result = await router.RouteAsync(parsed.Request);
WriteCliResult(result, parsed.Request.JsonOutput);
return result.ExitCode;

static void WriteCliResult(CliCommandResult result, bool json)
{
    var jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    if (json)
    {
        var payload = new
        {
            exitCode = result.ExitCode,
            success = result.Success,
            error = result.Error,
            text = result.Text,
            data = result.Data
        };
        Console.WriteLine(JsonSerializer.Serialize(payload, jsonOptions));
        return;
    }

    if (!string.IsNullOrEmpty(result.Error))
        Console.Error.WriteLine(result.Error);
    else if (!string.IsNullOrEmpty(result.Text))
        Console.WriteLine(result.Text);
}

static void ConfigureServices(IServiceCollection sc)
{
    sc.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
    var rootDir = ResolveRootDirectory();
    sc.Configure<TigerClawOptions>(opts =>
    {
        opts.Database.DataDirectory = Path.Combine(rootDir, "data");
        opts.Database.ConnectionString = $"Data Source={Path.Combine(opts.Database.DataDirectory, "tigerclaw.db")}";
        opts.Workspace.RootPath = rootDir;
        opts.Workspace.SkillsPath = "skills";
        opts.Workspace.WorkflowsPath = "workflows";
        opts.Workspace.ArtifactsPath = "artifacts";
        opts.IsInteractive = true;

        var apiKey = Environment.GetEnvironmentVariable("TIGERCLAW_MODEL_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
            opts.ModelRouting.ApiKey = apiKey.Trim();
        if (string.Equals(Environment.GetEnvironmentVariable("TIGERCLAW_USE_LLM_INTENT"), "true", StringComparison.OrdinalIgnoreCase))
            opts.ModelRouting.UseLlmIntentRouting = true;
    });

    sc.AddTigerClawInfrastructure();
    sc.AddTigerClawMemory();
    sc.AddTigerClawSkills(isInteractive: true);
    sc.AddTigerClawWorkflows();
    sc.AddTigerClawRuntime();
}

static async Task EnsureDatabaseAsync(IServiceProvider provider)
{
    try
    {
        var init = provider.GetRequiredService<TigerClaw.Infrastructure.Database.DatabaseInitializer>();
        await init.InitializeAsync();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Database init warning: {ex.Message}");
    }
}

static string ResolveRootDirectory()
{
    var baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
    for (var d = baseDir; !string.IsNullOrEmpty(d); d = Path.GetDirectoryName(d))
    {
        if (Directory.Exists(Path.Combine(d, "skills")) && Directory.Exists(Path.Combine(d, "workflows")))
            return d;
    }
    return Directory.GetCurrentDirectory();
}
